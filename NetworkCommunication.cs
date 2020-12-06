using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace dsproject
{
    internal class NetworkCommunication
    {
        private UdpClient _SendingClient { get; set; }
        private UdpClient _ReceiveClient { get; set; }
        private readonly string multicastGroupAddress = "239.1.2.3";
        private readonly int multicastGroupPort = 55000;
        private Queue<byte[]> _ReceivedPackets { get; set; }
        private bool receiveLoopRunning { get; set; }
        private bool _joined;


        public NetworkCommunication()
        {
            _ReceivedPackets = new Queue<byte[]>();
        }

        public void JoinGroup(int interfaceIndex)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 55000);
            _ReceiveClient = new UdpClient();
            var opt = new MulticastOption(IPAddress.Parse(multicastGroupAddress), interfaceIndex);
            _ReceiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _ReceiveClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, opt);
            _ReceiveClient.Client.Bind(localEndPoint);

            _SendingClient = new UdpClient();
            _SendingClient.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, opt);

            _joined = true;
        }

        public void SendMessage(byte[] data)
        {
            if (!_joined) return;
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(multicastGroupAddress), multicastGroupPort);
            _SendingClient.Send(data, data.Length, ipEndPoint);
            Debug.WriteLine("Sent message:" + Encoding.UTF8.GetString(data));
        }

        public byte[] GetMessage()
        {
            if (_ReceivedPackets.Count == 0)
            {
                return null;
            }

            return _ReceivedPackets.Dequeue();
        }

        public void StartReceiving()
        {
            if (!_joined) return;

            if (receiveLoopRunning == false)
            {
                Action action = () =>
                {
                    var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);

                    while (true)
                    {
                        _ReceivedPackets.Enqueue(_ReceiveClient.Receive(ref ipEndPoint));
                    }
                };

                Task.Run(action);

                receiveLoopRunning = true;
            }
        }
    }
}
