using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using msrp.wrap.cpim;

namespace msrp.wrap
{
    /// <summary>
    /// This singleton registers wrapper implementations.
    /// </summary>
    public class Wrap
    {
        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, string> wrappers = new Dictionary<string, string>();

        /// <summary>
        /// 
        /// </summary>
        protected Wrap()
        {
            registerWrapper(msrp.wrap.cpim.Message.WRAP_TYPE, typeof(msrp.wrap.cpim.Message).Name);
        }

        /// <summary>
        /// 
        /// </summary>
        private static Wrap INSTANCE = new Wrap();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Wrap getInstance()
        {
            return INSTANCE;
        }

        /// </summary>
        /// <param name="contenttype">for which content-type</param>
        /// <param name="wrapImplementator">the implementing wrapper-class</param>
        public void registerWrapper(string contenttype, string wrapImplementator)
        {
            wrappers.Add(contenttype, wrapImplementator);
        }

        /// <summary>
        /// Is the content-type a wrapper type?
        /// </summary>
        /// <param name="contenttype">the type</param>
        /// <returns>true = wrapper type.</returns>
        public bool isWrapperType(string contenttype)
        {
            return wrappers.ContainsKey(contenttype);
        }

        /// <summary>
        /// Get wrapper implementation object for this type.
        /// </summary>
        /// <param name="contenttype">the type</param>
        /// <returns>object implementing wrap/unwrap operations for this type.</returns>
        public WrappedMessage getWrapper(string contenttype) 
        {
		    try 
            {
                if (isWrapperType(contenttype))
                {
                    Type cls = Type.GetType(wrappers[contenttype]);
                    return (WrappedMessage)Activator.CreateInstance(cls);
                }
		    } 
            catch 
            {
			    
		    }

            return null;
	    }
    }
}
