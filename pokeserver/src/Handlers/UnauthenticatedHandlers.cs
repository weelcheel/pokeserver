using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using PokeFramework.Attributes;
using PokeFramework.Commands;
using PokeFramework.Redis;
using PokeFramework.User;
using PokeEntities.Postgres;
using PokeEntities.Postgres.Entities;

namespace PokeServer.Handlers;

public class UnauthenticatedHandlers(RedisClient redisClient, IConfiguration configuration, AppDbContext dbContext)
    : CommandProcessor(redisClient)
{
    private readonly AppDbContext _dbContext = dbContext;
    private readonly TokenValidationParameters _tokenValidationParameters = new()
    {
        ValidateIssuer = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = configuration["Jwt:Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!)),
        ValidateLifetime = true,
    };

    [CommandHandler(CommandType.AuthenticateUserPass)]
    public async Task HandleAuthenticateUserPass(Command command)
    {
        var context = await RedisClient.GetAsync<UserContext>($"userContext-{command.ConnectionId}");
        if (context == null)
        {
            await SendFailure(command);
            return;
        }
        
        // Expect command.CommandParams as UTF8: "username:password"
        if (command.CommandParams == null || command.CommandParams.Length == 0)
        {
            await SendFailure(command);
            return;
        }

        var creds = Encoding.UTF8.GetString(command.CommandParams);
        var parts = creds.Split(':', 2);
        if (parts.Length != 2)
        {
            await SendFailure(command);
            return;
        }
        var username = parts[0];
        var password = parts[1];

        // Look up user in database
        var user = await _dbContext.Players.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
        {
            await SendFailure(command);
            return;
        }

        // For now, compare plain text password (replace with hash check in production)
        if (user.Password != password)
        {
            await SendFailure(command);
            return;
        }

        // Issue JWT
        var handler = new JsonWebTokenHandler();
        var key = configuration["Jwt:SigningKey"]!;
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Subject = new System.Security.Claims.ClaimsIdentity(new[]
            {
                new System.Security.Claims.Claim("sub", user.UserId.ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256)
        });
        
        context.UserId = user.UserId.ToString();
        await RedisClient.SetAsync($"userContext-{command.ConnectionId}", context, TimeSpan.FromHours(2));
        await RedisClient.SetRawAsync($"connectionIdFromUser-{context.UserId}", command.ConnectionId,
            TimeSpan.FromHours(2));

        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var resultCommand = new Command(CommandType.AuthenticateResult, command.ConnectionId, user.UserId.ToString(), tokenBytes);
        await RedisHelper.SendMessageToConnectionAsync(RedisClient, command.ConnectionId, resultCommand);
    }

    [CommandHandler(CommandType.Authenticate)]
    public async Task HandleAuthenticate(Command command)
    {
        var context = await RedisClient.GetAsync<UserContext>($"userContext-{command.ConnectionId}");
        if (context == null)
        {
            await SendFailure(command);
            return;
        }

        if (command.CommandParams == null || command.CommandParams.Length == 0)
        {
            await SendFailure(command);
            return;
        }

        var jwt = Encoding.UTF8.GetString(command.CommandParams);
        var handler = new JsonWebTokenHandler();
        var result = await handler.ValidateTokenAsync(jwt, _tokenValidationParameters);

        if (!result.IsValid)
        {
            await SendFailure(command);
            return;
        }

        var userId = result.Claims["sub"]?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            await SendFailure(command);
            return;
        }

        context.UserId = userId;
        await RedisClient.SetAsync($"userContext-{command.ConnectionId}", context, TimeSpan.FromHours(2));
        await RedisClient.SetRawAsync($"connectionIdFromUser-{context.UserId}", command.ConnectionId,
            TimeSpan.FromHours(2));

        var successBytes = BitConverter.GetBytes(Constants.Success);
        var successCommand = new Command(CommandType.AuthenticateResult, command.ConnectionId, context.UserId,
            successBytes);
        await RedisHelper.SendMessageToConnectionAsync(RedisClient, command.ConnectionId, successCommand);
    }

    private async Task SendFailure(Command command)
    {
        var failBytes = BitConverter.GetBytes(Constants.Failure);
        var failCommand = new Command(CommandType.AuthenticateResult, command.ConnectionId, command.UserId,
            failBytes);
        await RedisHelper.SendMessageToConnectionAsync(RedisClient, command.ConnectionId, failCommand);
    }
}