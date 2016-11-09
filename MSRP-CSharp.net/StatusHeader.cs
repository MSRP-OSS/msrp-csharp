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
using MSRP.Exceptions;

namespace MSRP
{
    /// <summary>
    /// This class implements the Status header as defined in RFC 4975
    /// </summary>
    public class StatusHeader
    {
        /// <summary>
        /// 
        /// </summary>
        public int NS { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public ResponseCodes StatusCode { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Comment { get; private set; }

        /// <summary>
        /// Generates a new StatusHeader class
        ///
        /// Throws InvalidHeaderException if it was found any error with the parsing
        /// of this status header field
        /// </summary>
        /// <param name="ns">String representing the namespace defined in RFC4975</param>
        /// <param name="statusCode">String representing the status-code as defined in
        ///                          RFC4975</param>
        /// <param name="optionalComment">String representing the comment as defined in
        ///                               RFC4975</param>
        public StatusHeader(string ns, string statusCode, string comment)   
        {
            // sanity checks, exceptions should never be thrown here due to the fact
            // that the strings are already filtered by the regexp pattern.
            NS = int.Parse(ns);
            StatusCode = (ResponseCodes)(int.Parse(statusCode));
            Comment = comment;

            // Validate the namespace
            if (NS != 000) { throw new InvalidHeaderException("Error in Status header field, the given namespace is not supported"); }
        }
    }
}
