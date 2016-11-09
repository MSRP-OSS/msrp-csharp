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

namespace MSRP
{
    /// <summary>
    ///  Class to abstract the counting of the received bytes of a message there are
    /// two main types of messages, the ones with known max size and the ones without
    /// known max size.
    ///
    /// Also the class takes into consideration the fact that the reported bytes to
    /// count can overlap
    /// </summary>
    public class Counter
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(Counter));

        /// <summary>
        /// The ArrayList used to register the bytes
        /// </summary>
        List<List<long>> _counter;

        private const int VMIN = 0;

        private const int NRBYTESPOS = 1;

        private object _countLock = new object();

        /// <summary>
        /// Stores the number of bytes that the counter has
        /// </summary>
        private long _count = 0;
        public long Count 
        {
            get
            {
                lock (_countLock)
                {
                    return _count;
                }
            }
            private set
            {
                lock (_countLock)
                {
                    _count = value;
                }
            }
        }

        private object _nrConsecutiveBytesLock = new object();

        /// <summary>
        /// Stores the number of consecutive bytes from the start without "holes"
        /// 
        /// This method gives the number of consecutive received bytes. e.g. if the
        /// bitset is: 111011 this method will return 3;
        /// </summary>
        private long _nrConsecutiveBytes = 0;
        public long NrConsecutiveBytes 
        {
            get
            {
                lock (_nrConsecutiveBytesLock)
                {
                    return _nrConsecutiveBytes;
                }
            }
            set
            {
                lock (_nrConsecutiveBytesLock)
                {
                    _nrConsecutiveBytes = value;
                }
            }
        }

        private object _isCompleteLock = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if we have a complete message false otherwise (note: in
        ///          order for a message to be complete one needs to have received the
        ///          $ continuation flag and the message must not have "holes" in it)</returns>
        public bool IsComplete
        {
            get
            {
                lock (_isCompleteLock)
                {
                    if (!_dollarContinuationFlag) { return false; }

                    /* Checking for holes */
                    return _count == _nrConsecutiveBytes;
                }
            }
        }

        /// <summary>
        /// Creates the counter structure based on the message size
        /// </summary>
        /// <param name="message"></param>
        internal Counter(Message message)
        {
            _counter = new List<List<long>>();
        }

        private object _registerMethodLock = new object();

        /// <summary>
        /// Registers the given position as received updates the count number of
        /// bytes and the nr of consecutive bytes private variables
        /// 
        /// RV 20/08/2012 - Removed all DEBUG messages to logger, generated too much log-entries
        /// </summary>
        /// <param name="startingPosition">startingPosition the index position, in the overall message
        ///                                content that this count refers to</param>
        /// <param name="numberBytes">the offset regarding the startingPosition to which one
        ///                           should count the byte</param>
        /// <returns>true if there was a change on the number of contiguous bytes
        ///          accounted for, or false otherwise</returns>
        public bool Register(long startingPosition, long numberBytes)
        {
            lock (_registerMethodLock)
            {
                //_logger.Debug("Register: " + numberBytes.ToString() + " bytes received @ position " + startingPosition.ToString());

                long previousValueConsecutiveBytes = _nrConsecutiveBytes;

                List<long> valueToRegister = new List<long>();
                valueToRegister.Add(startingPosition);
                valueToRegister.Add(numberBytes);

                // Go through the list to find the position to insert the value
                bool inserted = false;

                foreach (List<long> toEvaluate in _counter)
                {
                    if (inserted) { break; }
                    else
                    {
                        long vMaxExisting = toEvaluate[VMIN] + toEvaluate[NRBYTESPOS];
                        long vMinToInsert = valueToRegister[0];
                        long vMaxToInsert = vMinToInsert + valueToRegister[1];

                        // case one - separate cluster before
                        if (vMinToInsert < toEvaluate[VMIN] && vMaxToInsert < toEvaluate[VMIN])
                        {
                            //_logger.Debug("1. separate cluster before detected");
                            // insert the new element before
                            int indexToInsert = _counter.IndexOf(toEvaluate) - 1;

                            if (indexToInsert == -1) { _counter.Insert(0, valueToRegister); }
                            else { _counter.Insert(indexToInsert, valueToRegister); }

                            inserted = true;
                            // add the second position to the count
                            Count += valueToRegister[NRBYTESPOS];
                            // update the nr consecutive bytes if it's the case:
                            if (vMinToInsert == 0) { NrConsecutiveBytes = valueToRegister[NRBYTESPOS]; }

                            // end the cycle
                            break;
                        }// if (vMinToInsert < toEvaluate[VMIN] && vMaxtoInsert <
                        // toEvaluate[VMIN])
                        // case two - Intersects and extends it in the beginning
                        else if (vMinToInsert < toEvaluate[VMIN] && vMaxExisting >= vMaxToInsert && vMaxToInsert >= toEvaluate[VMIN])
                        {
                            //_logger.Debug("2. Intersects and extends in the beginning detected");
                            valueToRegister[NRBYTESPOS] = vMaxExisting - valueToRegister[VMIN];

                            // replace the to evaluate with this one:
                            _counter[_counter.IndexOf(toEvaluate)] = valueToRegister;

                            inserted = true;
                            Count += (toEvaluate[VMIN] - valueToRegister[VMIN]);

                            // update the nr consecutive bytes if it's the case:
                            if (vMinToInsert == 0) { NrConsecutiveBytes = valueToRegister[NRBYTESPOS]; }

                            // end the cycle
                            break;
                        }// else if (vMinToInsert < toEvaluate[VMIN] && toEvaluate[VMAX] >=
                        // vMaxToInsert && vMaxToInsert >= toEvaluate[VMIN])
                        // case three - Within an existing cluster
                        else if (vMinToInsert >= toEvaluate[VMIN] && vMaxToInsert <= vMaxExisting)
                        {
                            //_logger.Debug("3. Within existing cluster detected");

                            // don't quite do anything, just break the cycle
                            inserted = true;
                            break;
                        }
                        // case four - Intersects and extends it further to the end
                        else if (vMinToInsert >= toEvaluate[VMIN] && vMinToInsert <= vMaxExisting && vMaxToInsert >= vMaxExisting)
                        {
                            //_logger.Debug("4. Intersects and extends further to end detected");

                            // remove the existing one and create the new one
                            _counter.Remove(toEvaluate);
                            valueToRegister[VMIN] = toEvaluate[VMIN];
                            valueToRegister[NRBYTESPOS] = vMaxToInsert - valueToRegister[VMIN];
                            Count -= toEvaluate[NRBYTESPOS];

                            if (toEvaluate[VMIN] == 0) { NrConsecutiveBytes = 0; }

                            // go to the next iteration
                            //continue;
                            break;
                        }// else if (vMinToInsert >= toEvaluate[VMIN] && vMinToInsert <=
                        // vMaxExisting && vMaxToInsert >= vMaxExisting )
                        // case five - Separate cluster after
                        else if (vMinToInsert > toEvaluate[VMIN] && vMinToInsert > vMaxExisting)
                        // don't do anything
                        {
                            //_logger.Debug("5. Separate cluster after detected");

                            // go to the next iteration
                            continue;
                        }// else if (vMinToInsert > toEvaluate[VMIN] && vMinToInsert >
                        // vMaxExisting)
                        else if (vMinToInsert < toEvaluate[VMIN] && vMaxToInsert > vMaxExisting)
                        // case six - bigger new cluster
                        {
                            //_logger.Debug("6. bigger new cluster detected");

                            // remove this one
                            _counter.Remove(toEvaluate);
                            Count -= toEvaluate[NRBYTESPOS];

                            if (toEvaluate[VMIN] == 0) { NrConsecutiveBytes = 0; }

                            //continue;
                            break;
                        }
                        else
                        {
                            _logger.Error("Serious error in counter algorithm, please report this error to the developers");
                        }
                    }
                }

                if (!inserted)
                {
                    //_logger.Debug("Not inserted and cycle ended");
                    _counter.Add(valueToRegister);

                    // update the number of counted and consecutive bytes
                    Count += valueToRegister[NRBYTESPOS];
                    if (valueToRegister[VMIN] == 0) { _nrConsecutiveBytes = valueToRegister[NRBYTESPOS]; }
                }

                return NrConsecutiveBytes != previousValueConsecutiveBytes;
            }
        }

        /// <summary>
        /// field that is used to register the receipt or not of the end of message
        /// continuation flag
        /// </summary>
        private bool _dollarContinuationFlag = false;

        /// <summary>
        /// Method used to notify the counter the receipt of $ continuation flag
        /// (which doesn't mean that the message is fully received due to the fact
        /// that the transactions could have been received in a different order *as
        /// in RFC*)
        /// </summary>
        public void ReceivedEndOfMessage()
        {
            _dollarContinuationFlag = true;
        }
    }
}
