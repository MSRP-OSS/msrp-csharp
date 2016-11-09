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

namespace MSRP.Utils
{
    /// <summary>
    /// Utility methods and fields to use when working with network addresses.
    /// </summary>
    public static class NetworkUtils
    {
        /// <summary>
        /// A string containing the "any" local address.
        /// </summary>
        public static string IN_ADDR_ANY = "0.0.0.0";

        /// <summary>
        /// Determines whether the address is the result of windows auto configuration.
        /// (i.e. One that is in the 169.254.0.0 network)
        /// </summary>
        /// <param name="addres">the address to inspect</param>
        /// <returns>true if the address is autoconfigured by windows, false otherwise.</returns>
        public static bool isWindowsAutoConfiguredIPv4Address(IPAddress addres)
        {
            return (addres.GetAddressBytes()[0] & 0xFF) == 169 && (addres.GetAddressBytes()[1] & 0xFF) == 254;
        }

        /// <summary>
        /// Determines whether the address is an IPv4 link local address. IPv4 link
        /// local addresses are those in the following networks:
        ///
        /// 10.0.0.0    to 10.255.255.255
        /// 172.16.0.0  to 172.31.255.255
        /// 192.168.0.0 to 192.168.255.255
        /// </summary>
        /// <param name="address">the address to inspect</param>
        /// <returns>true if address is a link local ipv4 address and false if not.</returns>
        public static bool isLinkLocalIPv4Address(IPAddress address)
        {
            //If IPv4Address
            if (!isIPv6Address(address.ToString()))
            {
                byte[] add = address.GetAddressBytes();

                return (((add[0] & 0xFF) == 10) || ((add[0] & 0xFF) == 172 && (add[1] & 0xFF) >= 16 && add[1] <= 31) || ((add[0] & 0xFF) == 192 && (add[1] & 0xFF) == 168)); 
            }
        
            return false;
        }

        /// <summary>
        /// Verifies whether <tt>address</tt> could be an IPv6 address string.
        /// </summary>
        /// <param name="address">the String that we'd like to determine as an IPv6 address.</param>
        /// <returns>true if the address containaed by <tt>address</tt> is an ipv6 address and false otherwise.</returns>
        public static bool isIPv6Address(String address)
        {
            return (address != null && address.IndexOf(':') != -1);
        }

        /// <summary>
        /// Het ophalen van de Complete Authority
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri getCompleteAuthority(Uri uri)
        {
            try
            {
                return new Uri(string.Format("{0}://{1}/", uri.Scheme, uri.Authority));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Extension function for URI for getCompleteAuthority
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static Uri GetCompleteAuthority(this Uri uri)
        {
            return getCompleteAuthority(uri);
        }
    }
}
