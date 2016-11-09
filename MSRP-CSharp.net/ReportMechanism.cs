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

namespace MSRP
{
    /// <summary>
    /// Public "interface" that allows one to define it's own report mechanisms of
    /// receiving/sending of messages
    /// 
    /// Receiving: It implements mechanisms to choose the granularity of successful
    /// REPORT requests
    /// 
    /// TODO: Sending: Implement mechanisms to choose the granularity of the
    /// updateSendStatus callbacks
    /// </summary>
    public abstract class ReportMechanism
    {
        private Dictionary<Message, Counter> _messageCounters = new Dictionary<Message, Counter>();

        /// <summary>
        /// Method that accounts for blocks of received body of the given message
        /// starting at the offset given by the byte-range header field of the
        /// associated transaction plus the chunkStartOffset
        /// </summary>
        /// <param name="message">the message of which the body bytes where received</param>
        /// <param name="transaction">the transaction associated with this block</param>
        /// <param name="chunkStartOffset">the absolute offset within the given message</param>
        /// <param name="chunkNrBytes">the number of bytes accounted for starting from the</param>
        public void CountReceivedBodyBlock(Message message, Transaction transaction, long chunkStartOffset, int chunkNrBytes)
        {
            Counter counter = GetCounter(message);
            counter.Register(chunkStartOffset, chunkNrBytes);

            long callCount = counter.Count;

            TriggerSuccessReport(message, transaction, message.LastCallReportCount, callCount);

            message.LastCallReportCount = callCount;
        }

        /// <summary>
        /// Method called upon when a write cycle is done.
        ///
        /// This method uses the abstract ShouldTriggerSentHook to decide upon
        /// calling the updateSendStatus or not and also flags the Connection
        /// Prioritizer so that this one can act and eventually pause the sending of
        /// the current message.
        ///
        /// Throws IllegalArgumentException if this method was called with an
        /// incoming message as argument
        /// </summary>
        /// <param name="outgoingMessage">the Message that is being sent and accounted for</param>
        /// <param name="numberBytesSent">the number of bytes that were sent
        ///                               (useful+overhead) in order to serve the sending of the
        ///                               outgoingMessage</param>
        public void CountSentBodyBytes(OutgoingMessage outgoingMessage, int numberBytesSent)
        {
            Session session = outgoingMessage.Session;

            if (ShouldTriggerSentHook(outgoingMessage, session, outgoingMessage.LastCallSentData))
            {
                session.TriggerUpdateSendStatus(session, outgoingMessage);
            }

            if (outgoingMessage.DataContainer != null)
            {
                outgoingMessage.LastCallSentData = outgoingMessage.DataContainer.CurrentReadOffset;
            }

            // TODO call the connection prioritizer method so far we will only check
            // to see if message is complete and account it on the session's sent
            // messages, if the connectionprioritizer is called the next lines
            // should be removed:
            // Store the sent message based on the success report
            if (outgoingMessage.WantSuccessReport)
            {
                outgoingMessage.Session.AddSentOrSendingMessage(outgoingMessage);
            }
        }

        /// <summary>
        /// Method that is used to retrieve the counter associated with the given
        /// message
        /// </summary>
        /// <param name="message">the message to retrieve the counter from</param>
        /// <returns>the counter associated with the given message</returns>
        public Counter GetCounter(Message message)
        {
            Counter counterToRetrieve = _messageCounters.Keys.Contains(message) ? _messageCounters[message] : null;

            if (counterToRetrieve == null)
            {
                counterToRetrieve = new Counter(message);
                _messageCounters.Add(message, counterToRetrieve);
            }

            return counterToRetrieve;
        }

        /// <summary>
        /// method used to eventually trigger an success report
        /// </summary>
        /// <param name="message">the message that triggered this call</param>
        /// <param name="transaction">the transaction that triggered this call</param>
        /// <param name="lastCallCount"></param>
        /// <param name="callCount"></param>
        public void TriggerSuccessReport(Message message, Transaction transaction, long lastCallCount, long callCount)
        {
            if (message.WantSuccessReport)
            {
                //use this mechanism also as a way of asserting also if a message
                //with a negative success report is complete or not
                if (ShouldGenerateReport(message, lastCallCount, callCount))
                {
                    //if there was a change in the number of bytes accounted for
                    MSRPStack.GenerateAndSendSuccessReport(message, transaction, null);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>an int with the default value of the triggerReportGranularity
        ///          basicly this value sets the number of received body bytes before
        ///          calling the triggerSuccessReport method</returns>
        public abstract int GetTriggerGranularity();

        /// <summary>
        /// Method to be implemented by the actual reportMechanism This method is
        /// called whenever any alteration to the number of bytes is detected on the
        /// message
        ///
        /// Hint: one always has access to the totalNrBytes of the message that may
        /// be useful to generate reports based on percentage (if applicable, because
        /// the totalNrBytes of the message may be unspecified/unknown)
        /// </summary>
        /// <param name="message">Message to which the alteration of the number of bytes
        ///                       ocurred (message being received)</param>
        /// <param name="lastCallCount"></param>
        /// <param name="callCount">the number of bytes received so far by the message</param>
        /// <returns>true if it's reasoned that a success report should be sent now,
        ///          false otherwise</returns>
        public abstract bool ShouldGenerateReport(Message message, long lastCallCount, long callCount);

        /// <summary>
        /// Method to be implemented by the actual reportMechanism This method is
        /// called whenever any significant alteration to the number of bytes is
        /// detected on the message.
        /// 
        /// The implementation of this method allows the specific application to
        /// decide upon the granularity of the callbacks to the updateSendStatus on
        /// the MSRPSessionListener
        /// </summary>
        /// <param name="outgoingMessage">the message that triggered the call</param>
        /// <param name="session">the session to which the message belongs</param>
        /// <param name="nrBytesLastCall">the number of bytes accounted for on the last call
        ///                               to this method - needed because we don't have a fixed</param>
        /// <returns>true if one should call the updateSendStatus trigger and false
        /// otherwise</returns>
        public abstract bool ShouldTriggerSentHook(Message outgoingMessage, Session session, long nrBytesLastCall);
    }
}
