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
    /// Interface for the wrapping and unwrapping of messages.
    /// </summary>
    public interface IWrappedMessage
    {
        /// <summary>
        /// Parse (unwrap) the buffered message.
        /// </summary>
        /// <param name="buffer">contains the message.</param>
        void Parse(byte[] buffer);

        /// <summary>
        /// Wrap a message in the wrapper-type.
        /// </summary>
        /// <param name="from">a from-header</param>
        /// <param name="to">a to-header</param>
        /// <param name="contentType">the content-type of the wrapped message</param>
        /// <param name="content">the content to wrap</param>
        /// <returns>the wrapped message as a byte-array.</returns>
        byte[] Wrap(string from, string to, string contentType, byte[] content);

        /// <summary>
        /// Return the content-type of the wrapped message.
        /// </summary>
        /// <returns>the content-type.</returns>
        string ContentType { get; }

        /// <summary>
        /// Return content of the specified header.
        /// </summary>
        /// <param name="name">name of the header</param>
        /// <returns>the value</returns>
        string GetHeader(string name);

        /// <summary>
        /// Return content of the wrapped header
        /// </summary>
        /// <param name="name">name of the header</param>
        /// <returns>the value</returns>
        string GetContentHeader(string name);

        /// <summary>
        /// Return content of the wrapped message.
        /// </summary>
        /// <returns>the content.</returns>
        byte[] MessageContent { get; }
    }
}
