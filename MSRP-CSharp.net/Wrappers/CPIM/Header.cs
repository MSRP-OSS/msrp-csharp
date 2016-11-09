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

namespace MSRP.Wrappers.CPIM
{
    /// <summary>
    /// CPIM Header
    /// </summary>
    public class Header 
    {
        public const string CPIM_TYPE = "message/cpim";

	    /// <summary>
	    /// Header name
	    /// </summary>
        public string Name { get; private set; }

	    /// <summary>
	    /// Header value
	    /// </summary>
        public string Value { get; private set; }

	    /// <summary>
	    /// Constructor
	    /// </summary>
	    /// <param name="name">Header name</param>
	    /// <param name="value">Header value</param>
	    public Header(string name, string value) 
        {
		    Name = name;
		    Value = value;
	    }

	    /// <summary>
	    /// Parse CPIM header
	    /// </summary>
	    /// <param name="data">Input data</param>
	    /// <returns>Header</returns>
	    public static Header ParseHeader(string data) 
        {
		    int index = data.IndexOf(":");
		    string key = data.Substring(0, index);
		    string value = data.Substring(index + 1);
		    return new Header(key.Trim(), value.Trim());
	    }

        /// <summary>
        /// See Object#hashCode()
        /// </summary>
        /// <returns></returns>
	    override public int GetHashCode() 
        {
		    return Name.GetHashCode();
	    }

	    /// <summary>
        /// see java.lang.Object#equals(java.lang.Object)
	    /// </summary>
	    /// <param name="obj"></param>
	    /// <returns></returns>
	    override public bool Equals(object obj) 
        {
		    return (obj != null && obj.GetType().Equals(GetType()) && ((Header)obj).Name.Equals(Name, StringComparison.CurrentCultureIgnoreCase));
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", Name, Value);
        }
    }
}
