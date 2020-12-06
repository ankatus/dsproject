using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace dsproject
{
    internal class GameState
    {
        private Stack<UnoCard> _deck;
        private UnoCard _playedCard;
        private int _playedCardIndex;
        private readonly List<UnoCard> _drawnCards;
        private int _seed;
        private bool _playAnyColor;

        public int NextTurnPlayerId { get; private set; }
        public bool CardDrawn { get; private set; }
        public bool PlayableCardInHand
        {
            get => LocalPlayer?.Hand.Any(CanPlayCard) ?? false;
        }
        public int TurnNumber { get; private set; }
        public Stack<UnoCard> Pile { get; }
        public List<PlayerInfo> Players { get; }
        public PlayerInfo LocalPlayer { get; set; }
        public TurnStatus TurnStatus { get; private set; }
        public GameStatus GameStatus { get; private set; }

        public GameState()
        {
            Pile = new Stack<UnoCard>();
            _deck = new Stack<UnoCard>();
            Players = new List<PlayerInfo>();
            _drawnCards = new List<UnoCard>();
            TurnStatus = TurnStatus.Waiting;
            GameStatus = GameStatus.NotStarted;
        }

        public StateUpdateInfo Update(TurnInfo previousTurn)
        {
            // Some validation
            if (GameStatus is GameStatus.NotStarted) return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Game not started" };
            if (TurnStatus is not TurnStatus.Waiting) return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Still processing local turn" };
            if (previousTurn.PlayerID != NextTurnPlayerId) return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Wrong player ID" };
            if (previousTurn.DrawnCards is null) return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Invalid data" };

            // Get previous turn player object
            var previousTurnPlayer = Players.Single(player => player.PlayerID == previousTurn.PlayerID);
            if (previousTurnPlayer is null)
                return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Unknown player" };

            // Update played/drawn cards
            UpdatePlayerCards(previousTurnPlayer, previousTurn.DrawnCards, previousTurn.PlayedCardIndex);
            if (previousTurn.PlayedCard is not null) Pile.Push(previousTurn.PlayedCard);
            previousTurn.DrawnCards.ForEach(_ => _deck.Pop());

            // Check if previous player won
            if (previousTurnPlayer.Hand.Count == 0)
            {
                GameStatus = GameStatus.Lost;
                return new StateUpdateInfo { Result = StateUpdateResult.Ok };
            }

            // Check if our turn
            if (previousTurn.NextTurnPlayerID == LocalPlayer.PlayerID)
            {
                StartTurn(previousTurn);
            }
            else
            {
                // Not our turn

                // Still check if reverse card was played
                if (previousTurn.PlayedCard?.Type is CardType.Reverse) ReverseTurnOrder();
                NextTurnPlayerId = previousTurn.NextTurnPlayerID;
            }

            return new StateUpdateInfo { Result = StateUpdateResult.Ok };
        }

        public TurnInfo GetTurn()
        {
            // Check if turn ready
            if (TurnStatus is not TurnStatus.Ready) return null;

            var turnInfo = new TurnInfo()
            {
                PlayerID = LocalPlayer.PlayerID,
                TurnNumber = TurnNumber,
                PlayedCard = _playedCard,
                PlayedCardIndex = _playedCardIndex,
                DrawnCards = new List<UnoCard>(_drawnCards),
                NextTurnPlayerID = NextTurnPlayerId,
            };

            // Clear local turn info
            CardDrawn = false;
            _playedCard = null;
            _playedCardIndex = -1;
            _drawnCards.Clear();
            TurnNumber++;

            TurnStatus = TurnStatus.Waiting;

            return turnInfo;
        }

        public TurnInfo InitGame(List<PlayerInfo> players, PlayerInfo localPlayer, int seed)
        {
            // Reset everything
            Reset();

            // Set seed
            _seed = seed;

            // Init players
            LocalPlayer = localPlayer;
            Players.AddRange(players);

            // Expect dealer to have next turn
            var dealer = players.Single(player => player.Dealer == true);
            if (dealer is null) throw new ArgumentException("No dealer specified", nameof(players));
            NextTurnPlayerId = dealer.PlayerID;

            // Randomize deck
            InitDeck();

            GameStatus = GameStatus.Dealing;

            if (LocalPlayer.Dealer)
            {
                // Let UI know first card can be played
                TurnStatus = TurnStatus.Ongoing;
            }

            return new TurnInfo { PlayerID = LocalPlayer.PlayerID };
        }

        public void DealSelf()
        {
            // Validation
            if (GameStatus is not GameStatus.Dealing) return;

            for (var i = 0; i < 7; i++)
            {
                var drawnCard = _deck.Pop();
                _drawnCards.Add(drawnCard);
                LocalPlayer.Hand.Add(drawnCard);
            }

            NextTurnPlayerId = GetNextPlayerId();

            GameStatus = GameStatus.Started;
            TurnStatus = TurnStatus.Ready;
        }

        public bool PlayerDrawCard()
        {
            if (CardDrawn) return false;

            var card = DrawCard();
            LocalPlayer.Hand.Add(card);
            _drawnCards.Add(card);
            CardDrawn = true;

            if (!PlayableCardInHand)
            {
                // End turn
                NextTurnPlayerId = GetNextPlayerId();
                TurnStatus = TurnStatus.Ready;
            }

            return true;
        }

        public bool PlayCard(int cardIndex)
        {
            if (LocalPlayer.Hand.Count <= cardIndex) return false;

            var card = LocalPlayer.Hand[cardIndex];

            if (!CanPlayCard(card)) return false;

            Pile.Push(card);
            LocalPlayer.Hand.RemoveAt(cardIndex);
            _playedCard = card;
            _playedCardIndex = cardIndex;

            // Check for card effects
            switch (card.Type)
            {
                case CardType.Wild:
                    // No effects
                    break;
                case CardType.WildDrawFour:
                    // No effects
                    // TODO: WildDrawFour special rule
                    break;
                case CardType.Skip:
                    // No effects
                    break;
                case CardType.DrawTwo:
                    // No effects
                    break;
                case CardType.Reverse:
                    ReverseTurnOrder();
                    break;
                case CardType.Number:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_playAnyColor) _playAnyColor = false;

            if (LocalPlayer.Hand.Count == 0)
            {
                GameStatus = GameStatus.Won;
            }

            NextTurnPlayerId = GetNextPlayerId();
            TurnStatus = TurnStatus.Ready;

            return true;
        }

        public bool PlayWildCard(UnoCard card, int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= LocalPlayer.Hand.Count) return false;

            var cardInHand = LocalPlayer.Hand[cardIndex];
            if (cardInHand.Type is not CardType.Wild) return false;

            if (card.Type is not CardType.Wild) return false;
            if (card.Color is CardColor.White) return false;

            if (!CanPlayCard(card)) return false;

            Pile.Push(card);
            LocalPlayer.Hand.RemoveAt(cardIndex);
            _playedCard = card;
            _playedCardIndex = cardIndex;

            if (_playAnyColor) _playAnyColor = false;

            if (LocalPlayer.Hand.Count == 0)
            {
                GameStatus = GameStatus.Won;
            }

            NextTurnPlayerId = GetNextPlayerId();
            TurnStatus = TurnStatus.Ready;

            return true;
        }

        public void Reset()
        {
            CardDrawn = false;
            Pile.Clear();
            _deck.Clear();
            Players.Clear();
            _drawnCards.Clear();
            LocalPlayer = null;
            _playedCard = null;
            _playedCardIndex = -1;
            NextTurnPlayerId = 0;
            TurnNumber = 0;
            _seed = 0;
            GameStatus = GameStatus.NotStarted;
            TurnStatus = TurnStatus.Waiting;
        }

        public void PlayFirstCard()
        {
            // Loop for redoing in case of WildDrawFour
            while (true)
            {
                var card = _deck.Pop();

                switch (card.Type)
                {
                    case CardType.Wild:
                        // No effect here
                        break;
                    case CardType.WildDrawFour:
                        // Reshuffle and do this again
                        ShufflePileToDeck();
                        continue;
                    case CardType.Skip:
                        // No effect here
                        break;
                    case CardType.DrawTwo:
                        // No effect here
                        break;
                    case CardType.Reverse:
                        // Play turn and reverse turn order
                        ReverseTurnOrder();
                        return;
                    case CardType.Number:
                        // No effect
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Pile.Push(card);
                _drawnCards.Add(card);
                _playedCard = card;
                _playedCardIndex = LocalPlayer.Hand.Count - 1;
                NextTurnPlayerId = GetNextPlayerId();
                TurnStatus = TurnStatus.Ready;
                break;
            }
        }

        private void StartTurn(TurnInfo previousTurn)
        {
            Debug.WriteLine("TURN STARTED");

            if (GameStatus == GameStatus.Won)
            {
                TurnStatus = TurnStatus.Ready;
                return;
            }

            TurnStatus = TurnStatus.Ongoing;

            if (previousTurn.PlayedCard is null)
            {
                Debug.WriteLine("Previous player did not play a card");
                return;
            }

            var card = previousTurn.PlayedCard;

            // Check if we are the dealer playing the first card
            if (LocalPlayer.Dealer && TurnNumber == 1)
            {
                // Wait for UI command to play first card
                return;
            }

            // Check if previous turn was dealer playing the first card
            var previousPlayer = GetPlayer(previousTurn.PlayerID);
            if (previousPlayer is null) throw new InvalidOperationException("Invalid player id");
            if (previousPlayer.Dealer && TurnNumber == 1)
            {
                Debug.WriteLine("Previous turn was dealer playing first card");

                switch (card.Type)
                {
                    case CardType.Wild:
                        Debug.WriteLine("Card Played: Wild");
                        _playAnyColor = true;
                        break;
                    case CardType.WildDrawFour:
                        Debug.WriteLine("Card Played: WildDrawFour");
                        // Shouldn't happen
                        break;
                    case CardType.Skip:
                        Debug.WriteLine("Card Played: Skip");
                        // We miss our turn
                        TurnStatus = TurnStatus.Ready;
                        NextTurnPlayerId = GetNextPlayerId();
                        break;
                    case CardType.DrawTwo:
                        Debug.WriteLine("Card Played: DrawTwo");
                        // We draw two cards and miss our turn
                        for (var i = 0; i < 2; i++)
                        {
                            var drawnCard = DrawCard();
                            _drawnCards.Add(drawnCard);
                            LocalPlayer.Hand.Add(drawnCard);
                        }
                        NextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.Reverse:
                        // In two-player game, reverse == skip
                        if (Players.Count == 2)
                        {
                            Debug.WriteLine("Card Played: Reverse (=skip, in 2-player game)");
                            // We miss our turn
                            TurnStatus = TurnStatus.Ready;
                            NextTurnPlayerId = GetNextPlayerId();
                        }
                        else
                        {
                            Debug.WriteLine("Card Played: Reverse");
                            // No effect here
                        }
                        return;
                    case CardType.Number:
                        Debug.WriteLine("Card Played: Number");
                        // No effect
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                // Just a normal turn

                Debug.WriteLine("Previous turn was a normal turn");

                // Check for possible card effects
                switch (card.Type)
                {
                    case CardType.Wild:
                        Debug.WriteLine("Card Played: Wild");
                        // No effects
                        break;
                    case CardType.WildDrawFour:
                        Debug.WriteLine("Card Played: WildDrawFour");
                        // We draw four cards and miss our turn
                        for (var i = 0; i < 4; i++)
                        {
                            var drawnCard = DrawCard();
                            _drawnCards.Add(drawnCard);
                            LocalPlayer.Hand.Add(drawnCard);
                        }
                        NextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.Skip:
                        Debug.WriteLine("Card Played: Skip");
                        // We miss our turn
                        NextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.DrawTwo:
                        Debug.WriteLine("Card Played: DrawTwo");
                        // We draw two cards and miss our turn
                        for (var i = 0; i < 2; i++)
                        {
                            var drawnCard = DrawCard();
                            _drawnCards.Add(drawnCard);
                            LocalPlayer.Hand.Add(drawnCard);
                        }
                        NextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.Reverse:
                        // In two-player game, reverse == skip
                        if (Players.Count == 2)
                        {
                            Debug.WriteLine("Card Played: Reverse (=skip, in 2-player game)");
                            NextTurnPlayerId = GetNextPlayerId();
                            TurnStatus = TurnStatus.Ready;
                        }
                        else
                        {
                            Debug.WriteLine("Card Played: Reverse");
                            ReverseTurnOrder();
                        }
                        break;
                    case CardType.Number:
                        Debug.WriteLine("Card Played: Number");
                        // No effects
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public bool CanPlayCard(UnoCard card)
        {
            // Out of cards
            if (!Pile.TryPeek(out var topCard)) return false;

            // Wildcard can always be played
            if (card.Type == CardType.Wild) return true;

            // Same card
            if (card == topCard) return true;

            // Same color
            if (card.Color == topCard.Color) return true;

            if (topCard.Type is CardType.Number && card.Type is CardType.Number)
            {
                // Same number
                if (card.Number == topCard.Number) return true;
            }
            else
            {
                // Same symbol
                if (card.Type == topCard.Type) return true;
            }

            // Effect of Wild as first discard
            if (_playAnyColor) return true;

            return false;
        }

        private void InitDeck()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var cards = new List<UnoCard>();

            // Red
            cards.Add(new UnoCard(CardType.Number, CardColor.Red, 0));
            for (var number = 1; number < 10; number++)
            {
                cards.Add(new UnoCard(CardType.Number, CardColor.Red, number));
                cards.Add(new UnoCard(CardType.Number, CardColor.Red, number));
            }
            cards.Add(new UnoCard(CardType.Skip, CardColor.Red, 0));
            cards.Add(new UnoCard(CardType.Skip, CardColor.Red, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Red, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Red, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Red, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Red, 0));

            // Yellow
            cards.Add(new UnoCard(CardType.Number, CardColor.Yellow, 0));
            for (var number = 1; number < 10; number++)
            {
                cards.Add(new UnoCard(CardType.Number, CardColor.Yellow, number));
                cards.Add(new UnoCard(CardType.Number, CardColor.Yellow, number));
            }
            cards.Add(new UnoCard(CardType.Skip, CardColor.Yellow, 0));
            cards.Add(new UnoCard(CardType.Skip, CardColor.Yellow, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Yellow, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Yellow, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Yellow, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Yellow, 0));

            // Green
            cards.Add(new UnoCard(CardType.Number, CardColor.Green, 0));
            for (var number = 1; number < 10; number++)
            {
                cards.Add(new UnoCard(CardType.Number, CardColor.Green, number));
                cards.Add(new UnoCard(CardType.Number, CardColor.Green, number));
            }
            cards.Add(new UnoCard(CardType.Skip, CardColor.Green, 0));
            cards.Add(new UnoCard(CardType.Skip, CardColor.Green, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Green, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Green, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Green, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Green, 0));

            // Blue
            cards.Add(new UnoCard(CardType.Number, CardColor.Blue, 0));
            for (var number = 1; number < 10; number++)
            {
                cards.Add(new UnoCard(CardType.Number, CardColor.Blue, number));
                cards.Add(new UnoCard(CardType.Number, CardColor.Blue, number));
            }
            cards.Add(new UnoCard(CardType.Skip, CardColor.Blue, 0));
            cards.Add(new UnoCard(CardType.Skip, CardColor.Blue, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Blue, 0));
            cards.Add(new UnoCard(CardType.DrawTwo, CardColor.Blue, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Blue, 0));
            cards.Add(new UnoCard(CardType.Reverse, CardColor.Blue, 0));

            // Wild
            cards.Add(new UnoCard(CardType.Wild, CardColor.White, 0));
            cards.Add(new UnoCard(CardType.Wild, CardColor.White, 0));
            cards.Add(new UnoCard(CardType.Wild, CardColor.White, 0));
            cards.Add(new UnoCard(CardType.Wild, CardColor.White, 0));

            // WildDrawFour
            // Not fully implemented
            // cards.Add(new UnoCard(CardType.WildDrawFour));
            // cards.Add(new UnoCard(CardType.WildDrawFour));
            // cards.Add(new UnoCard(CardType.WildDrawFour));
            // cards.Add(new UnoCard(CardType.WildDrawFour));

            // Shuffle cards
            cards = Utils.ShuffleList(cards, _seed).ToList();

            // Push cards to deck
            cards.ForEach(card => _deck.Push(card));
        }

        private void ShufflePileToDeck()
        {
            if (!Pile.TryPop(out var topCard))
            {
                throw new InvalidOperationException("Pile is empty");
            }

            // Pile to list, randomize it, make a new stack of it
            var pileList = Pile.ToList();
            var shuffledPile = (List<UnoCard>)Utils.ShuffleList(pileList, _seed);
            _deck = new Stack<UnoCard>(shuffledPile);

            // Clear pile
            Pile.Clear();

            // Push top card back in to pile
            Pile.Push(topCard);
        }

        private int GetNextPlayerId()
        {
            return GetNextPlayerId(LocalPlayer.PlayerID);
        }

        private int GetNextPlayerId(int id, int skip = 0)
        {
            for (var i = 0; i < Players.Count; i++)
            {
                if (Players[i].PlayerID != id)
                {
                    continue;
                }

                var nextPlayer = i == Players.Count - 1 ? Players[0] : Players[i + 1];

                return skip == 0 ? nextPlayer.PlayerID : GetNextPlayerId(nextPlayer.PlayerID, skip - 1);
            }

            throw new InvalidOperationException();
        }

        private void ReverseTurnOrder()
        {
            Players.Reverse();
        }

        private PlayerInfo GetPlayer(int playerId)
        {
            return Players.Single(player => player.PlayerID == playerId);
        }

        private UnoCard DrawCard()
        {
            // Try to draw a card from the deck
            if (!_deck.TryPop(out var drawnCard))
            {
                // Shuffle pile back into deck
                ShufflePileToDeck();

                // Draw a card from deck
                if (!_deck.TryPop(out drawnCard))
                {
                    // All cards are in players' hands
                    throw new InvalidOperationException("Deck and pile out of cards");
                }
            }

            return drawnCard;
        }

        private static void UpdatePlayerCards(PlayerInfo player, List<UnoCard> drawn, int playedCardIndex)
        {
            // Add drawn cards to hand
            player.Hand.AddRange(drawn);

            // Remove played card from hand
            for (var i = 0; i < player.Hand.Count; i++)
            {
                if (i == playedCardIndex)
                {
                    player.Hand.RemoveAt(i);
                    break;
                }
            }
        }
    }

    internal enum TurnStatus
    {
        Waiting, Ongoing, Ready
    }

    internal enum GameStatus
    {
        NotStarted, Dealing, Started, Won, Lost
    }
}