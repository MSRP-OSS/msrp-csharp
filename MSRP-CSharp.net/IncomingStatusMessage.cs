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

using MSRP.Exceptions;
using MSRP.Wrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MSRP
{
    /// <summary>
    /// An incoming MSRP message containing an IM message composition indication in XML.
    /// </summary>
    public class IncomingStatusMessage : IncomingMessage, IStatusMessage
    {
        private ImState _state = ImState.idle;
        private string _composeContentType = string.Empty;
        private DateTime _lastActive = DateTime.MinValue;
        private int _refresh = 0;
        private string _from = null;
        private string _to = null;

        public ImState State { get { return _state; } }
        public string ComposeContentType { get { return _composeContentType; } }
        public DateTime LastActive { get { return _lastActive; } }
        public int Refresh { get { return _refresh; } }
        public string From { get { return _from; } }
        public string To { get { return _to; } }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="messageId"></param>
        /// <param name="contentType"></param>
        /// <param name="size"></param>
        public IncomingStatusMessage(Session session, string messageId, string contentType, long size)
            : base(session, messageId, contentType, size, null)
        { }

        public IncomingStatusMessage(IncomingMessage toCopy)
            : base (toCopy)
        {
            _from = toCopy.WrappedMessage.GetHeader(Headers.FROM);
            _to = toCopy.WrappedMessage.GetHeader(Headers.TO);
        }

        public override Message Validate()
        {
            XmlDocument ComposingXml = null;

            try
            {
                byte[] content;
                if (IsWrapped)
                {
                    content = WrappedMessage.MessageContent;
                }
                else
                {
                    content = DataContainer.Get(0, Size);
                }

                ComposingXml = new XmlDocument();
                ComposingXml.LoadXml(RawContent);
            }
            catch (Exception e)
            {
                throw new ParseException("Invalid isComposing document", e);
            }

            XmlNodeList list = ComposingXml.GetElementsByTagName("state");

            if (list.Count == 0) { throw new Exception("Mandatory 'state' element missing in isComposing document"); }

            _state = (ImState)Enum.Parse(typeof(ImState), list[0].InnerText);
            list = ComposingXml.GetElementsByTagName("contenttype");
            if (list.Count > 0) { _composeContentType = list[0].InnerText; }
            list = ComposingXml.GetElementsByTagName("lastactive");
            if (list.Count > 0) { _lastActive = ParseDT(list[0].InnerText); }
            list = ComposingXml.GetElementsByTagName("refresh");
            if (list.Count > 0) { _refresh = int.Parse(list[0].InnerText); }

            return this;
        }

        private DateTime ParseDT(string timestamp) 
        {
            return DateTime.ParseExact(timestamp, "yyyy-MM-ddTHH:mm:ss", null);
	    }
    }
}
