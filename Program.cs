using IniParser;
using IniParser.Model;
using KFServerPerks.util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KFServerPerks
{
    public class Program
    {
        public static Mysql Database;

        public static Settings settings = Settings.Load();
        private static EndPoint serverEndPoint;
        private static Socket serverSocket = null;
        private static EndPoint clientEndPoint = null;
        private static List<string> connections = new List<string>();
        public static Dictionary<ENetID, MethodInfo> messageReceivers;

        private static void RegisterMessageReceivers()
        {
            messageReceivers = new Dictionary<ENetID, MethodInfo>();
            MethodInfo[] methods = typeof(Program).GetMethods()
                         .Where(m => m.GetCustomAttributes(typeof(ENetCommandType), false).Length > 0)
                         .ToArray();

            foreach (MethodInfo method in methods)
            {
                ENetCommandType attr = method.GetCustomAttribute<ENetCommandType>();
                messageReceivers.Add(attr.cmdInt, method);
            }
        }

        private static bool IsRegistered(IPEndPoint endpoint)
        {
            if (settings.AllowAll) return true;

            bool whitelisted = connections.Exists(element => element == endpoint.GetAddressString());
            if (!whitelisted)
                SendMessage(endpoint, ENetID.ID_ConnectionClosed);

            return whitelisted;
        }

        private static void StartListener()
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

        private static void OnMessageReceived(byte[] res, int length)
        {
            IPEndPoint receviedEndPoint = clientEndPoint as IPEndPoint;
            if (settings.Whitelist.Count > 0 && !settings.Whitelist.Contains(receviedEndPoint.Address.ToString()))
            {
                Logging.Log($"[WHITELIST] {receviedEndPoint.Address}:{receviedEndPoint.Port} sent a request but is not whitelisted");
                return;
            }

            ENetID cmd = (ENetID)res[0];
            string encoded = Encoding.ASCII.GetString(res, 1, length - 1);
            Logging.Log($"[RECEIVED] {receviedEndPoint.Address}:{receviedEndPoint.Port} {cmd} {length} bytes | {encoded.Replace(Environment.NewLine, "")}");

            if (cmd != ENetID.ID_HeresPassword && cmd != ENetID.ID_Open && cmd != ENetID.ID_KeepAlive)
                if (!IsRegistered(receviedEndPoint))
                    return;

            try
            {
                if (messageReceivers.ContainsKey(cmd))
                {
                    messageReceivers[cmd]?.Invoke(null, (new object[] {
                        receviedEndPoint,
                        encoded
                    }));
                }
            }
            catch (Exception e)
            {
                Logging.Log(e.Message, true);
            }
        }

        private static void SendMessage(EndPoint endpoint, ENetID type, string message = "")
        {
            SendMessage(endpoint as IPEndPoint, type, message);

            if (type == ENetID.ID_ConnectionClosed)
                CloseConnection(endpoint as IPEndPoint);
        }

        private static void SendMessage(IPEndPoint endpoint, ENetID type, string message = "")
        {
            if (IPAddress.IsLoopback(endpoint.Address) || endpoint.Address.Address == 0)
                return;

            byte[] msgarr = Encoding.ASCII.GetBytes(message);
            List<byte> dataList = new List<byte>(msgarr.Length + 1);

            dataList.Add(Convert.ToByte((int)type));
            dataList.AddRange(msgarr);

            byte[] broadcastmsg = dataList.ToArray();

            Logging.Log($"[SENT] {endpoint.Address}:{endpoint.Port} {type} {msgarr.Length} bytes | {message}");
            serverSocket.SendTo(broadcastmsg, broadcastmsg.Length, SocketFlags.None, clientEndPoint);
        }

        [ENetCommandType(ENetID.ID_Open)]
        public static void ConnectionStart(IPEndPoint endpoint, string data)
        {
            SendMessage(endpoint, ENetID.ID_RequestPassword);
        }

        [ENetCommandType(ENetID.ID_HeresPassword)]
        public static void CheckPassword(IPEndPoint endpoint, string data)
        {
            if (data == settings.ServerPassword)
            {
                SendMessage(endpoint, ENetID.ID_PasswordCorrect);
                connections.Add(endpoint.GetAddressString());
            }
            else
            {
                SendMessage(endpoint, ENetID.ID_ConnectionClosed);
            }
        }

        [ENetCommandType(ENetID.ID_KeepAlive)]
        public static void KeepAlive(IPEndPoint endpoint, string data)
        {
            Logging.Log("Keep alive request.");
        }

        [ENetCommandType(ENetID.ID_NewPlayer)]
        public static void GetPlayer(IPEndPoint endpoint, string data)
        {
            string[] tbl = data.Split(new char[] { '*' });
            string steamid64 = tbl[0];
            string name = tbl[1];

            // During the end phase and map transition/vote phases the server will spam NONE. It will finally send the real steamid and name once in the ready up/selection phase.
            if (steamid64.ToLower() == "none")
                return;

            User pl = new User(steamid64, User.ID_TYPE.STEAMID_64);
            if (pl.LoadFromDatabase())
            {
                SendMessage(endpoint, ENetID.ID_NewPlayer, $"{pl.InternalId}|{steamid64}");
                SendMessage(endpoint, ENetID.ID_SendPlayerData, $"{pl.InternalId}|{pl.KfStringFormat}" + Environment.NewLine);
            }
            else
            {
                SendMessage(endpoint, ENetID.ID_NewPlayer, $"{pl.InternalId}|{steamid64}");
                SendMessage(endpoint, ENetID.ID_SendPlayerData, $"{pl.InternalId}|{steamid64}|{(char)10}"); // Why does he want this. :(
            }

            Logging.Log($"Player received: {name} ({pl.Steamid32})");
        }

        [ENetCommandType(ENetID.ID_UpdatePlayer)]
        public static void UpdatePlayer(IPEndPoint endpoint, string data)
        {
            string[] dataArr = data.Split(new char[] { '|' });
            if (dataArr[0].ToLower() == "none")
                return;

            string[] stats = dataArr[1].Split(new char[] { ',' });
            User pl = new User(dataArr[0], User.ID_TYPE.INTERNAL);
            pl.SetStats(stats);
            pl.SaveToDatabase();

            Logging.Log($"Received UpdatePlayer for {pl.Steamid32}");
        }

        [ENetCommandType(ENetID.ID_ConnectionClosed)]
        public static void ConnectionClosed(IPEndPoint endpoint, string data)
        {
            Logging.Log("[RECEIVED] Connection closing.");
            CloseConnection(endpoint);
        }

        private static void CloseConnection(IPEndPoint endpoint)
        {
            string addressStr = endpoint.GetAddressString();
            if (connections.Remove(addressStr))
                Logging.Log($"[CONNECITON] No longer accepting connections from {addressStr}");
        }

        // Lazy. Slow. Bad.
        static void ConvertIniToMySQLDatabase(string fileName)
        {
            Logging.Log($"Converting Ini file {fileName} to database.");

            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile(fileName);

            int playerCount = data.Sections.Count;
            int playersNoData = 0;
            int playersParsed = 0;

            List<string> queries = new List<string>();
            List<OrderedDictionary> argArray = new List<OrderedDictionary>();

            Parallel.ForEach(data.Sections, new ParallelOptions
            {
                MaxDegreeOfParallelism = 3
            }, async (SectionData section) =>
            {
                if (section.Keys.Count == 0)
                {
                    ++playersNoData;
                    return;
                }

                KeyData plData = section.Keys.First();
                string steamid64 = section.SectionName;
                if (plData == null || plData.KeyName.ToLower() == "none" || section.Keys.Count == 0 || string.IsNullOrEmpty(steamid64) || steamid64.ToLower() == "none")
                {
                    ++playersNoData;
                    return;
                }

                string[] dataList = plData.Value.Split(',');
                if (dataList.Length < 25)
                {
                    ++playersNoData;
                    return;
                }

                User pl = new User(section.SectionName, User.ID_TYPE.STEAMID_64);
                pl.SetStats(plData.Value.Split(','));

                queries.Add(pl.GetQueryString());
                argArray.Add(pl.PerkData);

                ++playersParsed;
                await pl.SaveToDatabaseAsync(false);

                Console.Title = $"Sent {playersParsed}/{playerCount}";
            });

            Logging.Log($"Completed. Players Listed: {playerCount}, Invalid or empty data: {playersNoData}");
        }

        static void Main(string[] args)
        {
            Database = new Mysql(settings.MySQLHost, settings.MySQLUsername, settings.MySQLPasswword, settings.MySQLDatabase);

            if (args.Length > 0 && !string.IsNullOrEmpty(args[0]))
            {
                ConvertIniToMySQLDatabase(args[0]);

                Console.Write("\nDo you want to start up the perk server? [Y/N]: ");
                string r = Console.ReadLine();
                if (r.ToLower() != "y")
                    return;

                Console.Clear();
            }

            RegisterMessageReceivers();

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