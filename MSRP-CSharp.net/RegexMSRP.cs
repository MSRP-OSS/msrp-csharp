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
using System.Text.RegularExpressions;

namespace MSRP
{
    /// <summary>
    /// This class will make the parsers comply with the MSRP specification (more
    /// specifically with the formal syntax and will solve Issue #16). All of the
    /// regex patterns used by the parsers must come from this class.
    /// 
    /// TODO the majority of the formal syntax of the MSRP rfc should be represented
    /// here, which isn't done yet.
    /// </summary>
    public class RegexMSRP
    {
        // Starting with the basic MSRP building blocks
        private const string token_re = "[\\x21|\\x23-\\x27|\\x2A-2B|\\x2D-\\x2E|\\x30-\\x39|\\x41-\\x5A|\\x5E-\\x7E]{1,}";
        private const string msrp_scheme_re = "msrps?";
        private const string transport_re = "[A-Za-z0-9]";
        private const string transportParm_re = "[A-Za-z0-9]";

        static public Regex token { get { return new Regex(token_re); } }
        static public Regex msrp_scheme { get { return new Regex(msrp_scheme_re); } }
        static public Regex transport { get { return new Regex(transport_re); } }
        static public Regex transportParm { get { return new Regex(transportParm_re); } }

	    public static bool HasTransport(string input) 
        {
		    return transportParm.IsMatch(input);
	    }

	    public static bool HasTransport(Uri uri) 
        {
            if (uri.AbsolutePath.Length > 0) { return HasTransport(uri.AbsolutePath); }
		    
            return HasTransport(new Uri(uri.Authority));
	    }

	    public static bool IsMsrpUri(Uri uri) 
        {
		    return msrp_scheme.IsMatch(uri.Scheme) && uri.Authority != null && uri.Authority != string.Empty && HasTransport(uri);
	}
    }
}
