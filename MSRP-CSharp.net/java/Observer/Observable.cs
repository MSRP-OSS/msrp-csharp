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

namespace MSRP.Java.Observer
{
    /// <summary>
    /// This class represents an observable object, or "data" in the model-view paradigm. It can be subclassed to represent an object that the application wants to have observed. 
    /// 
    /// An observable object can have one or more observers. 
    /// An observer may be any object that implements interface Observer. 
    /// After an observable instance changes, an application calling the Observable's notifyObservers method causes all of its observers to be notified of the change by a call to their update method. 
    ///
    /// The order in which notifications will be delivered is unspecified. 
    /// The default implementation provided in the Observerable class will notify Observers in the order in which they registered interest, 
    /// but subclasses may change this order, use no guaranteed order, deliver notifications on separate threads, or may guarantee that their subclass follows this order, as they choose. 
    ///
    /// Note that this notification mechanism is has nothing to do with threads and is completely separate from the wait and notify mechanism of class Object. 
    /// </summary>
    public class Observable
    {
        /// <summary>
        /// List of observer
        /// </summary>
        private List<Observer> _observers = new List<Observer>();

        /// <summary>
        /// If this object has changed
        /// </summary>
        public bool HasChanged { get; private set; }

        /// <summary>
        /// The number of observers of this Observable object
        /// </summary>
        public int ObserverCount 
        {
            get
            {
                return _observers.Count;
            }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public Observable() { }

        /// <summary>
        /// Adds an observer to the set of observers for this object, provided that it is not the same as some observer already in the set.
        /// </summary>
        /// <param name="observer"></param>
        public void AddObserver(Observer observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        /// <summary>
        /// Deletes an observer from the set of observers of this object.
        /// </summary>
        /// <param name="observer"></param>
        public void DeleteObserver(Observer observer)
        {
            if (_observers.Contains(observer))
            {
                _observers.Remove(observer);
            }
        }

        /// <summary>
        /// Clears the observer list so that this object no longer has any observers.
        /// </summary>
        public void DeleteObservers()
        {
            _observers.Clear();
        }

        /// <summary>
        /// Marks this Observable object as having been changed; the hasChanged method will now return true.
        /// </summary>
        protected void SetChanged()
        {
            HasChanged = true;
        }

        /// <summary>
        /// Indicates that this object has no longer changed, or that it has already notified all of its observers of its most recent change, so that the hasChanged method will now return false.
        /// </summary>
        protected void ClearChanged()
        {
            HasChanged = false;
        }

        /// <summary>
        /// If this object has changed, as indicated by the hasChanged method, then notify all of its observers and then call the clearChanged method to indicate that this object has no longer changed.
        /// </summary>
        public void NotifyObservers()
        {
            if (HasChanged)
            {
                foreach (Observer observer in _observers)
                {
                    observer.Update(this);
                }

                ClearChanged();
            }
        }

        /// <summary>
        /// If this object has changed, as indicated by the hasChanged method, then notify all of its observers and then call the clearChanged method to indicate that this object has no longer changed.
        /// </summary>
        /// <param name="notifyObject"></param>
        public void NotifyObservers(object notifyObject)
        {
            if (HasChanged)
            {
                foreach (Observer observer in _observers)
                {
                    observer.Update(this, notifyObject);
                }

                ClearChanged();
            }
        }
    }
}
