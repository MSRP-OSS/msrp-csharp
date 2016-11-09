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

namespace MSRP.Events
{
    /// <summary>
    /// The <tt>MessageAbortedEvent</tt> is the event indicating that a MSRP message
    /// has been aborted. More details can be accessed through its methods
    ///
    /// This class captures all the cases where an MSRP can be seen as being aborted.
    /// Depending on the reasons, different actions should be performed
    /// </summary>
    public class MessageAbortedEvent : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        private const long serialVersionUID = 1L;
 
        /// <summary>
        /// The message got a continuation flag of # that means it was aborted
        /// </summary>
        public const int CONTINUATIONFLAG = 0;

        /// <summary>
        /// The message that got aborted
        /// </summary>
        public Message Message { get; private set; }

        /// <summary>
        /// The reason for this abort event
        /// </summary>
        public ResponseCodes Reason { get; private set; }

        /// <summary>
        /// The reason for the Message abortion. This is equivalent to the comment of
        /// the Status header in the formal syntax it is informational only as the
        /// main reason is determined by the response code
        /// </summary>
        public string ExtraReasonInfo { get; private set; }

        /// <summary>
        /// The Session where the message was aborted
        /// </summary>
        public Session Session { get; private set; }

        /// <summary>
        /// The Transaction object (with or without response) associated with the
        /// abort event
        /// </summary>
        private Transaction Transaction;
    
        /// <summary>
        /// Constructor used to create the abort event
        /// </summary>
        /// <param name="message">message the message that got aborted</param>
        /// <param name="session">reason the reason, one of: CONTINUATIONFLAG; RESPONSE4XX
        /// <see cref="CONTINUATIONFLAG"/>
        /// <see cref="RESPONSE400"/>
        /// <see cref="RESPONSE403"/>
        /// <see cref="RESPONSE413"/>
        /// <see cref="RESPONSE415"/>
        /// <see cref="RESPONSE481"/>
        /// </param>
        /// <param name="reason"></param>
        /// <param name="extraReasonInfo">extraReasonInfo this can be the string that can be on the body of a REPORT or null if it doesn't exist</param>
        /// <param name="transaction"></param>
        public MessageAbortedEvent(Message message, Session session, ResponseCodes reason, string extraReasonInfo, Transaction transaction)
            :base()
        {
            Message = message;
            Session = session;
            Reason = reason;
            ExtraReasonInfo = extraReasonInfo;
            Transaction = transaction;
        }
    }
}
