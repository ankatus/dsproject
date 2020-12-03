using System.Collections.Generic;

namespace dsproject
{
    internal interface IGameState
    {
        public StateUpdateInfo Update(TurnInfo previousTurn);
        public TurnInfo GetTurn();
        public TurnInfo InitGame(List<PlayerInfo> players, PlayerInfo localPlayer, int seed);
        public UnoCard DrawCard();
        public bool PlayCard(int cardIndex);
        public void Reset();
    }
}