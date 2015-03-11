using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BitDB
{
    public static class Core
    {
        public static string GetIP()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());

            foreach (var ip in host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork))
            {
                return ip.ToString();
            }
            return "0.0.0.0";
        }
    }
}