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
using MSRP.Exceptions;

namespace MSRP
{
    /// <summary>
    /// An implementation of the data container class.
    ///
    /// It uses a file read from/write to the data associated with an MSRP message
    /// See DataContainer
    /// </summary>
    public class FileDataContainer : DataContainer, IDisposable
    {
        private const string IOERR = "I/O problems: ";

        /// <summary>
        /// Creates a new DataContainer based on the given file, The file must be
        /// readable and writable. Note: if the file exists it's content will be
        /// overwritten
        /// </summary>
        /// <param name="filePath"></param>
        public FileDataContainer(string filePath)
            : base()
        {
            Size = File.Exists(filePath) ? (int)new FileInfo(filePath).Length : -1;

            _dataStream = new FileStream(filePath, FileMode.OpenOrCreate);
            _logger.Debug(string.Format("Created a FileDataContainer for file: {0}", filePath));
        }

        /// <summary>
        /// 
        /// </summary>
        override public void Dispose()
        {
            try
            {
                ((FileStream)_dataStream).Close();
                base.Dispose();

            }
            catch (IOException e)
            {
                _logger.Error(IOERR, e);
            }
        }
    }
}
