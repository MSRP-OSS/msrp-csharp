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
using System.Net;

namespace MSRP.Java
{
    /// <summary>
    /// Class with different functions to complete IPAddress with added functionality
    /// </summary>
    public class InetAddress
    {
        /// <summary>
        /// The IPAddress wich represents this InetAddress
        /// </summary>
        internal IPAddress _addr;
        
        /// <summary>
        /// Constructor with an IPAddress as string
        /// </summary>
        /// <param name="addr"></param>
        public InetAddress(string addr)
            : this(IPAddress.Parse(addr))
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="addr"></param>
        public InetAddress(IPAddress addr)
        {
            _addr = addr;
        }

        /// <summary>
        /// If it is a loopbackaddress
        /// </summary>
        public bool IsAnyLocalAddress
        {
            get
            {
                return IPAddress.IsLoopback(_addr);
            }
        }

        /// <summary>
        /// Equals with a other InetAddress
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public bool Equals(InetAddress addr)
        {
            return addr.ToString().Equals(addr.ToString());
        }

        /// <summary>
        /// Equals with a string
        /// </summary>
        /// <param name="addr"></param>
        /// <returns></returns>
        public bool Equals(string addr)
        {
            return addr.ToString().Equals(addr.ToString());
        }

        /// <summary>
        /// ToString() function override
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return _addr.ToString();
        }

        /// <summary>
        /// Equals with a object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj.ToString());
        }

        /// <summary>
        /// The host address
        /// </summary>
        /// <returns></returns>
        public string HostAddress
        {
            get
            {
                return ToString();
            }
        }

        /// <summary>
        /// Getting the HashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// The HostName
        /// </summary>
        public string HostName
        {
            get
            {
                return Dns.GetHostName();
            }
        }

        /// <summary>
        /// Retrieving Local HostName
        /// </summary>
        static public InetAddress LocalHostName
        {
            get
            {
                return new InetAddress(Dns.GetHostName());
            }
        }

        /// <summary>
        /// Retrieving InetAddresses from a host
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        static public InetAddress[] GetAllByName(string host)
        {
            List<InetAddress> addresses = new List<InetAddress>();

            foreach (IPAddress address in Dns.GetHostAddresses(host))
            {
                addresses.Add(new InetAddress(address));
            }

            return addresses.ToArray();
        }

        /// <summary>
        /// Retrieving InetAddress by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static InetAddress GetByName(string name)
        {
            return new InetAddress(Dns.GetHostEntry(name).AddressList[0]);
        }
    }
}
