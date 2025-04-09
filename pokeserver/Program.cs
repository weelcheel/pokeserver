using PokeServer.Game;
using PokeServer.Server;

var gameServer = new GameServer();
var tcpServer = new TcpServer(gameServer.ProcessConnection);

var tokenSource = new CancellationTokenSource();
await tcpServer.Listen(tokenSource.Token);