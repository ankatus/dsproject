﻿using System;
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
        private readonly Logger _logger;
        private NetworkCommunication _NetworkComms { get; set; }
        private GameState _GameState { get; set; }
        private int _UniqueID { get; set; }
        private uint _TurnInfoMessageNumber { get; set; }
        private uint _ResponseMessageNumber { get; set; }
        private uint _AdvertiseLobbyMessageNumber { get; set; }
        private uint _JoinLobbyMessageNumber { get; set; }
        private string _TurnInfoMessageID { get { return _UniqueID + "-" + _TurnInfoMessageNumber; } }
        private string _ResponseMessageID { get { return _UniqueID + "-" + _ResponseMessageNumber; } }
        private string _AdvertiseLobbyMessageID { get { return _UniqueID + "-" + _AdvertiseLobbyMessageNumber; } }
        private string _JoinLobbyMessageID { get { return _UniqueID + "-" + _JoinLobbyMessageNumber; } }
        private LobbyInfo _LobbyInfo { get; set; }
        private ConcurrentQueue<TurnInfoMessage> TurnInfoMessages { get; set; }
        private ConcurrentQueue<ResponseMessage> ResponseMessages { get; set; }
        private ConcurrentQueue<AdvertiseLobbyMessage> AdvertiseLobbyMessages { get; set; }
        private ConcurrentQueue<JoinLobbyMessage> JoinLobbyMessages { get; set; }
        private readonly string multicastGroupAddress = "239.0.0.100";
        private readonly int multicastGroupPort = 55000;

        public ConcurrentQueue<EventInfo> EventQueue { get; set; }

        public GameCoordinator(GameState gameState)
        {
            _NetworkComms = new NetworkCommunication();
            _GameState = gameState;

            _UniqueID = (_NetworkComms.SenderAddress + _NetworkComms.SenderPort).GetHashCode();
            _TurnInfoMessageNumber = 1;
            _ResponseMessageNumber = 1;
            _AdvertiseLobbyMessageNumber = 1;
            _JoinLobbyMessageNumber = 1;

            _logger = new Logger("log_" + _UniqueID);

            TurnInfoMessages = new ConcurrentQueue<TurnInfoMessage>();
            ResponseMessages = new ConcurrentQueue<ResponseMessage>();
            AdvertiseLobbyMessages = new ConcurrentQueue<AdvertiseLobbyMessage>();
            JoinLobbyMessages = new ConcurrentQueue<JoinLobbyMessage>();

            EventQueue = new ConcurrentQueue<EventInfo>();

            Task.Run(MessageReceiver);
        }

        public LobbyInfo JoinGame(string name, int lobbySize, int interfaceIndex, string address, int port)
        {
            _NetworkComms.JoinGroup(interfaceIndex, address, port);
            _NetworkComms.StartReceiving();

            Stopwatch sw = new Stopwatch();
            _LobbyInfo = new LobbyInfo();

            PlayerInfo localPlayerInfo = new PlayerInfo { PlayerID = _UniqueID, PlayerName = name };
            _logger.Log("Local player: " + localPlayerInfo.ToString());
            _logger.Log("Looking for available lobby with size " + lobbySize + "...");
            bool lobbyAvailable = false;
            sw.Start();
            //Look for other advertising their lobby and check if size matches
            while (sw.ElapsedMilliseconds < 2000)
            {
                bool messageAvailable = AdvertiseLobbyMessages.TryDequeue(out AdvertiseLobbyMessage receivedMsg);

                if (messageAvailable)
                {
                    //Check if advertised lobby size matches
                    if (receivedMsg.LobbySize == lobbySize)
                    {
                        lobbyAvailable = true;
                    }
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            sw.Reset();


            if (lobbyAvailable)
            {
                _logger.Log("Available lobby found, trying to join it...");

                int hostID = 0;
                while (true)
                {
                    bool messageAvailable = AdvertiseLobbyMessages.TryDequeue(out AdvertiseLobbyMessage receivedMsg);

                    if (messageAvailable)
                    {
                        //Check if advertised lobby size matches
                        if (receivedMsg.LobbySize != lobbySize) { continue; }

                        //Check if lobby advertisement was from our host
                        if (receivedMsg.Sender == hostID)
                        {
                            //Check if lobby is now full
                            if (receivedMsg.LobbyInfo.Players.Count == lobbySize)
                            {
                                //Lobby full, we can proceed initiating game                            
                                _LobbyInfo = receivedMsg.LobbyInfo;
                                _logger.Log("Lobby is now full");
                                break;
                            }
                        }
                        else
                        {
                            //Log message
                            Debug.WriteLine("Received correct AdvertiseLobbyMessages: " + JsonSerializer.Serialize(receivedMsg));

                            //Check if we are already in lobby
                            foreach (PlayerInfo info in receivedMsg.LobbyInfo.Players)
                            {
                                if (info.PlayerID == _UniqueID)
                                {
                                    //We are in this lobby so we can set hostID
                                    hostID = receivedMsg.Sender;
                                    _logger.Log("We are added to lobby");
                                }
                            }

                            //If we don't have host try to join game
                            if (hostID == 0)
                            {
                                JoinLobbyMessage joinLobbyMessage = new JoinLobbyMessage()
                                {
                                    Sender = _UniqueID,
                                    MsgID = _JoinLobbyMessageID,
                                    Name = name,
                                    Receiver = receivedMsg.Sender
                                };

                                _logger.Log("Sent request to join lobby, receiver: " + joinLobbyMessage.Receiver);
                                _NetworkComms.SendMessage(JsonSerializer.SerializeToUtf8Bytes(joinLobbyMessage));
                                _JoinLobbyMessageNumber++;
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            else
            {
                _logger.Log("No available lobby found, creating own lobby...");

                //Add local player to lobby
                _LobbyInfo.Players.Add(localPlayerInfo);

                string advertiseLobbyMessageID = _AdvertiseLobbyMessageID;
                _AdvertiseLobbyMessageNumber++;
                AdvertiseLobbyMessage advertiseLobbyMessage = new AdvertiseLobbyMessage()
                {
                    Sender = _UniqueID,
                    MsgID = advertiseLobbyMessageID,
                    LobbySize = lobbySize,
                    LobbyInfo = _LobbyInfo
                };

                byte[] advertiseLobbyMessageBytes = JsonSerializer.SerializeToUtf8Bytes(advertiseLobbyMessage);

                //Advertise lobby until lobby full
                while (_LobbyInfo.Players.Count < lobbySize)
                {
                    _logger.Log("Sent lobby advertisement");
                    _NetworkComms.SendMessage(advertiseLobbyMessageBytes);

                    Thread.Sleep(500);

                    bool joinLobbyMessageAvailable;
                    do
                    {
                        joinLobbyMessageAvailable = JoinLobbyMessages.TryDequeue(out JoinLobbyMessage joinLobbyMessage);

                        if (joinLobbyMessageAvailable)
                        {
                            //Log message
                            _logger.Log("Received response to lobby advertisement from: Name: " + joinLobbyMessage.Name + ", ID: " + joinLobbyMessage.Sender);

                            //Check if player is already in lobby
                            if (IsThisPlayerInLobby(joinLobbyMessage.Sender) == false)
                            {
                                //Add new player to lobby
                                PlayerInfo newPlayerInfo = new PlayerInfo { PlayerID = joinLobbyMessage.Sender, PlayerName = joinLobbyMessage.Name };
                                _LobbyInfo.Players.Add(newPlayerInfo);

                                //Update lobbyInfo in advertisement message
                                advertiseLobbyMessage.LobbyInfo = _LobbyInfo;

                                //Serialize advertisement message
                                advertiseLobbyMessageBytes = JsonSerializer.SerializeToUtf8Bytes(advertiseLobbyMessage);

                                _logger.Log("New player added to lobby: " + newPlayerInfo.ToString());
                            }
                        }
                    } while (joinLobbyMessageAvailable);
                }

                _logger.Log("Lobby is now full");

                //Lobby is now full, send some extra advertisements so all other nodes realize for sure that lobby is full
                for (int i = 0; i < 10; i++)
                {
                    _NetworkComms.SendMessage(advertiseLobbyMessageBytes);
                    Thread.Sleep(200);
                }
            }

            //Sort player list so that first one has smallest ID
            _LobbyInfo.Players.Sort((PlayerInfo a, PlayerInfo b) => { return a.PlayerID.CompareTo(b.PlayerID); });

            //Search smallest playerID
            int smallestPlayerID = int.MaxValue;
            foreach (PlayerInfo info in _LobbyInfo.Players)
            {
                if (info.PlayerID < smallestPlayerID)
                {
                    smallestPlayerID = info.PlayerID;
                }
            }

            //Set smallest playerID player as a dealer
            foreach (PlayerInfo info in _LobbyInfo.Players)
            {
                if (info.PlayerID == smallestPlayerID)
                {
                    info.Dealer = true;
                }
            }

            _logger.Log("Players:");
            foreach (PlayerInfo info in _LobbyInfo.Players)
            {
                _logger.Log(info.ToString());
            }
            _logger.Log("Initiating game...");
            _GameState.InitGame(_LobbyInfo.Players, _LobbyInfo.Players.Find(player => player.PlayerID == _UniqueID), smallestPlayerID);
            _logger.Log("Initing ready");

            return _LobbyInfo;
        }

        public LobbyInfo JoinGame(string name, int lobbySize, int interfaceIndex)
        {
            return JoinGame(name, lobbySize, interfaceIndex, multicastGroupAddress, multicastGroupPort);
        }
        public void EndTurn()
        {
            _logger.Log("Ending turn...");
            TurnInfo turnInfo = _GameState.GetTurn();

            if (turnInfo == null)
            {
                Debug.WriteLine("Received null turnInfo from GameState");
                return;
            }

            //Use same ID for all turnInfo messages for this turn
            string msgID = _TurnInfoMessageID;
            _TurnInfoMessageNumber++;
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
                    _logger.Log("Received response to turn info message from " + response.Sender);

                    //Check if this player is in lobby
                    if (IsThisPlayerInLobby(response.Sender) == false) { Debug.WriteLine("Receive msg error: player not in lobby"); continue; }

                    //Check if turn was approved
                    if (response.Approve == false) { Debug.WriteLine("Receive msg error: player did not approve turn, reason: " + response.ErrorString); continue; }

                    //Check if this player has already approved turn
                    if (ApprovedPlayers.Contains(response.Sender) == true) { Debug.WriteLine("Error: player has already approved turn"); continue; }

                    _logger.Log("Other player approved turn, PlayerID: " + response.Sender);
                    ApprovedPlayers.Add(response.Sender);

                    //Check if it was last required approvement
                    if (ApprovedPlayers.Count == requiredNumberOfPlayerApprovals) { break; }
                } while (responseAvailable);

                if (ApprovedPlayers.Count == requiredNumberOfPlayerApprovals)
                {
                    turnApproved = true;
                    break;
                }
            }

            if (turnApproved)
            {
                //All good
                _logger.Log("Turn ended successfully");
                //Clear unnessary responses
                ResponseMessages.Clear();
            }
            else
            {
                _logger.Log("Ending turn FAILED, did not receive approvement from other players");
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
            _logger.Log("Received turn info message from other player: " + JsonSerializer.Serialize(msg));

            StateUpdateInfo receivedInfo = _GameState.Update(msg.TurnInfo);

            bool approved = receivedInfo.Result == StateUpdateResult.Ok;

            if (approved)
            {
                _logger.Log("Received turn info message approved");
            }
            else
            {
                _logger.Log("Error processing turn info: " + receivedInfo.ErrorString);
                Debug.WriteLine("Error processing turn info: " + receivedInfo.ErrorString);
            }

            ResponseMessage response = new ResponseMessage() { Sender = _UniqueID, Receiver = msg.Sender, Approve = approved, ErrorString = receivedInfo.ErrorString, ReceivedMsgID = msg.MsgID, MsgID = _ResponseMessageID };
            _NetworkComms.SendMessage(JsonSerializer.SerializeToUtf8Bytes(response));
            _ResponseMessageNumber++;

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
                            case MessageType.TurnInfo:
                                HandleTurnInfoMessage(JsonSerializer.Deserialize<TurnInfoMessage>(data));
                                break;
                            case MessageType.Response:
                                ResponseMessage responseMessage = JsonSerializer.Deserialize<ResponseMessage>(data);

                                //Only add message to queue if we are receiver
                                if (responseMessage.Receiver == _UniqueID)
                                {
                                    ResponseMessages.Enqueue(responseMessage);
                                }

                                break;
                            case MessageType.AdvertiseLobby:
                                AdvertiseLobbyMessages.Enqueue(JsonSerializer.Deserialize<AdvertiseLobbyMessage>(data));
                                break;
                            case MessageType.JoinLobby:
                                JoinLobbyMessage joinLobbyMessage = JsonSerializer.Deserialize<JoinLobbyMessage>(data);

                                //Only add message to queue if we are receiver
                                if (joinLobbyMessage.Receiver == _UniqueID)
                                {
                                    JoinLobbyMessages.Enqueue(joinLobbyMessage);
                                }
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
    }
}
