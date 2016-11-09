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
using MSRP.Events;

namespace MSRP
{
    /// <summary>
    /// Interface used for callbacks by the MSRP stack.
    ///
    /// It provides callbacks for events at a message level (associated with some
    /// action regarding a message) in the session that is associated with the class
    /// that implements this interface.
    /// </summary>
    public interface IMSRPSessionListener
    {
        /// <summary>
        /// Accept or reject the incoming message. SHOULD ALWAYS
        /// assign a {@code DataContainer} to the given message, otherwise it is
        /// discarded.
        /// </summary>
        /// <param name="session">the session on which we have an incoming message</param>
        /// <param name="message">the message on which we should decide upon accepting or
        ///                       rejecting it</param>
        /// <returns>true if the message is to be accepted, false otherwise Note: if
        ///          the message is rejected one should call message.reject(code) to
        ///          specify the reason why, default reason is 413</returns>
        bool AcceptHook(Session session, IncomingMessage message);

        /// <summary>
        /// Signal a received message
        /// </summary>
        /// <param name="session">the session on which the message was received</param>
        /// <param name="message">the message received</param>
        void ReceiveMessage(Session session, Message message);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        void ReceivedNickname(Session session, Transaction request);

        /// <summary>
        /// Signal a received REPORT
        /// </summary>
        /// <param name="session">the Session on which the REPORT was received</param>
        /// <param name="report">the Transaction associated with the REPORT</param>
        void ReceivedReport(Session session, Transaction report);

        /// <summary>
        ///  A response to a NICKNAME request has been received. 
        /// </summary>
        /// <param name="session">the session on which the request was done</param>
        /// <param name="result">the response to this request.</param>
        void ReceivedNickNameResult(Session session, TransactionResponse result);

        /// <summary>
        /// Signal an aborted message
        /// </summary>
        /// <param name="abortEvent">the Message aborted event used</param>
        void AbortedMessageEvent(MessageAbortedEvent abortEvent);
        
        /// <summary>
        /// Signal updates on the sending status of a message.
        /// The granularity of such updates can be set by
        /// implementing {@link ReportMechanism#shouldTriggerSentHook(Message, Session, long)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="numberBytesSent">the total number of sent bytes belonging to the
        ///                               message</param>
        void UpdateSendStatus(Session session, Message message, long numberBytesSent);

        /// <summary>
        /// Signal that underlying connection to this session has ceased to be.
        /// </summary>
        /// <param name="session">the session it pertains to</param>
        /// <param name="cause">why was it lost?</param>
        void ConnectionLost(Session session, Exception cause);
    }
}
