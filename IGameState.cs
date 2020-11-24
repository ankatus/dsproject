namespace dsproject
{
    internal interface IGameState
    {
        public StateUpdateInfo Update(TurnInfo turn);
        public TurnInfo GetTurn();
        public void Start();
        public UnoCard DrawCard();
        public void PlayCard(UnoCard card);
        public void Reset();
    }
}