using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace dsproject
{
    internal class NetworkCommunication
    {
        UdpClient tClient;
        UdpClient rClient;
        string multicastGroupAddress = "239.1.2.3";
        int multicastGroupPort = 55000;
        UInt32 uniqueId;

        public NetworkCommunication()
        {
            Random rnd = new Random();
            uniqueId = (uint)rnd.Next(1, 1000000);

            tClient = new UdpClient();

            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 55000);
            rClient = new UdpClient();
            rClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            rClient.Client.Bind(localEndPoint);
            rClient.JoinMulticastGroup(IPAddress.Parse(multicastGroupAddress));

            //Thread recThread = new Thread(Receive);
            //recThread.Start();
            //Thread.Sleep(1000);
            //Thread.Sleep(1000);
            //SendMessageTest();
            //Thread.Sleep(1000);
            //SendMessageTest();
            //SendMessageTest();

        }

        public void WaitForPlayersToJoin()
        {
            Thread sendThread = new Thread(WaitForPlayersToJoinSendLoop);
            Thread recThread = new Thread(WaitForPlayersToJoinSendReceiveLoop);

            sendThread.Start();
            recThread.Start();
        }

        public void WaitForPlayersToJoinSendLoop()
        {
            int i = 0;

            CreateGameMessage msg = new CreateGameMessage(uniqueId);

            while (i < 10)
            {
                i++;

                SendMessage(msg);
                Thread.Sleep(1000);
            }
        }

        public void WaitForPlayersToJoinSendReceiveLoop()
        {
            while (true)
            {
                ReceiveMessage();
                Thread.Sleep(500);
            }
        }


        public void SendMessage(Message msg)
        {
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(multicastGroupAddress), multicastGroupPort);

            byte[] sentData = msg.GetBytes();
            tClient.Send(sentData, sentData.Length, ipEndPoint);
            Console.WriteLine("Sent message. Sender: " + msg.Sender + ", Type: " + msg.MsgType);
        }

        public Message ReceiveMessage()
        {
            var ipEndPoint2 = new IPEndPoint(IPAddress.Any, 0);

            byte[] recData = rClient.Receive(ref ipEndPoint2);

            Message msg = Message.CreateMessageFromBytes(recData);

            if (msg == null)
            {
                Console.WriteLine("Faulty message received.");
                Console.WriteLine(BitConverter.ToString(recData));
            }
            else
            {
                Console.WriteLine("Received message. Sender: " + msg.Sender + ", Type: " + msg.MsgType);
            }

            return msg;
        }





        // Test functions
        public void SendMessageTest()
        {
            string text = "SENT DATA TEST";
            byte[] sentData = Encoding.Default.GetBytes(text);

            IPEndPoint ipEndPoint1 = new IPEndPoint(IPAddress.Parse(multicastGroupAddress), multicastGroupPort);

            Console.WriteLine("Sent message: " + text);
            tClient.Send(sentData, sentData.Length, ipEndPoint1);  
        }

        public void ReceiveTest()
        {
            var ipEndPoint2 = new IPEndPoint(IPAddress.Any, 0);

            Console.WriteLine("Started receiving");
            while (true)
            {
                byte[] recData = rClient.Receive(ref ipEndPoint2);

                string receivedMessage = Encoding.Default.GetString(recData);

                Console.WriteLine("Received message: " + receivedMessage);
            }
        }
    }
}
