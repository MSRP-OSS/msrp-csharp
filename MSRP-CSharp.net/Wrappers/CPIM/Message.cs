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
using MSRP.Java;
using MSRP.Utils;

namespace MSRP.Wrappers.CPIM
{
    /// <summary>
    /// CPIM Message
    /// </summary>
    public class Message : IWrappedMessage 
    {
	    public const string WRAP_TYPE = Header.CPIM_TYPE;

	    /// <summary>
	    /// Message content
	    /// </summary>
	    private byte[] _msgContent = null;

	    /// <summary>
	    /// MIME headers
	    /// </summary>
	    private List<Header> _headers = new List<Header>();

	    /// <summary>
	    /// MIME content headers
	    /// </summary>
	    private List<Header> _contentHeaders = new List<Header>();

	    /// <summary>
	    /// Default constructor
	    /// </summary>
	    public Message() { }

        /// <summary>
        /// Returns content type
        /// </summary>
        /// <returns>The contenttype</returns>
	    public string ContentType 
        {
            get
            {
                return _contentHeaders.Find(ch => ch.Name == Headers.CONTENT_TYPE).Value;
            }
        }

        /// <summary>
        /// Returns MIME header
        /// </summary>
        /// <param name="name">Header name</param>
        /// <returns>Header value</returns>
        public string GetHeader(string name) 
        {
            return _headers.Find(h => h.Name == name).Value;
	    }

        /// <summary>
        /// Returns MIME content header
        /// </summary>
        /// <param name="name">Header name</param>
        /// <returns>Header value</returns>
        public string GetContentHeader(string name) 
        {
            return _contentHeaders.Find(ch => ch.Name == name).Value;
	    }

        /// <summary>
        /// Returns message content
        /// </summary>
        /// <returns>Content</returns>
        public byte[] MessageContent
        {
            get
            {
                return _msgContent;
            }
	    }

	    private const string CRLF = "\r\n";
	    private string EMPTY_LINE = CRLF+CRLF;
    
        /// <summary>
        /// Parse message/CPIM document
        /// </summary>
        /// <param name="buffer">input data</param>
	    public void Parse(byte[] buffer) 
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

		    string data =  CodedString.Decode(buffer, Encoding.UTF8);
		    int start = 0;
		    int end = data.IndexOf(EMPTY_LINE, start);
		    string[] lines;
		    // Read message headers
		    lines = data.Substring(start, end - start).Split(new string[] { CRLF }, StringSplitOptions.RemoveEmptyEntries);
		    foreach (string token in lines) 
            {
			    Header hd = Header.ParseHeader(token);
			    _headers.Add(hd);
		    }
		    // Read the MIME-encapsulated content headers
		    start = end + EMPTY_LINE.Length;
		    end = data.IndexOf(EMPTY_LINE, start);
		    lines = data.Substring(start, end - start).Split(new string[] { CRLF }, StringSplitOptions.RemoveEmptyEntries);
		    foreach (string token in lines) 
            {
			    Header hd = Header.ParseHeader(token);
			    _contentHeaders.Add(hd);
		    }
		    // Create the CPIM message
		    start = end + EMPTY_LINE.Length;
		    _msgContent = Encoding.UTF8.GetBytes(data.Substring(start));
	    }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="contentType"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public byte[] Wrap(string from, string to, string contentType, byte[] content)
        {
            _headers.Add(new Header(Headers.FROM, from));
            _headers.Add(new Header(Headers.TO, to));
            //		headers.add(new Header(Header.DATETIME, <SomeFormOfTimestamp>));
            _contentHeaders.Add(new Header(Headers.CONTENT_TYPE, contentType));
            _msgContent = content;
            return Encoding.UTF8.GetBytes(ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
		    StringBuilder sb = new StringBuilder(_msgContent.Length + (_headers.Count + _contentHeaders.Count) * 20);
		    foreach(Header h in _headers) 
            {
			    sb.Append(h).Append(CRLF);
	    	}
		    sb.Append(CRLF);
		    foreach(Header h in _contentHeaders) 
            {
			    sb.Append(h).Append(CRLF);		
            }
		    sb.Append(CRLF).Append(CodedString.Decode(_msgContent, Encoding.UTF8));
		    return sb.ToString();
	    }
    }
}
