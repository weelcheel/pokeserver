using PokeServer.Game;
using PokeServer.Game.Server;
using PokeServer.Server;

namespace PokeServer.Delegates;

public delegate Task ProcessConnection(Connection connection);