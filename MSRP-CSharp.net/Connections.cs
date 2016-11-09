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
using System.IO;
using System.Threading;
using MSRP.Exceptions;
using MSRP.Java;
using MSRP.Utils;
using MSRP.Java.Threads;
using System.Net.Sockets;
using System.Net;

namespace MSRP
{
    /// <summary>
    /// This is the class responsible for accepting incoming TCP connection requests
    /// and generating the Connection object.
    /// </summary>
    public class Connections : Connection
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        private static ILog _logger = LogManager.GetLogger(typeof(Connections));

        /// <summary>
        /// 
        /// </summary>
        private MSRPStack _stack = MSRPStack.GetInstance();

        private ThreadGroup _connectionsGroup = new ThreadGroup("MSRP Stack connections");

        /// <summary>
        /// 
        /// </summary>
        public ThreadGroup ConnectionsGroup 
        {
            get
            {
                return _connectionsGroup;
            }
            private set
            {
                _connectionsGroup = value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private bool _hasStarted = false;

        /// <summary>
        /// SingletonHolder is loaded on the first execution of
        /// Singleton.getInstance() or the first access to SingletonHolder.instance ,
        /// not before. private static class SingletonHolder { private final static
        /// Connections INSTANCE = new Connections();
        /// </summary>
        public Thread AssociatedThread { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        private Socket _serverSocketChannel = null;

        /// <summary>
        /// 
        /// </summary>
        private Dictionary<string, Session> _urisSessionsToIdentify = new Dictionary<string, Session>();

        /// <summary>
        /// @uml.property name="_connections"
        /// @uml.associationEnd multiplicity="(1 1)"
        ///                     inverse="connections:msrp.TransactionManager"
        /// </summary>
        new public TransactionManager TransactionManager { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private HashSet<Uri> _existingURISessions = new HashSet<Uri>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        public Connections(InetAddress address)
        {
            try
            {   
                _random = new Random();
               
                if (NetworkUtils.isLinkLocalIPv4Address(address._addr))
                {
                    _logger.Info(string.Format("Connections: given address is a local one: {0}", address));
                }

                _serverSocketChannel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); //SelectorProvider.provider().openServerSocketChannel();
                _serverSocketChannel.Bind(new IPEndPoint(address._addr, 0));
                _serverSocketChannel.Listen(10); //TODO: backlog uitzoeken wat is de beste waarde ? 

                // fill the localURI variable that contains the uri parts that are
                // associated with this connection (scheme[protocol], host and port)
                //Uri newLocalURI = new Uri("msrp", null, address.getHostAddress(), socket.getLocalPort(), null, null, null);
                IPEndPoint endpoint = (IPEndPoint)_serverSocketChannel.LocalEndPoint; 

                Uri newLocalURI = new Uri(string.Format("msrp://{0}:{1}", endpoint.Address.ToString(), endpoint.Port.ToString()));

                LocalURI = newLocalURI;
                Thread server = new Thread(new ThreadStart(ThreadRun)); //new Thread(this);
                server.Name = string.Format("Connections: {0} server", LocalURI);
                server.Start();
            }
            catch (Exception ex) { }
        }

        new private void ThreadRun()
        {
            Run();
        }

        // Protected constructor is sufficient to suppress unauthorized calls to the
        // constructor
        protected Connection Connection()
        {
            try
            {
                _random = new Random();
                
                bool localAddress = false;

                InetAddress newAddress = InetAddress.LocalHostName;

                // sanity check, check that the given address is a local one where a
                // socket
                // could be bound
                InetAddress[] local = InetAddress.GetAllByName(InetAddress.LocalHostName.HostName);

                foreach (InetAddress inetAddress in local)
                {
                    if (inetAddress.Equals(newAddress)) { localAddress = true; }
                }
                
                if (!localAddress) { throw new UriFormatException("the given adress is not a local one"); }

                // bind the socket to a local temp. port.
                _serverSocketChannel = new Socket(new SocketInformation()); //SelectorProvider.provider().openServerSocketChannel();
                _serverSocketChannel.Bind(new IPEndPoint(newAddress._addr, 0));
                
                // fill the localURI variable that contains the uri parts that are
                // associated with this connection (scheme[protocol], host and port)
                //Uri newLocalURI = new Uri("msrp", null, newAddress.getHostAddress(), socket.getLocalPort(), null, null, null);
                LocalURI = new Uri(string.Format("msrp://{0}:{1}", newAddress.HostAddress, _serverSocketChannel.GetLocalPort())); 
            }
            catch (Exception e)
            {
                _logger.Error("Error! Connection did not get an associated socket");
            }
            
            return this;
        }

        override public void Run()
        {
            _hasStarted = true;
            AssociatedThread = Thread.CurrentThread;

            try
            {
                // Use the current serverSocketChannel to accept new connections
                while (true)
                {
                    Connection connection = new Connection(_serverSocketChannel.Accept());
                    _stack.AddConnection(connection);
                    Thread newConnThread = new Thread(new ThreadStart(connection.ThreadRun));
                    newConnThread.Name = string.Format("connection: {0} by connections newConnThread", connection.LocalURI);
                    newConnThread.Start();
                }
            }
            catch { }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Uri GenerateNewUri()
        {
            if (LocalURI == null) { throw new ImplementationException("Absurd error, Connections doesn't have the needed socket info"); }

            Uri newURI = NewUri();
            int i = 0;

            while (_existingURISessions.Contains(newURI))
            {
                i++;
                newURI = NewUri();
            }
            _existingURISessions.Add(newURI);

            _logger.Debug(string.Format("generated new URI, value of i={0}", i));

            if (_hasStarted && AssociatedThread.IsAlive) { }
            else
            {
                AssociatedThread = new Thread(new ThreadStart(ThreadRun)); //new Thread(this);
                AssociatedThread.Name = string.Format("Connections: {0} associatedThread", LocalURI.ToString());
                AssociatedThread.Start();
                _hasStarted = true;
            }

            return newURI;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="session"></param>
        public void AddUriToIdentify(Uri uri, Session session)
        {
            _urisSessionsToIdentify.Add(uri.ToString(), session);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public Session SessionToIdentify(Uri uri)
        {
            Session session = null;

            if (_urisSessionsToIdentify.Keys.Contains(uri.ToString())) { session = _urisSessionsToIdentify[uri.ToString()]; }

            return session;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="session"></param>
        public void IdentifiedSession(Session session)
        {
            _urisSessionsToIdentify.Remove(session.Uri.ToString());
            _existingURISessions.Add(session.Uri);
            
            session.Connection = _stack.GetConnectionByLocalURI(session.GetNextURI().GetCompleteAuthority());

            _stack.AddActiveSession(session);
        }   

        public void StartConnectionThread(Connection connection, ThreadGroup ioOperationGroup)
        {
            Thread newConnectionThread = ioOperationGroup.CreateThread(connection.ThreadRun); //new Thread(ioOperationGroup, connection);
            newConnectionThread.Name = string.Format("Connections: {0} newConnectionThread", LocalURI);
            newConnectionThread.Start();
        }
    }
}
