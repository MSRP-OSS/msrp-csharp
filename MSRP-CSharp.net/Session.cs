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
using MSRP.Java;
using System.Net;
using System.IO;
using MSRP.Wrappers;

namespace MSRP
{
    /// <summary>
    /// An MSRP Session.
    /// 
    /// This interface, combined with the {@code MSRPSessionListener} is the primary
    /// interface for sending and receiving MSRP traffic. 
    /// The class contains a list of MSRP Messages with which it's currently
    /// associated.
    /// </summary>
    public class Session
    {
        private string _id = null;
        // RFC 3994 support: indication of message composition.
        private ImState _isComposing = ImState.idle;
        // what am I composing?			*/
        private string _composeContentType;
        // timestamp last active compose
        private DateTime _lastActive;
        // After this time, a refresh may be sent
        private DateTime _activeEnd;
        private long _refresh = 0;

        /// <summary>
        /// The Id of the Session
        /// </summary>
        public string Id 
        {
            get
            {
                if (_id == null)
                {
                    _id = Uri.LocalPath.Substring(1, Uri.LocalPath.IndexOf(';') - 1);
                }

                return _id;
            }
        }

        public ImState ImState 
        { 
            get 
            {
                if (_isComposing == ImState.active && _activeEnd <= DateTime.Now)
                {
                    EndComposing();
                }

                return _isComposing; 
            } 
        }

        /// <summary>
        /// Sets composing to IDLE, this is needed so new ACTIVE messages can be send
        /// </summary>
        private void EndComposing()
        {
            _isComposing = MSRP.ImState.idle;
            _activeEnd = DateTime.Now;
        }

        public DateTime LastActive { get { return _lastActive; } }

        /// <summary>
        /// Indicate on session that chatter is composing a message.
        /// An active message indication will be sent when appropriate.
        /// </summary>
        /// <param name="contentType">the type of message being composed.</param>
        public void SetActive(string contentType)
        {
            SetActive(contentType, 120);
        }

        /// <summary>
        /// Indicate on session that chatter is composing a message.
        /// An active message indication will be sent when appropriate.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="refresh"></param>
        public void SetActive(string contentType, int refresh)
        {
            if (contentType == null || contentType == string.Empty) { throw new IllegalUseException("Content-Type must be a valid string"); }

            _composeContentType = contentType;

            if (ShouldActiveTransitionBeSent(refresh))
            {
                SendMessage(new OutgoingStatusMessage(this, _isComposing, _composeContentType, refresh));
            }
        }

        /// <summary>
        /// Sets active and determines if a new send must be performed
        /// </summary>
        /// <param name="refresh"></param>
        /// <returns></returns>
        private bool ShouldActiveTransitionBeSent(int refresh)
        {
            DateTime now = DateTime.Now;
            _isComposing = ImState.active;
            _lastActive = now;

            if ((_activeEnd < now) || (_refresh != 0 && _activeEnd.AddSeconds((0 -_refresh / 2)) <= now))
            {
                if (refresh < 60) { refresh = 60; } // SHOULD not be allowed

                _refresh = refresh;
                _activeEnd = _lastActive.AddSeconds(refresh);
                return true;
            }
            return false;
        }

        /// <summary>
        /// The conferencing-version of {@link #setActive(String, int)}. The
	    /// indication will be wrapped within message/CPIM to retain conference
	    /// participant information.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="refresh"></param>
        /// <param name="from">rom-field content of the wrapped indication</param>
        /// <param name="to">to-field content of the wrapped indication</param>
        public void SetActive(String contentType, int refresh, string from, string to)
        {
            if (contentType == null || contentType.Length == 0) { throw new IllegalUseException("Content-Type must be a valid string"); }

            _composeContentType = contentType;

            if (ShouldActiveTransitionBeSent(refresh))
            {
                SendMessage(new OutgoingStatusMessage(this, _isComposing, _composeContentType, refresh, from, to));
            }
        }

        /// <summary>
        /// Same as SetActive(String, int, String, String)} but with a
        /// default refresh period of 120 sec.
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void SetActive(string contentType, string from, string to)
        {
            SetActive(contentType, 120, from, to);
        }

        /// <summary>
        /// Indicate on session that chatter is idle.
        /// An idle message indication will be sent when appropriate.
        /// </summary>
        public void SetIdle()
        {
            if (_isComposing == ImState.active)
            {
                EndComposing();
                SendMessage(new OutgoingStatusMessage(this, MSRP.ImState.idle, string.Empty, 0));
            }
        }

        /// <summary>
        /// The conferencing-version of {@link #setIdle()}.
	    /// The idle indication will be wrapped within message/CPIM to retain
	    /// conference participant information.
        /// </summary>
        /// <param name="from">from-field content of the wrapped indication</param>
        /// <param name="to">to-field content of the wrapped indication</param>
        public void SetIdle(string from, string to)
        {
            if (_isComposing == ImState.active)
            {
                EndComposing();
                SendMessage(new OutgoingStatusMessage(this, _isComposing, _composeContentType, 0, from, to));
            }
        }

        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(Session));

        /// <summary>
        /// Associates an interface to the session, used to process incoming messages
        /// </summary>
        private IMSRPSessionListener _myListener;

        /// <summary>
        /// 
        /// </summary>
        private MSRPStack _stack = MSRPStack.GetInstance();

        /// <summary>
        /// 
        /// </summary>
        private List<Uri> _toPath = new List<Uri>();
        public List<Uri> ToPath { get { return _toPath; } }

        /// <summary>
        /// 
        /// </summary>
        public TransactionManager TransactionManager { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public InetAddress LocalAddress { get; private set; }

        public bool IsSecure { get; private set; }

        public bool IsRelay { get; private set; }

        /// <summary>
        /// @desc the connection associated with this session
        /// @uml.property name="_connectoin"
        /// @uml.associationEnd inverse="_session:msrp.Connection"
        /// </summary>
        public MSRP.Connection Connection { get; set; }

        /// <summary>
        /// The queue of messages to send.
        ///
        /// @uml.property name="sendQueue"
        /// </summary>
        private List<Message> _sendQueue = new List<Message>();

        /// <summary>
        /// @uml.property name="_messagesSent" stores the sent/being sended messages
        /// according to the Success-Report field
        /// </summary>
        private Dictionary<string, Message> _messagesSentOrSending = new Dictionary<string, Message>();

        /// <summary>
        /// contains the messages being received
        /// </summary>
        private Dictionary<string, Message> _messagesReceive = new Dictionary<string, Message>();

        /// <summary>
        /// @uml.property name="_URI" the URI that identifies this session
        /// </summary>
        public Uri Uri { get; private set; }

        /// <summary>
        /// this field points to the report mechanism associated with this session
        /// the report mechanism basically is used to decide upon the granularity of
        /// the success reports
        /// 
        /// it will use the DefaultReportMechanism
        /// 
        /// See DefaultReportMechanism
        /// </summary>
        private ReportMechanism _reportMechanism = DefaultReportMechanism.GetInstance();

        /// <summary>
        /// This'll enable you to define your own granularity.
        /// </summary>
        public ReportMechanism ReportMechanism { get { return _reportMechanism; } set { _reportMechanism = value; } }

        /// <summary>
        /// Is session still valid (active)?
        /// 
        /// at this point this is used by the generation of the success report to
        /// assert if it should be sent or not quoting the RFC:
        /// 
        /// "Endpoints SHOULD NOT send REPORT requests if they have reason to believe
        /// the request will not be delivered. For example, they SHOULD NOT send a
        /// REPORT request for a session that is no longer valid."
        /// </summary>
        /// <returns>true or false depending if this is a "valid" (active?!) session
        /// or not</returns>
        public bool IsActive
        {
            get
            {
                // TODO implement some check.
                return true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if this session has messages to send false otherwise</returns>
        public bool HasMessagesToSend
        {
            get
            {
                return _sendQueue != null && _sendQueue.Count != 0;
            }
        }

        /// <summary>
        /// Create a session with the local address.
        /// The associated connection will be an active one.
        /// 
        /// Throws InternalErrorException if any error ocurred. More info about the
        /// error in the accompanying Throwable.
        /// </summary>
        /// <param name="isSecure">Is it a secure connection or not (use TLS)?</param>
        /// <param name="isRelay">is this a relaying session?</param>
        /// <param name="address">the address to use as local endpoint.</param>
        /// <returns></returns>
        public static Session Create(bool isSecure, bool isRelay, InetAddress address)
        {
            return new Session(isSecure, isRelay, address);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isSecure"></param>
        /// <param name="isRelay"></param>
        /// <param name="address"></param>
        Session(bool isSecure, bool isRelay, InetAddress address)
        {
            LocalAddress = address;
            IsSecure = isSecure;
            IsRelay = isRelay;

            try
            {
                Connection = new Connection(address._addr);

                // Generate new URI and add to list of connection-URIs.
                Uri = Connection.GenerateNewURI();
                _stack.AddConnection(Uri, Connection);

                _logger.Debug(string.Format("MSRP Session created: secure?[{0}], relay?[{1}] InetAddress: {2}", isSecure, isRelay, address));
            }
            catch (Exception e) // wrap exceptions to InternalError
            {
                throw new InternalError(e);
            }
        }

        /// <summary>
        /// Creates a session with the local address
        /// The associated connection will be a passive one.
        /// 
        /// Throws InternalErrorException if any error ocurred. More info about the
        /// error in the accompanying Throwable.
        /// </summary>
        /// <param name="isSecure">Is it a secure connection or not (use TLS)?</param>
        /// <param name="isRelay">is this a relaying session?</param>
        /// <param name="toUri">the destination URI that will contact this session.</param>
        /// <param name="address">the address to use as local endpoint.</param>
        /// <returns></returns>
        public static Session Create(bool isSecure, bool isRelay, Uri toUri, InetAddress address)
        {
            return new Session(isSecure, isRelay, toUri, address);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isSecure"></param>
        /// <param name="isRelay"></param>
        /// <param name="toURI"></param>
        /// <param name="address"></param>
        Session(bool isSecure, bool isRelay, Uri toURI, InetAddress address)
        {
            LocalAddress = address;
            IsSecure = isSecure;
            IsRelay = isRelay;

            try
            {
                Connection = MSRPStack.GetConnectionsInstance(address);
                Uri = ((Connections)Connection).GenerateNewUri();
                _stack.AddConnection(Uri, Connection);
            }
            catch (Exception e) // wrap exceptions to InternalError
            {
                _logger.Error("Error creating Connections: ", e);
                throw new InternalError(e);
            }

            ((Connections)Connection).AddUriToIdentify(Uri, this);
            _toPath.Add(toURI);

            _logger.Debug(string.Format("MSRP Session created: secure?[{0}], relay?[{1}], toURI=[{2}], InetAddress:", isSecure, isRelay, address));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public string ToString()
        {
            return string.Format("[session:{0}]", Id);
        }

        /// <summary>
        /// Add a listener to this session to catch any incoming traffic.
        /// </summary>
        /// <param name="listener">the session listener</param>
        public void AddListener(IMSRPSessionListener listener)
        {
            if (listener != null && listener is IMSRPSessionListener)
            {
                _myListener = listener;
                _logger.Debug(string.Format("MSRP Session Listener added to Session: {0}", this));
            }
            else
            {
                _logger.Debug(string.Format("Listener could not be added to Session: {0} because it didn't match the criteria", this));
            }
        }

        /// <summary>
        /// Sets the given uri's and establish the connection according RFC.
        /// 
        /// Throws IOException if there was a connection problem.
        /// </summary>
        /// <param name="uris">the to-path to use.</param>
        public void SetToPath(List<Uri> uris)
        {
            if (ToPath != null && ToPath.Count > 0) { throw new IllegalUseException("SetToPath can only be called once!"); }

            foreach (Uri uri in uris)
            {
                if (RegexMSRP.IsMsrpUri(uri)) { ToPath.Add(uri); }
                else
                {
                    throw new IllegalUseException(string.Format("Invalid To-URI: {0}", uri));
                }
            }
            Connection.AddEndPoint(GetNextURI(), LocalAddress);

            TransactionManager = Connection.TransactionManager;
            TransactionManager.AddSession(this);
            TransactionManager.Initialize(this);

            _stack.AddActiveSession(this);

            _logger.Debug(string.Format("Added {0} toPaths with URI[0]={1}", _toPath.Count.ToString(), uris[0].ToString()));
        }

        /// <summary>
        /// Send a bodiless message (keep-alive).
        /// </summary>
        public void SendAliveMessage()
        {
            SendMessage(new OutgoingAliveMessage());
        }

        /// <summary>
        /// send the given content over this session.
        /// 
        /// Throws IllegalUseException
        /// </summary>
        /// <param name="contentType">the type of content.</param>
        /// <param name="content">the content itself</param>
        /// <returns>the message-object that will be send, can be used
        ///          to abort large content.</returns>
        public OutgoingMessage SendMessage(string contentType, byte[] content)
        {
            return SendMessage(new OutgoingMessage(contentType, content));
        }

        /// <summary>
        /// Request the given nickname to be used with this session.
	    /// The nickname request will be send to the chatroom at the other end of 
	    /// this session.
	    ///
	    /// A result will be reported in SessionListener.ReceivedNickNameResult(Session, TransactionResponse)
        /// </summary>
        /// <param name="nickname">the name to use</param>
        /// <returns>the actual msrp request that is sent out</returns>
        public OutgoingMessage RequestNickname(string nickname)
        {
            return SendMessage(new OutgoingMessage(nickname));
        }

        /// <summary>
        /// Wrap the given content in another type and send over this session.
        /// </summary>
        /// <param name="wrapType">the (mime-)type to wrap it in.</param>
        /// <param name="from">from-field</param>
        /// <param name="to">to-field</param>
        /// <param name="contentType">the (mime-)type to wrap.</param>
        /// <param name="content">actual content</param>
        /// <returns>the message-object that will be send, can be used
        ///     to abort large content.</returns>
        public OutgoingMessage SendWrappedMessage(string wrapType, string from, string to, string contentType, byte[] content)
        {
            Wrap wrap = Wrap.GetInstance();
            if (wrap.IsWrapperType(wrapType))
            {
                IWrappedMessage wm = wrap.GetWrapper(wrapType);
                return SendMessage(new OutgoingMessage(wrapType, wm.Wrap(from, to, contentType, content)));
            }
            return null;
        }

        /// <summary>
        /// send the given file over this session.
        /// </summary>
        /// <param name="contentType">the type of file.</param>
        /// <param name="filePath">the file itself</param>
        /// <returns>the message-object that will be send, can be used
        ///          to abort large content.</returns>
        public OutgoingMessage SendMessage(string contentType, string filePath)
        {
            return SendMessage(new OutgoingMessage(contentType, filePath));
        }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public OutgoingMessage SendMessage(OutgoingMessage message)
        {
            message.Session = this;
            if (message.HasData && !(message is OutgoingStatusMessage)) { EndComposing(); }
            if (message.ContentType != null) { AddMessageToSend(message); }
            else { AddMessageOnTop(message); }

            return message;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        public void SendNickResult(Transaction request)
        {
            SendNickResult(request, ResponseCodes.RC200, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseCode"></param>
        /// <param name="comment"></param>
        public void SendNickResult(Transaction request, ResponseCodes responseCode, string comment)
        {
            if (request == null) { throw new InvalidOperationException("Null transaction specified"); }
            if (request is TransactionResponse)
            {
                request.TransactionManager.AddPriorityTransaction(request);
            }
            else
            {
                request.TransactionManager.GenerateResponse(request, responseCode, comment);
            }
        }

        /// <summary>
        /// Release all of the resources associated with this session.
        /// It could eventually, but not necessarily, close connections conforming to
        /// RFC 4975.
        /// After teardown, this session can no longer be used.
        /// </summary>
        public void TearDown()
        {
            // clear local resources
            _toPath = null;

            if (_sendQueue != null)
            {
                _sendQueue.ForEach(m => m.Discard());
                _sendQueue = null;
            }

            if (TransactionManager != null)
            {
                TransactionManager.RemoveSession(this);
                TransactionManager = null;
            }
            // FIXME: (msrp-31) allow connection reuse by sessions.
            if (Connection != null)
            {
                Connection.Close();
                Connection = null;
            }
            if (_stack != null)
            {
                _stack.RemoveActiveSession(this);
                _stack = null;
            }
        }

        /// <summary>
        /// Adds the given message to the top of the message to send queue
        ///
        /// this method is used when a message sending is paused so that when this
        /// session activity get's resumed it will continue sending this message
        /// </summary>
        /// <param name="message">the message to be added to the top of the message queue</param>
        internal void AddMessageOnTop(Message message)
        {
            if (_sendQueue != null) 
            { 
                _sendQueue.Insert(0, message);
                TriggerSending();
            }
        }

        /// <summary>
        /// Adds the given message to the end of the message to send queue.
        /// Kick off when queue is empty.
        /// </summary>
        /// <param name="message">the message to be added to the end of the message queue</param>
        internal void AddMessageToSend(Message message)
        {
            if (_sendQueue != null)
            {
                _sendQueue.Add(message);
                TriggerSending();
            }
        }

        /// <summary>
        /// Have txManager send awaiting messages from session.
        /// </summary>
        private void TriggerSending()
        {
            if (TransactionManager != null)
            {
                while (HasMessagesToSend)
                {
                    TransactionManager.GenerateTransactionsToSend(GetMessageToSend());
                }
            }
        }

        /// <summary>
        /// Returns and removes first message of the top of sendQueue
        /// </summary>
        /// <returns>first message to be sent from sendQueue</returns>
        public Message GetMessageToSend()
        {
            if (_sendQueue == null || _sendQueue.Count == 0) { return null; }

            Message messageToReturn = _sendQueue[0];

            _sendQueue.Remove(messageToReturn);

            return messageToReturn;
        }

        /// <summary>
        /// Delete message from the send-queue.
        /// </summary>
        /// <param name="message"></param>
        public void DelMessageToSend(Message message)
        {
            if (_sendQueue != null) { _sendQueue.Remove(message); }
        }

        /// <summary>
        /// retrieves a message from the sentMessages The sentMessages array may have
        /// messages that are currently being sent they are only stored for REPORT
        /// purposes.
        /// </summary>
        /// <param name="messageId">messageID of the message to retrieve</param>
        /// <returns>the message associated with the messageID</returns>
        public Message GetSentOrSendingMessage(string messageId)
        {
            return messageId != null && _messagesSentOrSending.Keys.Contains(messageId) ? _messagesSentOrSending[messageId] : null;
        }


        /// <summary>
        /// method used by an incoming transaction to retrieve the message object
        /// associated with it, if it's already being received
        /// </summary>
        /// <param name="messageId">messageID of the message to</param>
        /// <returns>the message being received associated with messageID or null if
        ///          there is none</returns>
        public Message GetReceivingMessage(string messageId)
        {
            return messageId != null && _messagesReceive.Keys.Contains(messageId) ? _messagesReceive[messageId] : null;
        }

        /// <summary>
        /// Put a message on the list of messages being received by this session.
        ///
        /// FIXME: in the future just put the queue of messages
        /// being received on the Stack as the Message object isn't necessarily bound
        /// to the Session
        /// </summary>
        /// <param name="message">the message to be put on the received messages queue</param>
        public void PutReceivingMessage(IncomingMessage message)
        {
            _messagesReceive.Add(message.MessageId, message);
        }

        /*
        Triggers to the Listener, not really sure if they are needed now, but
        later can be used to trigger some extra validations before actually
        calling the callback or cleanup after
        */

        /// <summary>
        /// trigger for the registered {@code MSRPSessionListener} callback.
        /// </summary>
        /// <param name="report">the transaction associated with the report request</param>
        public void TriggerReceivedReport(Transaction report)
        {
            _logger.Debug("Called the triggerReceivedReport hook");

            _myListener.ReceivedReport(this, report);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="response"></param>
        public void TriggerReceivedNickResult(TransactionResponse response)
        {
            _logger.Debug("Called the triggerReceivedNickResult hook");
            _myListener.ReceivedNickNameResult(this, response);
        }

        /// <summary>
        /// trigger for the registered {@code MSRPSessionListener} callback.
        /// </summary>
        /// <param name="message">the received message</param>
        public void TriggerReceiveMessage(Message message)
        {
            _logger.Debug("Called the triggerReceiveMessage hook");

            _myListener.ReceiveMessage(this, message);

            if (HasMessagesToSend) { TriggerSending(); }
        }

        /// <summary>
        /// trigger for the registered {@code MSRPSessionListener} callback.
        /// </summary>
        /// <param name="message">the message to accept or not</param>
        /// <returns>true or false if we are accepting the message or not</returns>
        public bool TriggerAcceptHook(IncomingMessage message)
        {
            _logger.Debug("Called the triggerAcceptHook hook");

            return _myListener.AcceptHook(this, message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        public void TriggerReceivedNickname(Transaction request)
        {
            _logger.Debug("Called the triggerReceivedNickname hook");
            _myListener.ReceivedNickname(this, request);
        }

        /// <summary>
        /// trigger for the registered {@code MSRPSessionListener} callback.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="outgoingMessage"></param>
        public void TriggerUpdateSendStatus(Session session, OutgoingMessage outgoingMessage)
        {
            _logger.Debug("Called the triggerUpdateSendStatus hook");

            _myListener.UpdateSendStatus(session, outgoingMessage, outgoingMessage.SentBytes);
        }


        private object _sessionListenerLock = new object();

        /// <summary>
        /// trigger for the registered {@code abortedMessageEvent} callback.
        /// </summary>
        /// <param name="message">the msrp message that was aborted</param>
        /// <param name="reason">the reason</param>
        /// <param name="extraReasonInfo">the extra information about the reason if any is
        ///                               present (it can be transported on the body of a REPORT
        ///                               request)</param>
        /// <param name="transaction">the transaction associated with the abort event</param>
        public void FireMessageAbortedEvent(Message message, ResponseCodes reason, string extraReasonInfo, Transaction transaction)
        {
            _logger.Debug("Called the fireMessageAbortedEvent");

            MessageAbortedEvent abortedEvent = new MessageAbortedEvent(message, this, reason, extraReasonInfo, transaction);
            IMSRPSessionListener sessionListener;

            lock (_sessionListenerLock)
            {
                sessionListener = _myListener;
            }

            sessionListener.AbortedMessageEvent(abortedEvent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cause"></param>
        public void TriggerConnectionLost(Exception cause)
        {
            _logger.Debug("triggerConnectionLost() called");
            _myListener.ConnectionLost(this, cause);
        }

        // End of triggers to the Listener

        /// <summary>
        /// Adds a message to the sent message list. Stored because of
        /// expected subsequent REPORT requests on this message
        /// </summary>
        /// <param name="message">the message to add</param>
        public void AddSentOrSendingMessage(Message message)
        {
            _messagesSentOrSending.Add(message.MessageId, message);
        }

        /// <summary>
        /// Delete a message that stopped being received from the
        /// being-received-queue of the Session.
        /// 
        /// NOTE: currently only called for {@code IncomingMessage} objects
        /// </summary>
        /// <param name="incomingMessage">the message to be removed</param>
        public void DelMessageToReceive(IncomingMessage message)
        {
            if (!_messagesReceive.Remove(message.MessageId))
            {
                _logger.Warn(string.Format("Message to receive not found nor deleted, id={0}", message.MessageId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Uri GetNextURI()
        {
            return _toPath[0];
        }
    }
}
