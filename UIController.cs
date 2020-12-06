using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Threading;

namespace dsproject
{
    // ReSharper disable once InconsistentNaming
    internal class UIController
    {
        private readonly Display _display;
        private readonly GameCoordinator _gameCoordinator;
        private readonly GameState _gameState;
        private readonly View _view;
        private ConsoleKey? _input;
        private int _selectedWildCardHandIndex;
        private bool _selectingWildcardColor; // Special state for when the player is selecting a color for a wildcard
        private bool _winTransmitted;

        public UIController(Display display, GameState gameState, GameCoordinator gameCoordinator)
        {
            _display = display;
            _view = new View(_display);
            _gameState = gameState;
            _gameCoordinator = gameCoordinator;
            _input = null;
            _selectingWildcardColor = false;
            _selectedWildCardHandIndex = 0;
            _winTransmitted = false;
        }

        public void JoinGame()
        {
            var prompt = "Display Name: ";
            _display.WriteString(prompt, 0, 0);
            _display.Update();
            Console.SetCursorPosition(prompt.Length, 0);
            var name = Console.ReadLine();


            int index;
            while (true)
            {
                _display.Clear();
                _display.WriteString("Select Interface:", 0, 0);
                var rows = ShowInterfaces(1, 0);
                _display.Update();
                Console.SetCursorPosition(0, rows);
                index = GetInterfaceIndex(Console.ReadKey(true).Key);

                if (index > -1) break;

                _display.Clear();
                _display.WriteString("Not a valid index!", 0, 0);
                _display.Update();
                Thread.Sleep(1000);
            }
            
            _display.Clear();
            prompt = "Group address (empty for default): ";
            _display.WriteString(prompt, 0, 0);
            _display.Update();
            Console.SetCursorPosition(prompt.Length, 0);
            var groupAddress = Console.ReadLine() ?? "";
            int groupPort = 0;

            if (groupAddress?.Length != 0)
            {
                while (true)
                {
                    _display.Clear();
                    prompt = "Group port: ";
                    _display.WriteString(prompt, 0, 0);
                    _display.Update();
                    Console.SetCursorPosition(prompt.Length, 0);
                    var groupPortString = Console.ReadLine();

                    if (int.TryParse(groupPortString, out groupPort)) break;
                    
                    _display.Clear();
                    _display.WriteString("Not a valid port!", 0, 0);
                    _display.Update();
                    Thread.Sleep(1000);
                }
            }

            int players;
            while (true)
            {
                prompt = "Group size (2-10): ";
                _display.Clear();
                _display.WriteString(prompt, 0, 0);
                _display.Update();
                Console.SetCursorPosition(prompt.Length, 0);
                var answer = Console.ReadLine();

                if (int.TryParse(answer, out players) && players is > 1 and < 11) break;

                _display.Clear();
                _display.WriteString("Not a valid group size!", 0, 0);
                _display.Update();
                Thread.Sleep(1000);
            }

            _display.Clear();
            _display.WriteString("Joining game...", 0, 0);
            _display.Update();

            try
            {
                if (groupAddress.Length == 0)_gameCoordinator.JoinGame(name, players, index);
                else _gameCoordinator.JoinGame(name, players, index, groupAddress, groupPort);
                
            }
            catch (Exception)
            {
                _display.Clear();
                _display.WriteString("Error joining game! Exiting...", 0, 0);
                _display.Update();
                Thread.Sleep(2000);
                return;
            }

            _view.LocalPlayerId = _gameState.LocalPlayer.PlayerID;
            _view.LocalPlayerName = _gameState.LocalPlayer.PlayerName;

            GameLoop();
        }

        public void GameLoop()
        {
            while (true)
            {
                _display.Clear();

                // Set this in case console is bugging out
                Console.CursorVisible = false;

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
                                PlayWildCard(pressed);
                                break;
                        }
                    }

                    var blueCard = new UnoCard(CardType.Wild, CardColor.Blue, 0);
                    var redCard = new UnoCard(CardType.Wild, CardColor.Red, 0);
                    var greenCard = new UnoCard(CardType.Wild, CardColor.Green, 0);
                    var yellowCard = new UnoCard(CardType.Wild, CardColor.Yellow, 0);

                    _view.ResetVisibleIndex();
                    _view.Hand.Clear();
                    _view.Hand.Add(blueCard);
                    _view.Hand.Add(redCard);
                    _view.Hand.Add(greenCard);
                    _view.Hand.Add(yellowCard);

                    _view.Message = "Choose a color for the wildcard.";
                    _view.MessageColor = ConsoleColor.Green;
                    _view.Draw();

                    _display.Update();

                    Thread.Sleep(100);

                    continue;
                }

                UpdateView();

                switch (_gameState.GameStatus)
                {
                    case GameStatus.Started:
                        switch (_gameState.TurnStatus)
                        {
                            case TurnStatus.Waiting:
                                _view.CurrentTurnPlayerId = _gameState.NextTurnPlayerId;

                                _view.Message = "Waiting for your turn...";
                                _view.MessageColor = ConsoleColor.Yellow;
                                _view.Draw();
                                break;
                            case TurnStatus.Ongoing:
                                _view.CurrentTurnPlayerId = _gameState.LocalPlayer.PlayerID;

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
                                    _view.Message = "You are the dealer! Press space to flip the first card,";
                                    _view.MessageColor = ConsoleColor.Green;
                                    _view.Draw();
                                    break;
                                }

                                // Can't play any cards even after drawing a new card
                                if (!_gameState.PlayableCardInHand && _gameState.CardDrawn)
                                {
                                    if (WasKeyPressed(ConsoleKey.Spacebar))
                                    {
                                        _gameCoordinator.EndTurn();
                                        _view.ResetVisibleIndex();
                                        continue;
                                    }

                                    _view.Message = "You can't play any cards, press space to end turn.";
                                    _view.MessageColor = ConsoleColor.Yellow;
                                    _view.Draw();
                                    break;
                                }

                                // "Normal" turn, can choose a card to play or draw a new card
                                if (!_gameState.CardDrawn)
                                {
                                    _view.Message =
                                        "Your turn! Select a card to play or press space to draw a new card.";
                                }
                                else
                                {
                                    _view.Message = "Your turn! Select a card to play.";
                                }

                                _view.MessageColor = ConsoleColor.Green;

                                // Check for input first
                                if (_input is not null)
                                {
                                    var pressed = (ConsoleKey)_input;
                                    // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
                                    switch (pressed)
                                    {
                                        case ConsoleKey.Spacebar:
                                            // Draw card
                                            if (_gameState.CardDrawn)
                                            {
                                                break;
                                            }

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

                                _view.Draw();
                                break;
                            case TurnStatus.Ready:
                                _view.CurrentTurnPlayerId = _gameState.LocalPlayer.PlayerID;

                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameCoordinator.EndTurn();
                                    _view.ResetVisibleIndex();
                                    continue;
                                }

                                _view.Message = "Turn done! Press space to send.";
                                _view.MessageColor = ConsoleColor.Red;
                                _view.Draw();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    case GameStatus.NotStarted:
                        _view.Message = "Waiting for game to start...";
                        _view.MessageColor = ConsoleColor.White;
                        _view.Draw();
                        break;
                    case GameStatus.Dealing:
                        switch (_gameState.TurnStatus)
                        {
                            case TurnStatus.Waiting:
                                _view.CurrentTurnPlayerId = _gameState.NextTurnPlayerId;

                                _view.Message = "Waiting for your turn...";
                                _view.MessageColor = ConsoleColor.White;
                                _view.Draw();
                                break;
                            case TurnStatus.Ongoing:
                                _view.CurrentTurnPlayerId = _gameState.LocalPlayer.PlayerID;

                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameState.DealSelf();
                                    continue;
                                }

                                _view.Message = "Press space to deal yourself a hand.";
                                _view.MessageColor = ConsoleColor.Green;
                                _view.Draw();
                                break;
                            case TurnStatus.Ready:
                                _view.CurrentTurnPlayerId = _gameState.LocalPlayer.PlayerID;

                                if (WasKeyPressed(ConsoleKey.Spacebar))
                                {
                                    _gameCoordinator.EndTurn();
                                    continue;
                                }

                                _view.Message = "Turn done! Press space to send.";
                                _view.MessageColor = ConsoleColor.Red;
                                _view.Draw();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        break;
                    case GameStatus.Won:
                        if (!_winTransmitted)
                        {
                            _gameCoordinator.EndTurn();
                            _winTransmitted = true;
                        }

                        _view.Message = "You've Won!";
                        _view.MessageColor = ConsoleColor.Green;
                        _view.Draw();
                        break;
                    case GameStatus.Lost:
                        _view.Message = "Someone else won!";
                        _view.MessageColor = ConsoleColor.Red;
                        _view.Draw();
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

                if (keyPress.Key == ConsoleKey.Enter)
                {
                    break;
                }
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
            {
                return;
            }

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var cardIndex = numberKey switch
            {
                ConsoleKey.D1 => 0 + _view.VisibleIndex * 5,
                ConsoleKey.D2 => 1 + _view.VisibleIndex * 5,
                ConsoleKey.D3 => 2 + _view.VisibleIndex * 5,
                ConsoleKey.D4 => 3 + _view.VisibleIndex * 5,
                ConsoleKey.D5 => 4 + _view.VisibleIndex * 5,
                _ => throw new InvalidOperationException()
            };

            if (cardIndex > _view.Hand.Count - 1)
            {
                return;
            }

            var card = _gameState.LocalPlayer.Hand[cardIndex];

            if (!_gameState.CanPlayCard(card))
            {
                return;
            }

            if (card.Type == CardType.Wild)
            {
                _selectingWildcardColor = true;
                _selectedWildCardHandIndex = cardIndex;
                return;
            }

            _gameState.PlayCard(cardIndex);
        }

        private void PlayWildCard(ConsoleKey numberKey)
        {
            if (numberKey is not
                ConsoleKey.D1 and not
                ConsoleKey.D2 and not
                ConsoleKey.D3 and not
                ConsoleKey.D4)
            {
                return;
            }

            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var cardIndex = numberKey switch
            {
                ConsoleKey.D1 => 0 + _view.VisibleIndex * 5,
                ConsoleKey.D2 => 1 + _view.VisibleIndex * 5,
                ConsoleKey.D3 => 2 + _view.VisibleIndex * 5,
                ConsoleKey.D4 => 3 + _view.VisibleIndex * 5,
                _ => throw new InvalidOperationException()
            };

            if (cardIndex > _view.Hand.Count - 1)
            {
                return;
            }

            var card = _view.Hand[cardIndex];

            if (_gameState.PlayWildCard(card, _selectedWildCardHandIndex))
            {
                _selectingWildcardColor = false;
            }
        }

        private int ShowInterfaces(int row, int col)
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            var rowIndex = row;
            for (var i = 0; i < networkInterfaces.Length; i++)
            {
                if (!networkInterfaces[i].SupportsMulticast) continue;
                if (!networkInterfaces[i].Supports(NetworkInterfaceComponent.IPv4)) continue;

                _display.WriteString("[ " + i + " ] " + networkInterfaces[i].Name, rowIndex, col);
                rowIndex++;
            }

            return rowIndex;
        }

        private static int GetInterfaceIndex(ConsoleKey numberKey)
        {
            var numberKeys = new List<ConsoleKey>
            {
                ConsoleKey.D0,
                ConsoleKey.D1,
                ConsoleKey.D2,
                ConsoleKey.D3,
                ConsoleKey.D4,
                ConsoleKey.D5,
                ConsoleKey.D6,
                ConsoleKey.D7,
                ConsoleKey.D8,
                ConsoleKey.D9
            };

            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            var allowedKeys = new List<(ConsoleKey key, int index)>();

            for (var i = 0; i < networkInterfaces.Length; i++)
            {
                if (i == numberKeys.Count) break;
                if (!networkInterfaces[i].SupportsMulticast) continue;
                if (!networkInterfaces[i].Supports(NetworkInterfaceComponent.IPv4)) continue;

                allowedKeys.Add((numberKeys[i], networkInterfaces[i].GetIPProperties().GetIPv4Properties().Index));
            }

            foreach (var (key, index) in allowedKeys)
            {
                if (key == numberKey) return index;
            }

            return -1;
        }

        private void UpdateView()
        {
            // Update top card
            if (_gameState.Pile.TryPeek(out var topCard))
            {
                _view.TopCard = topCard;
            }

            // Update hand
            _view.Hand.Clear();
            _view.Hand.AddRange(_gameState.LocalPlayer.Hand);

            // Update players
            _view.Players.Clear();
            _view.Players.AddRange(_gameState.Players);

            // Check if we should scroll hand
            if (_input is null)
            {
                return;
            }

            var pressed = (ConsoleKey)_input;
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (pressed)
            {
                case ConsoleKey.LeftArrow:
                    _view.DecreaseVisibleIndex();
                    break;
                case ConsoleKey.RightArrow:
                    _view.IncreaseVisibleIndex();
                    break;
            }
        }
    }
}