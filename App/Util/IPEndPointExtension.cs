using System.Net;

namespace KFServerPerks
{
    public static class IPEndPointExtension
    {
        public static string GetAddressString(this IPEndPoint ep)
        {
            return ep.Address + ":" + ep.Port;
        }
    }
}