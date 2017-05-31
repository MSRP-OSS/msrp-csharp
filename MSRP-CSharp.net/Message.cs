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

namespace MSRP
{
    /// <summary>
    /// Class that represents a generic MSRP message.
    /// </summary>
    public abstract class Message
    {
        /// <summary>
        /// content-type of an isComposing message
        /// </summary>
        public const string IMCOMPOSE_TYPE = "application/im-iscomposing+xml";

        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(Message));

        /// <summary>
        /// 
        /// </summary>
        public const int UNINITIALIZED = -2;

        /// <summary>
        /// 
        /// </summary>
        public const int UNKNOWN = -1;

        /// <summary>
        /// 
        /// </summary>
        public const string YES = "yes";

        /// <summary>
        /// 
        /// </summary>
        public const string NO = "no";

        /// <summary>
        /// 
        /// </summary>
        public const string PARTIAL = "partial";

        /// <summary>
        /// Variable that contains the size of this message as indicated on the
        /// byte-range header field
        /// </summary>
        private long _size = UNINITIALIZED;

        /// <summary>
        /// Note: even if the value is unknown at a first
        /// stage, if this is an incoming message, when the message is
        /// complete this method should report the actual size of the
        /// received message
        /// 
        /// the size (bytes) of the message, -1 if this value is
        /// uninitialized, -2 if the total size is unknown for this message.
        /// </summary>
        public long Size { get { return _size; } set { _size = value; } }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Number of bytes in this message as a string, '*' if unknown.</returns>
        public string SizeString
        {
            get
            {
                return Size == UNKNOWN ? "*" : Size.ToString();
            }
        }

        /// <summary>
        /// @uml.property name="failureReport"
        /// </summary>
        private string _failureReport = YES;

        public string FailureReport
        {
            get
            {
                return _failureReport;
            }
            set
            {
                if (value.ToLower().Trim() == "yes" || value.ToLower().Trim() == "no" || value.ToLower().Trim() == "partial")
                {
                    _failureReport = value.ToLower();
                    return;
                }
                else { throw new IllegalUseException("The failure report must be one of: partial yes no"); }
            }
        }

        /// <summary>
        /// Field used to conserve the abortion state of the message
        /// </summary>
        protected bool _aborted = false;

        /// <summary>
        /// Was message aborted?
        /// 
        /// <see cref="gotAborted()"/>
        /// </summary>
        /// <returns>true if the message was previously aborted (received the #
        ///          continuation flag, called the gotAborted() method) or false
        ///          otherwise</returns>
        public bool WasAborted { get { return _aborted; } }

        /// <summary>
        /// The field that contains the data associated with this message (it allows
        /// abstraction on the actual container of the data)
        /// 
        /// This method must be used by the acceptHook on the listener in order to
        /// allow the message to be received
        /// </summary>
        private DataContainer _dataContainer;
        public DataContainer DataContainer
        {
            get
            {
                return _dataContainer;
            }
            set
            {
                _dataContainer = value;

                if (_logger.IsDebugEnabled)
                {
                    string className = _dataContainer != null ? _dataContainer.GetType().Name : string.Empty;

                    _logger.Debug(string.Format("Altered the data container of Message: {0} to: {1}", MessageId, className));
                }
            }
        }

        /// <summary>
        /// a field that contains the number of bytes sent on the last call made to
        /// the shouldTriggerSentHook in Report Mechanism status
        ///
        /// ReportMechanism <see cref="shouldTriggerSentHook(Message, Session)"/>
        /// ReportMechanism.shouldTriggerSentHook(..)
        /// </summary>
        private long _lastCallSentData = 0;

        public long LastCallSentData { get { return _lastCallSentData; } set { _lastCallSentData = value; } }

        /// <summary>
        /// a field that contains the number of bytes sent on the last call made to
        /// the shouldGenerateReport in Report Mechanism status
        ///
        /// @see ReportMechanism <see cref="shouldGenerateReport(Message, long)"/>
        ///      ReportMechanism.shouldGenerateReport(Message, long)
        /// </summary>
        private long _lastCallReportCount = 0;

        public long LastCallReportCount { get { return _lastCallReportCount; } set { _lastCallReportCount = value; } }

        private ReportMechanism _reportmechanism = DefaultReportMechanism.GetInstance();

        /// <summary>
        /// this field points to the report mechanism associated with this message
        /// the report mechanism basicly is used to decide upon the granularity of
        /// the success reports
        /// </summary>
        public ReportMechanism ReportMechanism
        {
            get { return _reportmechanism; }
            protected set { _reportmechanism = value; }
        }

        /// <summary>
        /// Field to be used by the prioritizer.
        /// 
        /// Default value of 0 means no special priority
        /// 
        /// As an advice use the range -20 to 20 from higher priority to lowest (as
        /// in UNIX processes)
        /// </summary>
        private short _priority = 0;

        /// <summary>
        /// TODO WORKINPROGRESS to be used by the MessagePrioritizer
        /// </summary>
        public long Priority { get; set; }

        /// <summary>
        /// @uml.property name="_contentType"
        /// </summary>
        public string ContentType { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string Content
        {
            get
            {
                if (Direction == Direction.OUT || (Direction == Direction.IN && IsComplete))
                {
                    if (IsWrapped) { return CodedString.Decode(WrappedMessage.MessageContent, Encoding.UTF8); }
                    else { return RawContent; }
                }

                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public string RawContent
        {
            get
            {
                if (Direction == Direction.OUT || (Direction == Direction.IN && IsComplete))
                {
                    try
                    {
                        return CodedString.Decode(DataContainer.Get(0, Size).ToArray(), Encoding.UTF8);
                    }
                    catch (Exception e)
                    {
                        _logger.Info("No raw content to retrieve", e);
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// @uml.property name="messageId"
        /// </summary>
        virtual public string MessageId { get; protected set; }

        /// <summary>
        /// Handy method to retrieve the associated counter of this message
        /// </summary>
        /// <returns>The counter associated with this message</returns>
        public Counter Counter
        {
            get
            {
                return ReportMechanism.GetCounter(this);
            }
        }

        /// <summary>
        /// Session of this message is currently associated with
        /// </summary>
        public Session Session { get; internal set; }

        /// <summary>
        /// @uml.property name="successReport"
        /// </summary>
        private bool _successReport = false;

        /// <summary>
        /// Method used to set the success report field associated with this message.
        /// 
        /// true to set it to "yes" false to set it to "no"
        /// </summary>
        public bool SuccessReport { get { return _successReport; } set { _successReport = value; } }

        /// <summary>
        /// Get header content of successReport
        /// 
        /// success report header field of this message. True
        /// represents "yes" and false "no"
        /// </summary>
        public bool WantSuccessReport { get { return _successReport; } }

        /// <summary>
        /// Is this a wrapped message?
        /// </summary>
        public bool IsWrapped { get { return WrappedMessage != null; } }

        /// <summary>
        /// 
        /// </summary>
        public IWrappedMessage WrappedMessage { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public string Nickname { get; set; }

        /// <summary>
        /// This field keeps a reference to the last SEND transaction associated with
        /// this message
        /// </summary>
        internal Transaction LastSendTransaction { get; set; }

        /// <summary>
        /// Internal constructor used by the derived classes
        /// </summary>
        protected Message(Direction direction)
        {
            Direction = direction;
        }

        /// <summary>
        /// Construct by copying from existing message.
        /// </summary>
        /// <param name="toCopy">Existing message which must be used to create a new one</param>
        protected Message(Message toCopy)
        {
            SuccessReport = toCopy.SuccessReport;
            FailureReport = toCopy.FailureReport;
            _aborted = toCopy.WasAborted;
            DataContainer = toCopy.DataContainer;
            LastCallSentData = toCopy.LastCallSentData;
            LastCallReportCount = toCopy.LastCallReportCount;
            ReportMechanism = toCopy.ReportMechanism;
            Priority = toCopy.Priority;
            ContentType = toCopy.ContentType;
            MessageId = toCopy.MessageId;
            Session = toCopy.Session;
            WrappedMessage = toCopy.WrappedMessage;
            Nickname = toCopy.Nickname;
            LastSendTransaction = toCopy.LastSendTransaction;
        }

        /// <summary>
        /// The message transfer direction.
        /// </summary>
        /// <returns>returns the direction of the file transfer : IN or OUT.</returns>
        public Direction Direction { get; protected set; }

        /// <summary>
        /// Convenience method called internally by the constructors of this class
        /// to associate the given reportMechanism (or session's default) to the
        /// newly created message.
        ///
        /// MUST be called after a session is associated.
        /// </summary>
        /// <param name="reportMechanism"></param>
        protected void SetReportMechanism(ReportMechanism reportMechanism)
        {
            if (reportMechanism != null) { ReportMechanism = reportMechanism; }
            else if (Session != null) { ReportMechanism = Session.ReportMechanism; }
        }

        /// <summary>
        /// as this message object still unused content?
        /// </summary>
        /// <returns>true if this message still has some data to retrieve</returns>
        public bool HasData
        {
            get
            {
                return _dataContainer != null && _dataContainer.HasDataToRead;
            }
        }

        /// <summary>
        /// Fill the given array with DATA bytes, starting from offset
        /// and stopping at the array limit or end of data.
        /// Returns the number of bytes filled.
        /// 
        /// Throws ImplementationException when there was something wrong with the
        /// written code
        /// Throws InternalErrorException when there was an internal error that lead
        /// this operation to be an unsuccessful one
        /// </summary>
        /// <param name="outData">The byte array to fill</param>
        /// <param name="offset">The offset index to start filling the outData</param>
        /// <returns>The number of bytes filled</returns>
        public int Get(byte[] outData, int offset)
        {
            try
            {
                return DataContainer.Get(outData, offset);
            }
            catch (IndexOutOfRangeException e)
            {
                throw new ImplementationException(e);
            }
            catch (Exception e)
            {
                throw new InternalError(e);
            }
        }

        /// <summary>
        /// Returns the message id of this string
        /// </summary>
        /// <returns>The message id of this string</returns>
        override public string ToString()
        {
            return MessageId.ToString();
        }

        /// <summary>
        /// Returns true if message is completely sent or received, false otherwise.
        /// </summary>
        public abstract bool IsComplete { get; }

        /// <summary>
        ///  Message is ready to send or completely received, see if correct.
        /// Also the cue for any (un-)wrapping.
        /// </summary>
        /// <returns>the validated message</returns>
        public abstract Message Validate();

        /// <summary>
        /// Aborts the Outgoing or incoming message note, both arguments are
        /// irrelevant if this is an Outgoing message (as it's aborted with the #
        /// continuation flag and is no way to transmit the reason)
        /// </summary>
        /// <param name="reason">The Reason for the abort, only important if this is an
        ///                      Incoming message</param>
        /// <param name="reasonExtraInfo">The extra info about the abort, or null if it
        ///                               doesn't exist, this will be sent on the REPORT if we are
        ///                               aborting an Incoming message</param>
        public abstract void Abort(ResponseCodes reason, string reasonExtraInfo);


        /// <summary>
        /// Called by Transaction when it wants to notify a message that it got aborted.
        ///
        /// It is this method's responsibility to notify the listeners associated
        /// with it's session and change it's internal state accordingly
        /// </summary>
        /// <param name="transaction">The transaction that is associated with the abort</param>
        /// 
        /*
        * TODO reflect about the possibility to eliminate all the data associated
        * with this message or not. Could be done with a variable associated with
        * the stack, in some cases it may be useful to keep the data. ATM it
        * disposes the DataContainer associated with it
        */
        public void GotAborted(Transaction transaction)
        {
            _aborted = true;
            Discard();

            Session.FireMessageAbortedEvent(this, MessageAbortedEvent.CONTINUATIONFLAG, null, transaction);
        }

        /// <summary>
        /// This creates and fires a MessageAbortEvent
        /// 
        /// <see cref="MessageAbortEvent"/>
        /// </summary>
        /// <param name="reason">The reason for the Abort</param>
        /// <param name="extraReasonInfo">Eventually the String that was carried in the
        ///                               REPORT request that triggered this event, or null if none
        ///                               exists or is being considered</param>
        /// <param name="transaction"></param>
        public void fireMessageAbortedEvent(ResponseCodes reason, string extraReasonInfo, Transaction transaction)
        {
            Session.FireMessageAbortedEvent(this, reason, extraReasonInfo, transaction);
        }

        /// <summary>
        /// Let the message know it has served its' purpose.
        /// It will no longer be used and can be garbage collected. Free any resources.
        /// </summary>
        public void Discard()
        {
            if (DataContainer != null)
            {
                DataContainer.Dispose();
                DataContainer = null;
            }
        }
    }
}
