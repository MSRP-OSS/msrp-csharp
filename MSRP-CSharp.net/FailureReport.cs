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
using MSRP.Exceptions;
using MSRP.Java;

namespace MSRP
{
    /// <summary>
    /// Class used as a specific implementation of a Failure report that comes from
    /// the general transaction
    /// </summary>
    public class FailureReport : ReportTransaction
    {
        /// <summary>
        /// Create an outgoing failure report based on the RFC rules.
        /// 
        /// Throws InternalErrorException if the objects used have invalid states or
        /// any other kind of internal error
        /// Throws IllegalUseException if something like trying to send success
        /// reports on messages that don't want them is done.
        /// </summary>
        /// <param name="message">the message associated to the report</param>
        /// <param name="session">the session associated with the report</param>
        /// <param name="transaction">the originating transaction triggering this report.</param>
        /// <param name="ns">the three digit namespace associated with the status</param>
        /// <param name="responseCode">[400; 403; 408; 413; 415; 423; 481; 501; 506]</param>
        /// <param name="comment">the optional comment as defined in RFC 4975.</param>
        public FailureReport(Message message, Session session, Transaction transaction, string ns, ResponseCodes responseCode, string comment)
            : base(message, session, transaction)
        {
            if (transaction.FailureReport != message.FailureReport)
            {
                throw new InternalError("Report request of originating transaction differs from that of the message");
            }

            if (message.FailureReport == Message.NO)
            {
                throw new IllegalUseException("Constructing a failure report for a message that explicitly didn't want them?");
            }

            if ((int)responseCode < 400 || (int)responseCode > 599)
            {
                throw new IllegalUseException("Wrong response code! Must be a valid code as defined in RFC 4975");
            }

            MakeReportHeader(transaction, ns, responseCode, comment);
        }
    }
}
