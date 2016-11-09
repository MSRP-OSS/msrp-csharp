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

namespace MSRP
{
    /// <summary>
    /// TransactionType
    /// </summary>
    public enum TransactionType
    {
        /// <summary>
        /// Transaction associated with the SEND method
        /// </summary>
        SEND = 0,

        /// <summary>
        /// Transaction associated with the REPORT method
        /// </summary>
        REPORT = 1,

        /// <summary>
        /// Transaction associated with the NICKNAME method
        /// </summary>
        NICKNAME = 2,

        /// <summary>
        /// Represents the unsupported methods
        /// </summary>
        UNSUPPORTED = 3,

        /// <summary>
        /// Transaction that is a response to a SEND or NICKNAME
        /// </summary>
        RESPONSE = 4
    }
}
