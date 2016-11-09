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
using MSRP.Utils;

namespace MSRP
{
    /// <summary>
    /// Represents the transaction for REPORT requests
    /// </summary>
    public class ReportTransaction : Transaction
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if still has data, excluding end of line, false otherwise</returns>
        new public bool HasData
        {
            get
            {
                if (_readIndex[HEADER] >= _headerBytes.Length) { return false; }
                if (_interrupted) { return false; }
                return true;
            }
        }

        /// <summary>
        /// Construct a report to send.
        /// </summary>
        /// <param name="message">the associated message to report on.</param>
        /// <param name="session">the originating session.</param>
        /// <param name="transaction">the originating transaction.</param>
	    public ReportTransaction(Message message, Session session, Transaction transaction)
            :base(transaction.TransactionManager.GenerateNewTID(), TransactionType.REPORT, transaction.TransactionManager, Direction.OUT)
	    {
            //"Must check to see if session is valid" as specified in RFC4975 valid
            //being for now if it exists or not FIXME(?!)
            if (session == null || !session.IsActive) { throw new InternalError(string.Format("not a valid session: {0}", session)); }

            if (session != message.Session)
            {
                throw new InternalError("Generating report: this session and associated message session differ!");
            }

            if (transaction == null || transaction.TotalMessageBytes == Message.UNINITIALIZED)
            {
                throw new InternalError("Invalid argument or in generating a report, the total number of bytes of this message was unintialized");
            }

            Message = message;
            _continuation_flag = ContinuationFlags.END;
	    }

	    /// <summary>
	    /// Utility routine to make an outgoing report.
	    /// </summary>
	    /// <param name="transaction">the originating transaction to report on</param>
	    /// <param name="ns">status namespace</param>
	    /// <param name="statusCode">status code to return</param>
	    /// <param name="comment">optional comment for the status field.</param>
	    protected void MakeReportHeader(Transaction transaction, string ns, ResponseCodes statusCode, string comment)
	    {
		    StringBuilder header = new StringBuilder(256);

            header.Append("MSRP ").Append(TransactionId).Append(" REPORT\r\nTo-Path:");

            Uri[] toPathURIs = transaction.FromPath;
            for (int i = 0; i < toPathURIs.Length; i++)
            {
                header.Append(" ").Append(toPathURIs[i]);
            }
            header.Append("\r\nFrom-Path: ").Append(Message.Session.Uri).Append("\r\nMessage-ID: ").Append(Message.MessageId);

            long totalBytes = transaction.TotalMessageBytes;
            header.Append("\r\nByte-Range: 1-").Append(Message.Counter.NrConsecutiveBytes).Append("/");
            if (totalBytes == Message.UNKNOWN) { header.Append("*"); }
            else { header.Append(totalBytes); }
            
            // TODO validate the comment with a regex in RegexMSRPFactory that
            // validates the comment is utf8text, if not, log it and skip comment
            header.Append("\r\nStatus: ").Append(ns).Append(" ").Append((int)statusCode);
            if (comment != null) { header.Append(" ").Append(comment); }
            header.Append("\r\n");

            _headerBytes = header.ToString().Encode(Encoding.UTF8);
	    }

       /// <summary>
       /// 
       /// </summary>
       /// <param name="outData"></param>
       /// <param name="offset"></param>
       /// <returns></returns>
        override public int GetData(byte[] outData, int offset)
        {
            if (_interrupted && _readIndex[ENDLINE] <= (7 + TransactionId.Length + 2))
            {
                // old line: FIXME to remove these lines if no problems are
                // encountered running the tests return getEndLineByte();
                throw new ImplementationException("Data already retrieved, should be retrieving endline");
            }
            if (_interrupted)
            {
                throw new ImplementationException("Message interrupted, should be retrieving endline");
            }
           
            int bytesCopied = 0;
            bool stopCopying = false;
            int spaceRemaining = outData.Length - offset;
            while ((bytesCopied < spaceRemaining) && !stopCopying)
            {
                if (offset > (outData.Length - 1)) { throw new IndexOutOfRangeException(); }

                if (_readIndex[HEADER] < _headerBytes.Length)
                {
                    // if we are processing the header
                    int bytesToCopy = 0;

                    if ((outData.Length - offset) < (_headerBytes.Length - _readIndex[HEADER]))
                    {
                        // if the remaining bytes on the outdata is smaller than the
                        // remaining bytes on the header then fill the outdata with
                        // that length
                        bytesToCopy = (outData.Length - offset);
                    }
                    else
                    {
                        bytesToCopy = (int)(_headerBytes.Length - _readIndex[HEADER]);
                    }

                    //System.arraycopy(headerBytes, (int)offsetRead[HEADERINDEX], outData, offset, bytesToCopy);
                    Array.Copy(_headerBytes, (int)_readIndex[HEADER], outData, offset, bytesToCopy);

                    _readIndex[HEADER] += bytesToCopy;
                    bytesCopied += bytesToCopy;
                    offset += bytesCopied;
                }
               
                if (!_interrupted && (_readIndex[HEADER] >= _headerBytes.Length)) { stopCopying = true; }
            }

            return bytesCopied;
        }
    }
}
