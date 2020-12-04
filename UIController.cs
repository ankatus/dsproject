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
        private readonly WaitingView _waitingView;
        private readonly CardsView _cardsView;
        private readonly GameState _gameState;
        private readonly GameCoordinator _gameCoordinator;
        private ConsoleKey? _input;

        public UIController(Display display, GameState gameState, GameCoordinator gameCoordinator)
        {
            _display = display;
            _cardsView = new CardsView(_display);
            _waitingView = new WaitingView(_display);
            _gameState = gameState;
            _gameCoordinator = gameCoordinator;
            _input = null;
        }

        public void JoinGame()
        {
            // TODO: Interfaceing with coordinator

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
                    Debug.WriteLine("Key pressed: " + _input.ToString());
                }
                else
                {
                    _input = null;
                }

                switch (_gameState.GameStatus)
                {
                    case GameStatus.Started:
                        switch (_gameState.TurnStatus)
                        {
                            case TurnStatus.Waiting:
                                _waitingView.Message = "Waiting for your turn...";
                                _waitingView.MessageColor = ConsoleColor.Yellow;
                                _waitingView.Draw();
                                break;
                            case TurnStatus.Ongoing:
                                if (_gameState.LocalPlayer.Dealer && _gameState.TurnNumber == 1)
                                {
                                    if (WasKeyPressed(ConsoleKey.Spacebar))
                                    {
                                        _gameState.PlayFirstCard();
                                        Debug.WriteLine("Played first card");
                                        continue;
                                    }
                                    // Dealer's turn to play first card
                                    _waitingView.Message = "You are the dealer! Press space to flip the first card,";
                                    _waitingView.MessageColor = ConsoleColor.Yellow;
                                    _waitingView.Draw();
                                    break;
                                }


                                if (PlayableCards() == 0)
                                {
                                    if (WasKeyPressed(ConsoleKey.Spacebar))
                                    {
                                        _gameState.DrawCard();
                                        Debug.WriteLine("Drew card when none was playable");
                                        continue;
                                    }

                                    _waitingView.Message = "You can't play any cards, press space to draw new card";
                                    _waitingView.MessageColor = ConsoleColor.Yellow;
                                    _waitingView.Draw();                                  
                                }
                                else
                                {
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
                                }
                                break;
                            case TurnStatus.Ready:
                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameCoordinator.EndTurn();
                                    continue;
                                }
                                _waitingView.Message = "Turn done! Press space to send.";
                                _waitingView.MessageColor = ConsoleColor.Red;
                                _waitingView.Draw();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    case GameStatus.NotStarted:
                        _waitingView.Message = "Waiting for game to start...";
                        _waitingView.MessageColor = ConsoleColor.White;
                        _waitingView.Draw();
                        break;
                    case GameStatus.Dealing:
                        switch (_gameState.TurnStatus)
                        {
                            case TurnStatus.Waiting:
                                _waitingView.Message = "Waiting for your turn...";
                                _waitingView.MessageColor = ConsoleColor.White;
                                _waitingView.Draw();
                                break;
                            case TurnStatus.Ongoing:
                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameState.DealSelf();
                                    continue;
                                }
                                _waitingView.Message = "Press space to deal yourself a hand.";
                                _waitingView.MessageColor = ConsoleColor.Green;
                                _waitingView.Draw();
                                break;
                            case TurnStatus.Ready:
                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameCoordinator.EndTurn();
                                    continue;
                                }
                                _waitingView.Message = "Turn done! Press space to send.";
                                _waitingView.MessageColor = ConsoleColor.Red;
                                _waitingView.Draw();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
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
                _waitingView.TopCard = topCard;
                _cardsView.TopCard = topCard;
            }

            _cardsView.Hand.Clear();
            _cardsView.Hand.AddRange(_gameState.LocalPlayer.Hand);
        }

        private int PlayableCards()
        {
            int playableCards = 0;

            foreach (UnoCard card in _gameState.LocalPlayer.Hand)
            {
                if (_gameState.CanPlayCard(card)) playableCards++;
            }

            return playableCards;
        }
    }
}
