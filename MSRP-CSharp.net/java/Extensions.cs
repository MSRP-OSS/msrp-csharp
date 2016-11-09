using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace msrp.java
{
    static public class SocketExtensions
    {
        static public string getHostAddress(this Socket socket)
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
        }

        static public int getLocalPort(this Socket socket)
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Port;
        }
    }

    static public class MemoryStreamExtensions
    {
        static public void Flip(this MemoryStream ms)
        {
            //ms.Length = ms.Position;
            ms.Position = 0;
        }

        static public char GetCharacter(this MemoryStream ms)
        {
            return Convert.ToChar(ms.ReadByte());
        }

        static public bool HasRemaining(this MemoryStream ms)
        {
            return ms.Position < ms.Length;
        }

        static public MemoryStream Limit(this MemoryStream ms, int newLimit)
        {
            MemoryStream newMemoryStream = new MemoryStream(newLimit);
            newMemoryStream.Write(ms.ToArray(), 0, newLimit);

            return newMemoryStream;
        }
    }

    static public class UriExtensions
    {
        static public IPAddress GetIPAddress(this Uri uri)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(uri.DnsSafeHost);

            return addresses[0];
        }
    }

    static public class SystemExtensions
    {
        static public string GetString(this char[] ca)
        {
            StringBuilder sb = new StringBuilder();

            foreach (char c in ca)
            {
                sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
