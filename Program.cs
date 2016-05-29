using System;
using System.Net;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Reflection;

namespace KFServerPerks
{

    public enum ENetID
    {
        ID_None = 0,
        ID_Open,
        ID_RequestPassword,
        ID_HeresPassword,
        ID_ConnectionClosed,
        ID_PasswordCorrect,
        ID_NewPlayer,
        ID_BadNewPlayer,
        ID_SendPlayerData,
        ID_UpdatePlayer,
        ID_KeepAlive
    }

    class Program
    {
        private static string Password = "nope";
        private static int    Timeout  = 60;
        private static int    Port     = 5000;

        private static UdpClient soc        = null;
        private static IPEndPoint endpoint  = null;

        [AttributeUsage(System.AttributeTargets.Method)]
        public class ENetIDCommandType : Attribute
        {
            public string cmdType;
            public ENetID cmdInt;

            public ENetIDCommandType( string name, ENetID cmd )
            {
                this.cmdType = name;
                this.cmdInt  = cmd;
            }
        }

        static void StartListener()
        {
            soc      = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
            endpoint = new IPEndPoint(IPAddress.Any, 5000);

            while (soc.Client != null)
                OnMessageReceived(soc.Receive(ref endpoint));

            soc.Close();
        }

        static void OnMessageReceived(byte[] res )
        {
            byte cmd    = res[0];
            byte[] data = new ArraySegment<byte>( res, 1, res.Length - 1 ).ToArray();
   
            Console.WriteLine("Listening on {0}:{1}", endpoint.Address, endpoint.Port );
            Console.WriteLine("Received information: Type: {0} | Command: {1}", cmd, data.Length );

            /* Attempt to map message type to method with the correct attribute. */
            MethodInfo[] methods = typeof(Program).GetMethods()
                         .Where(m => m.GetCustomAttributes(typeof(ENetIDCommandType), false).Length > 0)
                         .ToArray();

            for (int I = 0; I < methods.Length; I++)
            {
                MethodInfo method        = methods[I];
                ENetIDCommandType attr   = method.GetCustomAttribute<ENetIDCommandType>();
                string name = attr.cmdType;
                ENetID id   = attr.cmdInt;

                if (((int)id == (int)cmd))
                {
                    Console.WriteLine("[{0}] {1}.", DateTime.Now, name);
                    method.Invoke( null, ( new object[] {
                        System.Text.Encoding.ASCII.GetString( data )
                    }));
                    break;
                }
            }
        }

        static void SendMessage( ENetID type, string message = "" )
        {
            byte[] msgarr       = System.Text.Encoding.ASCII.GetBytes( message );
            byte[] broadcastmsg = new byte[msgarr.Length + 1];

            // >__>
            broadcastmsg[0]     = (byte)((int)type);
            broadcastmsg        = broadcastmsg.Concat(msgarr).ToArray<byte>();

            Console.WriteLine(broadcastmsg[0].ToString());
            soc.Send(broadcastmsg, broadcastmsg.Length, endpoint);
        }

        [ENetIDCommandType( "ConnectionStart", ENetID.ID_Open )]
        public static void ConnectionStart( string data )
        {
            SendMessage(ENetID.ID_RequestPassword);
        }

        [ENetIDCommandType("CheckPassword", ENetID.ID_HeresPassword)]
        public static void CheckPassword(string data)
        {
            if (data == Password)
            {
                // Add address to the whitelist.
                SendMessage(ENetID.ID_PasswordCorrect);
                return;
            }
            else
            {
                SendMessage(ENetID.ID_ConnectionClosed);
                return;
            }
        }

        [ENetIDCommandType("KeepAlive", ENetID.ID_KeepAlive)]
        public static void KeepAlive( string data )
        {
            Console.WriteLine("Keep alive request.");
        }

        static void Main(string[] args)
        {
            Console.Title = "KillingFloor Perk Server 0.1";

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
