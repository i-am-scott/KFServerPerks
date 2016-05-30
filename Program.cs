using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using KFServerPerks.util;
using System.Collections.Generic;

namespace KFServerPerks
{

    class Program
    {
        private static int Port        = 6000;
        private static string Password = "nope";

        private static UdpClient  soc      = null;
        private static IPEndPoint endpoint = null;

        private static List<string> connections;

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

        static bool IsRegistered(IPEndPoint endpoint)
        {
            bool whitelisted = connections.Exists(element => element == endpoint.Address.ToString());

            if ( !whitelisted)
            {
                Logging.Log($"UNAUTHORIZED CONNECTION MADE.");
                SendMessage(endpoint, ENetID.ID_ConnectionClosed );
            }

            return whitelisted;
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
            ENetID cmd     = (ENetID)res[0];
            byte[] data    = new ArraySegment<byte>(res, 1, res.Length - 1).ToArray();
            string encoded = Encoding.ASCII.GetString(data);

            Logging.Log( $"[RECEIVED] {endpoint.Address}:{endpoint.Port} {cmd} {data.Length} bytes | {encoded}" );

            /* Attempt to map message type to method with the correct attribute. */
            MethodInfo[] methods = typeof(Program).GetMethods()
                         .Where(m => m.GetCustomAttributes(typeof(ENetCommandType), false).Length > 0)
                         .ToArray();

            //if (cmd != ENetID.ID_HeresPassword && cmd != ENetID.ID_Open && cmd != ENetID.ID_KeepAlive);
               // if (!IsRegistered(endpoint))
               //     return;

            bool found = false;

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
                            endpoint,
                            encoded
                        }));

                        found = true;
                        break;
                    }
                }
            }catch( Exception e )
            {
                Logging.Log( e.Message, true);
            }

            if (!found)
                MessageReceived(endpoint, encoded);
        }

        static void SendMessage( IPEndPoint endpoint, ENetID type, string message = "" )
        {
            byte[] msgarr       = Encoding.ASCII.GetBytes(message);
            List<byte> dataList = new List<byte>(msgarr.Length + 1);

            dataList.Add(Convert.ToByte((int)type));
            dataList.AddRange(msgarr);

            byte[] broadcastmsg = dataList.ToArray();

            Logging.Log( $"[SENT] {endpoint.Address}:{endpoint.Port} {type} {msgarr.Length} bytes | {message}");

            soc.Send(broadcastmsg, broadcastmsg.Length, endpoint );
        }

        static void MessageReceived(IPEndPoint endpoint, string text)
        {

        }

        [ENetCommandType("ConnectionStart", ENetID.ID_Open)]
        public static void ConnectionStart(IPEndPoint endpoint, string data)
        {
            SendMessage( endpoint, ENetID.ID_RequestPassword );
        }

        [ENetCommandType("CheckPassword", ENetID.ID_HeresPassword)]
        public static void CheckPassword( IPEndPoint endpoint, string data)
        {
            if (data == Password)
            {
                SendMessage( endpoint, ENetID.ID_PasswordCorrect );
                connections.Add(endpoint.Address.ToString());
                return;
            }
            else
            {
                SendMessage( endpoint, ENetID.ID_ConnectionClosed );
                return;
            }
        }
 
        [ENetCommandType("KeepAlive", ENetID.ID_KeepAlive)]
        public static void KeepAlive( IPEndPoint endpoint, string data )
        {
            Logging.Log("Keep alive request.");
        }

        [ENetCommandType("NewPlayer", ENetID.ID_NewPlayer)]
        public static void NewPlayer(IPEndPoint endpoint, string data)
        {
            string[] tbl      = data.Split(new char[] { '*' });
            string steamid64  = tbl[0];
            string steamid32  = ConvertToSteamID32(steamid64);
            string name       = tbl[1];

            SendMessage(endpoint, ENetID.ID_NewPlayer, $"0|{steamid64}" );

            // Requires you to send back the users data, if its new then return nothing.
            SendMessage(endpoint, ENetID.ID_SendPlayerData, $"0|" );

            Logging.Log($"Player received: {name} ({steamid32})");
        }

        [ENetCommandType("UpdatePlayer", ENetID.ID_UpdatePlayer)]
        public static void UpdatePlayer( IPEndPoint endpoint, string data )
        {
            string[] dataArr = data.Split(new char[] { '|' });
            int playerId = int.Parse(dataArr[0]);
            string[] stats = dataArr[1].Split(new char[] { ',' });

            Stats playerstats = new Stats(playerId, stats);
        }

        static void Main(string[] args)
        {
            Console.Title            = "KillingFloor Perk Server 0.1";
            Console.ForegroundColor  = ConsoleColor.Gray;

            Logging.Log($"Listening on Port {Port} with Password '{Password}'.");

            connections = new List<string>();

            Thread thr = new Thread(new ThreadStart(StartListener));
            thr.IsBackground = true;
            thr.Start();

            while (thr.ThreadState != ThreadState.Stopped)
                if (Console.ReadLine() == "exit")
                    break;
                else
                    SendMessage( endpoint, ENetID.ID_KeepAlive, Console.ReadLine() );

            soc?.Close();
        }

    }
}
