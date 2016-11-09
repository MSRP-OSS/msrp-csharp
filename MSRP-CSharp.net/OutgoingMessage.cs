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

namespace MSRP
{
    /// <summary>
    /// Generic outgoing MSRP message
    /// </summary>
    public class OutgoingMessage : Message
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(OutgoingMessage));

        /// <summary>
        /// 
        /// </summary>
        override public string MessageId
        {
            get
            {
                if (base.MessageId == null || base.MessageId.Length < 1)
                {
                    base.MessageId = MSRPStack.GetInstance().GenerateMessageId();
                }

                return base.MessageId;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="data"></param>
        public OutgoingMessage(string contentType, byte[] data)
            : this() 
        {
            if (contentType != null)
            {
                ContentType = contentType;
                DataContainer = new MemoryDataContainer(data);
                Size = data.Length;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="contentType"></param>
        /// <param name="filePath"></param>
        public OutgoingMessage(string contentType, string filePath)
            :this()
        {
            if (contentType == null || contentType.Length < 1)
                throw new IllegalUseException("Content-type must be specified");
            ContentType = contentType;
            DataContainer = new FileDataContainer(filePath);
            Size = DataContainer.Size;
        }

        /// <summary>
        /// Constructor for MSRP Nickname
        /// </summary>
        /// <param name="sendingSession"></param>
        /// <param name="nickname"></param>
        public OutgoingMessage(string nickname)
            : this()
        {
            Nickname = nickname;
        }

        /// <summary>
        /// Constructor used internally
        /// </summary>
        protected OutgoingMessage() : base(Direction.OUT) { }

        /// is not being sent it won't be anymore. If the message is being sent, the
        /// <summary>
        /// This method is intended to abort a message's send process. If the message
        /// current SEND transaction will end with the # continuation-flag char and
        /// further data belonging to the message will not be sent. On the other end,
        /// if the message is being sent, receiving the # continuation-flag will
        /// trigger a call to the abortedMessageEvent method on MSRPSessionListener
        /// binded to the session.
        /// MSRPSessionListener <see cref="abortedMessageEvent(msrp.event.MessageAbortedEvent())"/>
        /// </summary>
        /// <param name="reason">Irrelevant for an OutgoingMessage</param>
        /// <param name="extraReasonInfo">Irrelevant for an OutgoingMessage</param>
        override public void Abort(ResponseCodes reason, string extraReasonInfo)
        {
            _logger.Debug(string.Format("Going to abort an OutgoingMessage, reason: {0} comment: {1}", reason, extraReasonInfo));

            if (Session == null) { throw new InternalError("Abort() called on message with no session."); }

            TransactionManager transactionManager = Session.TransactionManager;
            if (transactionManager == null) { throw new InternalError("Abort() called on message with no transaction manager."); }

            // internally signal this message as aborted
            _aborted = true;

            // remove this message from the list of messages to send of the session
            Session.DelMessageToSend(this);

            transactionManager.AbortMessage(this);
        }

        /// <summary>
        /// Returns the sent bytes determined by the offset of the data container
        /// </summary>
        /// <returns>the number of sent bytes</returns>
        public long SentBytes
        {
            get
            {
                return DataContainer != null ? DataContainer.CurrentReadOffset : 0;
            }
        }

        /// <summary>
        /// msrp.Message <see cref="isComplete()"/>
        /// </summary>
        /// <returns></returns>
        override public bool IsComplete
        {
            get
            {
                if (_logger.IsDebugEnabled) { _logger.Debug(string.Format("IsOutgoingComplete({0}, sent[{1}])? {2}", SentBytes, Size, SentBytes == Size)); }

                return SentBytes == Size;
            }
        }

        /// <summary>
        /// Interrupts all of the existing and interruptible SEND request
        /// transactions associated with this message that are on the transactions to
        /// be sent queue, and gets this message back on top of the messages to send
        /// queue of the respective session.
        /// 
        /// This method is meant to be called internally by the ConnectionPrioritizer.
        /// 
        /// Throws InternalErrorException if this method, that is called internally,
        /// was called with the message in an invalid state
        /// </summary>
        internal void Pause()
        {
            // Sanity checks:
            if (IsComplete) { throw new InternalError("Pause() was called on a complete message!"); }
            if (Session == null) { throw new InternalError("Pause() called on message with no session."); }

            TransactionManager transactionManager = Session.TransactionManager;
            if (transactionManager == null) { throw new InternalError("The transaction manager associated with this message is null and the pause method was called upon it"); }

            try
            {
                transactionManager.InterruptMessage(this);
            }
            catch (IllegalUseException e)
            {
                throw new InternalError(e);
            }

            // FIXME: How to resume? as this is just re-scheduling....
            // Session.AddMessageOnTop(this);
        }

        /// <summary>
        /// 
        /// </summary>
        override public Message Validate()
        {
            return this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("OutMsg({0})", MessageId == null || MessageId.Length < 1 ? "new" : MessageId);
        }
    }
}
