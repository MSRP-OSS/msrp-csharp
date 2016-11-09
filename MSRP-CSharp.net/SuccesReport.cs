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
    /// Class used as a specific implementation of a Success report that comes from
    /// the general transaction class.
    /// </summary>
    public class SuccessReport : ReportTransaction
    {
        /// <summary>
        /// Create an outgoing success report based on the RFC rules.
        /// 
        /// Throws InternalErrorException if the objects used have invalid states or
        /// any other kind of internal error.
        /// Throws IllegalUseException if something like trying to send success
        /// reports on messages that don't want them.
        /// </summary>
        /// <param name="message">the message associated to the report</param>
        /// <param name="session">the session associated with the report</param>
        /// <param name="transaction">the originating transaction that triggered this report.</param>
        /// <param name="comment">optional comment as defined in RFC 4975.</param>
        public SuccessReport(Message message, Session session, Transaction transaction, string comment)
            : base(message, session, transaction)
        {
            if (transaction.WantSuccessReport != message.WantSuccessReport)
            {
                throw new InternalError("Report request of the originating transaction differs from that of the message");
            }

            if (!message.WantSuccessReport)
            {
                throw new IllegalUseException("Constructing a success report for a message that didn't want one?");
            }

            MakeReportHeader(transaction, "000", ResponseCodes.RC200, comment);
        }
    }
}
