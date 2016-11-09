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
using log4net;
using MSRP.Exceptions;
using System.IO;
using System.Text.RegularExpressions;
using MSRP.Java;
using MSRP.Utils;

namespace MSRP
{
    /// <summary>
    /// Class that represents a MSRP Transaction (either request or response,
    /// incoming or outgoing). It is responsible for parsing all of the data related
    /// with the transaction (either incoming or outgoing). When enough data is
    /// received to take action upon, it notifies the TransactionManager and the
    /// global MSRPStack classes (The communication between these classes is done via
    /// the Observer design pattern)
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// the method associated with this transaction we call it transactionType
        /// </summary>
        public TransactionType TransactionType { get; protected set; }

        /// <summary>
        /// Field that defines the type of transaction it is, regarding the
        /// direction, incoming or outgoing
        /// </summary>
        public Direction Direction { get; protected set; }

        /// <summary>
        /// this variable is used to denote if this transaction has "content-stuff"
        /// or not Used to know if one should add the extra CRLF after the data or
        /// not
        /// </summary>
        public bool HasContentStuff { get; set; }

        /// <summary>
        /// the From-Path parsed to the Transaction containing the associated
        /// From-Path URIs from left to right in a growing index order
        /// </summary>
        public Uri[] FromPath { get; protected set; }

        /// <summary>
        /// the To-Path parsed to the Transaction containing the associated To-Path
        /// URIs from left to right in a growing index order
        /// </summary>
        public Uri[] ToPath { get; protected set; }

        /// <summary>
        /// the message associated with this transaction
        /// </summary>
        public Message Message { get; protected set; }

        /// <summary>
        /// Array containing the index of various pieces of the transaction that have
        /// been read already: header, content-stuff end (=CRLF) and end-line.
        /// </summary>
        protected long[] _readIndex = new long[3];

        /// <summary>
        /// Constants used to index the transaction pieces
        /// </summary>
        protected const int HEADER = 0;
        protected const int ENDLINE = 1;
        protected const int DATA = 2;

        /// <summary>
        /// the identifier of this transaction
        /// </summary>
        public string TransactionId { get; protected set; }

        /// <summary>
        /// @uml.property name="_transactionManager"
        /// @uml.associationEnd multiplicity="(1 1)"
        ///         inverse="_transactions:msrp.TransactionManager"
        /// </summary>
        public MSRP.TransactionManager TransactionManager { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        protected byte[] _headerBytes = new byte[MAXHEADERBYTES];

        /// <summary>
        /// Variable that tells if this Transaction is interrupted (paused or
        /// aborted)
        /// </summary>
        protected bool _interrupted = false;

        /// <summary>
        /// variable that has the byte associated with the end of transaction char
        /// one of: $+#
        /// </summary>
        protected ContinuationFlags _continuation_flag;

        /// <summary>
        /// The constant used to access the byteRange first field that has the number
        /// of the first byte of the chunk bound to this transaction
        ///
        /// See #byteRange
        /// </summary>
        private const int CHUNKSTARTBYTEINDEX = 0;

        /// <summary>
        /// The constant used to access the byteRange second field that has the
        /// number of the last byte of the chunk bound to this transaction
        /// 
        /// See #byteRange
        /// </summary>
        private const int CHUNKENDBYTEINDX = 1;

        /// <summary>
        /// The constant used to access the byteRange second field that has the
        /// number of the last byte of the chunk bound to this transaction
        /// 
        /// See #byteRange
        /// </summary>
        private const int CHUNKENDBYTEINDEX = 1;

        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(Transaction));

        /// <summary>
        /// 
        /// </summary>
        private const int UNKNOWN = -2;

        /// <summary>
        /// 
        /// </summary>
        private const int UNINTIALIZED = -1;

        /// <summary>
        /// 
        /// </summary>
        private const int NOTFOUND = -1;

        /// <summary>
        /// Maximum number of bytes allowed for the header data strings (so that we
        /// don't have a DoS by memory exaustion)
        /// </summary>
        private const int MAXHEADERBYTES = 3024;

        /// <summary>
        /// 
        /// </summary>
        private const int ALLBYTES = 0;

        /// <summary>
        /// if this is a complete transaction Note: A transaction could be created
        /// and being in the filling process and is only considered complete when
        /// signaled by the connection class
        /// </summary>
        private bool _completeTransaction = false;

        /// <summary>
        /// On the process of construction of the transaction by parsing of strings
        /// this variable is used to denote if we have completed the parsing of the
        /// headers
        /// </summary>
        private bool _headerComplete = false;

        /// <summary>
        /// 
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// two vector array that stores the information about the start and end,
        /// respectively index 0 and 1, associated with the Byte-Range parsed to the
        /// transaction the value -2 is reserved as unknown
        /// </summary>
        private long[] _byteRange = new long[2];

        public long[] ByteRange { get { return _byteRange; } set { _byteRange = value; } }


        /// <summary>
        /// value associated with the Byte-Range parsed to the transaction refering
        /// to the number of bytes of the body.
        /// 
        /// The values -2 and -1 are reserved as unknown and uninitialized
        /// respectively
        /// </summary>
        private long _totalMessageBytes = -1;

        /// <summary>
        /// The last Byte-Range field that should represent the total number of bytes
        /// of the Message reported on this transaction.
        /// </summary>
        public long TotalMessageBytes { get { return _totalMessageBytes; } set { _totalMessageBytes = value; } }

        /// <summary>
        /// @uml.property name="_connection"
        /// @uml.associationEnd multiplicity="(1 1)"
        ///             inverse="_transactions:msrp.Connection"
        /// </summary>
        public MSRP.Connection Connection { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TransactionResponse Response { get; set; }

        /// <summary>
        /// Field representing the "Failure-report" field which has a default value
        /// of "yes" if it's not present
        /// </summary>
        private string _failureReport = "yes";

        /// <summary>
        /// 
        /// </summary>
        public string FailureReport { get { return _failureReport; } }

        /// <summary>
        /// Field that represents the value of the success report by default and if
        /// is omitted is considered false
        /// </summary>
        private bool _successReport = false;

        /// <summary>
        /// true if the success report value of the header for this
        /// transaction is 'yes', 'false' otherwise (default).
        /// </summary>
        /// <returns></returns>
        public bool WantSuccessReport
        {
            get
            {
                return _successReport;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string MessageId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// 
        /// </summary>
        //private StringBuffer headerBuffer = new StringBuffer();
        private MemoryStream _headerBuffer = new MemoryStream();

        /// <summary>
        /// if this is a valid transaction or if it has any problem with it assume
        /// for starters it's always valid
        /// </summary>
        private bool _validTransaction = true;


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsValid { get { return _validTransaction; } }

        /// <summary>
        /// 
        /// </summary>
        public StatusHeader StatusHeader { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        private MSRPStack _stack = MSRPStack.GetInstance();

        /// <summary>
        /// 
        /// </summary>
        public Session _session = null;

        /// <summary>
        /// When the method is called, the transaction should always have a session
        /// associated with it
        ///
        /// Throws ImplementationException if this transaction has no session
        /// associated with it
        /// </summary>
        public Session Session
        {
            get
            {
                if (_session == null) { throw new ImplementationException("No associated session!"); }

                return _session;
            }
            private set
            {
                _session = value;
            }
        }

        /// <summary>
        /// The byte array that contains the body bytes of the transaction in the
        /// case that the body doesn't belong to a message
        /// </summary>
        private byte[] _bodyBytes;

        /// <summary>
        /// The convenience Byte Buffer used to manipulate the body bytes
        /// </summary>
        //private ByteBuffer bodyByteBuffer;
        private MemoryStream _bodyByteBuffer;

        /// <summary>
        /// the real chunk size of this message and not the one reported in the
        /// Byte-Range header field
        /// </summary>
        private int _realChunkSize = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the actual number of body bytes that this transaction currently
        ///          holds</returns>
        protected int NrBodyBytes
        {
            get { return _realChunkSize; }
        }

        /// <summary>
        /// Method used by the TransactionManager to assert if this is or not an
        /// incoming response
        /// </summary>
        /// <returns></returns>
        virtual public bool IsIncomingResponse
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// variable that controls if this is an interruptible transaction or not
        /// </summary>
        private bool _interruptible = false;

        /// <summary>
        /// Method that asserts if a transaction is interruptible or not.
        ///
        /// according to the RFC:
        /// "Any chunk that is larger than 2048 octets MUST be interruptible"
        /// 
        /// Also REPORT requests and responses to transactions shouldn't be
        /// interruptible 
        /// </summary>
        /// <returns>true if the transaction is interruptible false otherwise.</returns>
        public bool IsInterruptible
        {
            get
            {
                return _interruptible;
            }
        }

        /// <summary>
        /// identifies if this transaction has an outgoing response
        /// </summary>
        /// <returns>true if it has an _outgoing_ response</returns>
        public bool HasResponse
        {
            get
            {
                return Response != null;
            }
        }

        /// <summary>
        /// Asserts if this transaction is a request or a response
        /// </summary>
        /// <returns>true if it is a request or false if it is</returns>
        public bool IsRequest
        {
            get
            {
                return (TransactionType == TransactionType.REPORT || TransactionType == TransactionType.SEND || TransactionType == TransactionType.NICKNAME);
            }
        }

        /// <summary>
        /// note: Returns false for responses because they have
        /// their own end-line inside their content.
        /// </summary>
        /// <returns>true = all data has been read, except the end-line.</returns>
        virtual public bool HasEndLine
        {
            get
            {
                if (HasData) { return false; }

                if (_readIndex[ENDLINE] > (7 + TransactionId.Length + 2)) { return false; }

                return true;
            }
        }

        /// <summary>
        /// TODO: take dynamic creation of an end of transaction into account
        /// </summary>
        /// <returns> </returns>
        virtual public bool HasData
        {
            get
            {
                if (_interrupted) { return false; }
                if (_readIndex[HEADER] >= _headerBytes.Length && !Message.HasData) { return false; }

                return true;
            }
        }

        /// <summary>
        /// Has some data from this transaction already been sent? 
        /// </summary>
        public bool HasSentData
        {
            get
            {
                return _readIndex[HEADER] > 0;
            }
        }

    

        /// <summary>
        /// Generic constructor for (possibly incoming) transactions
        /// </summary>
        /// <param name="tid"></param>
        /// <param name="transType"></param>
        /// <param name="manager"></param>
        /// <param name="direction"></param>
        public Transaction(string tid, TransactionType transType, TransactionManager manager, Direction direction)
        {
            _logger.Info(string.Format("Transaction created Tx-{0}[{1}], handled by {2}", transType, tid, manager));

            Direction = direction;
            TransactionManager = manager;
            _readIndex[HEADER] = _readIndex[ENDLINE] = _readIndex[DATA] = 0;
            _byteRange[0] = _byteRange[1] = _totalMessageBytes = UNINTIALIZED;
            TransactionType = transType;
            TransactionId = tid;
            InitializeDataStructures();
        }

        /// <summary>
        /// Constructor used to send new simple short transactions used for single
        /// transaction messages
        /// </summary>
        /// <param name="send"></param>
        /// <param name="messageToSend"></param>
        /// <param name="manager"></param>
        public Transaction(OutgoingMessage messageToSend, TransactionManager manager)
        {
            TransactionManager = manager;
            TransactionId = manager.GenerateNewTID();
            Message = messageToSend;

            if (messageToSend.Size > 0 && messageToSend.IsComplete)
            {
                throw new IllegalUseException("The constructor of this transaction was called with a completely sent message");
            }

            if (Message.Nickname != null && Message.Nickname != string.Empty) { MakeNickHeader(); }
            else { MakeSendHeader(); }

            // by default have the continuation flag to be the end of message
            _continuation_flag = ContinuationFlags.END;
            InitializeDataStructures();

            _logger.Info(string.Format("Created {0} associated Message-ID: {1}", ToString(), messageToSend));
        }

        /// <summary>
        /// 
        /// </summary>
        private void MakeNickHeader()
        {
            TransactionType = TransactionType.NICKNAME;

            Session session = Message.Session;
            List<Uri> uris = session.ToPath;
            Uri toPathUri = uris[0];
            Uri fromPathUri = session.Uri;

            StringBuilder header = new StringBuilder(256);
            header.Append("MSRP ").Append(TransactionId).Append(" NICKNAME\r\nTo-Path: ")
                  .Append(toPathUri.ToString()).Append("\r\nFrom-Path: ")
                  .Append(fromPathUri.ToString()).Append("\r\nUse-Nickname: \"")
                  .Append(Message.Nickname).Append("\"\r\n");

            _headerBytes = header.ToString().Encode(Encoding.UTF8);
        }

        /// <summary>
        /// 
        /// </summary>
        private void MakeSendHeader()
        {
            TransactionType = TransactionType.SEND;

            Session session = Message.Session;
            List<Uri> uris = session.ToPath;
            Uri toPathUri = uris[0];
            Uri fromPathUri = session.Uri;
            string messageID = Message.MessageId;

            StringBuilder header = new StringBuilder(256);
            header.Append("MSRP ").Append(TransactionId).Append(" SEND\r\nTo-Path: ")
                  .Append(toPathUri.ToString()).Append("\r\nFrom-Path: ")
                  .Append(fromPathUri.ToString()).Append("\r\nMessage-ID: ")
                  .Append(messageID).Append("\r\n");

            if (Message.WantSuccessReport) { header.Append("Success-Report: yes\r\n"); }
            if (Message.FailureReport.ToLower() != "yes")
            {
                /* note: if omitted, failure report is assumed to be yes */
                header.Append("Failure-Report: ").Append(Message.FailureReport).Append("\r\n");
            }

            string ct = Message.ContentType;
            if (ct != null)
            {
                /*
                 * first value of the Byte-Range header field is the
                 * currentReadOffset + 1, or the current number of already sent
                 * bytes + 1 because the first field is the number of the first byte
                 * being sent:
                 */
                long firstByteChunk = ((OutgoingMessage)Message).SentBytes + 1;
                /*
                 * Currently all transactions are interruptible, solving Issue #25
                 * if ((message.getSize() - ((OutgoingMessage)message).getSize()) >
                 * 								Stack.MAX_UNINTERRUPTIBLE_CHUNK) {
                 */
                _interruptible = true;

                header.Append("Byte-Range: ").Append(firstByteChunk).Append("-*/").Append(Message.SizeString).Append("\r\n");

                if (ct == string.Empty) { ct = "text/plain"; }
                header.Append("Content-Type: ").Append(ct).Append("\r\n\r\n");
            }

            _headerBytes = header.ToString().Encode(Encoding.UTF8);
        }

        /// <summary>
        /// Explicit super constructor
        /// </summary>
        protected Transaction()
        {
            _logger.Info("transaction created by the empty constructor");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            StringBuilder toReturn = new StringBuilder(40);
            toReturn.Append("Tx-").Append(TransactionType).Append("[").Append(TransactionId).Append("]");
            if (HasResponse)
            {
                toReturn.Append(", response code[").Append(Response.TRResponseCode).Append("]");
                if (Response.Comment != null && Response.Comment.Length > 0) { toReturn.Append("-").Append(Response.Comment); }
            }
            return toReturn.ToString();
        }

        /// <summary>
        /// Used to parse the raw data between the connection and the transaction it
        /// identifies the header and fills the body if existing Also it should find
        /// eventual errors on the data received and generate an 400 response throw
        /// an (exception or other methods ?!) This method is also responsible for
        /// accounting for the data received calling the appropriate functions in the
        /// ReportMechanism
        /// 
        /// Throws InvalidHeaderException if an error was found with the parsing of
        /// the header
        /// Throws ImplementationException this is here for debug purposes mainly
        /// See ReportMechanism#countReceivedBodyBlock(Message, Transaction, long,
        /// int)
        /// </summary>
        /// <param name="incData">the data to parse to the transaction</param>
        /// <param name="offset">the starting point to be parsed on the given toParse array</param>
        /// <param name="length">the number of bytes to be considered starting at the offset
        ///                      position</param>
        /// <param name="inContentStuff">tells the parse method if the data in the
        ///                                   incData is binary or usascii text</param>
        public void Parse(byte[] incData, int offset, int length, bool inContentStuff)
        {
            if (!inContentStuff)
            {
                // Trims and assembles the received data via the toParse string
                // so that we get a buffer with the whole headers before we try to
                // analyze it
                string toParse = CodedString.Decode(incData, offset, length, Encoding.UTF8);

                // if the transaction is marked as complete or invalid, calls to
                // this method will do nothing
                if (!_validTransaction) { return; }

                if (_completeTransaction)
                {
                    // it's rather odd that we have a complete transaction and we
                    // are still parsing data to it, so throw an exception
                    throw new ImplementationException("Error: trying to parse data to a complete transaction!");
                }

                int i = 0;
                while (i < toParse.Length)
                {
                    if (!_headerComplete)
                    {
                        try
                        {
                            int j;

                            while (i < toParse.Length && !IsHeaderBufferComplete)
                            {
                                j = toParse.IndexOf("\r\n", i);
                                if (j == -1)
                                {
                                    AddHeaderBuffer(toParse.Substring(i));
                                    i = toParse.Length;
                                }
                                else
                                {
                                    AddHeaderBuffer(toParse.Substring(i, j - i + 2));
                                    i = j + 2;
                                }
                            }

                            if (IsHeaderBufferComplete)
                            {
                                RecognizeHeader();
                                ProccessHeader();
                                _headerComplete = true;
                                _logger.Debug(string.Format("Parsed header of Tx[{0}]", TransactionId));
                            }
                        }
                        catch (Exception e)
                        {
                            _validTransaction = false;

                            _logger.Warn(string.Format("Exception parsing Tx-{0}[{1}] returning without parsing", TransactionType, TransactionId));

                            return;
                        }

                    }// if (!headercomplete)
                    if (_headerComplete)
                    {
                        if (!IsValid) { _logger.Warn(string.Format("parsed invalid Tx[{0}].", TransactionId)); }

                        int moreData = toParse.Length - i;
                        if (moreData > 0) { _logger.Warn("parsed header but more data to come, is preparser ok?"); }
                        break;
                    }
                }
            } // if (!inContentStuff)
            else
            {
                //ByteBuffer incByteBuffer = ByteBuffer.wrap(incData, offset, length);
                MemoryStream incBuffer = new MemoryStream(incData, offset, length);

                if (!_headerComplete)
                {
                    _logger.Warn("parsing content-stuff without headers? - quit.");
                    return;
                }
                if (!IsValid)			// no valid transaction? -> return
                {
                    _logger.Warn(string.Format("parsing invalid Tx[{0}]? - quit.", TransactionId));
                    return;
                }

                try
                {
                    byte[] data;
                    
                    if (!IsIncomingResponse && Message != null && TransactionType == TransactionType.SEND)
                    {
                        // put remaining data on the container, update realChunkSize.
                        // Account the reported bytes (automatically calls trigger)
                        // 
                        // TODO validate byteRange values for non negatives etc
                        long start = (_byteRange[CHUNKSTARTBYTEINDEX] - 1) + _realChunkSize;
                        int blockSize = Message.ReportMechanism.GetTriggerGranularity();
                        data = new byte[blockSize];
                        int size2Copy = blockSize;

                        _logger.Debug(string.Format("{0} parsing body, starting {1}, size {2}", this, start, incBuffer.Length - incBuffer.Position));
                        
                        while (incBuffer.HasRemaining())
                        {
                            if (blockSize > incBuffer.Remaining()) 
                            {
                        	    size2Copy = (int)incBuffer.Remaining();
                                data = new byte[size2Copy];
                            }

                            incBuffer.Read(data, 0, size2Copy);

                            Message.DataContainer.Put(start, data);
                            _realChunkSize += size2Copy;
                            Message.ReportMechanism.CountReceivedBodyBlock(Message, this, start, size2Copy);
                            start += size2Copy;
                        }
                    }
                    else
                    {
                        _logger.Debug(string.Format("parsing the body of  non-send or non message from Tx-<?>[{0}], nr of bytes={1}", TransactionId, (incBuffer.Length - incBuffer.Position).ToString()));

                        data = new byte[(int)incBuffer.Length - (int)incBuffer.Position];
                        incBuffer.Read(data, 0, (int)incBuffer.Length - (int)incBuffer.Position);

                        if (data.Length > 0)
                        {
                            if (_bodyByteBuffer == null) { _bodyByteBuffer = new MemoryStream(data); }  //ByteBuffer.wrap(byteData); 
                            else
                            {
                                _bodyByteBuffer.Write(data, 0, (int)data.Length);
                                _realChunkSize += (int)data.Length;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(string.Format("caught an exception while parsing Tx[{0}], generating 400 response", TransactionId), e);

                    try
                    {
                        TransactionManager.GenerateResponse(this, ResponseCodes.RC400, string.Format("Parsing exception: {0}", e.Message));
                    }
                    catch (IllegalUseException e2)
                    {
                        throw new ImplementationException(e2);
                    }
                }
            }
        }

        /// <summary>
        /// Responsible for marking the needed elements so that further processing
        /// can be correctly done.
        ///
        /// Note: this is called by the reader thread, as such, it should do
        /// minimum work and leave the rest to the transaction manager thread
        /// </summary>
        /// <param name="Flag">the character associated with the end of
        ///                                transaction ($, + or #)</param>
        public void SignalizeEnd(char Flag)
        {
            if (Direction == Direction.OUT) { throw new InternalError("Wrong use of signalizeEnd"); }

            _continuation_flag = (ContinuationFlags)Flag;

            if (!_headerComplete)
            {
                try
                {
                    RecognizeHeader();
                    ProccessHeader();
                    _validTransaction = true;
                    _headerComplete = true;
                }
                catch (InvalidHeaderException e)
                {
                    _validTransaction = false;
                }
            }

            if (_headerComplete)
            {
                // body from the end of transaction line
                if (_byteRange[1] != 0 && _byteRange[1] != UNINTIALIZED && TransactionType == TransactionType.SEND)
                {
                    // update of the chunk size with the actual data bytes that were
                    // parsed
                    _byteRange[1] = _realChunkSize;
                }

                /*
                signal the counter that one received the end of message
                continuation flag
             
                call the report mechanism function so that it can call the should
                generate report
                */
                if (TransactionType == TransactionType.SEND && !IsIncomingResponse && Message != null && (byte)Flag == (byte)ContinuationFlags.END)
                {
                    Message.ReportMechanism.GetCounter(Message).ReceivedEndOfMessage();
                }

                if (TransactionType == TransactionType.SEND && !IsIncomingResponse && _continuation_flag == ContinuationFlags.ABORT)
                {
                    /*
                    if we received a send request with a continuation flag of
                    aborted we should notify the message via the method
                    message.gotAborted so that it can change itself and notify
                    the appropriate listener that is associated with this message
                    */
                    if (Message == null)
                    {
                        // TODO log it and maybe try to recover from the error (?!)
                        throw new Exception("Error! implementation error, we should always have a message associated at this point");
                    }

                    Message.GotAborted(this);
                }
            }

            string aux = _headerBuffer.ToString();
            _headerBytes = aux.Encode(Encoding.UTF8);
            _completeTransaction = true;
        }

        /// <summary>
        /// Fills the given array with DATA (header and content excluding
        /// end of line) bytes starting from offset and stopping at the array limit
        /// or end of data and returns the number of bytes filled
        ///
        /// Throws ImplementationException if this function was called when there
        /// was no more data or if it was interrupted
        /// Throws IndexOutOfBoundsException if the offset is bigger than the length
        /// of the byte buffer to fill
        /// Throws InternalErrorException if something went wrong while trying to
        /// get this data
        /// </summary>
        /// <param name="outData">the byte array to fill</param>
        /// <param name="offset">the offset index to start filling the outData</param>
        /// <returns>the number of bytes filled</returns>
        virtual public int GetData(byte[] outData, int offset)
        {
            if (_interrupted || _readIndex[ENDLINE] > 0) { throw new ImplementationException("Called Transaction.get() when it should've been Transaction.getEndLineByte()"); }

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
                        // Remaining bytes on outData smaller than remaining on
                        // header. Fill outData with that length.
                        bytesToCopy = (outData.Length - offset);
                    }
                    else
                    {
                        bytesToCopy = (int)(_headerBytes.Length - _readIndex[HEADER]);
                    }

                    //System.arraycopy(headerBytes, (int) offsetRead[HEADERINDEX], outData, offset, bytesToCopy);
                    Array.Copy(_headerBytes, (int)_readIndex[HEADER], outData, offset, bytesToCopy);

                    _readIndex[HEADER] += bytesToCopy;
                    bytesCopied += bytesToCopy;
                    offset += bytesToCopy;

                    continue;
                }
                if (!_interrupted && Message.HasData)
                {
                    HasContentStuff = true;

                    int chunk = Message.Get(outData, offset);
                    bytesCopied += chunk;
                    offset += chunk;

                    continue;
                }
                if (!_interrupted && !Message.HasData && (_readIndex[HEADER] >= _headerBytes.Length))
                {
                    stopCopying = true;
                }
            }

            return bytesCopied;
        }

        /// <summary>
        /// Gets a byte for the end of transaction line
        ///
        /// Throws InternalErrorException if this was called with all of the end of
        /// line bytes already returned
        /// </summary>
        /// <returns>a byte of the end of transaction line</returns>
        private byte GetEndLineByte()
        {
            if (HasContentStuff && _readIndex[DATA] < 2)
            {
                // Add the extra CRLF separating the data and the end-line
                if (_readIndex[DATA]++ == 0) { return 13; }
                else { return 10; }
            }

            if (_readIndex[ENDLINE] <= 6)
            {
                _readIndex[ENDLINE]++;

                return (byte)'-';
            }

            int endlen = TransactionId.Length + 7;
            if (_readIndex[ENDLINE] > 6 && (_readIndex[ENDLINE] < endlen))
            {
                byte[] byteTID = TransactionId.Encode(Encoding.UTF8);
                return byteTID[(int)(_readIndex[ENDLINE]++ - 7)];
            }

            if (_readIndex[ENDLINE] > endlen && _readIndex[ENDLINE] <= endlen + 2)
            {
                if (_readIndex[ENDLINE]++ == endlen + 2) { return 10; }

                return 13;
            }

            if (_readIndex[ENDLINE] > endlen + 2)
            {
                throw new InternalError("Error: getEndLineByte() called without available bytes to get");
            }

            _readIndex[ENDLINE]++;

            return (byte)_continuation_flag;
        }

        /// <summary>
        /// Gets the complete endline which will fit in the buffer
        /// </summary>
        /// <param name="data">The buffer to place the endline in</param>
        /// <param name="offset">Start of the endline</param>
        /// <returns>The size of the inserted endline into the buffer</returns>
        internal int GetEndLine(byte[] data, int offset)
        {
    	    for (int i = 0; i < data.Length - offset; i++)
    	    {
    		    if (HasEndLine)
    			    data[offset + i] = GetEndLineByte();
    		    else
    			    return i;
    	    }
    	    return 0;
        }

        /// <summary>
        /// Interrupts this transaction by setting the internal flag and appropriate
        /// continuation flag (+)
        ///
        /// Throws IllegalUseException if this method was unapropriately called
        /// (meaning the transaction can't be interrupted either because
        /// it's not an OutgoingMessage or is not interruptible)
        /// </summary>
        public void Interrupt()
        {
            if (!IsInterruptible || Message.Direction != Direction.OUT)
            {
                throw new IllegalUseException(string.Format("Transaction.interrupt({0}) was called but is non interruptible", TransactionId));
            }
            if (((OutgoingMessage)Message).SentBytes != Message.Size)
            {
                // FIXME (?!) TODO check to see if there can be the case where the
                // message when gets interrupted has no remaining bytes left to be
                // sent due to possible concurrency here
                _continuation_flag = ContinuationFlags.IRQ;

                _logger.Info(string.Format("Interrupted transaction {0}", TransactionId));

                _interrupted = true;
            }
        }

        /// <summary>
        /// Is this transaction interrupted?
        /// </summary>
        public bool Interrupted
        {
            get
            {
                return _interrupted;
            }
        }

        /// <summary>
        /// Method used to abort the transaction. This method switches the
        /// continuation flag and marks this transaction as interrupted
        /// </summary>
        public void Abort()
        {
            _logger.Info(string.Format("Aborting transaction: {0}", this));
            _continuation_flag = ContinuationFlags.ABORT;
            _interrupted = true;
            // let's wake up the write thread
            TransactionManager.Connection.NotifyWriteThread();
        }

        /// <summary>
        ///  was this transaction aborted?
        /// </summary>
        public bool Aborted
        {
            get
            {
                return _interrupted && _continuation_flag == ContinuationFlags.ABORT;
            }
        }

        /// <summary>
        /// Rewind positions on the read offsets of this transaction.
        ///
        /// It's main purpose it's to allow the transaction manager to
        /// rewind the data prior to interrupting the transaction when an end-line
        /// is found on the content of the transaction.
        ///
        /// Throws IllegalUseException if this method was called to do for instance
        /// a rewind on a response
        /// </summary>
        /// <param name="numberPositionsToRewind">the number of positions to rewind on this
        ///                                       transaction.</param>
        public void Rewind(int numberPositionsToRewind)
        {
            // make sure we aren't trying to rewind a response
            if (HasResponse) { throw new IllegalUseException("Trying to rewind a response"); }

            // make sure we aren't trying to rewind on the header:
            if (_readIndex[HEADER] < _headerBytes.Length) { throw new IllegalUseException("Trying to rewind the header"); }

            // No sense in rewinding if it doesn't have any data
            if (!HasContentStuff) { throw new IllegalUseException("Trying to rewind empty transaction"); }

            // rewinds the given nr of positions the data container
            DataContainer dataContainer = Message.DataContainer;
            dataContainer.RewindRead(numberPositionsToRewind);
        }

        /// <summary>
        /// Retrieves the data associated with the body of this transaction
        ///
        /// Throws InternalErrorException if there was some kind of exception this
        /// exception is thrown with the triggering exception within
        /// </summary>
        /// <param name="size">the number of bytes or zero for the whole data</param>
        /// <returns>an array of bytes with the transaction's body or null if it
        ///          doesn't exist</returns>
        protected byte[] GetBody(int size)
        {
            if (TransactionType != TransactionType.SEND)
            {
                if (size == ALLBYTES)
                {
                    return _bodyByteBuffer.ToArray();
                }

                byte[] dst = new byte[size];
                int i = 0;
                for (; i < dst.Length; i++)
                {
                    if (_bodyByteBuffer.Position < _bodyByteBuffer.Length) { dst[i] = Convert.ToByte(_bodyByteBuffer.ReadByte()); }
                }

                return dst;
            }
            else
            {
                DataContainer dc = Message.DataContainer;
                
                MemoryStream auxByteBuffer;

                try
                {
                    if (_byteRange[0] == UNINTIALIZED || _byteRange[0] == UNKNOWN)
                    {
                        throw new InternalError("the limits of this this transaction are unknown/unintialized can't satisfy request");
                    }

                    long start = _byteRange[0] - 1;
                    if (size == ALLBYTES) { auxByteBuffer = new MemoryStream(dc.Get(start, _byteRange[1] - (start))); }
                    else { auxByteBuffer = new MemoryStream(dc.Get(start, size)); }
                }
                catch (Exception e)
                {
                    throw new InternalError(e);
                }

                return auxByteBuffer.ToArray();
            }
        }

        /// <summary>
        /// Constructor-method to initialize data structures as needed.
        /// 
        /// Currently uses this transaction's TransactionType to assert if there is
        /// need to reserve space for this transaction body
        /// </summary>
        private void InitializeDataStructures()
        {
            if (TransactionType != TransactionType.SEND && TransactionType != TransactionType.NICKNAME)
            {
                _bodyBytes = new byte[MSRPStack.MAX_NONSEND_BODYSIZE];
                _bodyByteBuffer = new MemoryStream(_bodyBytes); //ByteBuffer.wrap(bodyBytes);
            }
        }

        /// <summary>
        /// Adds the given data to the buffer checking if it already doesn't exceed
        /// the maximum limit of bytes without an \r\n in that case an Exception is
        /// thrown
        ///
        /// Throws InvalidHeaderException if MAXBYTES would be passed with the
        /// addition of stringToAdd
        /// </summary>
        /// <param name="toAdd">the string to add to the buffer used for storage of
        ///                         complete lines for analyzing posteriorly</param>
        private void AddHeaderBuffer(string toAdd)
        {
            int len = (int)(toAdd.Length + _headerBuffer.Length);

            if (len > MAXHEADERBYTES) { throw new InvalidHeaderException(string.Format("Trying to parse a line of {0} bytes when the limit is {1}", len, MAXHEADERBYTES)); }
            else
            {
                byte[] dataToAdd = toAdd.Encode(Encoding.UTF8);

                _headerBuffer.Write(dataToAdd, 0, dataToAdd.Length);
            }
        }

        private Regex _endOfHeaderWithoutContent = new Regex("^To-Path: .{10,}\r\nFrom-Path: .{10,}\r\n", RegexOptions.Singleline);

        private Regex _endOfHeaderWithContent = new Regex(".*(\r\n){2}.*", RegexOptions.Singleline);

        /// <summary>
        /// Has headerbuffer all of the header data?
        /// </summary>
        /// <returns>true if headerBuffer has all of header-data</returns>
        private bool IsHeaderBufferComplete
        {
            get
            {
                // in case of incoming response the header
                // ends with the from-paths last uri and CRLF
                Match isHeaderComplete;
                if (IsIncomingResponse) { isHeaderComplete = _endOfHeaderWithoutContent.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8)); }
                else
                {
                    /* In case of a transaction with 'content-stuff' */
                    isHeaderComplete = _endOfHeaderWithContent.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                }

                return isHeaderComplete.Success;
            }
        }


        /// <summary>
        /// Method that takes into account the validTransaction field of the
        /// transaction and other checks in order to assert if this is a valid
        /// transaction or not.
        /// 
        /// TODO complete this method so that it catches any incoherences with the
        /// protocol syntax. All of the syntax problems should be found at this point
        /// 
        /// TODO Semantic validation: validate that the mandatory headers, regarding,
        /// the method are present.
        /// 
        /// TODO check to see if there is any garbage on the transaction (bytes
        /// remaining in the headerBuffer that aren't assigned to any valid field ?!)
        /// </summary>
        private void Validate()
        {
            return;
        }

        /// <summary>
        /// Assign a session and message to the transaction when headers are complete
        ///
        /// called whenever the transaction's headers are complete and used
        /// to validate the headers, to generate the needed responses ASAP. (It
        /// also permits one to start receiving the body already knowing which
        /// session it belongs, besides also generating the responses a lot sooner)
        /// </summary>
        private void ProccessHeader()
        {
            // if this is a test (the connection of the TransactionManager is null)
            // then skip this step! (THIS IS ONLY HERE FOR DEBUG PURPOSES) 
            if (TransactionManager.Connection == null)
            {
                _logger.Debug("DEBUG MODE: should only appear if transaction is a dummy one!");
                return;
            }

            // If the transaction is an incoming response, atm do nothing. TODO(?!)
            if (IsIncomingResponse) { return; }

            if (TransactionType == TransactionType.UNSUPPORTED)
            {
                try
                {
                    TransactionManager.GenerateResponse(this, ResponseCodes.RC501, null);
                }
                catch (IllegalUseException e)
                {
                    _logger.Error(string.Format("Generating response: {0}", ResponseCode.ToString(ResponseCodes.RC501)), e);
                }
                return;
            }

            Validate();

            // make sure that a message is valid (originates 400 responses)
            if (!IsValid)
            {
                try
                {
                    TransactionManager.GenerateResponse(this, ResponseCodes.RC400, "Transaction found invalid");
                }
                catch { }
            }

            Session relatedSession = TransactionManager.AssociatedSession(ToPath[0]);
            if (relatedSession == null)
            {
                // No session associated, go see if there is one in the list of
                // yet to be validated Connections
                Connections connectionsInstance = MSRPStack.GetConnectionsInstance(TransactionManager.Connection.LocalAddress);
                relatedSession = connectionsInstance.SessionToIdentify(ToPath[0]);

                if (relatedSession == null)
                {
                    /*
                    if there are no sessions associated with this transaction
                    manager and also no sessions available to identify associated
                    with the ToPath URI we have one of two cases: - either this
                    transaction belongs to another active session (give a 506
                    response) - or this session doesn't exist at all (give a 481
                    response)
                    */
                    ResponseCodes rspCode;

                    if (_stack.IsActive(ToPath[0])) { rspCode = ResponseCodes.RC506; }
                    else { rspCode = ResponseCodes.RC481; }

                    try
                    {
                        TransactionManager.GenerateResponse(this, rspCode, null);
                    }
                    catch (IllegalUseException e)
                    {
                        _logger.Error(string.Format("Generating response: {0}", ResponseCode.ToString(rspCode)), e);
                    }
                }
                else
                {
                    // session found
                    if (_stack.IsActive(ToPath[0]))
                    {
                        // but also with another, then give the r506 response and
                        // log this rare event! (that shouldn't have happened)
                        try
                        {
                            TransactionManager.GenerateResponse(this, ResponseCodes.RC506, null);
                        }
                        catch (IllegalUseException e)
                        {
                            _logger.Error(string.Format("Generating response: {0}", ResponseCode.ToString(ResponseCodes.RC506)), e);
                        }

                        _logger.Error("Error! received a request that is yet to identify and is associated with another session!");

                        return;
                    }

                    // associate this session with this transaction manager and
                    // remove it from the list of sessions yet to be identified
                    connectionsInstance.IdentifiedSession(relatedSession);
                    Session = relatedSession;
                    TransactionManager.AddSession(relatedSession);
                    AssociateMessage();
                }
            }
            else
            {
                // this is one of the sessions for which this transaction manager is
                // responsible
                Session = relatedSession;
                AssociateMessage();
            }
        }

        /// <summary>
        /// Associates this session with the given messageID. If this is a send
        /// request: If this message doesn't exist on the context of the session then
        /// it gets created. It is assumed that this.session is different from null
        /// If this is a report request if a message can't be found the transaction
        /// is rendered invalid and it gets logged, the message is set to null It
        /// also updates the reference to the last transaction in the associated
        /// message
        /// 
        /// @param messageID the message-ID of the Message to associate
        /// </summary>
        private void AssociateMessage()
        {
            Message = _session.GetSentOrSendingMessage(MessageId);

            // check if this is a transaction for an already existing message
            if (_session.GetReceivingMessage(MessageId) != null)
            {
                Message = _session.GetReceivingMessage(MessageId);

                if (Message.WasAborted)
                /*
                if the message was previously aborted it shouldn't be on the
                queue, log the event, delete it from the list of the messages to
                be received by the bound session and continue the process TODO
                FIXME: eventually need to check with the stack if the messageID
                is known and not only with the session and act according to the
                RFC
                */
                {
                    _session.DelMessageToReceive((IncomingMessage)Message);
                    Message = null;
                }
            }

            if (Message == null)
            {
                switch (TransactionType)
                {
                    case TransactionType.NICKNAME:
                        _session.TriggerReceivedNickname(this);
                        break;
                    case TransactionType.SEND:
                        Message = IncomingMessageFactory.CreateMessage(_session, MessageId, ContentType, _totalMessageBytes);

                        IncomingMessage inMsg = (IncomingMessage)Message;
                        Message.SuccessReport = _successReport;

                        try
                        {
                            Message.FailureReport = _failureReport;
                        }
                        catch (IllegalUseException e1)
                        {
                            // TODO invalidate this transaction and
                            // trigger the appropriate response
                        }

                        bool result = inMsg is IncomingAliveMessage || _session.TriggerAcceptHook(inMsg);

                        if (result && inMsg.Result != ResponseCodes.RC200)
                        {
                            inMsg.Result = ResponseCodes.RC200;

                            if (inMsg is IncomingAliveMessage || inMsg.DataContainer != null)
                            {
                                // put on receiving message "list" of the Session
                                Session.PutReceivingMessage(inMsg);
                            }
                            else
                            {
                                // if user didn't assign DataContainer to message;
                    	        // discard & log.
                    	        _logger.Error(string.Format("{0} no datacontainer given to store incoming data, discarding incoming message {1}", this, inMsg));
                                result = false;
                            }
                        }

                        if (!result)
                        {
                            // The message is to be discarded!
                            _validTransaction = false;
                            _completeTransaction = true;

                            try
                            {
                                TransactionManager.GenerateResponse(this, inMsg.Result, "Message rejected by user");
                            }
                            catch (IllegalUseException e)
                            {
                                // the user set an invalid result, let's log it and
                                // resend it with the 413 default
                                _logger.Warn("Tried to use an invalid response code as a response, gone with the default 413");

                                try
                                {
                                    TransactionManager.GenerateResponse(this, ResponseCodes.RC413, "Message rejected by user");
                                }
                                catch (IllegalUseException e1)
                                {
                                    _logger.Error(string.Format("Exception caught generating 413 response for transaction: {0}", this), e1);
                                }

                            }
                        }
                        break;
                    case TransactionType.REPORT:
                        _validTransaction = false;

                        // the RFC tells us to silently ignore the request if no message
                        // can be associated with it so we'll just log it
                        _logger.Warn(string.Format("Warning! incoming report request for an unknown message to the stack. Message-ID: {0}", MessageId));
                        break;
                }
            }

            // lets update the reference in the Message to this transaction if this
            // is a SEND transaction and an associated message has been found
            if (Message != null && TransactionType == TransactionType.SEND)
            {
                Message.LastSendTransaction = this;
            }
        }

        private Regex _asciiPattern = new Regex("[\x00-\x7F]+", RegexOptions.Singleline);
        private Regex _headers = new Regex("(^To-Path:) (.{10,})(\r\n)(From-Path:) (.{10,})(\r\n)([\x00-\x7F]*)", RegexOptions.IgnoreCase);
        private Regex _messageIDPattern = new Regex("(.*)(Message-ID:) ([a-zA-Z0-9]([a-zA-Z0-9]|\\.|\\-|\\+|\\%|\\=){3,31})(\r\n)(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private Regex _byteRangePattern = new Regex("(.*)(Byte-Range:) ([0-9]+)-([0-9]+|\\*)/([0-9]+|\\*)(\r\n)(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private Regex _contentTypePattern = new Regex("(.*)(Content-Type:) ([^/]{1,30}/[^;\r\n]{1,30})(;.*)?\r\n(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private Regex _fReportPattern = new Regex("(.*)(Failure-Report:) ([^\r\n]*)(\r\n)(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private Regex _sReportPattern = new Regex("(.*)(Success-Report:) ([^\r\n]*)(\r\n)(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private Regex _nicknamePattern = new Regex("(.*)(Use-Nickname:) +\"([^\"]+)\"[^\r\n]*\r\n(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        private Regex _statusPattern = new Regex("(.*)(Status:) ([0-9]{3}) ([0-9]{3})([^\r\n]*)\r\n(.*)", RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>
        /// will recognize the header stored on headerBuffer initializing all of the
        /// variables related to the header and checking for some violations of the
        /// protocol
        /// 
        /// Throws InvalidHeaderException if it's found that the header is invalid
        /// for some reason
        /// </summary>
        private void RecognizeHeader()
        {
            Match matcher;

            // If the characters aren't all ascii send an invalid header
            matcher = _asciiPattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
            if (!matcher.Success) { throw new InvalidHeaderException("Error, non-ascii characters contained in the header"); }

            // headers = To-Path CRLF From-Path CRLF 1*( header CRLF )
            matcher = _headers.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
            if (!matcher.Success)
            {
                throw new InvalidHeaderException(string.Format("Transaction doesn't have valid to/from-path headers. Transaction: {0} headerBuffer: {1}", TransactionType, _headerBuffer));
            }

            try
            {
                string[] toPaths = matcher.Groups[2].Value.Split(' ');
                Uri[] toPath = new Uri[toPaths.Length];
                int i = 0;
                foreach (string path in toPaths)
                {
                    toPath[i] = new Uri(path);
                    i++;
                }
                ToPath = toPath;
            }
            catch (Exception e)
            {
                throw new InvalidHeaderException("Problem parsing to-path(s)", e);
            }
            try
            {
                string[] fromPaths = matcher.Groups[5].Value.Split(' ');
                Uri[] fromPath = new Uri[fromPaths.Length];
                int i = 0;
                foreach (string path in fromPaths)
                {
                    fromPath[i] = new Uri(path);
                    i++;
                }
                FromPath = fromPath;
            }
            catch (Exception e)
            {
                throw new InvalidHeaderException("Problem parsing from-path(s)", e);
            }

            // If we are receiving a response the processing ends here
            if (IsIncomingResponse) { return; }

            switch (TransactionType) // Method specific headers
            {
                case TransactionType.REPORT:
                    // Report request specific headers:
                    // 'Status:' processing
                    matcher = _statusPattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                    if (matcher.Success)
                    {
                        string ns = matcher.Groups[3].Value;
                        string statusCode = matcher.Groups[4].Value;
                        string comment = matcher.Groups[5].Value;
                        StatusHeader = new StatusHeader(ns, statusCode, comment);
                    }
                    /* $FALL_THROUGH */
                    goto case TransactionType.SEND;
                case TransactionType.SEND:
                    /* Message-ID processing: */
                    matcher = _messageIDPattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                    if (matcher.Success)
                    {
                        MessageId = matcher.Groups[3].Value;
                    }
                    else { throw new InvalidHeaderException("MessageID not found"); }

                    /* Byte-Range processing: */
                    matcher = _byteRangePattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                    if (matcher.Success)
                    {
                        _byteRange[0] = int.Parse(matcher.Groups[3].Value);
                        if (matcher.Groups[4].Value == "*") { _byteRange[1] = UNKNOWN; }
                        else { _byteRange[1] = int.Parse(matcher.Groups[4].Value); }
                        if (matcher.Groups[5].Value == "*") { _totalMessageBytes = UNKNOWN; }
                        else { _totalMessageBytes = int.Parse(matcher.Groups[5].Value); }
                    }
                    matcher = _contentTypePattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                    if (matcher.Success) { ContentType = matcher.Groups[3].Value; }

                    /* Report processing: */
                    matcher = _fReportPattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                    if (matcher.Success)
                    {
                        string value = matcher.Groups[3].Value.Trim().ToLower();
                        if (Regex.IsMatch(value, "yes|no|partial")) { _failureReport = value; }
                        else { _logger.Warn(string.Format("Failure-Report invalid value found: {0}", value)); }
                    }
                    matcher = _sReportPattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                    if (matcher.Success)
                    {
                        string value = matcher.Groups[3].Value.Trim().ToLower();
                        if (value == "yes") { _successReport = true; }
                        else if (value == "no") { _successReport = false; }
                        else { _logger.Warn(string.Format("Success-Report invalid value found: {0}", value)); }
                    }
                    /* Report request specific headers: */
                    if (TransactionType == TransactionType.REPORT)
                    {
                        /* 'Status:' processing */
                        matcher = _statusPattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                        if (matcher.Success)
                        {
                            string ns = matcher.Groups[3].Value;
                            string statusCode = matcher.Groups[4].Value;
                            string comment = matcher.Groups[5].Value;
                            StatusHeader = new StatusHeader(ns, statusCode, comment);
                        }
                    }
                    break;
                case MSRP.TransactionType.NICKNAME:

                    matcher = _nicknamePattern.Match(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8));
                    if (matcher.Success)
                    {
                        Nickname = matcher.Groups[3].Value;
                    }
                    else { throw new InvalidHeaderException("Nickname not found"); }

                    if (_fReportPattern.IsMatch(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8))) { _logger.Warn(this + " failure report included in NICKNAME request, ignoring..."); }
                    if (_sReportPattern.IsMatch(CodedString.Decode(_headerBuffer.ToArray(), Encoding.UTF8))) { _logger.Warn(this + " success report included in NICKNAME request, ignoring..."); }

        	        break;
                case TransactionType.UNSUPPORTED:
                    // nothing to do (yet)
                    break;
            }
        }

        /// <summary>
        /// Byte value of the $', '+' and '#' char (in utf8) continuation_flag
        /// </summary>
        protected enum ContinuationFlags
        {
            END = 36,
            IRQ = 43,
            ABORT = 35,
        }
    }
}