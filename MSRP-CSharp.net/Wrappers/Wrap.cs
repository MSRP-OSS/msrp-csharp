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
using MSRP.Wrappers.CPIM;

namespace MSRP.Wrappers
{
    /// <summary>
    /// This singleton registers wrapper implementations.
    /// </summary>
    public class Wrap
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, string> _wrappers = new Dictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        protected Wrap()
        {
            RegisterWrapper(MSRP.Wrappers.CPIM.Message.WRAP_TYPE, typeof(MSRP.Wrappers.CPIM.Message).FullName);
        }

        /// <summary>
        /// 
        /// </summary>
        private static Wrap _instance = new Wrap();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Wrap GetInstance()
        {
            return _instance;
        }

        /// </summary>
        /// <param name="contenttype">for which content-type</param>
        /// <param name="wrapImplementator">the implementing wrapper-class</param>
        public void RegisterWrapper(string contenttype, string wrapImplementator)
        {
            _wrappers.Add(contenttype, wrapImplementator);
        }

        /// <summary>
        /// Is the content-type a wrapper type?
        /// </summary>
        /// <param name="contenttype">the type</param>
        /// <returns>true = wrapper type.</returns>
        public bool IsWrapperType(string contenttype)
        {
            return _wrappers.ContainsKey(contenttype);
        }

        /// <summary>
        /// Get wrapper implementation object for this type.
        /// </summary>
        /// <param name="contenttype">the type</param>
        /// <returns>object implementing wrap/unwrap operations for this type.</returns>
        public IWrappedMessage GetWrapper(string contenttype) 
        {
		    try 
            {
                if (IsWrapperType(contenttype))
                {
                    Type cls = Type.GetType(_wrappers[contenttype]);
                    return (IWrappedMessage)Activator.CreateInstance(cls);
                }
		    } 
            catch 
            {
			    
		    }

            return null;
	    }
    }
}
