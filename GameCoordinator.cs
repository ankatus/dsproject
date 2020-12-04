using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace dsproject
{
    internal class GameCoordinator : ICoordinator
    {
        private NetworkCommunication _NetworkComms { get; set; }
        private IGameState _GameState { get; set; }
        private int _UniqueID { get; set; }
        private LobbyInfo _LobbyInfo { get; set; }
        private ConcurrentQueue<JoinGameMessage> JoinGameMessages { get; set; }
        private ConcurrentQueue<TurnInfoMessage> TurnInfoMessages { get; set; }
        private ConcurrentQueue<ResponseMessage> ResponseMessages { get; set; }

        public ConcurrentQueue<EventInfo> EventQueue { get; set; }

        public GameCoordinator(IGameState gameState)
        {
            _NetworkComms = new NetworkCommunication();
            _GameState = gameState;

            Random rnd = new Random();
            _UniqueID = rnd.Next(1, 1000000);

            JoinGameMessages = new ConcurrentQueue<JoinGameMessage>();
            TurnInfoMessages = new ConcurrentQueue<TurnInfoMessage>();
            ResponseMessages = new ConcurrentQueue<ResponseMessage>();

            EventQueue = new ConcurrentQueue<EventInfo>();

            Task.Run(MessageReceiver);
        }

        public LobbyInfo JoinGame(string name, int lobbySize)
        {
            LobbyInfo lobbyInfo = new LobbyInfo();

            _NetworkComms.StartReceiving();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Action action = () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    JoinGameMessage msg = new JoinGameMessage() { Sender = _UniqueID, Name = name, LobbySize = lobbySize, TimeStamp = DateTime.Now };
                    _NetworkComms.SendMessage(JsonSerializer.SerializeToUtf8Bytes(msg));
                    Console.WriteLine("Sent message:" + JsonSerializer.Serialize(msg));
                    Thread.Sleep(1000);
                }
            };

            Task.Run(action);

            while (lobbyInfo.Players.Count < lobbySize)
            {
                bool messageAvailable = JoinGameMessages.TryDequeue(out JoinGameMessage msg);

                if (messageAvailable == false)
                {
                    Thread.Sleep(500);
                    continue;
                }

                //Check if JoinLobbyMessage was for same lobby size
                if (msg.LobbySize == lobbySize)
                {
                    Console.WriteLine("Received correct JoinGameMessage: " + JsonSerializer.Serialize(msg));

                    bool newPlayer = true;

                    //Check if player wanting to join is already in lobby
                    foreach (PlayerInfo info in lobbyInfo.Players)
                    {
                        if (info.PlayerID == msg.Sender)
                        {
                            newPlayer = false;
                        }
                    }

                    if (newPlayer)
                    {
                        Console.WriteLine("New player found");
                        lobbyInfo.Players.Add(new PlayerInfo { PlayerID = msg.Sender, PlayerName = msg.Name });
                    }
                }

                Thread.Sleep(500);
            }

            cancellationTokenSource.Cancel();

            lobbyInfo.Players.Sort((PlayerInfo a, PlayerInfo b) =>
            {
                return a.PlayerID.CompareTo(b.PlayerID);
            });

            // VALIDATE LOBBY, send lobbyinfo to other and check that they have same

            _LobbyInfo = lobbyInfo;
            return lobbyInfo;
        }

        public void EndTurn()
        {
            TurnInfo turnInfo = _GameState.GetTurn();

            DateTime dataSentTime = DateTime.Now;
            TurnInfoMessage turnInfoMsg = new TurnInfoMessage { TurnInfo = turnInfo, Sender = _UniqueID, TimeStamp = dataSentTime };
            byte[] turnInfoMsgBytes = JsonSerializer.SerializeToUtf8Bytes(turnInfoMsg);

            bool turnApproved = false;
            List<int> ApprovedPlayers = new List<int>();

            for (int i = 0; i < 10; i++)
            {
                _NetworkComms.SendMessage(turnInfoMsgBytes);

                for (int j = 0; j < _LobbyInfo.Players.Count; j++)
                {
                    bool responseAvailable = ResponseMessages.TryDequeue(out ResponseMessage response);

                    if (responseAvailable)
                    {
                        //Check if response is to our TurnInfoMessage
                        if (response.Receiver == _UniqueID && response.TimeStamp == dataSentTime)
                        {
                            //Check if this player is in lobby
                            if (IsThisPlayerInLobby(response.Sender))
                            {
                                //Check if turn was approved
                                if (response.Approve)
                                {
                                    //Check if this player has already approved turn
                                    if (!ApprovedPlayers.Contains(response.Sender))
                                    {
                                        ApprovedPlayers.Add(response.Sender);
                                    }
                                }
                                else
                                {
                                    //TODO
                                    //WHAT HERE?
                                }
                            }
                        }
                    }
                    else
                    {
                        //TODO
                        //Wait for responses, sleep for a bit
                        //Probably better way to do this
                        Thread.Sleep(500);
                    }
                }

                if (ApprovedPlayers.Count == _LobbyInfo.Players.Count)
                {
                    turnApproved = true;
                }
            }

            if (turnApproved)
            {
                //All good
            }
            else
            {
                // Add something to event queue?
                // return false?
            }
        }

        private bool IsThisPlayerInLobby(int playerId)
        {
            foreach (PlayerInfo info in _LobbyInfo.Players)
            {
                if (info.PlayerID == playerId)
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleTurnInfoMessage(TurnInfoMessage msg)
        {
            //Ignore our own message
            if (msg.Sender != _UniqueID)
            {
                StateUpdateInfo receivedInfo = _GameState.Update(msg.TurnInfo);

                //bool approved = receivedInfo.Result == StateUpdateResult.Ok;

                //ResponseMessage response = new ResponseMessage() { Sender = _UniqueID, Receiver = msg.Sender, Approve=approved, TimeStamp = DateTime.Now };
                //_NetworkComms.SendMessage(JsonSerializer.SerializeToUtf8Bytes(response));
                //Console.WriteLine("Sent approve message:" + JsonSerializer.Serialize(response));
            }
        }

        private void MessageReceiver()
        {
            while (true)
            {
                byte[] data = _NetworkComms.GetMessage();

                if (data == null)
                {
                    Thread.Sleep(500);
                    continue;
                }

                Message msg = JsonSerializer.Deserialize<Message>(data);

                if (msg != null)
                {
                    switch (msg.MsgType)
                    {
                        case MessageType.JoinGame:
                            JoinGameMessages.Enqueue(JsonSerializer.Deserialize<JoinGameMessage>(data));
                            break;
                        case MessageType.TurnInfo:
                            HandleTurnInfoMessage(JsonSerializer.Deserialize<TurnInfoMessage>(data));
                            break;
                        case MessageType.Response:
                            ResponseMessages.Enqueue(JsonSerializer.Deserialize<ResponseMessage>(data));
                            break;
                        default:
                            Console.WriteLine("Unknown message type: " + msg.MsgType);
                            Console.WriteLine(BitConverter.ToString(data));
                            break;
                    }
                }
            }
        }

        public void ExitGame()
        {
            throw new NotImplementedException();
        }
    }
}
