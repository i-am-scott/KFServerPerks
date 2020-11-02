using KFServerPerks.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace KFServerPerks
{
    public class Program
    {
        public static Settings settings = Settings.Load();
        private static EndPoint serverEndPoint;
        private static Socket serverSocket = null;
        private static EndPoint clientEndPoint = null;
        private static List<string> connections = new List<string>();

        static bool IsRegistered(IPEndPoint endpoint)
        {
            if (settings.AllowAll) return true;

            bool whitelisted = connections.Exists(element => element == endpoint.Address + ":" + endpoint.Port);
            if (!whitelisted)
            {
                Logging.Log($"UNAUTHORIZED CONNECTION MADE.");
                SendMessage(endpoint, ENetID.ID_ConnectionClosed);
            }

            return whitelisted;
        }

        static void StartListener()
        {
            serverEndPoint = new IPEndPoint(IPAddress.Any, settings.ServerPort);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.Bind(serverEndPoint);
            clientEndPoint = new IPEndPoint(IPAddress.Any, 6000);

            while (true)
            {
                byte[] data = new byte[2048];
                int recv = serverSocket.ReceiveFrom(data, ref clientEndPoint);
                OnMessageReceived(data, recv);
            }
        }

        static void OnMessageReceived(byte[] res, int length)
        {
            ENetID cmd = (ENetID)res[0];
            string encoded = Encoding.ASCII.GetString(res, 1, length - 1);

            IPEndPoint receviedEndPoint = clientEndPoint as IPEndPoint;
            Logging.Log($"[RECEIVED] {receviedEndPoint.Address}:{receviedEndPoint.Port} {cmd} {length} bytes | {encoded}");

            /* Attempt to map message type to method with the correct attribute. */
            MethodInfo[] methods = typeof(Program).GetMethods()
                         .Where(m => m.GetCustomAttributes(typeof(ENetCommandType), false).Length > 0)
                         .ToArray();

            if (cmd != ENetID.ID_HeresPassword && cmd != ENetID.ID_Open && cmd != ENetID.ID_KeepAlive)
                if (!IsRegistered(receviedEndPoint))
                    return;

            try
            {
                for (int I = 0; I < methods.Length; I++)
                {
                    MethodInfo method = methods[I];
                    ENetCommandType attr = method.GetCustomAttribute<ENetCommandType>();
                    string name = attr.cmdType;
                    ENetID id = attr.cmdInt;

                    if (((int)id == (int)cmd))
                    {
                        method.Invoke(null, (new object[] {
                            receviedEndPoint,
                            encoded
                        }));
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logging.Log(e.Message, true);
            }
        }

        static void SendMessage(EndPoint endpoint, ENetID type, string message = "")
        {
            SendMessage(endpoint as IPEndPoint, type, message);
        }

        static void SendMessage(IPEndPoint endpoint, ENetID type, string message = "")
        {
            byte[] msgarr = Encoding.ASCII.GetBytes(message);
            List<byte> dataList = new List<byte>(msgarr.Length + 1);

            dataList.Add(Convert.ToByte((int)type));
            dataList.AddRange(msgarr);

            byte[] broadcastmsg = dataList.ToArray();

            Logging.Log($"[SENT] {endpoint.Address}:{endpoint.Port} {type} {msgarr.Length} bytes | {message}");
            serverSocket.SendTo(broadcastmsg, broadcastmsg.Length, SocketFlags.None, clientEndPoint);
        }

        [ENetCommandType("ConnectionStart", ENetID.ID_Open)]
        public static void ConnectionStart(IPEndPoint endpoint, string data)
        {
            SendMessage(endpoint, ENetID.ID_RequestPassword);
        }

        [ENetCommandType("CheckPassword", ENetID.ID_HeresPassword)]
        public static void CheckPassword(IPEndPoint endpoint, string data)
        {
            if (data == settings.ServerPassword)
            {
                SendMessage(endpoint, ENetID.ID_PasswordCorrect);
                connections.Add(endpoint.Address + ":" + endpoint.Port);
                return;
            }
            else
            {
                SendMessage(endpoint, ENetID.ID_ConnectionClosed);
                return;
            }
        }

        [ENetCommandType("KeepAlive", ENetID.ID_KeepAlive)]
        public static void KeepAlive(IPEndPoint endpoint, string data)
        {
            Logging.Log("Keep alive request.");
        }

        [ENetCommandType("GetPlayer", ENetID.ID_NewPlayer)]
        public static void GetPlayer(IPEndPoint endpoint, string data)
        {
            string[] tbl = data.Split(new char[] { '*' });
            string steamid64 = tbl[0];
            string name = tbl[1];

            if (steamid64.ToLower() == "none")
            {
                Logging.Log("[RECEIVED] Player Id received as None! No idea what this means. It should be the player's steamid64. IGNORING.");
                return;
            }

            using (User pl = new User(steamid64, User.ID_TYPE.STEAMID_64))
            {
                if (pl.LoadFromDatabase())
                {
                    SendMessage(endpoint, ENetID.ID_NewPlayer, $"{pl.Id}|{steamid64}");
                    SendMessage(endpoint, ENetID.ID_SendPlayerData, $"{pl.Id}|{pl.KfStringFormat}" + Environment.NewLine);
                }
                else
                {
                    SendMessage(endpoint, ENetID.ID_NewPlayer, $"{pl.Id}|{steamid64}");
                    SendMessage(endpoint, ENetID.ID_SendPlayerData, $"{pl.Id}|{steamid64}|{(char)10}"); // Why does he want this. :(
                }

                Logging.Log($"Player received: {name} ({pl.Steamid32})");
            };

        }

        [ENetCommandType("UpdatePlayer", ENetID.ID_UpdatePlayer)]
        public static void UpdatePlayer(IPEndPoint endpoint, string data)
        {
            string[] dataArr = data.Split(new char[] { '|' });
            if (dataArr[0].ToLower() == "none")
            {
                Logging.Log("[RECEIVED] Player Id received as None!");
                return;
            }

            string playerId = "STEAM_0:" + (int.Parse(dataArr[0][0].ToString()) - 1) + ":" + dataArr[0].Substring(1);
            string[] stats = dataArr[1].Split(new char[] { ',' });

            using (User pl = new User(playerId, User.ID_TYPE.STEAMID_32))
            {
                pl.SetStats(stats);
                pl.SaveToDatabase();

                Logging.Log($"Received UpdatePlayer for {playerId}");
            };
        }

        static void Main(string[] args)
        {
            Console.Title = "KillingFloor Perk Server 0.1";
            Console.ForegroundColor = ConsoleColor.Gray;
            Logging.Log($"Listening on Port {settings.ServerPort} with Password '{settings.ServerPassword}'.");

            Thread thr = new Thread(new ThreadStart(StartListener));
            thr.IsBackground = true;
            thr.Start();

            while (thr.ThreadState != ThreadState.Stopped)
                if (Console.ReadLine() == "exit")
                    break;
                else
                    SendMessage(clientEndPoint, ENetID.ID_KeepAlive, Console.ReadLine());

            serverSocket?.Close();
        }

    }
}
