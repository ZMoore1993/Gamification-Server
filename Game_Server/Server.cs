using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Game_Server
{
    internal class Server
    {
        //Networking stuff
        //private List<Client> clientPool;    //Client pool
        //private TcpListener server;         //Tcp listener used by server, root of all network communications
        internal static WebSocketServer wssv;       //WebSocketServer
        //public static Dictionary<string, Client> online_clients; //key is the websocket id; so far used in Save.cs
        //private WebSocketSessionManager sessions;

        //private WebSocketListener server;
        //private Rfc6455 rfc6455;
        //private CancellationTokenSource cancellation;
        private int _connectionCount;       //Number of connections ever made while server was running, not client count
                                            //private SocketPolicyServer socketPolicyServer;  //Used to serve all socket policy requests
                                            //private UdpListener _udpListener;

        //UDP socket - all UDP will go through this socket!
        //private Socket udpSocket;

        //Database stuff
        private static MongoServer DBServer;       //Link to database server
        private static MongoDatabase DB;           //Database
        private static MongoCollection<BsonDocument> PlayerCollection; //List of all players, available for clients to iterate through

        //Ports and addresses
        public static Int32 TcpPort = 60001;            //TCP port number
        //public static Int32 UdpPort = 60001;            //UDP port, used in above UDP socket
        //public static Int32 SocketPolicyPort = 60002;   //Socket policy port number

        //Etc
        public static string ServerVersion = "1.0";   //Arbitrary numbering schemes FTW. Next version: 1.1
        public static string AvailableClientVersion = "1.0";
        //private WorldManager worldManager;
        public static Logger logger = new Logger(100, ServerVersion);

        public static MongoCollection<BsonDocument> Players { get { return PlayerCollection; } }
        public static MongoServer MServer { get { return DBServer; } }

        public Server()
        {
            //Instantiate server
            //server = new TcpListener(IPAddress.Parse("0.0.0.0"), TcpPort);
            //*/
            wssv = new WebSocketServer(TcpPort);//var wssv = new WebSocketServer(TcpPort, true);
            //var cert = ConfigurationManager.AppSettings["ServerCertFile"];
            //var passwd = ConfigurationManager.AppSettings["CertFilePassword"];
            //wssv.SslConfiguration.ServerCertificate = new X509Certificate2(cert, passwd);
            //*/
            //sessions = new WebSocketSessionManager();
            wssv.AddWebSocketService<Client>("/Client");
            wssv.AddWebSocketService<AccountCreation>("/AccountCreation");
            wssv.KeepClean = false;

            //Instantiate socket policy server
            //socketPolicyServer = new SocketPolicyServer(SocketPolicyPort);

            //Create UDP socket
            /*/udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, UdpPort));
            
            //Instantiate multicast server
            _udpListener = new UdpListener(udpSocket);/*/


            //Connect to local database
            DBServer = new MongoClient("mongodb://localhost:443").GetServer();
            DB = DBServer.GetDatabase("test");
            
            PlayerCollection = DB.GetCollection<BsonDocument>("playersV2");
            
            
            //Create World Manager
            //worldManager = new WorldManager();

            //Create world for worldManager
            //worldManager.CreateWorld();
        }

        public void Run()
        {
            //Start the server
            //server.Start();
            wssv.Start();


            //Start the socket policy server
            //socketPolicyServer.Start();

            //Start the UDP multicast server
            //_udpListener.Start();

            //*
            //Accept new clients
            try
            {
                Console.WriteLine("waiting on port " + TcpPort);
                while(true)
                {

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                //server.Stop();
                wssv.Stop(CloseStatusCode.Normal, "server closed normally\n");
            }//*/
            
            
            //Console.ReadKey(true);
            //wssv.Stop();
            //
            //
            Console.WriteLine("> server closed");
            Console.WriteLine("Press any key to exit...");
            Console.Read();
        }
        

        static int CountSpaces(string key)
        {
            return key.Length - key.Replace(" ", string.Empty).Length;
        }

        static string ReadLine(Stream stream)
        {
            var sb = new StringBuilder();
            var buffer = new List<byte>();
            while (true)
            {
                buffer.Add((byte)stream.ReadByte());
                var line = Encoding.ASCII.GetString(buffer.ToArray());
                if (line.EndsWith(Environment.NewLine))
                {
                    return line.Substring(0, line.Length - 2);
                }
            }
        }

        static byte[] GetBigEndianBytes(int value)
        {
            var bytes = 4;
            var buffer = new byte[bytes];
            int num = bytes - 1;
            for (int i = 0; i < bytes; i++)
            {
                buffer[num - i] = (byte)(value & 0xffL);
                value = value >> 8;
            }
            return buffer;
        }
    }
}
