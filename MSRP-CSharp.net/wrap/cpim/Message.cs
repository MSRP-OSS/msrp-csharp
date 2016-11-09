using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using msrp.java;

namespace msrp.wrap.cpim
{
    /// <summary>
    /// CPIM Message
    /// </summary>
    public class Message : WrappedMessage 
    {
	    public const string WRAP_TYPE = Header.CPIM_TYPE;

	    /// <summary>
	    /// Message content
	    /// </summary>
	    private byte[] msgContent = null;

	    /// <summary>
	    /// MIME headers
	    /// </summary>
	    private List<Header> headers = new List<Header>();

	    /// <summary>
	    /// MIME content headers
	    /// </summary>
	    private List<Header> contentHeaders = new List<Header>();

	    /// <summary>
	    /// Default constructor
	    /// </summary>
	    public Message() { }
        
        /// <summary>
        /// 
        /// </summary>
        private static Header ContentType = new Header(Header.CONTENT_TYPE, null);

        /// <summary>
        /// Returns content type
        /// </summary>
        /// <returns>The contenttype</returns>
	    public string getContentType() 
        {
            return contentHeaders.Find(ch => ch.getName() == Header.CONTENT_TYPE).getValue();
        }

        /// <summary>
        /// Returns MIME header
        /// </summary>
        /// <param name="name">Header name</param>
        /// <returns>Header value</returns>
        public string getHeader(string name) 
        {
            return headers.Find(h => h.getName() == name).getValue();
	    }

        /// <summary>
        /// Returns MIME content header
        /// </summary>
        /// <param name="name">Header name</param>
        /// <returns>Header value</returns>
        public string getContentHeader(string name) 
        {
            return contentHeaders.Find(ch => ch.getName() == name).getValue();
	    }

        /// <summary>
        /// Returns message content
        /// </summary>
        /// <returns>Content</returns>
        public string getMessageContent() 
        {
            return JavaString.Create(msgContent, Encoding.UTF8);
	    }

	    private const string CRLF = "\r\n";
	    private string EMPTY_LINE = CRLF+CRLF;
    
        /// <summary>
        /// Parse message/CPIM document
        /// </summary>
        /// <param name="buffer">input data</param>
	    public void parse(byte[] buffer) 
        {
		    /* CPIM sample:
	        From: MR SANDERS <im:piglet@100akerwood.com>
	        To: Depressed Donkey <im:eeyore@100akerwood.com>
	        DateTime: 2000-12-13T13:40:00-08:00
	        Subject: the weather will be fine today

	        Content-type: aintext/pl
	        Content-ID: <1234567890@foo.com>

	        Here is the text of my message.
	        */

		    string data =  java.JavaString.Create(buffer, Encoding.UTF8);
		    int start = 0;
		    int end = data.IndexOf(EMPTY_LINE, start);
		    string[] lines;
		    // Read message headers
		    lines = data.Substring(start, end).Split(new string[] { CRLF }, StringSplitOptions.RemoveEmptyEntries);
		    foreach (string token in lines) 
            {
			    Header hd = Header.parseHeader(token);
			    headers.Add(hd);
		    }
		    // Read the MIME-encapsulated content headers
		    start = end + EMPTY_LINE.Length;
		    end = data.IndexOf(EMPTY_LINE, start);
		    lines = data.Substring(start, end).Split(new string[] { CRLF }, StringSplitOptions.RemoveEmptyEntries);
		    foreach (string token in lines) 
            {
			    Header hd = Header.parseHeader(token);
			    contentHeaders.Add(hd);
		    }
		    // Create the CPIM message
		    start = end + EMPTY_LINE.Length;
		    msgContent = Encoding.UTF8.GetBytes(data.Substring(start));
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="contentType"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public byte[] wrap(string from, string to, string contentType, byte[] content)
        {
            headers.Add(new Header(Header.FROM, from));
            headers.Add(new Header(Header.TO, to));
            //		headers.add(new Header(Header.DATETIME, <SomeFormOfTimestamp>));
            contentHeaders.Add(new Header(Header.CONTENT_TYPE, contentType));
            msgContent = content;
            return Encoding.UTF8.GetBytes(ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
		    StringBuilder sb = new StringBuilder(msgContent.Length + (headers.Count + contentHeaders.Count) * 20);
		    foreach(Header h in headers) 
            {
			    sb.Append(h).Append(CRLF);
	    	}
		    sb.Append(CRLF);
		    foreach(Header h in contentHeaders) 
            {
			    sb.Append(h).Append(CRLF);		
            }
		    sb.Append(CRLF).Append(JavaString.Create(msgContent, Encoding.UTF8));
		    return sb.ToString();
	    }
    }
}
