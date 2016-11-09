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
using System.Text.RegularExpressions;
using log4net;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using MSRP.Exceptions;
using MSRP.Java;
using MSRP.Java.Threads;
using MSRP.Utils;
using MSRP.Java.Observer;

namespace MSRP
{
    /// <summary>
    /// This class represents an MSRP connection.
    ///
    /// It has one pair of threads associated for writing and reading.
    /// 
    /// It is also responsible for some parsing, including: Identifying MSRP
    /// transaction requests and responses; Pre-parsing - identifying what is the
    /// content of the transaction from what isn't; Whenever a transactions is found,
    /// parse its data using the Transaction's parse method;
    /// </summary>
    public class Connection : Observable, IRunnable
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(Connection));

        public static int OUTPUTBUFFERLENGTH = 2048;

        public static int INPUTBUFFERLENGTH = 2048;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="newSocketChannel"></param>
        public Connection(Socket newSocketChannel)
        {
            _preParser = new PreParser(this);
            _random = new Random();

            _socket = newSocketChannel;
            _socket.Blocking = true; //TODO : Property maken van socket, en dan als deze wordt gezet direct Blocking = true zetten

            IPEndPoint endpoint = (IPEndPoint)_socket.RemoteEndPoint;

            Uri newLocalURI = new Uri(string.Format("msrp://{0}:{1}", endpoint.Address, endpoint.Port));  //new Uri("msrp", null, socket.getHostAddress(), socket.getLocalPort(), null, null, null);
            LocalURI = newLocalURI;

            TransactionManager = new TransactionManager(this);
        }

        /// <summary>
        /// Create a new connection object.
        /// This connection will create a socket and bind itself to a free port.
        ///
        /// Throws URISyntaxException there was a problem generating the connection
        /// dependent part of the URI
        /// Throws IOException if there was a problem with the creation of the
        /// socket
        /// </summary>
        /// <param name="address">hostname/IP used to bound the new MSRP socket</param>
        public Connection(IPAddress address)
        {
            _preParser = new PreParser(this);
            _random = new Random();

            TransactionManager = new TransactionManager(this);

            // activate the connection:
            if (NetworkUtils.isLinkLocalIPv4Address(address))
            {
                _logger.Info(string.Format("Connection: given address is a local one: {0}", address));
            }

            _socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp); //socketChannel;
            _socket.Blocking = true; //TODO : Property maken van socket, en dan als deze wordt gezet direct Blocking = true zetten
            IPEndPoint endpoint = new IPEndPoint(address, 0);
            _socket.Bind(endpoint);

            // fill the localURI variable that contains the uri parts that are
            // associated with this connection (scheme[protocol], host and port)
            endpoint = (IPEndPoint)_socket.LocalEndPoint;

            Uri newLocalURI = new Uri(string.Format("msrp://{0}:{1}", endpoint.Address, endpoint.Port)); //new Uri("msrp", null, address.getHostAddress(), socket.getLocalPort(), null, null, null);
            LocalURI = newLocalURI;
        }

        /// <summary>
        /// 
        /// </summary>
        public TransactionManager TransactionManager { get; private set; }

        /// <summary>
        /// private field used mainly to generate new session uris this method should
        /// contain all the uris of the sessions associated with this connection TODO
        /// 
        /// FIXME ?! check to see if this sessions is needed even though the
        /// associatedSessions on transactionManager exists and should contain the
        /// same information
        /// </summary>
        private HashSet<Uri> _sessions = new HashSet<Uri>();

        private Socket _socket = null;

        protected Random _random;

        /// <summary>
        /// Local URI associated with the connection
        /// </summary>
        public Uri LocalURI { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        protected bool IsEstablished
        {
            get
            {
                return _socket != null ? _socket.Connected : false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected HashSet<Uri> SessionURIs
        {
            get
            {
                return _sessions;
            }
        }

        private object _generateNewUriMethodLock = new object();

        /// <summary>
        /// Generates the new URI and validates it against the existing URIs of the
        /// sessions that this connection handles
        ///
        /// Throws URISyntaxException If there is a problem with the generation of
        /// the new URI
        /// </summary>
        /// <returns></returns>
        public Uri GenerateNewURI()
        {
            lock (_generateNewUriMethodLock)
            {
                Uri newURI = NewUri();

                /* The generated URI must be unique, if not, generate another.
                 * This is what the following while does
                 */
                int i = 0;
                while (_sessions.Contains(newURI))
                {
                    i++;
                    newURI = NewUri();
                }
                _sessions.Add(newURI);
                _logger.Debug(string.Format("generated new URI, value of i={0}", i));
                return newURI;
            }
        }

        /// <summary>
        /// Generate a new local URI with a unique session-path.
        /// </summary>
        /// <returns>the generated URI</returns>
        protected Uri NewUri()
        {
            byte[] randomBytes = new byte[8];

            GenerateRandom(randomBytes);

            if (_logger.IsDebugEnabled) { _logger.Debug(string.Format("Random bytes generated: {0}:END", CodedString.Decode(randomBytes, Encoding.UTF8))); }

            // Generate new using current local URI.
            //newURI = new Uri(localURI.Scheme, localURI.UserInfo, localURI.Host, localURI.Port, "/" + (new String(randomBytes, Charset.forName("us-ascii"))) + ";tcp", localURI.Query, localURI.Fragment);
            return new Uri(string.Format("msrp://{0}:{1}/{2};tcp", LocalURI.Host, LocalURI.Port, CodedString.Decode(randomBytes, Encoding.UTF8)));
        }

        // IMPROVE it could be improved by adding the rest of the unreserved
        // characters according to rfc3986 (-._~)
        // IMPROVE can be improved the speed by not doing so much calls to the
        // Random class
        /// <summary>
        /// Generates a number of random alpha-numeric and digit codes in US-ASCII
        /// </summary>
        /// <param name="byteArray">the byte array that will contain the newly generated
        ///                         bytes. the number of generated bytes is given by the length of
        ///                         the byteArray</param>
        private void GenerateRandom(byte[] byteArray)
        {
            _random.NextBytes(byteArray);
            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] < 0) { byteArray[i] *= 0; } //-1

                while (!((byteArray[i] >= 65 && byteArray[i] <= 90) || (byteArray[i] >= 97 && byteArray[i] <= 122) || (byteArray[i] <= 57 && byteArray[i] >= 48)))
                {
                    if (byteArray[i] > 122) { byteArray[i] -= (byte)_random.Next(byteArray[i]); }
                    if (byteArray[i] < 48) { byteArray[i] += (byte)_random.Next(5); }
                    else { byteArray[i] += (byte)_random.Next(10); }
                }
            }
        }

        /// <summary>
        /// Returns if the socket associated with the connection is bound
        /// </summary>
        /// <returns></returns>
        public bool IsBound
        {
            get
            {
                return _socket != null ? _socket.IsBound : false;
            }
        }

        /// <summary>
        /// @uml.property name="_session"
        /// @uml.associationEnd inverse="_connectoin:msrp.Session"
        /// </summary>
        public MSRP.Session Session { get; private set; }

        /// <summary>
        /// close this connection/these threads
        /// </summary>
        public void Close()
        {
            if (_closing) { return; }// already closed 

            _closing = true;

            //RV 19/04/2012 - socket.Close() is benodigd omdat deze in blocking-modus staat!!
            try
            {
                if (_socket != null)
                {
                    _socket.Close();
                }
            }
            catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void MessageInterrupt(Message message)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="transactionManager"></param>
        /// <param name="transactionCode"></param>
        public void NewTransaction(Session session, Message message, TransactionManager transactionManager, String transactionCode)
        {
        }

        /// <summary>
        /// @desc - Read (reads from the stream of the socket)
        /// @desc - Validation of what is being read
        /// @desc - Misc. Interrupts due to read errors (mem, buffers etc)
        /// </summary>
        public void Read()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public void SessionClose(Session session)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        public Connection()
        {
            _preParser = new PreParser(this);
        }

        /// <summary>
        /// Connection closing?
        /// </summary>
        protected bool _closing = false;

        /// <summary>
        /// 
        /// </summary>
        private Thread _writeThread = null;

        /// <summary>
        /// 
        /// </summary>
        private Thread _readThread = null;

        /// <summary>
        /// 
        /// </summary>
        private void WriteCycle()
        {
            // TODO FIXME should remove this line here when we get a better model
            // for the threads
            //Thread.CurrentThread.Name = "Connection: " + localURI + " writeCycle thread";

            byte[] outData; // = new byte[OUTPUTBUFFERLENGTH];
            //ByteBuffer outByteBuffer = ByteBuffer.wrap(outData);

            int wroteNrBytes = 0;
            while (!_closing)
            {
                try
                {
                    if (TransactionManager.HasDataToSend)
                    {
                        outData = new byte[OUTPUTBUFFERLENGTH];
                        int toWriteNrBytes;

                        //NIET MEER BENODIGD outByteBuffer = new MemoryStream(); //.clear();
                        // toWriteNrBytes = transactionManager.dataToSend(outData);
                        // FIXME remove comment and change method name after the
                        // tests go well
                        toWriteNrBytes = TransactionManager.GetDataToSend(outData);
                        outData = ((MemoryStream)new MemoryStream(outData).Limit(toWriteNrBytes)).ToArray();

                        _logger.Debug(string.Format("Sending MSRP, size {0} bytes : {1}{1}{2}", outData.Length, Environment.NewLine, CodedString.Decode(outData, Encoding.UTF8)));

                        wroteNrBytes = 0;
                        while (wroteNrBytes != toWriteNrBytes)
                        {
                            if (_closing)
                            {
                                _logger.Info("Connection already closed, while sending a message!");
                                break;
                            }

                            SocketError se;
                            wroteNrBytes += _socket.Send(outData, 0, outData.Length, SocketFlags.None, out se);

                            if (se != SocketError.Success) { throw new Exception(string.Format("Socket error occurred on writeCycle: {0}", se.ToString())); }
                        }
                    }
                    else
                    {
                        lock (_writeThread)
                        {
                            // TODO FIXME do this in another way, maybe with notify!
                            _writeThread.Join(200);
                        }
                    }
                }
                catch (Exception e)
                {
                    //RV 20/04/2012 - Behouden van de catch hier behoud de code-synchroniteit tussen de JAVA implementatie!!
                    if (!_closing) { throw new ConnectionWriteException(e); }
                }
            }
        }

        /// <summary>
        /// Used to pre-parse the received data by the read cycle
        /// 
        /// See #readCycle()
        /// See PreParser#preParse(byte[])
        /// </summary>
        private PreParser _preParser = null; //new PreParser();

        /// <summary>
        /// 
        /// </summary>
        private void ReadCycle()
        {
            byte[] inData = new byte[OUTPUTBUFFERLENGTH];
            MemoryStream inByteBuffer; // = new MemoryStream(inData); //ByteBuffer inByteBuffer = ByteBuffer.wrap(inData);
            int readNrBytes = -1;
            SocketError se = SocketError.NotInitialized;

            while (readNrBytes != 0 && !_closing)
            {
                try
                {
                    byte[] data2receive = new byte[INPUTBUFFERLENGTH];

                    readNrBytes = _socket.Receive(data2receive, 0, INPUTBUFFERLENGTH, SocketFlags.None, out se);

                    if (se != SocketError.Success) { throw new Exception(string.Format("Socket error occurred on readCycle: {0}", se.ToString())); }

                    else if (readNrBytes != -1 && readNrBytes != 0)
                    {
                        inByteBuffer = (MemoryStream)new MemoryStream(data2receive).Limit(readNrBytes);

                        _logger.Debug(string.Format("Received MSRP, size {0} bytes : {1}{1}{2}", inByteBuffer.Length, Environment.NewLine, CodedString.Decode(inByteBuffer.ToArray(), Encoding.UTF8)));

                        // commented, too much output was being generated:
                        // logger.Debug("Read: " + readNrBytes + " bytes of data from: " + socketChannel.socket().getRemoteSocketAddress().toString());
                        _preParser.PreParse(inByteBuffer.ToArray(), (int)inByteBuffer.Length); //.limit());
                    }
                }
                catch (Exception e)
                {
                    if (!_closing) { throw new ConnectionReadException(e); }
                }
            }
        }

        public void ThreadRun()
        {
            Run();
        }

        /// <summary>
        /// Constantly receives and sends new transactions
        /// </summary>
        virtual public void Run()
        {
            // Sanity checks
            if (!IsBound && !IsEstablished)
            {
                // if the socket is not bound to a local address or is
                // not connected, it shouldn't be running
                _logger.Error("Error!, Connection shouldn't be running yet");

                return;
            }
            if (!IsEstablished || !IsBound)
            {
                _logger.Error("Error! got a unestablished either or unbound connection");

                return;
            }

            if (_writeThread == null && _readThread == null)
            {
                _writeThread = Thread.CurrentThread;
                _readThread = _ioOperationGroup.CreateThread(ThreadRun);
                _readThread.Name = string.Format("Connection: {0} readThread", LocalURI);
                _readThread.Start();
            }

            try
            {
                if (_writeThread == Thread.CurrentThread)
                {
                    WriteCycle();
                    _writeThread = null;
                }
                if (_readThread == Thread.CurrentThread)
                {
                    ReadCycle();
                    _readThread = null;
                }
            }
            catch (ConnectionLostException cle)
            {
                NotifyConnectionLoss(cle.InnerException);
            }
            catch (Exception e)
            {
                _logger.Error(e.Message);
            }

            return;
        }

        /// <summary>
        /// Notify sessions related to this connection that this connection is lost. 
        /// </summary>
        /// <param name="exception">the reason it was lost.</param>
        private void NotifyConnectionLoss(Exception exception) 
        {
		    List<Session> attachedSessions = new List<Session>();
		    foreach (Session s in MSRPStack.GetInstance().ActiveSessions) 
            {
			    if (this == s.Connection)
				attachedSessions.Add(s);
		    }
		    
            // No concurrent modifications: have active sessions remove
		    // themselves from the stack
		    foreach(Session s in attachedSessions) 
            {
			    s.TriggerConnectionLost(exception);
		    }
	    }

        /// <summary>
        /// 
        /// </summary>
        private bool _receivingTransaction = false;

        /// <summary>
        /// 
        /// </summary>
        internal Transaction _incomingTransaction = null;

        /// <summary>
        /// Find the MSRP start of the transaction stamp
        ///
        /// the TID has to be at least 64bits long = 8chars
        /// given as a reasonable limit of 20 for the transaction id
        /// although non normative. Also the method name will have the same 20 limit
        /// and has to be a Upper case word like SEND
        /// </summary>
        private static Regex _req_start = new Regex("(^MSRP) ([A-Za-z0-9]{8,20}) ([A-Z]{1,20})\r\n(.*)", RegexOptions.Singleline);

        private static Regex _resp_start = new Regex("(^MSRP) ([A-Za-z0-9]{8,20}) ((\\d{3})([^\r\n]*)\r\n)(.*)", RegexOptions.Singleline);

        /// <summary>
        /// Parse the incoming data, identifying transaction start or end,
        /// creating a new transaction according RFC.
        ///
        /// Throws ConnectionParserException Generic error
        /// </summary>
        /// <param name="incomingBytes">raw byte data to be handled</param>
        /// <param name="offset">the starting position in the given byte array we should
        ///                      consider for processing</param>
        /// <param name="length">the number of bytes to process starting from the offset
        ///                      position</param>
        /// <param name="inContentStuff">true if it is receiving data regarding the body
        ///                                 of a transaction, false otherwise</param>
        internal void Parser(byte[] incomingBytes, int offset, int length, bool inContentStuff)
        {
            if (inContentStuff)
            {
                try
                {
                    _incomingTransaction.Parse(incomingBytes, offset, length, inContentStuff);
                }
                catch { }
            }
            else
            {
                // We are receiving headers.

                string incomingString = CodedString.Decode(incomingBytes, offset, length, Encoding.UTF8);
                string toParse = incomingString;
                string tID;

                //For calls containing multiple transactions in incomingString
                List<string> txRest = new List<string>();

                do
                {
                    /*
                     * Deal with reception of multiple transactions.
                     */
                    if (txRest.Count > 0)
                    {
                        toParse = txRest[0];
                        txRest.RemoveAt(0);
                    }
                    if (txRest.Count > 0)
                    {
                        throw new Exception("Error! the restTransactions was never meant to have more than one element!");
                    }

                    if (!_receivingTransaction)
                    {
                        Match matchRequest = _req_start.Match(toParse);
                        Match matchResponse = _resp_start.Match(toParse);

                        if (matchRequest.Success)
                        {
                            // Retrieve TID and create new transaction
                            _receivingTransaction = true;
                            tID = matchRequest.Groups[2].Value;
                            toParse = matchRequest.Groups[4].Value;
                            string type = matchRequest.Groups[3].Value.ToUpper();
                           
                            TransactionType tType = TransactionType.UNSUPPORTED;
                            if (Enum.TryParse(type, out tType)) { _logger.Debug(string.Format("Parsing incoming request Tx-{0}[{1}]", tType.ToString(), tID.ToString())); }
                            else { _logger.Warn(string.Format("Unsupported transaction type: Tx-{0}[{1}]", type, tID)); }

                            try
                            {
                                _incomingTransaction = new Transaction(tID, tType, TransactionManager, Direction.IN);
                            }
                            catch (IllegalUseException e)
                            {
                                _logger.Error("Cannot create an incoming transaction", e);
                            }

                            if (tType == TransactionType.UNSUPPORTED)
                            {
                                _incomingTransaction.SignalizeEnd('$');
                                _logger.Warn(string.Format("Found an unsupported transaction type for[{0}] signalised end and called update", tID));
                                SetChanged();
                                NotifyObservers(tType);
                                // XXX:? receivingTransaction = false;
                            }
                        }
                        else if (matchResponse.Success)
                        {
                            _receivingTransaction = true;
                            tID = matchResponse.Groups[2].Value;
                            int status = int.Parse(matchResponse.Groups[4].Value);
                            string comment = matchResponse.Groups[5].Value;
                            if (matchResponse.Groups.Count >= 7) { toParse = matchResponse.Groups[6].Value; }

                            _incomingTransaction = TransactionManager.GetTransaction(tID);

                            if (_incomingTransaction == null)
                            {
                                _logger.Error("Received response for unknown transaction");
                                // TODO: cannot continue without a known transaction, proper abort here
                            }
                            _logger.Debug(string.Format("Found response to transaction: {0}", tID));

                            try
                            {
                                Transaction trResponse = new TransactionResponse(_incomingTransaction, (ResponseCodes)status, comment, Direction.IN);
                                _incomingTransaction = trResponse;
                            }
                            catch (IllegalUseException e)
                            {
                                throw new ParseException("Cannot create transaction response", e);
                            }
                        }
                        else
                        {
                            _logger.Error(string.Format("Start of transaction not found while parsing:\n{0}", incomingString));
                            throw new ParseException(string.Format("Error, start of the transaction not found on thread: {0}", Thread.CurrentThread.Name));
                        }
                    }
                    if (_receivingTransaction)
                    {
                        // Split multiple transactions.
                        tID = _incomingTransaction.TransactionId;
                        Regex endTransaction = new Regex("(.*)(-------" + tID + ")([$+#])(\r\n)(.*)?", RegexOptions.Singleline);
                        Match matcher = endTransaction.Match(toParse);
                        if (matcher.Success)
                        {
                            _logger.Debug(string.Format("found the end of the transaction: {0}", tID));

                            toParse = matcher.Groups[1].Value + matcher.Groups[2].Value + matcher.Groups[3].Value + matcher.Groups[4].Value;

                            // add any remaining data to restTransactions
                            if (matcher.Groups.Count >= 6 && matcher.Groups[5].Value != null && matcher.Groups[5].Value != string.Empty)
                            {
                                txRest.Add(matcher.Groups[5].Value);
                            }
                        }
                        
                        // identify if transaction has content-stuff or not:
                        // 'Content-Type 2CRLF' from formal syntax.
                        string tokenRegex = RegexMSRP.token.ToString();
                        Regex contentStuff = new Regex("(.*)(Content-Type:) (" + tokenRegex + "/" + tokenRegex + ")(\r\n\r\n)(.*)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        matcher = contentStuff.Match(toParse);
                        if (matcher.Success)
                        {
                            _logger.Debug(string.Format("transaction {0} was found to have contentstuff", tID));
                            _incomingTransaction.HasContentStuff = true;
                        }
                        if (_incomingTransaction.HasContentStuff)
                        {
                            // strip 1 CRLF from string to parse...
                            endTransaction = new Regex("(.*)(\r\n)(-------" + tID + ")([$+#])(\r\n)(.*)?", RegexOptions.Singleline);
                        }

                        matcher = endTransaction.Match(toParse);

                        if (matcher.Success)
                        {
                            // we have a complete end of transaction
                            try
                            {
                                _incomingTransaction.Parse(matcher.Groups[1].Value.Encode(Encoding.UTF8), 0, matcher.Groups[1].Value.Length, inContentStuff);
                            }
                            catch (Exception e) { }

                            if (_incomingTransaction.HasContentStuff) { _incomingTransaction.SignalizeEnd(matcher.Groups[4].Value[0]); }
                            else { _incomingTransaction.SignalizeEnd(matcher.Groups[3].Value[0]); }
                                

                            SetChanged();
                            NotifyObservers(_incomingTransaction);
                            _receivingTransaction = false;
                        }
                        else
                        {
                            try
                            {
                                _incomingTransaction.Parse(Encoding.UTF8.GetBytes(toParse), 0, toParse.Length, inContentStuff);
                            }
                            catch (Exception e)
                            {
                                _logger.Error("Exception parsing data to a transaction:", e);
                            }
                        }
                    }
                }
                while (txRest.Count > 0);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private ThreadGroup _ioOperationGroup = new ThreadGroup(Guid.NewGuid().ToString());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="address"></param>
        public void AddEndPoint(Uri uri, InetAddress address)
        {
            // Adds the given endpoint to the socket address and starts the
            // listening/writing cycle
            IPEndPoint remoteAddress = new IPEndPoint(uri.GetIPAddress(), uri.Port);
            /*
            TODO FIXME probably the new TransactionManager isn't needed, however
            i'll still create it but copy the values needed for automatic testing
            SubIssue #1
            */
            /*bool testingOld = false;

            string presetTidOld = "";
            // -- start of the code that enables a transaction test.
            if (transactionManager != null)
            {
                testingOld = transactionManager.testing;
                presetTidOld = transactionManager.presetTID;
            }
            transactionManager = new TransactionManager(this);
            transactionManager.testing = testingOld;
            transactionManager.presetTID = presetTidOld;
            // -- end of the code that enables a transaction test.*/

            _socket.Connect(remoteAddress);
            Connections connectionsInstance = MSRPStack.GetConnectionsInstance(address);

            _ioOperationGroup = new ThreadGroup(connectionsInstance.ConnectionsGroup, string.Format("IO OP connection {0} group", uri.ToString()));
            connectionsInstance.StartConnectionThread(this, _ioOperationGroup);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>the InetAddress of the locally bound IP</returns>
        public InetAddress LocalAddress
        {
            get
            {
                return new InetAddress(((IPEndPoint)_socket.LocalEndPoint).Address); //(socket.getHostAddress());
            }
        }

        /// <summary>
        /// Method used to notify the write cycle thread
        /// </summary>
        public void NotifyWriteThread()
        {
            if (_writeThread != null)
            {
                lock (_writeThread)
                {
                    Monitor.Pulse(_writeThread);
                }
            }
        }
    }
}
