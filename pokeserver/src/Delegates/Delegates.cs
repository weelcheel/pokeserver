using PokeServer.Game;
using PokeServer.Server;

namespace PokeServer.Delegates;

public delegate Task ProcessConnection(Connection connection);
public delegate Task CommandHandler(GameServer gameServer, Connection connection, Command command);