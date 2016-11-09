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
    /// IM indication of message composition.
    /// Loosely based on RFC 3994: indication of message composition for IM
    /// </summary>
    public interface IStatusMessage
    {
        /// <summary>
        /// current state of the message composer (active, idle...)
        /// </summary>
	    ImState State { get; }
        
        /// <summary>
        /// the type of content the user is composing (text, video..)
        /// </summary>
        string ComposeContentType { get; }

        /// <summary>
        /// timestamp when last active was seen.
        /// </summary>
        DateTime LastActive { get; }

        /// <summary>
        /// currently used refresh interval in seconds.
        /// </summary>
        int Refresh { get; }

        /// <summary>
        /// user that sent the indication (conferencing support)
        /// </summary>
        string From { get; }

        /// <summary>
        /// user that the indication is meant for (conferencing support)
        /// </summary>
        string To { get; }
    }
}
