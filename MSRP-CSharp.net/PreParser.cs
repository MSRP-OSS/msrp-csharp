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
using System.IO;
using MSRP.Java;
using MSRP.Exceptions;
using MSRP.Utils;
using log4net;

namespace MSRP
{
    /// <summary>
    /// Class used to pre-parse the incoming data. The main purpose of this class
    /// is to correctly set the receivingBinaryData variable so that accurately
    /// it states if this connection is receiving data in binary format or not
    /// </summary>
    public class PreParser
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(PreParser));

        /// <summary>
        /// The Connection to which this PreParser belongs
        /// </summary>
        public Connection Connection { get; private set; }

        /// <summary>
        /// Is this instance currently receiving header data or content-stuff?
        /// </summary>
        private bool _inContentStuff = false;

        /// <summary>
        /// 
        /// </summary>
        private short _preState = 0;

        /// <summary>
        /// Save the possible start of the end-line.
        /// Maximum size: 2 bytes for CRLF after data; 7 for '-';
        ///				32 for transactid; 1 for continuation flag;
        /// 				2 for closing CRLF; == Total: 44 bytes.
        /// </summary>
        private MemoryStream _wrapBuffer = new MemoryStream(44);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connection"></param>
        public PreParser(Connection connection)
        {
            Connection = connection;
        }

        /// <summary>
        /// Method that implements a small state machine in order to identify if
        /// the incomingData belongs to the headers or to the body. This method
        /// is responsible for changing the value of the variable
        /// receivingBinaryData to the correct value and then calling the parser.
        ///
        /// Throws ConnectionParserException if there ocurred an exception while
        /// calling the parser method of this class
        /// 
        /// See #receivingBinaryData
        /// </summary>
        /// <param name="incomingData">incomingData the incoming byte array that contains the data
        ///                            received</param>
        /// <param name="length">the number of bytes of the given incomingData array to
        ///                      be considered for preprocessing.</param>
        public void PreParse(byte[] incomingData, int length)
        {
            //ByteBuffer data = ByteBuffer.wrap(incomingData, 0, length);
            MemoryStream data = new MemoryStream(incomingData, 0, length);

            // this variable keeps the index of the last time the data was sent
            // to be processed
            int indexProcessed = 0;

            if (_wrapBuffer.Position != 0)
            {
                // in case we have data to append, append it
                int bytesToPrepend = (int)_wrapBuffer.Position;
                byte[] prependedData = new byte[(bytesToPrepend + incomingData.Length)];
                _wrapBuffer.Flip();
                _wrapBuffer.Read(prependedData, 0, bytesToPrepend);
                _wrapBuffer = new MemoryStream();
                data.Read(prependedData, bytesToPrepend, length);

                // now we substitute the old data for the new one with the
                // appended bytes
                data = new MemoryStream(prependedData); //ByteBuffer.wrap(incAppendedData);

                // now we must set forward the position of the buffer so that it
                // doesn't read again the stored bytes
                data.Position = bytesToPrepend;
            }

            while (data.HasRemaining())
            {
                // we have two distinct points of start for the algorithm,
                // either we are in the binary state or in the text state
                if (!_inContentStuff)
                {
                    switch (_preState)
                    {
                        case 0:
                            if (data.GetCharacter() == '\r') { _preState++; }
                            break;
                        case 1:
                            if (data.GetCharacter() == '\n') { _preState++; }
                            else { Reset(data); }
                            break;
                        case 2:
                            if (data.GetCharacter() == '\r') { _preState++; }
                            else { Reset(data); }
                            break;
                        case 3:
                            if (data.GetCharacter() == '\n')
                            {
                                _preState = 0;
                                /*
                                if the state (binary or text) changed since the
                                beginning of the preprocessing of the data, then
                                we should call the parser with the data we have
                                so far
                                */
                                Connection.Parser(data.ToArray(), indexProcessed, (int)(data.Position - indexProcessed), _inContentStuff);

                                indexProcessed = (int)data.Position;
                                if (Connection._incomingTransaction == null) { throw new ParseException("no transaction found"); }

                                _inContentStuff = true;
                            }
                            else { Reset(data); }
                            break;
                    }
                } // if (!receivingBinaryData)
                else // hunt for end-line
                {
                    switch (_preState)
                    {
                        case 0:
                            if (data.GetCharacter() == '\r') { _preState++; }
                            break;
                        case 1:
                            if (data.GetCharacter() == '\n') { _preState++; }
                            else { Reset(data); }
                            break;
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 8:
                            if (data.GetCharacter() == '-') { _preState++; }
                            else { Reset(data); }
                            break;
                        default:
                            if (_preState >= 9)
                            {
                                if (Connection._incomingTransaction == null) { throw new ParseException("no transaction found"); }

                                int tidSize = Connection._incomingTransaction.TransactionId.Length;
                                if (tidSize == (_preState - 9))
                                {
                                    //End of Tx-id, look for valid continuation flag.
                                    char incChar = data.GetCharacter();

                                    if (incChar == '+' || incChar == '$' || incChar == '#') { _preState++; }
                                    else { Reset(data); }
                                }
                                else if ((_preState - 9) > tidSize)
                                {
                                    if ((_preState - 9) == tidSize + 1)
                                    {
                                        // we should expect the CR here
                                        if (data.GetCharacter() == '\r') { _preState++; }
                                        else { Reset(data); }
                                    }
                                    else if ((_preState - 9) == tidSize + 2)
                                    {
                                        // we should expect the LF here 
                                        if (data.GetCharacter() == '\n')
                                        {
                                            _preState++;
                                            // body received so process all of the
                                            // data we have so far excluding CRLF
                                            // and "end-line" that later must be
                                            // parsed as text.
                                            data.Position = data.Position - _preState;
                                            Connection.Parser(data.ToArray(), indexProcessed, (int)data.Position - indexProcessed, _inContentStuff);
                                            indexProcessed = (int)data.Position;
                                            _inContentStuff = false;

                                            /*Connection.parser(data.ToArray(), indexLastChange, preState, receivingBodyData);
                                            data.Position = data.Position + preState;
                                            indexLastChange = (int)data.Position;*/

                                            _preState = 0;
                                        }
                                        else { Reset(data); }
                                    }
                                }
                                else if ((tidSize > (_preState - 9)) && data.GetCharacter() == Connection._incomingTransaction.TransactionId[_preState - 9])
                                {
                                    _preState++;
                                }
                                else { Reset(data); }
                            }// end of default:

                            break;
                    }
                }// else from if(!receivingBinaryData)
            }// while (bufferIncData.hasRemaining())

            // We scanned everything, process remaining data.
            // Exclude any end-line state (when scanning for end-line after
            // content-stuff) to be wrapped to next scan.

            int endOfData = (int)data.Position;
            if (_inContentStuff && _preState != 0)
            {
                endOfData -= _preState;		/* here we save the state */
                try
                {
                    _wrapBuffer.Write(data.ToArray(), endOfData, _preState);
                }
                catch (OutOfMemoryException ex)
                {
                    _logger.Error(string.Format(
                        "Error wrapping {0} bytes (from[{1}] to[{2}])\nContent:[{3}]",
                        _preState.ToString(), endOfData.ToString(), data.Position.ToString(),
                        CodedString.Decode(data.ToArray(), Encoding.UTF8).Substring(endOfData, (int)data.Position)
                    ));
                    throw ex;
                }
            }
            
            Connection.Parser(data.ToArray(), indexProcessed, endOfData - indexProcessed, _inContentStuff);
        }

        /// <summary>
        /// Rewind 1 position in given buffer (if possible) and reset state.
        /// </summary>
        /// <param name="buffer">the buffer to rewind</param>
        private void Reset(MemoryStream buffer)
        {
            _preState = 0;
            int position = (int)buffer.Position;
            if (position != 0) { buffer.Position = position - 1; }
        }
    }
}
