using System.Collections.Concurrent;

namespace dsproject
{
    internal interface ICoordinator
    {
        public ConcurrentQueue<EventInfo> EventQueue { get; }

        public LobbyInfo JoinGame(ConnectionInfo connection);
        public void EndTurn();
        public void ExitGame();
    }
}