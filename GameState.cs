﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace dsproject
{
    internal class GameState : IGameState
    {
        private Stack<UnoCard> _deck;
        private int _nextTurnPlayerId;
        private UnoCard _playedCard;
        private readonly List<UnoCard> _drawnCards;
        private int _seed;
        private bool _playAnyColor;
        
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
            if (previousTurn.PlayerID != _nextTurnPlayerId) return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Wrong player ID" };
            if (previousTurn.DrawnCards is null) return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Invalid data" };

            // Get previous turn player object
            var previousTurnPlayer = Players.Single(player => player.PlayerID == previousTurn.PlayerID);
            if (previousTurnPlayer is null)
                return new StateUpdateInfo { Result = StateUpdateResult.Error, ErrorString = "Unknown player" };

            // Update played/drawn cards
            UpdatePlayerCards(previousTurnPlayer, previousTurn.DrawnCards, previousTurn.PlayedCard);
            if (previousTurn.PlayedCard is not null) Pile.Push(previousTurn.PlayedCard);
            previousTurn.DrawnCards.ForEach(_ => _deck.Pop());

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
                _nextTurnPlayerId = previousTurn.NextTurnPlayerID;
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
                DrawnCards = new List<UnoCard>(_drawnCards),
                NextTurnPlayerID = _nextTurnPlayerId,
            };

            // Clear local turn info
            _playedCard = null;
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
            _nextTurnPlayerId = dealer.PlayerID;

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

            _nextTurnPlayerId = GetNextPlayerId();

            GameStatus = GameStatus.Started;
            TurnStatus = TurnStatus.Ready;
        }

        public UnoCard DrawCard()
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

            _drawnCards.Add(drawnCard);
            LocalPlayer.Hand.Add(drawnCard);
            return drawnCard;
        }

        public bool PlayCard(int cardIndex)
        {
            if (LocalPlayer.Hand.Count <= cardIndex) return false;

            var card = LocalPlayer.Hand[cardIndex];

            if (!CanPlayCard(card)) return false;

            Pile.Push(card);
            LocalPlayer.Hand.RemoveAt(cardIndex);
            _playedCard = card;

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

            if (LocalPlayer.Hand.Count == 0)
            {
                // TODO: Win game
            }

            _nextTurnPlayerId = GetNextPlayerId();
            TurnStatus = TurnStatus.Ready;

            return true;
        }

        public void Reset()
        {
            Pile.Clear();
            _deck.Clear();
            Players.Clear();
            _drawnCards.Clear();
            LocalPlayer = null;
            _playedCard = null;
            _nextTurnPlayerId = 0;
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
                TurnStatus = TurnStatus.Ready;
                break;
            }
        }

        private void StartTurn(TurnInfo previousTurn)
        {
            Debug.WriteLine("TURN STARTED");

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
                        _nextTurnPlayerId = GetNextPlayerId();
                        break;
                    case CardType.DrawTwo:
                        Debug.WriteLine("Card Played: DrawTwo");
                        // We draw two cards and miss our turn
                        for (var i = 0; i < 2; i++)
                        {
                            _drawnCards.Add(DrawCard());
                        }
                        _nextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.Reverse:
                        Debug.WriteLine("Card Played: Reverse");
                        // No effect here
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
                            _drawnCards.Add(DrawCard());
                        }
                        _nextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.Skip:
                        Debug.WriteLine("Card Played: Skip");
                        // We miss our turn
                        _nextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.DrawTwo:
                        Debug.WriteLine("Card Played: DrawTwo");
                        // We draw two cards and miss our turn
                        for (var i = 0; i < 2; i++)
                        {
                            _drawnCards.Add(DrawCard());
                        }
                        _nextTurnPlayerId = GetNextPlayerId();
                        TurnStatus = TurnStatus.Ready;
                        break;
                    case CardType.Reverse:
                        Debug.WriteLine("Card Played: Reverse");
                        ReverseTurnOrder();
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

            // Same card
            if (card == topCard) return true;

            // Same color
            if (card.Color == topCard.Color) return true;

            if (topCard.Type is CardType.Number)
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
            if (_playAnyColor)
            {
                _playAnyColor = false;
                return true;
            }

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
            cards.Add(new UnoCard(CardType.Wild));
            cards.Add(new UnoCard(CardType.Wild));
            cards.Add(new UnoCard(CardType.Wild));
            cards.Add(new UnoCard(CardType.Wild));

            // WildDrawFour
            cards.Add(new UnoCard(CardType.WildDrawFour));
            cards.Add(new UnoCard(CardType.WildDrawFour));
            cards.Add(new UnoCard(CardType.WildDrawFour));
            cards.Add(new UnoCard(CardType.WildDrawFour));

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

        private static void UpdatePlayerCards(PlayerInfo player, List<UnoCard> drawn, UnoCard played)
        {
            // Add drawn cards to hand
            player.Hand.AddRange(drawn);

            // Remove played card from hand
            for (var i = 0; i < player.Hand.Count; i++)
            {
                if (player.Hand[i] == played)
                {
                    player.Hand.RemoveAt(i);
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
        NotStarted, Dealing, Started
    }
}