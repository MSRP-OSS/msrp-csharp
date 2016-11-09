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
    /// A class can extend the Observer abstract class when it wants to be informed of changes in observable objects
    /// </summary>
    public abstract class Observer
    {
        /// <summary>
        /// This method is called whenever the observed object is changed.
        /// </summary>
        /// <param name="observable">the observable object.</param>
        /// <param name="observableObject">an argument passed to the notifyObservers method.</param>
        abstract public void Update(Observable observable, object observableObject);

        /// <summary>
        /// This method is called whenever the observed object is changed.
        /// </summary>
        /// <param name="observable">the observable object.</param>
        abstract public void Update(Observable observable);
    }
}
