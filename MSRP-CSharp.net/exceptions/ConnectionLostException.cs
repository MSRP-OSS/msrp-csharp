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

namespace MSRP.Exceptions
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionLostException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ConnectionLostException()
            : this(string.Empty, null)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public ConnectionLostException(string message)
            : this(message, null)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="innerException"></param>
        public ConnectionLostException(Exception innerException)
            : this(string.Empty, innerException)
        {

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ConnectionLostException(string message, Exception innerException)
            : base(message, innerException)
        {

        }
    }
}
