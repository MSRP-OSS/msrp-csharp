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
using System.IO;
using MSRP.Java;
using MSRP.Utils;

namespace MSRP
{
    /// <summary>
    /// This class represents a response to a Transaction, which is considered a
    /// transaction as well TODO use the parser to validate the response ?!
    /// </summary>
    public class TransactionResponse : Transaction
    {
        /// <summary>
        /// 
        /// </summary>
        //protected MemoryStream content;
        protected MemoryStream Content { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ResponseCodes TRResponseCode { get; set; }

        /// <summary>
        /// Is the result ok?
        /// </summary>
        public bool IsOK { get { return !ResponseCode.IsError(TRResponseCode); } }

        /// <summary>
        /// 
        /// </summary>
        public string Comment { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public TransactionType Response2Type { get; protected set; }

        /// <summary>
        /// Seen that we use the content field to put the end line we will always
        /// return false on this method call
        /// </summary>
        /// <returns></returns>
        override public bool HasEndLine
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public bool IsIncomingResponse
        {
            get
            {
                return Direction == Direction.IN;
            }
        }

        /// <summary>
        /// an int representing the number of bytes remaining for this
        /// response
        /// </summary>
        /// <returns></returns>
        public int NumberBytesRemaining
        {
            get
            {
                //return content.remaining();

                return (int)Content.Length - (int)Content.Position;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public bool HasData
        {
            get
            {
                return Content.Position < Content.Length;
            }
            //return content.hasRemaining();
        }

        /// <summary>
        /// Creates the transaction response
        ///
        /// Throws IllegalUseException if at least one of the arguments is
        /// incompatible
        /// </summary>
        /// <param name="transaction">the original transaction that gave birth to this
        ///                           response</param>
        /// <param name="responseCode">the code, must be supported by the RFCs 4975 or 4976</param>
        /// <param name="comment">the comment as defined in RFC 4975 formal syntax,
        ///                               as the comment is optional, it can also be null if no comment
        ///                               is desired</param>
        /// <param name="direction">the direction of the transaction</param>
        public TransactionResponse(Transaction transaction, ResponseCodes responseCode, string comment, Direction direction)
        {
            // original transaction must be a SEND transaction
            if (transaction.TransactionType != TransactionType.SEND && transaction.TransactionType != TransactionType.NICKNAME)
            {
                throw new IllegalUseException(string.Format("Creating a Response with an original transaction that isn't a SEND or NICKNAME: {0}", transaction.ToString()));
            }

            TransactionType = TransactionType.RESPONSE;
            Direction = direction;
            TRResponseCode = responseCode;
            Comment = comment;

            // copy the values from the original transaction to this one
            Message = transaction.Message;
            TransactionId = transaction.TransactionId;
            TransactionManager = transaction.TransactionManager;
            transaction.Response = this;

            if (direction == Direction.IN) { CreateIncomingResponse(transaction, responseCode, comment); }
            else { CreateOutgoingResponse(transaction, responseCode, comment); }
        }

        /// <summary>
        /// Constructor to create the outgoing transaction
        /// </summary>
        /// <param name="transaction">the transaction related with this</param>
        /// <param name="responseCode">one of the response codes defined on RFC 4975</param>
        /// <param name="comment"></param>
        /// <param name="direction"></param>
        private void CreateOutgoingResponse(Transaction transaction, ResponseCodes responseCode, string comment)
        {
            StringBuilder response = new StringBuilder(256);
            response.Append("MSRP ").Append(transaction.TransactionId).Append(" ").Append((int)responseCode);
            if (comment != null && comment != string.Empty) { response.Append(" ").Append(comment); }

            response.Append("\r\nTo-Path: ").Append(transaction.FromPath[transaction.FromPath.Length - 1]);
            response.Append("\r\nFrom-Path: ").Append(transaction.ToPath[transaction.ToPath.Length - 1]);
            response.Append("\r\n-------").Append(transaction.TransactionId).Append("$\r\n");

            FromPath = transaction.ToPath;
            ToPath = transaction.FromPath;
            byte[] contentBytes = response.ToString().Encode(Encoding.UTF8);
            Content = new MemoryStream(contentBytes); //ByteBuffer.wrap(contentBytes);
            //content.rewind();
        }

        /// <summary>
        /// Constructor to create the incoming transaction
        /// </summary>
        /// <param name="transaction">the transaction related with this</param>
        /// <param name="responseCode">one of the response codes defined on RFC 4975</param>
        /// <param name="comment"></param>
        /// <param name="direction"></param>
        private void CreateIncomingResponse(Transaction transaction, ResponseCodes responseCode, string comment)
        {
            Response2Type = transaction.TransactionType;
        }

        /// <summary>
        /// Method that gets bulks of data (int maximum)
        ///
        /// Throws IndexOutOfBoundsException if the offset is bigger than the array
        /// length
        /// </summary>
        /// <param name="toFill">byte array to be filled</param>
        /// <param name="offset">the offset index to start filling the outData</param>
        /// <returns>the number of bytes filled of the array</returns>
        override public int GetData(byte[] toFill, int offset)
        {
            int remainingBytes = NumberBytesRemaining; //content.remaining();
            int lengthToTransfer = 0;
            if ((toFill.Length - offset) > remainingBytes) { lengthToTransfer = remainingBytes; }
            else { lengthToTransfer = (toFill.Length - offset); }

            Content.Read(toFill, offset, lengthToTransfer);

            return lengthToTransfer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return string.Format("Transaction response of Tx[{0}] response code[{1}]", TransactionId, TRResponseCode);
        }
    }
}
