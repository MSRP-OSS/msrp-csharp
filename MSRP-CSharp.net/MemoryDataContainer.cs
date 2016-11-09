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
using MSRP.Exceptions;
using System.IO;

namespace MSRP
{
    /// <summary>
    ///  An implementation of the data container class.
    ///
    /// It uses a file read from/write to the data associated with an MSRP message
    /// 
    /// See DataContainer 
    /// </summary>
    public class MemoryDataContainer : DataContainer, IDisposable
    {
        /// <summary>
        /// Creates a blank DataContainer that stores the data in memory
        /// </summary>
        /// <param name="size">the maximum size of the data</param>
        public MemoryDataContainer(int size)
            : base()
        {
            Size = size;
            _dataStream = new MemoryStream(Size);
        }

        /// <summary>
        /// Creates a DataContainer used to interface with the given data byte array
        /// </summary>
        /// <param name="data">the byte[] containing the data</param>
        public MemoryDataContainer(byte[] data)
            : base()
        {
            if (data.Length > MSRPStack.ShortMessageBytes) { throw new InternalBufferOverflowException(); }

            Size = data.Length;
            _dataStream = new MemoryStream(data);
        }
    }
}
