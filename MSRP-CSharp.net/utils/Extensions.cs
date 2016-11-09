//
// Copyright © Massxess BV This file is part of MSRP-CSharp.net Stack.
// 
// MSRP-CSharp.net Stack is free software: you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as published by the
// Free Software Foundation, version 3 or later.
// 
// MSRP-CSharp.net Stack is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
// FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License
// for more details.
// 
// You should have received a copy of the GNU Lesser General Public License
// along with MSRP-CSharp.net Stack. If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace MSRP.Utils
{
    /// <summary>
    /// Extensions for a Socket
    /// </summary>
    static public class SocketExtensions
    {
        /// <summary>
        /// Getting HostAddress of a Socket
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        static public string GetHostAddress(this Socket socket)
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();
        }

        /// <summary>
        /// Getting LocalPort of a Socket
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        static public int GetLocalPort(this Socket socket)
        {
            return ((IPEndPoint)socket.RemoteEndPoint).Port;
        }
    }

    /// <summary>
    /// Extensions for a Stream
    /// </summary>
    static public class StreamExtensions
    {
        /// <summary>
        /// Flipping the Stream to start again, so position is 0
        /// </summary>
        /// <param name="stream"></param>
        static public void Flip(this Stream stream)
        {
            //ms.Length = ms.Position;
            stream.Position = 0;
        }

        /// <summary>
        /// Reading a character from the Stream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static public char GetCharacter(this Stream stream)
        {
            return Convert.ToChar(stream.ReadByte());
        }

        /// <summary>
        /// Determines if we are at the end of the stream
        /// 
        /// Should be a property but to make compatibly with JAVA and not to extend MemoryStream use an extension-method
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static public bool HasRemaining(this Stream stream)
        {
            //return ms.Position < ms.Length;
            return stream.Remaining() > 0;
        }

        /// <summary>
        /// Determines if we are at the end of the stream
        /// 
        /// Should be a property but to make compatibly with JAVA and not to extend MemoryStream use an extension-method
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        static public long Remaining(this Stream stream)
        {
            return stream.Length - stream.Position;
        }


        /// <summary>
        /// Limiting a stream, basically it creates a new Stream with the given length
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="newLimit"></param>
        /// <returns></returns>
        static public Stream Limit(this Stream stream, int newLimit)
        {
            Stream newStream = (Stream)Activator.CreateInstance(stream.GetType(), newLimit);
            
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                newStream.Write(ms.ToArray(), 0, newLimit);
            }
           
            return newStream;
        }
    }

    /// <summary>
    /// Extensions for a URI
    /// </summary>
    static public class UriExtensions
    {
        /// <summary>
        /// Getting the IPAddress from a URI
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        static public IPAddress GetIPAddress(this Uri uri)
        {
            IPAddress[] addresses = Dns.GetHostAddresses(uri.DnsSafeHost);

            return addresses[0];
        }
    }
    
    /// <summary>
    /// Miscellaneous extensions 
    /// </summary>
    static public class SystemExtensions
    {
        /// <summary>
        /// Getting the String from an char array
        /// </summary>
        /// <param name="ca"></param>
        /// <returns></returns>
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
