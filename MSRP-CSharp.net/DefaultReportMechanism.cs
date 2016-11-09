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
    /// Default report mechanism offered by this stack as a singleton
    ///
    /// When receiving: It generates success reports upon message completion.
    ///
    /// When sending: It generates sent bytes notifications in the following manner:
    /// If the message is smaller than 500KB, twice, when half is sent and when all
    /// is sent For bigger than 500KB messages, for every ~10% of sent bytes of the
    /// message, the sent hook is triggered
    /// 
    /// See comments and details of the inherited methods for more information
    ///
    /// TODO change the shouldGenerateReport method
    /// </summary>
    public class DefaultReportMechanism : ReportMechanism
    {
        private DefaultReportMechanism()
        {
        }

        /// <summary>
        /// SingletonHolder is loaded on the first execution of
        /// Singleton.getInstance() or the first access to SingletonHolder.instance ,
        /// not before.
        /// </summary>
        private static class Singleton
        {
            internal static DefaultReportMechanism INSTANCE = new DefaultReportMechanism();
        }

        public static DefaultReportMechanism GetInstance()
        {
            return Singleton.INSTANCE;
        }

        /// <summary>
        /// See msrp.ReportMechanism#shouldGenerateReport(msrp.Message, int)
        ///
        /// This method is called every time getTriggerGranularity() of the message
        /// is received
        ///
        /// The default success report granularity is the whole message
        /// 
        /// See #getTriggerGranularity()
        /// </summary>
        /// <param name="message"></param>
        /// <param name="lastCallCount"></param>
        /// <param name="CallCount"></param>
        /// <returns></returns>
        override public bool ShouldGenerateReport(Message message, long lastCallCount, long CallCount)
        {
            return message.IsComplete;
        }

        /// <summary>
        /// if the message size is unknown dont trigger
        ///
        /// The default sent hook granularity is for each 10% of the message if the
        /// message is bigger than 500K
        /// 
        /// else only trigger once when it passes the 49% to 50% barrier
        ///
        /// also if the message is complete trigger it
        /// </summary>
        /// <param name="message"></param>
        /// <param name="session"></param>
        /// <param name="nrBytesLastCall"></param>
        /// <returns></returns>
        override public bool ShouldTriggerSentHook(Message message, Session session, long nrBytesLastCall)
        {
            if (message.IsComplete) { return true; }

            long size = message.Size;

            if (size < 0) { return false; }
            else
            {
                long lastPercentage = nrBytesLastCall * 100 / message.Size;
                long currentPercentage = message.DataContainer.CurrentReadOffset * 100 / message.Size;

                if (size <= 500 * 1024)
                {
                    return lastPercentage < 50 && currentPercentage >= 50;
                }
                else
                {
                    return lastPercentage / 10 == (currentPercentage / 10) - 1;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        override public int GetTriggerGranularity()
        {
            return 1024;
        }
    }
}
