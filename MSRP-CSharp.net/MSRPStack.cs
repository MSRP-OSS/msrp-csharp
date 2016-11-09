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
using System.Net;
using MSRP.Exceptions;
using MSRP.Utils;
using MSRP.Java;
using System.Net.NetworkInformation;
using MSRP.Java.Observer;

namespace MSRP
{
    /// <summary>
    /// Global MSRP singleton class.
    ///
    /// This class should contain all methods that must be global or that its outcome
    /// must somewhat depend on knowing about all of the existing MSRP objects like:
    /// sessions, connections, transactions, messages, others(?).
    /// </summary>
    public class MSRPStack : Observer
    {
        static private object _getConnectionsInstanceLock = new object();

        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(MSRPStack));

        /// <summary>
        /// Field that has the number of bytes of the short message
        /// </summary>
        private static int _shortMessageBytes = 1024 * 1024;

        /// <summary>
        /// Method used to set the short message bytes of this stack.
        ///
        /// A "short" message is a message that can be put in memory. the definition
        /// of this short message parameter is used to allow the stack to handle
        /// safely messages without storing them in file and without consuming too
        /// much memory. To note: that ATM the number of received messages that need
        /// to be stored (which success report = yes) has no way of being controlled
        /// FIXME
        /// </summary>
        public static int ShortMessageBytes { get { return _shortMessageBytes; } set { _shortMessageBytes = value; } }

        /// <summary>
        /// Stores all the connections objects mapped to the address they are bound to
        /// </summary>
        private static Dictionary<string, Connections> _addressConnections = new Dictionary<string, Connections>();

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, Connection> _localUriConnections;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, Connection> _sessionConnections;

        /// <summary>
        /// The {@link Session}s that are active in this stack.
        /// </summary>
        private Dictionary<string, Session> _activeSessions;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<Session> ActiveSessions
        {
            get
            {
                return _activeSessions.Values.ToList();
            }
        }

        /// <summary>
        /// Private constructor, MSRPInstance is a SINGLETON
        /// constructor
        /// </summary>
        private MSRPStack()
        {
            _localUriConnections = new Dictionary<string, Connection>();
            _sessionConnections = new Dictionary<string, Connection>();
            _activeSessions = new Dictionary<string, Session>();
        }

        /// <summary>
        /// SingletonHolder is loaded on the first execution of
        /// Singleton.getInstance() or the first access to SingletonHolder.instance,
        /// not before.
        /// </summary>
        static private MSRPStack _instance = null;

        public static MSRPStack GetInstance()
        {
            if (_instance == null) { _instance = new MSRPStack(); }

            return _instance;
        }

        /// <summary>
        /// in rfc 4975: "Non-SEND request bodies MUST NOT be larger than 10240
        /// octets."
        /// </summary>
        public const int MAX_NONSEND_BODYSIZE = 10240;

        /// <summary>
        /// RFC 4975: Maximum un-interruptible chunk-size in octets.
        /// </summary>
	    public const int MAX_UNINTERRUPTIBLE_CHUNK = 2048;

        /// <summary>
        /// Generate a new unique message-ID
        /// </summary>
        /// <returns></returns>
        public string GenerateMessageId()
        {
            string messageId = string.Empty;
           
            byte[] id = Guid.NewGuid().ToByteArray();
            foreach (byte b in id)
            {
                messageId += string.Format("{0:X2}", b);
            }

            return messageId;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="address">the ip address to bind to</param>
        /// <returns>a Connections instance bound to the given address if it exists,
        ///          or creates one</returns>
        public static Connections GetConnectionsInstance(InetAddress address)
        {
            lock (_getConnectionsInstanceLock)
            {
                Connections toReturn = null;

                if (_addressConnections.Keys.Contains(address._addr.ToString()))
                {
                    toReturn = _addressConnections[address._addr.ToString()];
                }
                else
                {
                    toReturn = new Connections(address);
                    _addressConnections.Add(address._addr.ToString(), toReturn);
                }

                return toReturn;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="observable"></param>
        /// <param name="observableObject"></param>
        public override void Update(Observable observable, object observableObject)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="observable"></param>
        public override void Update(Observable observable)
        {

        }

        /// <summary
        /// TODO (?!) relocate method?! needs to be a method?! FIXME (?!) REFACTORING
        /// Method that generates and sends a success report based on the range of
        /// the original transaction or of the whole message It interrupts any
        /// interruptible ongoing transaction as specified in rfc 4975
        /// </summary>
        /// <param name="message">Message associated with the report to be generated</param>
        /// <param name="transaction">Transaction that triggered the need to send the report is used
        ///                           to gather the range of bytes on which this report will report
        ///                           on and the associated session aswell. the value of transaction
        ///                           can be null if we are invoking this method in order to
        ///                           generate a report for the whole message</param>
        /// <param name="comment"></param>
        public static void GenerateAndSendSuccessReport(Message message, Transaction transaction, string comment)
        {
            try
            {
                Session session = transaction.Session;
                Transaction successReport = new SuccessReport(message, session, transaction, comment);

                session.TransactionManager.AddPriorityTransaction(successReport);
            }
            catch (InternalError e)
            {
                _logger.Error("InternalError trying to send success report", e);
            }
            catch (ImplementationException e)
            {
                _logger.Error("ImplementationException trying to send success report", e);
            }
            catch (IllegalUseException e)
            {
                _logger.Error("IllegalUse of success report", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public void AddActiveSession(Session session)
        {
            _activeSessions.Add(session.Uri.ToString(), session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public void RemoveActiveSession(Session session)
        {
            _activeSessions.Remove(session.Uri.ToString());
        }

        /// <summary>
        /// adds the received connection into the connections list
        /// </summary>
        /// <param name="connection"></param>
        public void AddConnection(Connection connection)
        {
            if (connection == null) { return; }

            _localUriConnections.Add(connection.LocalURI.ToString(), connection);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public bool IsActive(Uri uri)
        {
            return _activeSessions.ContainsKey(uri.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public Session GetSession(Uri uri)
        {
            Session session = null;
            if (IsActive(uri)) { session = _activeSessions[uri.ToString()]; }

            return session;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>an activeConnection</returns>
        protected Connection GetActiveConnection()
        {
            foreach (Connection conn in _sessionConnections.Values)
            {
                if (conn.IsBound) return conn;
            }

            return null;
        }

        /// <summary>
        /// adds a connection associated with the session URI
        /// </summary>
        /// <param name="uri">the URI to add to the existing connections</param>
        /// <param name="connection">the connection associated with this URI</param>
        public void AddConnection(Uri uri, Connection connection)
        {
            _sessionConnections.Add(uri.ToString(), connection);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="localUriToSearch">localUriToSearch</param>
        /// <returns>returns the connection associated with the given local uri</returns>
        public Connection GetConnectionByLocalURI(Uri uri)
        {
            Connection connection = null;
            if (_localUriConnections.Keys.Contains(uri.ToString())) { connection = _localUriConnections[uri.ToString()]; }

            return connection;
        }
    }
}
