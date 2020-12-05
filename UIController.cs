using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ConsoleKey = System.ConsoleKey;

namespace dsproject
{
    internal class UIController
    {
        private readonly Display _display;
        private readonly CardsView _cardsView;
        private readonly GameState _gameState;
        private readonly GameCoordinator _gameCoordinator;
        private ConsoleKey? _input;
        private bool _selectingWildcardColor; // Special state for when the player is selecting a color for a wildcard

        public UIController(Display display, GameState gameState, GameCoordinator gameCoordinator)
        {
            _display = display;
            _cardsView = new CardsView(_display);
            _gameState = gameState;
            _gameCoordinator = gameCoordinator;
            _input = null;
            _selectingWildcardColor = false;
        }

        public void JoinGame()
        {
            var prompt = "Display Name: ";
            _display.WriteString(prompt, 0, 0);
            _display.Update();
            Console.SetCursorPosition(prompt.Length, 0);
            var name = Console.ReadLine();

            //_display.Clear();
            //_display.WriteString("Select Interface:", 0, 0);
            //var rows = ShowInterfaces(1, 0);
            //_display.Update();
            //Console.SetCursorPosition(0, rows);
            //var index = Console.ReadKey(true);

            prompt = "Group size: ";
            _display.Clear();
            _display.WriteString(prompt, 0, 0);
            _display.Update();
            Console.SetCursorPosition(prompt.Length, 0);
            var size = Console.ReadLine();
            //TODO check that size is interger

            //prompt = "Group address: ";
            //_display.Clear();
            //_display.WriteString(prompt, 0, 0);
            //_display.Update();
            //Console.SetCursorPosition(prompt.Length, 0);
            //var answer = Console.ReadLine();

            _display.Clear();
            _display.WriteString("Joining game...", 0, 0);
            _display.Update();
            Thread.Sleep(2000);

            LobbyInfo lobbyInfo = _gameCoordinator.JoinGame(name, Int32.Parse(size));

            GameLoop();
        }

        public void GameLoop()
        {
            while (true)
            {
                _display.Clear();
                UpdateCards();

                // Get input
                if (Console.KeyAvailable)
                {
                    _input = Console.ReadKey(true).Key;
                    Debug.WriteLine("Key pressed: " + _input);
                }
                else
                {
                    _input = null;
                }

                // Check if we should scroll hand
                if (_input is not null)
                {
                    var pressed = (ConsoleKey)_input;
                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                    switch (pressed)
                    {
                        case ConsoleKey.LeftArrow:
                            _cardsView.DecreaseVisibleIndex();
                            break;
                        case ConsoleKey.RightArrow:
                            _cardsView.IncreaseVisibleIndex();
                            break;
                    }
                }

                if (_selectingWildcardColor)
                {
                    // Check for card selection
                    if (_input is not null)
                    {
                        var pressed = (ConsoleKey)_input;
                        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                        switch (pressed)
                        {
                            case ConsoleKey.D1:
                            case ConsoleKey.D2:
                            case ConsoleKey.D3:
                            case ConsoleKey.D4:
                                PlayCard(pressed);
                                break;
                        }
                    }

                    var blueCard = new UnoCard(CardType.Wild, CardColor.Blue, 0);
                    var redCard = new UnoCard(CardType.Wild, CardColor.Red, 0);
                    var greenCard = new UnoCard(CardType.Wild, CardColor.Green, 0);
                    var yellowCard = new UnoCard(CardType.Wild, CardColor.Yellow, 0);

                    _cardsView.Hand.Clear();
                    _cardsView.Hand.Add(blueCard);
                    _cardsView.Hand.Add(redCard);
                    _cardsView.Hand.Add(greenCard);
                    _cardsView.Hand.Add(yellowCard);

                    _cardsView.Message = "Choose a color for the wildcard.";
                    _cardsView.MessageColor = ConsoleColor.Green;
                    _cardsView.Draw();

                    continue;
                }

                switch (_gameState.GameStatus)
                {
                    case GameStatus.Started:
                        switch (_gameState.TurnStatus)
                        {
                            case TurnStatus.Waiting:
                                _cardsView.Message = "Waiting for your turn...";
                                _cardsView.MessageColor = ConsoleColor.Yellow;
                                _cardsView.Draw();
                                break;
                            case TurnStatus.Ongoing:

                                // Dealer flipping the first card
                                if (_gameState.LocalPlayer.Dealer && _gameState.TurnNumber == 1)
                                {
                                    if (WasKeyPressed(ConsoleKey.Spacebar))
                                    {
                                        _gameState.PlayFirstCard();
                                        Debug.WriteLine("Played first card");
                                        continue;
                                    }
                                    // Dealer's turn to play first card
                                    _cardsView.Message = "You are the dealer! Press space to flip the first card,";
                                    _cardsView.MessageColor = ConsoleColor.Yellow;
                                    _cardsView.Draw();
                                    break;
                                }

                                // Can't play any cards even after drawing a new card
                                if (!_gameState.PlayableCardInHand && _gameState.CardDrawn)
                                {
                                    if (WasKeyPressed(ConsoleKey.Spacebar))
                                    {
                                        _gameCoordinator.EndTurn();
                                        _cardsView.ResetVisibleIndex();
                                        continue;
                                    }
                                    _cardsView.Message = "You can't play any cards, press space to end turn.";
                                    _cardsView.MessageColor = ConsoleColor.Yellow;
                                    _cardsView.Draw();
                                    break;
                                }

                                // "Normal" turn, can choose a card to play or draw a new card
                                _cardsView.Message = "Your turn to play a card!";
                                _cardsView.MessageColor = ConsoleColor.Green;

                                // Check for input first
                                if (_input is not null)
                                {
                                    var pressed = (ConsoleKey)_input;
                                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                                    switch (pressed)
                                    {
                                        case ConsoleKey.Spacebar:
                                            // Draw card
                                            if (_gameState.CardDrawn) break;
                                            _gameState.PlayerDrawCard();
                                            break;
                                        case ConsoleKey.D1:
                                        case ConsoleKey.D2:
                                        case ConsoleKey.D3:
                                        case ConsoleKey.D4:
                                        case ConsoleKey.D5:
                                            PlayCard(pressed);
                                            break;
                                    }

                                }

                                _cardsView.Draw();
                                break;
                            case TurnStatus.Ready:
                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameCoordinator.EndTurn();
                                    _cardsView.ResetVisibleIndex();
                                    continue;
                                }
                                _cardsView.Message = "Turn done! Press space to send.";
                                _cardsView.MessageColor = ConsoleColor.Red;
                                _cardsView.Draw();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    case GameStatus.NotStarted:
                        _cardsView.Message = "Waiting for game to start...";
                        _cardsView.MessageColor = ConsoleColor.White;
                        _cardsView.Draw();
                        break;
                    case GameStatus.Dealing:
                        switch (_gameState.TurnStatus)
                        {
                            case TurnStatus.Waiting:
                                _cardsView.Message = "Waiting for your turn...";
                                _cardsView.MessageColor = ConsoleColor.White;
                                _cardsView.Draw();
                                break;
                            case TurnStatus.Ongoing:
                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameState.DealSelf();
                                    continue;
                                }
                                _cardsView.Message = "Press space to deal yourself a hand.";
                                _cardsView.MessageColor = ConsoleColor.Green;
                                _cardsView.Draw();
                                break;
                            case TurnStatus.Ready:
                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameCoordinator.EndTurn();
                                    continue;
                                }
                                _cardsView.Message = "Turn done! Press space to send.";
                                _cardsView.MessageColor = ConsoleColor.Red;
                                _cardsView.Draw();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        break;
                    case GameStatus.Won:
                        _cardsView.Message = "You've Won!";
                        _cardsView.MessageColor = ConsoleColor.Green;
                        _cardsView.Draw();
                        break;
                    case GameStatus.Lost:
                        _cardsView.Message = "Someone else won!";
                        _cardsView.MessageColor = ConsoleColor.Red;
                        _cardsView.Draw();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                _display.Update();
                Thread.Sleep(100);
            }
        }

        private void WaitForEnter()
        {
            while (true)
            {
                var keyPress = Console.ReadKey(true);

                if (keyPress.Key == ConsoleKey.Enter) break;
            }
        }

        private bool WasKeyPressed(ConsoleKey key)
        {
            return _input == key;
        }

        private void PlayCard(ConsoleKey numberKey)
        {
            if (numberKey is not
                ConsoleKey.D1 and not
                ConsoleKey.D2 and not
                ConsoleKey.D3 and not
                ConsoleKey.D4 and not
                ConsoleKey.D5)
                return;

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var cardIndex = numberKey switch
            {
                ConsoleKey.D1 => 0 + _cardsView.VisibleIndex * 5,
                ConsoleKey.D2 => 1 + _cardsView.VisibleIndex * 5,
                ConsoleKey.D3 => 2 + _cardsView.VisibleIndex * 5,
                ConsoleKey.D4 => 3 + _cardsView.VisibleIndex * 5,
                ConsoleKey.D5 => 4 + _cardsView.VisibleIndex * 5,
                _ => throw new InvalidOperationException(),
            };

            if (cardIndex > _cardsView.Hand.Count) return;

            var card = _gameState.LocalPlayer.Hand[cardIndex];

            if (!_gameState.CanPlayCard(card)) return;

            if (card.Type == CardType.Wild)
            {
                _selectingWildcardColor = true;
            }

            _gameState.PlayCard(cardIndex);
        }

        private int ShowInterfaces(int row, int col)
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            var rowIndex = row;
            for (var i = 0; i < networkInterfaces.Length; i++)
            {
                _display.WriteString(networkInterfaces[i].Name + " " + i, rowIndex, col);
                rowIndex++;
            }

            return rowIndex;
        }



        private void UpdateCards()
        {
            if (_gameState.Pile.TryPeek(out var topCard))
            {
                _cardsView.TopCard = topCard;
            }

            _cardsView.Hand.Clear();
            _cardsView.Hand.AddRange(_gameState.LocalPlayer.Hand);
        }
    }
}
