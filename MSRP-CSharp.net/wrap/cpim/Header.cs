using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace msrp.wrap.cpim
{
    /// <summary>
    /// CPIM Header
    /// </summary>
    public class Header 
    {
	    public const string CPIM_TYPE = "message/cpim";
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

	    /// <summary>
	    /// Header name
	    /// </summary>
	    private string name;

	    /// <summary>
	    /// Header value
	    /// </summary>
	    private string value;

	    /// <summary>
	    /// Constructor
	    /// </summary>
	    /// <param name="name">Header name</param>
	    /// <param name="value">Header value</param>
	    public Header(string name, string value) 
        {
		    this.name = name;
		    this.value = value;
	    }

	    public string getName() { return name; }

	    public string getValue() { return value; }

	    /// <summary>
	    /// Parse CPIM header
	    /// </summary>
	    /// <param name="data">Input data</param>
	    /// <returns>Header</returns>
	    public static Header parseHeader(string data) 
        {
		    int index = data.IndexOf(":");
		    string key = data.Substring(0, index);
		    string value = data.Substring(index+1);
		    return new Header(key.Trim().ToLower(), value.Trim());
	    }

        /// <summary>
        /// See Object#hashCode()
        /// </summary>
        /// <returns></returns>
	    override public int GetHashCode() 
        {
		    return this.name.GetHashCode();
	    }

	    /// <summary>
        /// see java.lang.Object#equals(java.lang.Object)
	    /// </summary>
	    /// <param name="obj"></param>
	    /// <returns></returns>
	    override public bool Equals(object obj) 
        {
		    return (obj != null && obj.GetType().Equals(this.GetType()) && ((Header)obj).getName().Equals(this.name, StringComparison.CurrentCultureIgnoreCase));
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}: {1}", name, value);
        }
    }
}
