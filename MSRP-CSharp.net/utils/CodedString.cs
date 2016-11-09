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

namespace MSRP.Utils
{
    /// <summary>
    /// Static class for easy encoding/decoding strings
    /// </summary>
    static public class CodedString
    {
        /// <summary>
        /// Decoding a string
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        static public string Decode(byte[] data, string encoding)
        {
            return Decode(data, 0, data.Length, Encoding.GetEncoding(encoding));
        }

        /// <summary>
        /// Decoding a string
        /// </summary>
        /// <param name="data"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        static public string Decode(byte[] data, Encoding encoding)
        {
            return Decode(data, 0, data.Length, encoding);
        }

        /// <summary>
        /// Decoding a string
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        static public string Decode(byte[] data, int offset, int length, string encoding)
        {
            return Decode(data, offset, length, Encoding.GetEncoding(encoding));
        }

        /// <summary>
        /// Decoding a string
        /// </summary>
        /// <param name="data">ByteArray containing the string</param>
        /// <param name="offset">Start</param>
        /// <param name="length">Length to decode</param>
        /// <param name="encoding">The used encoding</param>
        /// <returns></returns>
        static public string Decode(byte[] data, int offset, int length, Encoding encoding)
        {
            return encoding.GetString(data, offset, length);
        }

        static public byte[] Encode(this string String, string encoding)
        {
            return String.Encode(Encoding.GetEncoding(encoding));
        }

        /// <summary>
        /// Encode a string
        /// </summary>
        /// <param name="String"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        static public byte[] Encode(this string String, Encoding encoding)
        {
            return encoding.GetBytes(String);
        }
    }
}
