using System.Net;
using System.Net.Sockets;
using UnityEngine;

public static class NetworkInfo
{
    public static int defaultPort = 7910;

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        Debug.LogError("No network adapters with an IPv4 address in the system!");
        return "Unavailable";
    }
}
