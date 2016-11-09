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

namespace MSRP
{
    /// <summary>
    /// Create the correct IncomingMessage object, depending on the content-type.
    /// </summary>
    public class IncomingMessageFactory
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="messageId"></param>
        /// <param name="contentType"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        static public IncomingMessage CreateMessage(Session session, string messageId, string contentType, long size)
        {
            switch (contentType)
            {
                case null :
                    return new IncomingAliveMessage(session, messageId);
                case Message.IMCOMPOSE_TYPE:
                    return new IncomingStatusMessage(session, messageId, contentType, size);
                default:
                    return new IncomingMessage(session, messageId, contentType, size, null);
            }
        }
    }
}
