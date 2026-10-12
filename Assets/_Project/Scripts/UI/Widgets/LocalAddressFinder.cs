using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Fireflies.UI.Widgets
{
    // IPs de este equipo en la red local, para que el anfitrión sepa cuál compartir.
    public static class LocalAddressFinder
    {
        public static List<string> FindIPv4Addresses()
        {
            List<string> addresses = new List<string>();
            try
            {
                foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                    {
                        addresses.Add(address.ToString());
                    }
                }
            }
            catch (SocketException)
            {
                addresses.Clear();
            }

            return addresses;
        }
    }
}
