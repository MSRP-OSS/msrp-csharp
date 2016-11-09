using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace msrp
{
    /// <summary>
    /// Interface for the wrapping and unwrapping of messages.
    /// </summary>
    public interface WrappedMessage
    {
        /// <summary>
        /// Parse (unwrap) the buffered message.
        /// </summary>
        /// <param name="buffer">contains the message.</param>
        void parse(byte[] buffer);

        /// <summary>
        /// Wrap a message in the wrapper-type.
        /// </summary>
        /// <param name="from">a from-header</param>
        /// <param name="to">a to-header</param>
        /// <param name="contentType">the content-type of the wrapped message</param>
        /// <param name="content">the content to wrap</param>
        /// <returns>the wrapped message as a byte-array.</returns>
        byte[] wrap(string from, string to, string contentType, byte[] content);

        /// <summary>
        /// Return the content-type of the wrapped message.
        /// </summary>
        /// <returns>the content-type.</returns>
        string getContentType();

        /// <summary>
        /// Return content of the specified header.
        /// </summary>
        /// <param name="name">name of the header</param>
        /// <returns>the value</returns>
        string getHeader(string name);

        /// <summary>
        /// Return content of the wrapped header
        /// </summary>
        /// <param name="name">name of the header</param>
        /// <returns>the value</returns>
        string getContentHeader(string name);

        /// <summary>
        /// Return content of the wrapped message.
        /// </summary>
        /// <returns>the content.</returns>
        string getMessageContent();
    }
}
