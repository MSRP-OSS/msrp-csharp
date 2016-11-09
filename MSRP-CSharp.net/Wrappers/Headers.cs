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

namespace MSRP.Wrappers
{
    /// <summary>
    /// Standard headers for a wrapped message
    /// </summary>
    public sealed class Headers
    {
        public const string CONTENT_TYPE = "Content-type";
        public const string FROM = "From";
        public const string TO = "To";
        public const string CC = "cc";
        public const string DATETIME = "DateTime";
        public const string SUBJECT = "Subject";
        public const string NS = "NS";
        public const string CONTENT_LENGTH = "Content-length";
        public const string REQUIRE = "Require";
        public const string CONTENT_DISPOSITION = "Content-Disposition";
    }
}
