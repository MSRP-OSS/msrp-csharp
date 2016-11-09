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
using System.Collections;

namespace MSRP.Utils
{
    public sealed class ThreadSafeList<T> : IEnumerable<T>
    {
        private List<T> _list = new List<T>();
        private object _listLock = new object();

        public void Add(T value)
        {
            lock (_listLock)
            {
                _list.Add(value);
            }
        }

        public void Insert(int index, T value)
        {
            lock (_listLock)
            {
                _list.Insert(index, value);
            }
        }

        public void Remove(T value)
        {
            lock (_listLock)
            {
                _list.Remove(value);
            }
        }

        public bool Contains(T value)
        {
            lock (_listLock)
            {
                return _list.Contains(value);
            }
        }

        public int Count { get { lock (_listLock) { return _list.Count; } } }
        public T this[int index]
        {
            get { lock (_listLock) { return _list[index]; } }
            set { lock (_listLock) { _list[index] = value; } }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return (IEnumerator<T>)new ThreadSafeListEnumarator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ThreadSafeListEnumarator<T>(this);
        }
    }

    public class ThreadSafeListEnumarator<T> : IEnumerator<T>
    {
        ThreadSafeList<T> _list = null;
        private int _index = -1;

        internal ThreadSafeListEnumarator(ThreadSafeList<T> list)
        {
            _list = list;
        }

        public object Current
        {
            get { return (object)_list[_index]; }
        }

        public bool MoveNext()
        {
            _index++;

            return _index < _list.Count;
        }

        public void Reset()
        {
            _index = -1;
        }

        T IEnumerator<T>.Current
        {
            get { return (T)Current; }
        }

        public void Dispose()
        {

        }
    }
}
