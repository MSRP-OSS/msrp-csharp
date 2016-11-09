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
using MSRP.Events;
using MSRP.Utils;
using MSRP.Java;
using MSRP.Java.Observer;
using System.Threading;

namespace MSRP
{
    /// <summary>
    /// This class is responsible for managing the transactions associated with a
    /// connection (that can have many sessions).
    ///  
    /// It generates the automatic responses, and also triggers some reporting
    /// mechanisms and some of the callbacks on the MSRPSessionListener 
    /// </summary>
    public class TransactionManager : Observer
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(TransactionManager));

        /// <summary>
        /// @uml.property name="_connections"
        /// </summary>
        public Connection Connection { get; private set; }

        /// <summary>
        /// @uml.property name="_transactionCounter"
        /// </summary>
        private byte _counter = 0;
        protected byte Counter { get { return _counter; } set { _counter = value; } }

        /// <summary>
        /// @uml.property name="_transactions"
        /// @uml.associationEnd multiplicity="(0 -1)"
        ///      inverse="_transactionManager:msrp.Transaction"
        /// </summary>
        private object _transactionsToSendLock = new object();
        private ThreadSafeList<Transaction> _transactionsToSend = new ThreadSafeList<Transaction>();

        /// <summary>
        /// @uml.property name="_transactions"
        /// @uml.associationEnd multiplicity="(0 -1)"
        ///      inverse="_transactionManager:msrp.Transaction"
        /// </summary>
        private Dictionary<string, Transaction> _existingTransactions = new Dictionary<string, Transaction>();

        /// <summary>
        /// @uml.property name="_transactions"
        /// @uml.associationEnd multiplicity="(0 -1)"
        ///      inverse="_transactionManager:msrp.Transaction"
        /// </summary>
        private object _associatedSessionsLock = new object();
        private Dictionary<Uri, Session> associatedSessions = new Dictionary<Uri, Session>();

        /// <summary>
        /// Variable used so that some method can behave in a different way for
        /// automatic testing purposes.
        /// 
        /// Methods that may behave differently:
        /// 
        /// @see #generateNewTID()
        /// </summary>
        public bool Testing = false;

        /// <summary>
        /// Variable used for testing purposes in conjunction with the testing
        /// boolean flag
        /// 
        /// See #testing
        /// See #generateNewTID()
        /// </summary>
        public string PresetTID;

        /// <summary>
        /// Generates and queues the TransactionResponse with the given response code
        ///
        /// Throws InternalErrorException if queuing the response got us an error
        /// Throws IllegalUseException if the arguments are invalid
        /// </summary>
        /// <param name="originalTransaction"></param>
        /// <param name="responseCode">one of the response codes listed in RFC 4975</param>
        /// <param name="optionalComment">the comment as defined in RFC 4975 formal syntax,
        ///                               as the comment is optional, it can also be null if no comment
        ///                               is desired</param>
        private void GenerateAndQueueResponse(Transaction originalTransaction, ResponseCodes responseCode, string optionalComment)
        {
            TransactionResponse trResponse = new TransactionResponse(originalTransaction, responseCode, optionalComment, Direction.OUT);

            originalTransaction.Response = trResponse;
            AddPriorityTransaction(trResponse);
        }

        /// <summary>
        /// generates and queues the response of the given transaction, taking into
        /// account the Report header fields.
        ///
        /// Throws InternalErrorException if queuing the response got us an error
        /// Throws IllegalUseException if the arguments or their state is invalid
        /// </summary>
        /// <param name="transaction">the transaction that we are responding to</param>
        /// <param name="responseCode">the response code to respond with</param>
        /// <param name="comment">responseComment the optional string 'comment' as specified in rfc
        ///                               4975 syntax</param>
        public void GenerateResponse(Transaction transaction, ResponseCodes responseCode, string comment)
        {
            if (transaction == null) { throw new InvalidOperationException("null tranaction specified"); }

            //RV 21/08/2012 - Kan alleen nog maar van enum ResponseCodes zijn dus altijd correct!
            //if (!ResponseCode.IsValid(responseCode)) { throw new InvalidOperationException("Invalid response code"); }

            // TODO validate comment based on utf8text

            _logger.Debug(string.Format("Response being sent for Transaction tId: {0} response code: {1}",  transaction.TransactionId, responseCode));

            string reportFlag = transaction.FailureReport;

            // TODO generate responses based on success report field

            // generate the responses based on the failure report field
            if (reportFlag == null || reportFlag.ToLower() == "yes" || reportFlag.ToLower() == "partial")
            {
                if (reportFlag != null && reportFlag.ToLower() == "partial" && !ResponseCode.IsError(responseCode)) { return; }
                else
                {
                    try
                    {
                        GenerateAndQueueResponse(transaction, responseCode, comment);
                    }
                    catch (InternalError e)
                    {
                        _logger.Error(string.Format("Generating a {0} response for transaction: {1}", responseCode, transaction.ToString()), e);
                    }
                }
            }
        }

        /// <summary>
        /// Convenience method that gets the session associated with the given
        /// transaction
        /// </summary>
        /// <param name="transaction">transaction from which to get an associated session</param>
        /// <returns>the session associated with this transaction</returns>
        public Session GetAssociatedSession(Transaction transaction)
        {
            return MSRPStack.GetInstance().GetSession(transaction.ToPath[0]);
        }

        /// <summary>
        /// this is when a received transaction will give a 200 response code this
        /// function is called independently of the (Failure/Success)-Report field
        /// 
        /// basicly this function is called whenever any request is error free and
        /// awaiting to be processed (although the misleading name of the function
        /// this function may not generate any kind of response
        /// </summary>
        /// <param name="transaction"></param>
        protected void R200ProcessRequest(Transaction transaction)
        {
            _logger.Debug(string.Format("called r200 with {0}, message-id[{1}], associated connection (localURI): {2}", transaction, transaction.MessageId, Connection.LocalURI));

            if (transaction.TransactionType == TransactionType.SEND)
            {
                try
                {
                    GenerateResponse(transaction, ResponseCodes.RC200, null);
                }
                catch (IllegalUseException e1)
                {
                    _logger.Error(string.Format("Generating a success report for transaction: {0}", transaction));
                }

                IncomingMessage message = (IncomingMessage)transaction.Message;
                if (message != null && message.IsComplete)
                {
                    _logger.Debug(string.Format("transaction: tId {0} has an associated message message-id: {1} that is complete", transaction.TransactionId, transaction.MessageId));

                    // if we have a complete message with content
                    // get the associated session
                    Session session = GetAssociatedSession(transaction);

                    // TODO sanity check: check to see if message already
                    // exists
                    // (?!
                    // no use atm also think twice about
                    // maintaining the receivedMessages on Session)

                    try
                    {
                        IncomingMessage validated = (IncomingMessage)message.Validate();

                        long callCount = validated.Counter.Count;

                        validated.ReportMechanism.TriggerSuccessReport(validated, transaction, validated.LastCallReportCount, callCount);

                        validated.LastCallReportCount = callCount;

                        session.TriggerReceiveMessage(validated);
                    }
                    catch (Exception e)
                    {
                        try
                        {
                            FailureReport fr = new FailureReport(message, session, transaction, "000", ResponseCodes.RC400, e.Message);
                            AddPriorityTransaction(fr);
                        }
                        catch { }
                    }
                }
            }

            if (transaction.TransactionType == TransactionType.REPORT)
            {
                StatusHeader transactionStatusHeader = transaction.StatusHeader;
                string statusCodeString = transactionStatusHeader.StatusCode.ToString();

                _logger.Debug(string.Format("{0} is a report! Status code: {1}", transaction.ToString(), statusCodeString));

                // at the moment just trigger the report, doesn't save it or send
                // it:
                //
                // if (transactionStatusHeader.getNamespace() == 0)
                // REMOVE FIXME TODO the implementation exception should
                // be handled like this?!
                try
                {
                    transaction.Session.TriggerReceivedReport(transaction);
                }
                catch (ImplementationException e)
                {
                    _logger.Error("Calling triggerReceivedReport", e);
                }
            }

        }

        /// <summary>
        /// Dummy constructor used for test purposes
        /// </summary>
        protected TransactionManager() { }

        /// <summary>
        /// Constructor used by the stack the TransactionManager always has a
        /// connection associated with it
        /// </summary>
        /// <param name="connection">the connection associated with the new TransactionManager</param>
        public TransactionManager(Connection connection)
        {
            Connection = connection;

            Connection.DeleteObservers();
            Connection.AddObserver(this);
        }

        /// <summary>
        /// the new Transaction ID generated randomly and making sure that it
        /// doesn't exist on the existingTransactions that is the list of
        /// existing transactions that this transaction manager manages. It
        /// may return also a preset transaction ID for debug and test
        /// purposes.
        /// </summary>
        /// <returns></returns>
        public string GenerateNewTID()
        {
            // next two lines used for automatic testing purposes
            if (Testing && PresetTID != null)
            {
                // we can only generate once a presetTID otherwise the transaction
                // manager will be unable to generate new transactions

                string tidToReturn = PresetTID; //new string(presetTID);
                PresetTID = null;
                return tidToReturn;
            }

            byte[] tid = new byte[8];
            string newTID;

            do
            {
                TextUtils.generateRandom(tid);
                newTID = CodedString.Decode(tid, Encoding.UTF8);
            }
            while(_existingTransactions.ContainsKey(newTID));

            return newTID;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="observable"></param>
        public override void Update(Observable observable)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="observable"></param>
        /// <param name="observableObject"></param>
        override public void Update(Observable observable, object observableObject)
        {
            // TODO Check to see if the associated transaction belongs to this
            // session
            // Sanity check, check if this is the right type of object
            if (!(observable is Connection))
            {
                _logger.Error("Error! TransactionManager was notified with the wrong type of object associated");
                return;
            }

            // Sanity check, check if this is the right type of Observable
            if (!(observableObject is Transaction))
            {
                _logger.Error("Error! TransactionManager was notified with the wrong observable type");
                return;
            }

            Connection connection = (Connection)observable;
            Transaction transaction = (Transaction)observableObject;

            _logger.Debug(string.Format("UPDATE of TransactionManager called. Received {0} associated connection (localURI): {1}", transaction.ToString(), connection.LocalURI));

            // if the transaction is an incoming response, atm do nothing. TODO(?!)
            if (transaction.TransactionType == TransactionType.RESPONSE)
            {
                TransactionResponse transactionResponse = (TransactionResponse)transaction;

                _logger.Debug(string.Format("{0} is an incoming response and has been processed by the transactionManager for connection (localURI): {1}", transaction.ToString(), connection.LocalURI));

                ProcessResponse(transactionResponse);
                return;
            }

            if (transaction.TransactionType == TransactionType.UNSUPPORTED)
            {

                // "important" question: does this 501 response precedes the 506 or
                // not?! should the to-path also be checked before?!
                _logger.Debug(string.Format("{0} is not supported and has been processed by the transactionManager for connection (localURI): {1}", transaction, connection.LocalURI));

                // TODO r501();
                return;
            }


            // if it's a valid transaction call and a response hasn't been generated
            // yet, generate the r200 method otherwise ignore this call
            if (transaction.IsValid && transaction.IsRequest) { R200ProcessRequest(transaction); }

            _logger.Debug(string.Format("Transaction tID: {0} has been processed by the transactionManager for connection localURI: {1}", transaction.TransactionId, Connection.LocalURI));
        }

        /// <summary>
        /// This method generates the appropriate actions inside the stack
        /// </summary>
        /// <param name="response">the transaction that contains the response
        ///                                   being processed</param>
        private void ProcessResponse(TransactionResponse response)
        {
            if (response.Response2Type == TransactionType.NICKNAME)
            {
                response.Message.Session.TriggerReceivedNickResult(response);
            }
            // let's see if this response is worthy of a abort event
            else if (ResponseCode.IsAbortCode(response.TRResponseCode))
            {
                try
                {
                    response.Message.Abort(MessageAbortedEvent.CONTINUATIONFLAG, null);
                }
                catch (InternalError e)
                {
                    _logger.Error(string.Format("Exception caught aborting the message: {0}", response.Message), e);
                }
                catch (IllegalUseException e)
                {
                    _logger.Error(string.Format("Exception caught aborting the message: {0}", response.Message), e);
                }

                // TODO support the comment Issue #29
                response.Message.fireMessageAbortedEvent(response.TRResponseCode, null, response);
            }
        }

        /// <summary>
        /// Method used by an incoming Transaction to retrieve the session associated
        /// with it
        /// </summary>
        /// <param name="uriSession"></param>
        /// <returns>the session associated with uriSession or null if there is no
        ///          such session by that uri associated with this object</returns>
        public Session AssociatedSession(Uri uriSession)
        {
            Session session = null;
            if (associatedSessions.Keys.Contains(uriSession)) { return associatedSessions[uriSession]; }

            return session;
        }

        /// <summary>
        /// Associates the given session to this transaction manager and hencefore
        /// with the unique connection associated with this transaction manager Also
        /// bind the session to the transaction manager
        /// </summary>
        /// <param name="session">the Session to be added to the list of associated sessions
        /// of this transaction manager</param>
        public void AddSession(Session session)
        {
            session.TransactionManager = this;
            associatedSessions.Add(session.Uri, session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public void RemoveSession(Session session)
        {
            associatedSessions.Remove(session.Uri);
        }

        /// <summary>
        /// a Collection of the sessions associated with this transaction
        /// manager
        /// </summary>
        /// <returns></returns>
        //public Dictionary<Uri, Session> getAssociatedSessions()
        public List<Session> GetAssociatedSessions()
        {
            List<Session> result = new List<Session>();

            foreach (Uri uri in associatedSessions.Keys)
            {
                result.Add(associatedSessions[uri]);
            }

            return result;
        }

        /// <summary>
        /// initializes the given session by sending an existent message of the
        /// message queue or sending a new empty send without body
        /// </summary>
        /// <param name="session">the session to initialize</param>
        public void Initialize(Session session)
        {
            if (session.HasMessagesToSend)
            {
                GenerateTransactionsToSend(session.GetMessageToSend());
            }
            else
            {
                try
                {
                    session.SendAliveMessage();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex.Message);
                }
            }
        }

        /// <summary>
        /// method used to generate the transactions to be sent based on the message
        /// also updates the reference in the Message of the lastSendTransaction of
        /// the newly created transaction
        /// </summary>
        public void GenerateTransactionsToSend(Message messageToSend)
        {
            Transaction newTransaction = null;

            try
            {
                if (messageToSend == null || messageToSend.Direction != Direction.OUT) 
                {
                    throw new IllegalUseException("No or invalid message to send specified");
                }

                Message validated = messageToSend.Validate();
            
                newTransaction = new Transaction((OutgoingMessage)validated, this);
            }
            catch (Exception ex)
            {
                _logger.Error("Error validating message to send, ignoring. Reason: ", ex);
                return;
            }

            lock (this)
            {
                // TODO : possibly split the message into several transactions
                // Add the transaction to the known list of existing transactions
                // this is used to generate unique TIDs in the connection and to
                // be used when a response to a transaction is received
                _existingTransactions.Add(newTransaction.TransactionId, newTransaction);

                // change the reference to the lastSendTransaction of the message
                messageToSend.LastSendTransaction = newTransaction;

                AddTransactionToSend(newTransaction, UNIMPORTANT);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private const int UNIMPORTANT = -1;

        /// <summary>
        /// Adds the given transaction to the queue of transactions to send and wakes
        /// up the write thread of the associated connection
        /// </summary>
        /// <param name="transaction">transactionToSend</param>
        /// <param name="positionIndex">the position in which to add the transaction, if -1
        ///                             (UNIMPORTANT) just run an .add</param>
        private void AddTransactionToSend(Transaction transaction, int positionIndex)
        {
            lock (_transactionsToSendLock)
            {
                if (positionIndex != UNIMPORTANT) { _transactionsToSend.Insert(positionIndex, transaction); }
                else { _transactionsToSend.Add(transaction); }
            }
            Connection.NotifyWriteThread();
        }

        /// <summary>
        /// Remove this transaction from the send queue.
        /// 
        /// In case this is an interrupted transaction, generate and queue the rest.
        /// </summary>
        /// <param name="tx"></param>
        private void RemoveTransactionToSend(Transaction tx)
        {
            lock (_transactionsToSendLock)
            {
                if (_transactionsToSend.Contains(tx))
                {
                    _transactionsToSend.Remove(tx);

                    if (tx.Interrupted && !tx.Aborted)
                    {
                        GenerateTransactionsToSend(tx.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the transaction queue for existing transactions to be sent
        /// </summary>
        /// <returns>true if this transaction manager has data on queue to send and
        ///          false otherwise</returns>
        public bool HasDataToSend
        {
            get
            {
                lock (_transactionsToSendLock)
                {
                    return (_transactionsToSend.Count != 0);
                }
            }
        }

        private OutgoingDataValidator _outgoingDataValidator = new OutgoingDataValidator();

        /// <summary>
        /// Method used by the connection object to retrieve a byte array of data
        /// to be sent by the connection.
        ///
        /// 3 mechanisms are active here:
        /// - 1 piggyback multiple transactions to send into the byte array.
        /// - 2 split large data over multiple byte-array blocks
        /// - 3 interrupt transactions that contain endline-data in the content
        /// 	(and split into multiple transactions, using the validator).
        /// 
        /// It is also at this level that the sending of bytes is accounted for
        /// purposes of triggering the sendUpdateStatus and the prioritiser
        /// </summary>
        /// <param name="outData">the byte array to fill with data to be sent</param>
        /// <returns>the number of bytes filled on outData</returns>
        public int GetDataToSend(byte[] outData)
        {
            int byteCounter = 0;

            //Variable that accounts the number of bytes per transaction sent
            int bytesToAccount = 0;

            lock (_transactionsToSendLock)
            {
                while (byteCounter < outData.Length && HasDataToSend)
                {
                    Transaction t = _transactionsToSend[0];
                    _outgoingDataValidator.Init(t.TransactionId);

                    bool stopTransmission = false;
                    while (byteCounter < outData.Length && !stopTransmission)
                    {
                        if (t.HasData)
                        {
                            // if we are still transmitting data
                            int result = t.GetData(outData, byteCounter);
                            byteCounter += result;
                            bytesToAccount += result;
                        }
                        else
                        {
                            // Let's check to see if we should transmit the end of line
                            if (t.HasEndLine)
                            {
                                /*
                                the first time we get here we should check if the
                                end-line was found, and if it was we should rewind,
                                interrupt the transaction and set the bytesToAccount
                                back the appropriate positions and also the index i
                                we can also do the reset of the outgoingDataValidator
                                because we have for certain that the end-line won't
                                appear again on the content before the transaction
                                finishes
                                */
                                _outgoingDataValidator.Parse(outData, byteCounter);
                                _outgoingDataValidator.Reset();
                                if (_outgoingDataValidator.DataHasEndLine)
                                {
                                    int rewindAmount = _outgoingDataValidator.Amount2Rewind();
                                    t.Rewind(rewindAmount);
                                    t.Interrupt();
                                    byteCounter -= rewindAmount;
                                    bytesToAccount -= rewindAmount;
                                    continue;
                                }

                                int nrBytes = t.GetEndLine(outData, byteCounter);
                                byteCounter += nrBytes;
                                bytesToAccount += nrBytes;
                            }
                            else
                            {
                                // Removing the given transaction from the queue of
                                // transactions to send
                                RemoveTransactionToSend(t);

                                // we should also reset the outgoingDataValidator, so
                                // that future calls to the parser won't misjudge the
                                //correct end-line as an end-line on the body content.
                                _outgoingDataValidator.Reset();

                                stopTransmission = true; // let's get the next transaction if any
                            }
                        }
                    }// end of transaction while

                    stopTransmission = false;
                    /*
                    the buffer is full and or the transaction has been removed from
                    the list of transactions to send and if that was the case the
                    outgoingValidator won't make a false positive because it has been
                    reset
                    */
                    _outgoingDataValidator.Parse(outData, byteCounter);
                    if (_outgoingDataValidator.DataHasEndLine)
                    {
                        int rewindAmount = _outgoingDataValidator.Amount2Rewind();
                        t.Rewind(rewindAmount);
                        t.Interrupt();
                        byteCounter -= rewindAmount;
                        bytesToAccount -= rewindAmount;

                        int nrBytes = t.GetEndLine(outData, byteCounter);
                        byteCounter += nrBytes;
                        bytesToAccount += nrBytes;

                        RemoveTransactionToSend(t);
                        _outgoingDataValidator.Reset();
                    }

                    // account for the bytes sent from this transaction if they should
                    // be accounted for
                    if (!t.IsIncomingResponse && t.TransactionType == TransactionType.SEND && !t.HasResponse)
                    {
                        // reporting the sent update status seen that this is an
                        // outgoing send request
                        OutgoingMessage transactionMessage = (OutgoingMessage)t.Message;
                        if (transactionMessage != null)
                        {
                            transactionMessage.ReportMechanism.CountSentBodyBytes(transactionMessage, bytesToAccount);
                            bytesToAccount = 0;
                        }
                    }
                }// end of main while, the one that goes across transactions
            }

            return byteCounter;
        }

        /// <summary>
        /// Method used only for automatic test purposes
        ///
        /// See #existingTransactions
        /// </summary>
        /// <returns>the collection of the values of the existingTransactions variable</returns>
        protected List<Transaction> GetExistingTransactions()
        {
            List<Transaction> result = new List<Transaction>();

            foreach (string s in _existingTransactions.Keys)
            {
                result.Add(_existingTransactions[s]);
            }

            return result;

            //return existingTransactions;
        }

        /// <summary>   
        /// See #existingTransactions
        /// </summary>
        /// <param name="tid">the String with the transaction id of the desired transaction</param>
        /// <returns>the transaction with transaction id given, if it exists on the
        ///          existingTransactions Hashmap, or null otherwise</returns>
        public Transaction GetTransaction(string tid)
        {
            Transaction transaction = null;
            if (_existingTransactions.Keys.Contains(tid)) { transaction = _existingTransactions[tid]; }

            return transaction;
        }

        /// <summary>
        /// Inserts transaction to send at the first interruptible spot in the queue.
        /// Or appends it when a transaction is being processed and interrupts that
        /// transaction.
        ///
        /// It's responsible for appropriate queueing of REPORT and responses
        /// Throws IllegalUseException if the transaction argument is invalid
        /// </summary>
        /// <param name="transaction">the REPORT or response transaction</param>
        public void AddPriorityTransaction(Transaction transaction)
        {
            // sanity check, shouldn't be needed:
            if (transaction.TransactionType != TransactionType.RESPONSE && transaction.TransactionType != TransactionType.REPORT)
            {
                throw new IllegalUseException("the addPriorityTransaction was called with a transaction that isn't a response/REPORT");
            }

            if (transaction.Direction != Direction.OUT)
            {
                throw new IllegalUseException(string.Format("the addPriorityTransaction was called with an invalid direction transaction, direction: ", transaction.Direction));
            }

            // Make sure that this response doesn't put itself ahead of other
            // priority transactions:
            lock (_transactionsToSendLock)
            {
                for (int i = 0; i < _transactionsToSend.Count; i++)
                {
                    Transaction t = _transactionsToSend[i];
                    if (t.IsInterruptible)
                    {
                        if (i == 0 && t.HasSentData) 
                        { 
                            AddTransactionToSend(transaction, 1);
                            t.Interrupt();
                        }
                        else { AddTransactionToSend(transaction, i); }

                        return;
                    }
                }
            }

            // No interruptible transactions to send, just add the one given.
            AddTransactionToSend(transaction, UNIMPORTANT);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void InterruptMessage(Message message)
        {
    	    lock(_transactionsToSendLock)
    	    {
	            foreach(Transaction t in _transactionsToSend)
                {
                    if (t.TransactionType == TransactionType.SEND && t.Message == message && t.IsInterruptible)
                    {
                        t.Interrupt();
                    }
                }
    	    }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        public void AbortMessage(Message message)
        {
    	    lock(_transactionsToSendLock)
    	    {
    		    bool first = true;
                foreach (Transaction t in _transactionsToSend)
                {
                    if (t.TransactionType == TransactionType.SEND && t.Message == message)
                    {
                        _logger.Debug(string.Format("{0} aborted", t.ToString()));
                        if (first)
                        {
                            t.Abort();
                            first = false;
                        }
                        else { RemoveTransactionToSend(t); }
                    }
                }
    	    }
        }
    }

    /// <summary>
    /// Class used to validate the outgoing data to what concerns the transaction
    /// id validation.
    /// 
    /// This class gives the needed methods to check if there is an end-line on
    /// the body of the transaction currently being sent or not.
    /// 
    /// As written in RFC 4975: " If the request contains a body, the sender MUST
    /// ensure that the end- line (seven hyphens, the transaction identifier, and
    /// a continuation flag) is not present in the body. [...] Some
    /// implementations may choose to scan for the closing sequence as they send
    /// the body, and if it is encountered, simply interrupt the chunk at that
    /// point and start a new transaction with a different transaction identifier
    /// to carry the rest of the body."
    ///
    /// The approach of interrupting the ongoing transaction and create a new one
    /// was the one chosen and implemented by this library
    /// </summary>
    public class OutgoingDataValidator
    {

        /// <summary>
        /// This variable is true if the end-line was found and false otherwise.
        /// Calls to the dataHasEndLine reset this variable to false.
        /// 
        /// See #dataHasEndLine()
        /// </summary>
        private bool _foundEndLine = false;

        /// <summary>
        /// It contains the number of bytes we should rewind the read offsets of
        /// the transaction
        /// </summary>
        private int _toRewind = 0;

        /// <summary>
        /// Variable used to store the transaction ID that this class is using to
        /// look for the end-line
        /// </summary>
        private string _transactionId = null;

        /// <summary>
        /// Variable used by the method parse to assert in which state the parser
        /// is so that we can save the state of this state machine between calls
        /// </summary>
        private short _state = 0;

        /// <summary>
        /// Assert if the data we have so far contains the end-line
        /// 
        /// NOTE: This method resets the hasEndLine variable so by doing
        /// two consecutive calls to this method the result of the second
        /// can never be true.
        /// See #foundEndLine
        /// </summary>
        /// <returns>true if the data parsed so far has the end of line or false
        ///          otherwise</returns>
        public bool DataHasEndLine
        {
            get
            {
                bool auxBoolean = _foundEndLine;
                _foundEndLine = false;
                return auxBoolean;
            }
        }

        /// <summary>
        /// Method used to initialize this class
        /// </summary>
        /// <param name="transactionId">the transaction id to be used in the search of
        ///                             the end-line</param>
        public void Init(string transactionId)
        {
            _transactionId = transactionId;
        }

        /// <summary>
        /// Method used to reset the outgoingValidator. After this method future
        /// calls to the parse won't parse anything before a call to the init
        /// method is done
        /// </summary>
        public void Reset()
        {
            _transactionId = null;
        }

        /// <summary>
        /// This method is used to parse the data to be searched for the end line
        /// characters
        /// 
        /// throws ImplementationException if it was detected that this method
        /// was used in an incorrect way
        /// </summary>
        /// <param name="outputData">the data to be parsed and searched for the end line
        ///                          string</param>
        /// <param name="length">how many bytes of the outputData vector should be
        ///                      searched</param>
        public void Parse(byte[] outputData, int length)
        {
            if (_transactionId == null) { return; }

            if (outputData.Length < length)
            {
                throw new ImplementationException("method called with argument length too big");
            }

            // if we found already the end of line and haven't reset the value
            // with a call to hasEndLine and we call the parse that generates an
            // ImplementationException
            if (_foundEndLine)
            {
                throw new ImplementationException("Error, bad use of the class outgoingDataValidator on TransactionManager, after calling parse a call should always be made to the dataHasEndLine");
            }

            for (int i = 0; i < length; i++)
            {
                switch (_state)
                {
                    case 0:
                        if (outputData[i] == '-') { _state++; }
                        break;
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                    case 6:
                        if (outputData[i] == '-') { _state++; }
                        else { _state = 0; }
                        break;
                    default:
                        if (_state >= 7)
                        {
                            if ((_state - 7) < _transactionId.Length && outputData[i] == _transactionId[_state - 7]) { _state++; }
                            else if ((_state - 7 >= _transactionId.Length && (outputData[i] == '$' || outputData[i] == '#' || outputData[i] == '+')))
                            {
                                _foundEndLine = true;
                                _toRewind = length - i + _state;

                                //if we had a end-line splitted by buffers we
                                // rewind to the beginning of the data in this
                                // buffer and then interrupt the transaction
                                if (_toRewind > length) { _toRewind = length; }

                                _state = 0;
                            }
                            else { _state = 0; }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// this method returns the number of positions we should rewind
        /// on the buffer and on the transaction's read offset before we
        /// interrupt the current transaction
        /// </summary>
        /// <returns></returns>
        public int Amount2Rewind()
        {
            return _toRewind;
        }
    }
}