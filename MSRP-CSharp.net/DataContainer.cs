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

using log4net;
using MSRP.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MSRP
{
    /// <summary>
    /// Class used to provide abstraction to the container of the actual data on the
    /// message. Also has a mechanism to allow the validation of the received data,
    /// the validator, which isn't yet fully implemented and available
    /// </summary>
    public abstract class DataContainer : IDisposable
    {
        /// <summary>
        /// The logger associated with this class
        /// </summary>
        protected static ILog _logger = null;

        /// <summary>
        /// The maximum number of bytes that can be stored on memory.
        ///
        /// this maximum number of bytes is defined by the MSRPStack short message
        ///  
        /// MSRPStack#setShortMessageBytes(int)
        /// MSRPStack.setShortMessageBytes(int)
        /// </summary>
        public int MAXIMUMNUMBERBYTES = MSRPStack.ShortMessageBytes;

        /// <summary>
        /// Convenience number 0 that used as size argument on the get operations
        /// represents all of the remaining bytes
        /// </summary>
        public const int ALLBYTES = 0;

        /// <summary>
        /// field used to compose the validator of the content type to this class
        /// 
        /// The validator should act as an interface and it's WORK IN PROGRESS, the
        /// main idea is that this class will be responsible for validating the
        /// content type contained by this data container
        /// 
        /// The validator makes more sense in a receiving data context although it
        /// can be used while sending the data
        /// </summary>
        public Validator Validator { get; set; }

        /// <summary>
        /// 
        /// </summary>
        protected Stream _dataStream = null;

        /// <summary>
        /// The size of the datacontainer
        /// </summary>
        public int Size { get; protected set; }
        
        /// <summary>
        /// Method used to retrieve the current number of bytes, also called the read
        /// offset, of the given data container
        /// </summary>
        /// <returns>the current offset (number of bytes)</returns>
        virtual public long CurrentReadOffset
        {
            get
            {
                return _dataStream != null ? _dataStream.Position : 0;
            }
        }

        /// <summary>
        /// Method used to assert if this data container still has data available for
        /// reading
        /// </summary>
        /// <returns>true if this data container still has data to retrieve</returns>
        virtual public bool HasDataToRead
        {
            get
            {
                if (_dataStream == null) { return false; }

                return _dataStream.Position < _dataStream.Length;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected DataContainer()
        {
            _logger = LogManager.GetLogger(this.GetType());
        }

        /// <summary>
        /// Puts the given data relative to the startingIndex position
        ///
        /// Throws Exception if there was any other kind of Exception
        /// </summary>
        /// <param name="startingIndex">the given index to start putting the data on the
        ///                             appropriate data container</param>
        /// <param name="dataToPut">the byte array to store starting at startingIndex</param>
        virtual public void Put(long startingIndex, byte[] dataToPut)
        {
            try
            {
                _dataStream.Position = startingIndex;
                _dataStream.Write(dataToPut, 0, dataToPut.Length);
            }
            catch (NotSupportedException e)
            {
                throw new NotEnoughStorageException(string.Format("Putting {0} bytes of data starting in {1} on a buffer with {2}", dataToPut.Length, startingIndex, Size), e);
            }
        }

        /// <summary>
        /// sequential put of byte
        ///
        /// Throws NotEnoughStorageException if there isn't anymore storage
        /// available on this data Container
        /// Throws Exception if there was any other kind of Exception
        /// </summary>
        /// <param name="byteToPut">byteToPut the byte to put in the relative</param>
        virtual public void Put(byte byteToPut)
        {
            try
            {
                _dataStream.WriteByte(byteToPut);
            }
            catch (NotSupportedException e)
            {
                throw new NotEnoughStorageException(e);
            }
        }

        /// <summary>
        /// Puts the given single byte of data relative to the startingIndex position
        /// 
        /// Throws NotEnoughStorageException if there isn't anymore storage
        /// available on this data Container
        /// Throws Exception if there was any other kind of Exception
        /// </summary>
        /// <param name="startingIndex">the given index to start putting the data on the
        ///                             appropriate data container</param>
        /// <param name="byteToPut">the single byte to store starting at startingIndex</param>
        virtual public void Put(long startingIndex, byte byteToPut)
        {
            if (startingIndex < 0) { throw new IllegalUseException("The starting index should be >= 0"); }

            try
            {
                //byteBuffer.put((int) startingIndex, byteToPut);
                _dataStream.Position = startingIndex;
                _dataStream.WriteByte(byteToPut);
            }
            catch (NotSupportedException e)
            {
                throw new NotEnoughStorageException();
            }
        }


        /// <summary>
        /// Method used to retrieve the data from the data container
        ///
        /// deprecated due to performance issues it's better to use
        /// {@link #get(byte[], int)}
        ///
        /// Throws NotEnoughDataException if the request couldn't be satisfied due
        /// to the fact that there isn't anymore available data to
        /// retrieve
        /// Throws Exception if there was any other kind of Exception
        /// </summary>
        /// <returns>a byte of data</returns>
        [Obsolete("due to performance issues it's better to use {@link #get(byte[], int)}")]
        public byte Get()
        {
            try
            {
                return Convert.ToByte(_dataStream.ReadByte());
            }
            catch (IndexOutOfRangeException e)
            {
                throw new NotEnoughDataException(e);
            }
        }

        /// <summary>
        /// Method used to retrieve the data from the data container
        ///
        /// method not advisable to use when retrieving data from a file
        /// Throws NotEnoughDataException if the request couldn't be satisfied due
        /// to the fact that there isn't enough available data to
        /// retrieve as requested
        /// Throws Exception if there was any other kind of Exception
        /// Throws IllegalUseException if the number of bytes to retrieve is bigger
        /// than the fixed limit of MAXIMUMNUMBERBYTES
        /// </summary>
        /// <param name="offsetIndex">offsetIndex the offset index to start reading the data</param>
        /// <param name="size">size the number of bytes to read or zero to get all the remaining
        ///                    data counting from the offset position</param>
        /// <returns>a ByteArray containing a copy of the requested data. To note:
        ///          one can write in this byte buffer without altering the actual
        ///          content;</returns>
        virtual public byte[] Get(long offsetIndex, long size)
        {
            if (size <= 0) { size = _dataStream.Length - offsetIndex; }

            if (offsetIndex < 0) { throw new IllegalUseException("negative size or offsetindex"); }

            if (_dataStream.Length < (offsetIndex + size)) { throw new NotEnoughDataException(); }

            try
            {
                long positionSaved = _dataStream.Position;

                 byte[] newByteArray = new byte[(int)size];
                _dataStream.Position = (int)offsetIndex;
                _dataStream.Read(newByteArray, (int)offsetIndex, (int)size);

                _dataStream.Position = positionSaved;

                return newByteArray;
            }
            catch (IllegalUseException e)
            {
                throw new NotEnoughDataException(e);
            }
            catch (OutOfMemoryException e)
            {
                throw new NotEnoughDataException(e);
            }
        }

        /// <summary>
        /// Method used to retrieve data to fill the destination buffer or until
        /// there is no more data
        ///
        /// Throws IndexOutOfBoundsException if the offset is bigger than the length
        /// of the dst byte array
        /// Throws Exception if there was any other kind of Exception
        /// </summary>
        /// <param name="dst">the byte array to fill</param>
        /// <param name="offset">the offset where to start to fill the byte array</param>
        /// <returns>the number of bytes that got copied to dst</returns>
        virtual public int Get(byte[] dst, int offset)
        {
            int bytesToCopy = 0;
            if (_dataStream.Length - _dataStream.Position < dst.Length - offset) { bytesToCopy = (int)_dataStream.Length - (int)_dataStream.Position; }
            else { bytesToCopy = dst.Length - offset; }

            _dataStream.Read(dst, offset, bytesToCopy);

            return bytesToCopy;
        }

        /// <summary>
        /// Method used to rewind the read buffer the supplied number of positions.
        /// Example: a call to the {@link #get()} followed by a call to this function
        /// with nrPositions = 1 followed by another call to {@link #get()} will
        /// return the same value
        /// </summary>
        /// <param name="nrPositions"></param>
        virtual public void RewindRead(long nrPositions)
        {
            _dataStream.Position = (int)(_dataStream.Position - nrPositions);
        }

        /// <summary>
        /// Method used to dispose this data container resources. Frees up any memory
        /// blocks of data reserved Should be called explicitly!
        /// </summary>
        virtual public void Dispose()
        {
            _dataStream.Dispose(); 
            _dataStream = null;
        }
    }
}
