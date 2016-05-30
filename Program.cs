using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using KFServerPerks.util;

namespace KFServerPerks
{

    class Program
    {
        private static int Port = 5000;
        private static string Password = "nope";

        private static UdpClient soc = null;
        private static IPEndPoint endpoint = null;

        [AttributeUsage(System.AttributeTargets.Method)]
        public class ENetCommandType : Attribute
        {
            public string cmdType;
            public ENetID cmdInt;

            public ENetCommandType(string name, ENetID cmd)
            {
                this.cmdType = name;
                this.cmdInt = cmd;
            }
        }

        static string ConvertToSteamID32(string steamid64)
        {
            long sid;
            string steamId = "";

            if (long.TryParse(steamid64, out sid))
            {
                long accountid = sid - (sid >> 32 << 32);
                int lastBit = accountid % 2 == 0 ? 0 : 1;
                steamId = "STEAM_0:" + lastBit + ":" + (accountid >> 1).ToString();
            }

            return steamId;
        }

        static void StartListener()
        {
            soc = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
            endpoint = new IPEndPoint(IPAddress.Any, 5000);

            while (soc.Client != null)
                OnMessageReceived(soc.Receive(ref endpoint));

            soc.Close();
        }

        static void OnMessageReceived(byte[] res)
        {
            byte cmd = res[0];
            byte[] data = new ArraySegment<byte>(res, 1, res.Length - 1).ToArray();
            string encoded = System.Text.Encoding.ASCII.GetString(data);

            Logging.Log($"Listening on {endpoint.Address}:{endpoint.Port}");
            Logging.Log($"Received information: Type: {cmd} | Command: {data.Length}");

            /* Attempt to map message type to method with the correct attribute. */
            MethodInfo[] methods = typeof(Program).GetMethods()
                         .Where(m => m.GetCustomAttributes(typeof(ENetCommandType), false).Length > 0)
                         .ToArray();

            for (int I = 0; I < methods.Length; I++)
            {
                MethodInfo method = methods[I];
                ENetCommandType attr = method.GetCustomAttribute<ENetCommandType>();
                string name = attr.cmdType;
                ENetID id = attr.cmdInt;

                if (((int)id == (int)cmd))
                {
                    Logging.Log($"[{DateTime.Now}] {name} {encoded}");
                    method.Invoke(null, (new object[] {
                        encoded
                    }));
                    break;
                }
            }
        }

        static void SendMessage(ENetID type, string message = "")
        {
            byte[] msgarr = System.Text.Encoding.ASCII.GetBytes(message);
            byte[] broadcastmsg = new byte[msgarr.Length + 1];

            // >__>
            broadcastmsg[0] = (byte)((int)type);
            broadcastmsg = broadcastmsg.Concat(msgarr).ToArray<byte>();

            soc.Send(broadcastmsg, broadcastmsg.Length, endpoint);
        }

        [ENetCommandType("ConnectionStart", ENetID.ID_Open)]
        public static void ConnectionStart(string data)
        {
            SendMessage(ENetID.ID_RequestPassword);
        }

        [ENetCommandType("CheckPassword", ENetID.ID_HeresPassword)]
        public static void CheckPassword(string data)
        {
            if (data == Password)
            {
                SendMessage(ENetID.ID_PasswordCorrect);
                return;
            }
            else
            {
                SendMessage(ENetID.ID_ConnectionClosed);
                return;
            }
        }

        [ENetCommandType("KeepAlive", ENetID.ID_KeepAlive)]
        public static void KeepAlive(string data)
        {
            Logging.Log("Keep alive request.");
        }

        [ENetCommandType("NewPlayer", ENetID.ID_NewPlayer)]
        public static void NewPlayer(string data)
        {
            string[] tbl = data.Split(new char[] { '*' });
            string steamid64 = ConvertToSteamID32(tbl[0]);
            string name = tbl[1];

            Logging.Log($"Player received: {name} ({steamid64})");
        }

        [ENetCommandType("UpdatePlayer", ENetID.ID_UpdatePlayer)]
        public static void UpdatePlayer(string data)
        {
            string[] dataArr = data.Split(new char[] { '|' });
            int playerId = int.Parse(dataArr[0]);
            string[] stats = dataArr[1].Split(new char[] { ',' });

            Stats playerstats = new Stats(playerId, stats);

        }

        static void Main(string[] args)
        {
            Console.Title = "KillingFloor Perk Server 0.1";
            Logging.Log($"Listening on Port {Port} with Password '{Password}'.");

            Thread thr = new Thread(new ThreadStart(StartListener));
            thr.IsBackground = true;
            thr.Start();

            while (thr.ThreadState != ThreadState.Stopped)
                if (Console.ReadLine() == "exit")
                    break;

            soc?.Close();
        }

    }
} 
