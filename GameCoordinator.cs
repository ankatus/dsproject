using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace dsproject
{
    internal class GameCoordinator
    {
        private NetworkCommunication _NetworkComms { get; set; }
        private GameState _GameState { get; set; }
        private int _UniqueID { get; set; }
        private uint _MsgNumber { get; set; }
        private string _MsgID { get { return _UniqueID + "-" + _MsgNumber; } }
        private LobbyInfo _LobbyInfo { get; set; }
        private ConcurrentQueue<JoinGameMessage> JoinGameMessages { get; set; }
        private ConcurrentQueue<TurnInfoMessage> TurnInfoMessages { get; set; }
        private ConcurrentQueue<ResponseMessage> ResponseMessages { get; set; }

        public ConcurrentQueue<EventInfo> EventQueue { get; set; }

        public GameCoordinator(GameState gameState)
        {
            _NetworkComms = new NetworkCommunication();
            _GameState = gameState;

            Random rnd = new Random();
            _UniqueID = rnd.Next(1, 1000000);
            _MsgNumber = 1;

            JoinGameMessages = new ConcurrentQueue<JoinGameMessage>();
            TurnInfoMessages = new ConcurrentQueue<TurnInfoMessage>();
            ResponseMessages = new ConcurrentQueue<ResponseMessage>();

            EventQueue = new ConcurrentQueue<EventInfo>();

            Task.Run(MessageReceiver);
        }

        public LobbyInfo JoinGame(string name, int lobbySize, int interfaceIndex)
        {
            LobbyInfo lobbyInfo = new LobbyInfo();

            _NetworkComms.JoinGroup(interfaceIndex);
            _NetworkComms.StartReceiving();

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            Action action = () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    JoinGameMessage msg = new JoinGameMessage() { Sender = _UniqueID, Name = name, LobbySize = lobbySize, MsgID = _MsgID };
                    _NetworkComms.SendMessage(JsonSerializer.SerializeToUtf8Bytes(msg));
                    _MsgNumber++;

                    //Don't spam messages
                    Thread.Sleep(2000);
                }
            };

            //Start JoinGameMessage sending loop
            Task.Run(action);

            //Add local player to lobby
            PlayerInfo localPlayerInfo = new PlayerInfo { PlayerID = _UniqueID, PlayerName = name };
            lobbyInfo.Players.Add(localPlayerInfo);

            //Loop until lobby is full
            while (lobbyInfo.Players.Count < lobbySize)
            {
                bool messageAvailable = JoinGameMessages.TryDequeue(out JoinGameMessage msg);

                if (messageAvailable)
                {
                    //Check if JoinLobbyMessage was for the same lobby size
                    if (msg.LobbySize == lobbySize)
                    {
                        //Log message
                        Debug.WriteLine("Received correct JoinGameMessage: " + JsonSerializer.Serialize(msg));

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
                            PlayerInfo newPlayerInfo = new PlayerInfo { PlayerID = msg.Sender, PlayerName = msg.Name };
                            lobbyInfo.Players.Add(newPlayerInfo);
                            Debug.WriteLine("New player found and added to lobby: " + newPlayerInfo.ToString());
                        }
                    }
                }
                else
                {
                    //If no message found, sleep a bit
                    Thread.Sleep(1000);
                }
            }

            cancellationTokenSource.Cancel();

            //Sort player list so that first one has smallest ID
            lobbyInfo.Players.Sort((PlayerInfo a, PlayerInfo b) => { return a.PlayerID.CompareTo(b.PlayerID); });

            //Search smallest playerID
            int smallestPlayerID = int.MaxValue;
            foreach (PlayerInfo info in lobbyInfo.Players)
            {
                if (info.PlayerID < smallestPlayerID)
                {
                    smallestPlayerID = info.PlayerID;
                }
            }

            //Set smallest playerID player as a dealer
            foreach (PlayerInfo info in lobbyInfo.Players)
            {
                if (info.PlayerID == smallestPlayerID)
                {
                    info.Dealer = true;
                }
            }

            // TODO
            // VALIDATE LOBBY, send lobbyinfo to other and check that they have same

            Debug.WriteLine("Lobby is full");
            Debug.WriteLine("Players:");
            foreach (PlayerInfo info in lobbyInfo.Players)
            {
                Debug.WriteLine(info.ToString());
            }
            Debug.WriteLine("Initiating game...");
            _GameState.InitGame(lobbyInfo.Players, localPlayerInfo, smallestPlayerID);
            Debug.WriteLine("Initing ready");

            _LobbyInfo = lobbyInfo;
            return lobbyInfo;
        }

        public void EndTurn()
        {
            Debug.WriteLine("Ending turn...");
            TurnInfo turnInfo = _GameState.GetTurn();

            if (turnInfo == null)
            {
                Debug.WriteLine("Received null turnInfo from GameState");
                return;
            }

            string msgID = _MsgID;
            TurnInfoMessage turnInfoMsg = new TurnInfoMessage { TurnInfo = turnInfo, Sender = _UniqueID, MsgID = msgID };
            byte[] turnInfoMsgBytes = JsonSerializer.SerializeToUtf8Bytes(turnInfoMsg);

            bool turnApproved = false;
            List<int> ApprovedPlayers = new List<int>(); //Players who have approved this turn
            int requiredNumberOfPlayerApprovals = _LobbyInfo.Players.Count - 1; //-1 because player does not approve local turn

            //TODO, implement some time here? now sends turn max 10 times then fails
            for (int i = 0; i < 10; i++)
            {
                _NetworkComms.SendMessage(turnInfoMsgBytes);

                //Sleep a bit to give others time to check turn and response
                Thread.Sleep(500);

                bool responseAvailable;
                do
                {
                    responseAvailable = ResponseMessages.TryDequeue(out ResponseMessage response);

                    if (responseAvailable == false) { continue; }

                    //Check if response is to our message
                    if (response.Receiver != _UniqueID) { Debug.WriteLine("Receive msg error: response was to some other message"); continue; }

                    //Check if response is to same message as we sent
                    if (response.ReceivedMsgID != msgID) { Debug.WriteLine("Receive msg error: message ID did not match"); continue; }

                    //Log message
                    Debug.WriteLine("Received response to turn info message: " + JsonSerializer.Serialize(response));

                    //Check if this player is in lobby
                    if (IsThisPlayerInLobby(response.Sender) == false) { Debug.WriteLine("Receive msg error: player not in lobby"); continue; }

                    //Check if turn was approved
                    if (response.Approve == false) { Debug.WriteLine("Receive msg error: player did not approve turn, reason: " + response.ErrorString); continue; }

                    //Check if this player has already approved turn
                    if (ApprovedPlayers.Contains(response.Sender) == true) { Debug.WriteLine("Error: player has already approved turn"); continue; }

                    Debug.WriteLine("Received response approved turn");
                    ApprovedPlayers.Add(response.Sender);

                    //Check if it was last required approvement
                    if (ApprovedPlayers.Count == requiredNumberOfPlayerApprovals) { break; }
                } while (responseAvailable);

                if (ApprovedPlayers.Count == requiredNumberOfPlayerApprovals)
                {
                    Debug.WriteLine("Turn approved");
                    turnApproved = true;
                    break;
                }
            }

            if (turnApproved)
            {
                //All good
                Debug.WriteLine("Ending turn finished");
                //Clear unnessary responses
                ResponseMessages.Clear();
            }
            else
            {
                // Add something to event queue?
                // return false?
                Debug.WriteLine("Ending turn FAILED, did not receive approvement from other players");
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
            Debug.WriteLine("Received turn info message from other player: " + JsonSerializer.Serialize(msg));

            StateUpdateInfo receivedInfo = _GameState.Update(msg.TurnInfo);

            bool approved = receivedInfo.Result == StateUpdateResult.Ok;

            if (approved == false)
            {
                Debug.WriteLine("Error processing turn info: " + receivedInfo.ErrorString);
            }

            ResponseMessage response = new ResponseMessage() { Sender = _UniqueID, Receiver = msg.Sender, Approve = approved, ErrorString = receivedInfo.ErrorString, ReceivedMsgID = msg.MsgID, MsgID = _MsgID };
            _NetworkComms.SendMessage(JsonSerializer.SerializeToUtf8Bytes(response));
            _MsgNumber++;
        }

        private void MessageReceiver()
        {
            while (true)
            {
                byte[] data = _NetworkComms.GetMessage();

                if (data == null)
                {
                    Thread.Sleep(50);
                    continue;
                }

                Message msg = JsonSerializer.Deserialize<Message>(data);

                if (msg != null)
                {
                    //Ignore our own message
                    if (msg.Sender != _UniqueID)
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
                                Debug.WriteLine("Unknown message type: " + msg.MsgType);
                                Debug.WriteLine(BitConverter.ToString(data));
                                break;
                        }
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
