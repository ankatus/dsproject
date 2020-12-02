using System.Collections.Concurrent;

namespace dsproject
{
    internal interface ICoordinator
    {
        public ConcurrentQueue<EventInfo> EventQueue { get; }

        public LobbyInfo JoinGame(string name, int lobbySize);
        public void EndTurn();
        public void ExitGame();
    }
}