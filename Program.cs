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

    public static class Settings
    {
        public static int    Port = 6000;
        public static string Password = "nope";

        public static string MySQLHost;
        public static string MySQLPass;
        public static string MySQLDatabase;
        public static string MySQLPort;
    }

    class Program
    {
        private static UdpClient  soc      = null;
        private static IPEndPoint endpoint = null;

        private static List<string> connections;

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
            soc = new UdpClient(new IPEndPoint(IPAddress.Any, Settings.Port ));
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
            Console.WriteLine(text);
        }

        [ENetCommandType("ConnectionStart", ENetID.ID_Open)]
        public static void ConnectionStart(IPEndPoint endpoint, string data)
        {
            SendMessage( endpoint, ENetID.ID_RequestPassword );
        }

        [ENetCommandType("CheckPassword", ENetID.ID_HeresPassword)]
        public static void CheckPassword( IPEndPoint endpoint, string data)
        {
            if (data == Settings.Password)
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

        [ENetCommandType("GetPlayer", ENetID.ID_NewPlayer)]
        public static void GetPlayer(IPEndPoint endpoint, string data)
        {
            string[] tbl      = data.Split(new char[] { '*' });
            string steamid64  = tbl[0];
            string name       = tbl[1];

            User ply           = new User(steamid64, User.ID_TYPE.STEAMID_64);

            string[] id32split = ply.steamid32.Split(new char[] { ':' });
            string condencedId = (int.Parse(id32split[1]) + 1) + id32split[2];

            // Requires you to send back the users data, if its new then return nothing.
            SendMessage(endpoint, ENetID.ID_NewPlayer, $"{condencedId}|{steamid64}" );
            SendMessage(endpoint, ENetID.ID_SendPlayerData, $"{condencedId}|{ (char)10 }" );

            Logging.Log($"Player received: {name} ({ply.steamid32})");
        }

        [ENetCommandType("UpdatePlayer", ENetID.ID_UpdatePlayer)]
        public static void UpdatePlayer( IPEndPoint endpoint, string data )
        {
            string[] dataArr = data.Split(new char[] { '|' });
            string playerId  = "STEAM_0:" + ( int.Parse(dataArr[0][0].ToString()) - 1 ) + ":" + dataArr[0].Substring(1);
            string[] stats   = dataArr[1].Split(new char[] { ',' });

            User playerstats = new User(playerId, User.ID_TYPE.STEAMID_32);
            playerstats.SetStats(stats);
            playerstats.SaveToDatabase();
  
            Logging.Log($"Received UpdatePlayer for {playerId}");
        }

        static void Main(string[] args)
        {
            Console.Title            = "KillingFloor Perk Server 0.1";
            Console.ForegroundColor  = ConsoleColor.Gray;

            Logging.Log($"Listening on Port {Settings.Port} with Password '{Settings.Password}'.");

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
