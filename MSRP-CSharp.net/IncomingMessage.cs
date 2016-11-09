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
using log4net;
using System.Diagnostics;
using MSRP.Exceptions;
using MSRP.Events;
using MSRP.Wrappers;

namespace MSRP
{
    /// <summary>
    /// This class is used to generate incoming messages
    /// </summary>
    public class IncomingMessage : Message
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(IncomingMessage));

        /// <summary>
        /// Constructor called internally when receiving an incoming message used by
        /// the IncomingMessage class
        /// </summary>
        /// <param name="session"></param>
        /// <param name="messageId"></param>
        /// <param name="contentType"></param>
        /// <param name="size">the size of the incoming message (should be bigger than -2
        ///                    and -1 for unknown (*) total size</param>
        /// <param name="reportMechanism"></param>
        public IncomingMessage(Session session, string messageId, string contentType, long size, ReportMechanism reportMechanism)
            : base(Direction.IN)
        {
            Session = session;
            ContentType = contentType;
            MessageId = messageId;
            SetReportMechanism(reportMechanism);
            Size = size;
        }

        /// <summary>
        /// Constructor that derives it's values from the object to clone.
        /// </summary>
        /// <param name="toCopy"></param>
        protected IncomingMessage(IncomingMessage toCopy):base(toCopy)
        {
            
            Result = toCopy.Result;
        }

        /// <summary>
        /// Method that uses the associated counter of this message to assert if the
        /// message is complete or not
        /// </summary>
        /// <returns>true if the message is complete, false otherwise</returns>
        override public bool IsComplete
        {
            get
            {
                bool toReturn = Counter.IsComplete;

                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug(string.Format("IsComplete({0}: received {1} of {2})? {3}", this, Counter.Count, Size, toReturn));
                }

                return toReturn;
            }
        }

        /// <summary>
        /// Returns the number of received bytes so far reported by the associated
        /// Counter class
        /// </summary>
        /// <returns>The number of received bytes so far</returns>
        public long GetReceivedBytes
        {
            get
            {
                long count = Counter.Count;
                _logger.Debug(string.Format("Received {0} bytes.", count));
                return count;
            }
        }

        /// <summary>
        /// Contains the response code of the accept hook call
        /// </summary>
        private ResponseCodes _result = ResponseCodes.RC413;

        public ResponseCodes Result { get { return _result; } set { _result = value; } }

        /// <summary>
        /// 
        /// </summary>
        override public Message Validate()
        {
            if (Size > 0)
            {
                if (ContentType == null) { throw new InvalidHeaderException("no content type."); }
                if (Wrap.GetInstance().IsWrapperType(ContentType))
                {
                    WrappedMessage = Wrap.GetInstance().GetWrapper(ContentType);
                    WrappedMessage.Parse(DataContainer.Get(0, Size));
                }

                if (WrappedMessage != null && WrappedMessage.ContentType == Message.IMCOMPOSE_TYPE)
                {
                    Message toValidate = new IncomingStatusMessage(this);
                    return toValidate.Validate();
                }
            }

            return this;
        }

        /// <summary>
        /// Convenience method used to reject an incoming message. Its equivalent to
        /// call abort with response 413
        /// </summary>
        public void Reject()
        {
            try
            {
                Abort(ResponseCodes.RC413, null);
            }
            catch (IllegalUseException e)
            {
                _logger.Error("Implementation error! abort called internally with invalid arguments", e);
            }
        }

        /// <summary>
        /// If the last transaction hasn't yet a given response given, a response is
        /// generated, otherwise a REPORT request with the namespace 000 (equivalent
        /// to a response) is generated
        /// </summary>
        /// <param name="reason">one of <tt>MessageAbortEvent</tt> response codes except CONTINUATIONFLAG</param>
        /// <param name="reasonExtraInfo">corresponds to the comment as defined on RFC 4975 formal syntax. If null, it isn't sent any comment.</param>
        override public void Abort(ResponseCodes reason, string reasonExtraInfo)
        {
            // Sanity checks
            if (LastSendTransaction == null) { throw new InternalError("abort was called on an incoming message without an assigned Transaction!"); }
            if (!ResponseCode.IsAbortCode(reason)) { throw new IllegalUseException("The reason must be one of the response codes on MessageAbortedEvent excluding the continuation flag reason"); }

            // Check to see if we already responded to the transaction being
            // received/last transaction known
            if (!LastSendTransaction.HasResponse) { LastSendTransaction.TransactionManager.GenerateResponse(LastSendTransaction, reason, reasonExtraInfo); }
            else // let's generate the REPORT
            {
                FailureReport failureReport = new FailureReport(this, Session, LastSendTransaction, "000", reason, reasonExtraInfo);

                // send it
                Session.TransactionManager.AddPriorityTransaction(failureReport);
            }

            // mark this message as aborted
            _aborted = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("InMsg({0})", MessageId);
        }
    }
}
