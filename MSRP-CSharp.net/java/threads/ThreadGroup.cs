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
using System.Threading;
using System.Collections.ObjectModel;

namespace MSRP.Java.Threads
{
    /// <summary>
    /// Ported from Java, a way to keep track of your running threads
    /// Threads can be called with an IRunnable object(implements Run), also with a method
    /// 
    /// TODO: Correctly dispose of exited threads!
    /// </summary>
    public class ThreadGroup
    {
        /// <summary>
        /// Name of the ThreadGroup
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Threads maintained with IRunnable objects
        /// </summary>
        private List<ThreadObject> _group = new List<ThreadObject>();

        /// <summary>
        /// All threads
        /// </summary>
        private List<Thread> _threads = new List<Thread>();

        /// <summary>
        /// A Parent ThreadGroup
        /// </summary>
        public ThreadGroup Parent { get; private set; }

        /// <summary>
        /// Delegates for Threads who use a method to be executed
        /// </summary>
        public delegate void Target();

        /// <summary>
        /// Constructor, where there is no Parent ThreadGroup
        /// </summary>
        /// <param name="name">Name of the ThreadGroup</param>
        public ThreadGroup(string name) : this(null, name) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parent">The Parent ThreadGroup</param>
        /// <param name="name">Name of the ThreadGroup</param>
        public ThreadGroup(ThreadGroup parent, string name)
        {
            Parent = parent;
            Name = name;
        }

        /// <summary>
        /// Creating a thread with a class which implements IRunnable
        /// </summary>
        /// <param name="runnableObject">The class which will be executed</param>
        /// <returns>The Thread</returns>
        public Thread CreateThread(IRunnable runnableObject)
        {
            ThreadObject threadObject = new ThreadObject(runnableObject);
            Thread newThread = new Thread(new ThreadStart(threadObject.Execute));
            threadObject.Thread = newThread;
            _group.Add(threadObject);
            _threads.Add(newThread);

            return newThread;
        }

        /// <summary>
        /// Creating a thread with a function
        /// </summary>
        /// <param name="target">The function to execute</param>
        /// <returns>The Thread</returns>
        public Thread CreateThread(Target target)
        {
            Thread newThread = new Thread(new ThreadStart(target));
            _threads.Add(newThread);

            return newThread;
        }

        /// <summary>
        /// Internal class used to start threads, which implement IRunnable
        /// </summary>
        private class ThreadObject
        {
            /// <summary>
            /// Class instance to call Run from
            /// </summary>
            public IRunnable RunnableObject { get; set; }

            /// <summary>
            /// The thread which runs the IRunnable class
            /// </summary>
            public Thread Thread { get; set; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="runnableObject"></param>
            public ThreadObject(IRunnable runnableObject)
            {
                RunnableObject = runnableObject;
            }

            /// <summary>
            /// Called when the thread is started, so Run() gets called on the RunnableObject
            /// </summary>
            public void Execute()
            {
                RunnableObject.Run();
            }
        }
    }
}
