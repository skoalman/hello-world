// Software License Agreement (BSD License)
// 
// Copyright (c) 2007, Peter Dennis Bartok <PeterDennisBartok@gmail.com>
// All rights reserved.
// 
// Redistribution and use of this software in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
// 
// * Redistributions of source code must retain the above
//   copyright notice, this list of conditions and the
//   following disclaimer.
// 
// * Redistributions in binary form must reproduce the above
//   copyright notice, this list of conditions and the
//   following disclaimer in the documentation and/or other
//   materials provided with the distribution.
// 
// * Neither the name of Peter Dennis Bartok nor the names of its
//   contributors may be used to endorse or promote products
//   derived from this software without specific prior
//   written permission of Yahoo! Inc.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED
// WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
// ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR
// TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
// ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Manzana
{
	/// <summary>
	/// Exposes a stream to a file on an iPhone, supporting both synchronous and asynchronous read and write operations
	/// </summary>
	public class iPhoneFile : Stream {
		private enum OpenMode {
			None = 0,
			Read = 2,
			Write = 3,
		}

		#region Fields
		private OpenMode	mode;
		private long		handle;
		private iPhone		phone;
        private static String      fileName;
		#endregion	// Fields;

		#region Constructors
		private iPhoneFile(iPhone phone, long handle, OpenMode mode) : base() {
			this.phone = phone;
			this.mode = mode;
			this.handle = handle;
		}

		#endregion	// Constructors

		#region Public Properties
		/// <summary>
		/// gets a value indicating whether the current stream supports reading.
		/// </summary>
		public override bool CanRead {
			get { 
				if (mode == OpenMode.Read) {
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports seeking.
		/// </summary>
		public override bool CanSeek {
			get { return false; }
		}

		/// <summary>
		/// Gets a value that determines whether the current stream can time out. 
		/// </summary>
		public override bool CanTimeout {
			get {
				return true;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the current stream supports writing
		/// </summary>
		public override bool CanWrite {
			get {
				if (mode == OpenMode.Write) {
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Gets the length in bytes of the stream . 
		/// </summary>
		public override long Length {
            get { return phone.FileSize(fileName); }
		}

		/// <summary>
		/// Gets or sets the position within the current stream
		/// </summary>
		public override long Position {
			get {
				uint ret;
				ret = 0;

				MobileDevice.AFCFileRefTell(phone.AFCHandle, handle, ref ret);
				return (long)ret;
			}
			set {
				this.Seek(value, SeekOrigin.Begin);
			}
		}

		/// <summary>
		/// Sets the length of this stream to the given value. 
		/// </summary>
		/// <param name="value">The new length of the stream.</param>
		public override void SetLength(long value) {
			int ret;

			ret = MobileDevice.AFCFileRefSetFileSize(phone.AFCHandle, handle, (uint)value);
		}
		#endregion	// Public Properties

		#region Public Methods
		/// <summary>
		/// Releases the unmanaged resources used by iPhoneFile
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected override void Dispose(bool disposing) {
			if (disposing) {
				if (handle != 0) {
					MobileDevice.AFCFileRefClose(phone.AFCHandle, handle);
					handle = 0;
				}
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read
		/// </summary>
		/// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source. </param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
		/// <param name="count">The maximum number of bytes to be read from the current stream.</param>
		/// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
		public override int Read(byte[] buffer, int offset, int count) {
			uint	len;
			int		ret;
			byte[]	temp;

			if (mode != OpenMode.Read) {
				throw new NotImplementedException("Stream open for writing only");
			}

			if (offset == 0) {
				temp = buffer;
			} else {
				temp = new byte[count];
			}
			len = (uint)count;
			ret = MobileDevice.AFCFileRefRead(phone.AFCHandle, handle, temp, ref len);
			if (ret != 0) {
				throw new IOException("AFCFileRefRead error = " + ret.ToString());
			}
			if (temp != buffer) {
				Buffer.BlockCopy(temp, 0, buffer, offset, (int)len);
			}
			return (int)len;
		}

		/// <summary>
		/// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written. 
		/// </summary>
		/// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
		/// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
		/// <param name="count">The number of bytes to be written to the current stream.</param>
		public override void Write(byte[] buffer, int offset, int count) {
			int		ret;
			uint	len;
			byte[] temp;

			if (mode != OpenMode.Write) {
				throw new NotImplementedException("Stream open for reading only");
			}

			if (offset == 0) {
				temp = buffer;
			} else {
				temp = new byte[count];
				Buffer.BlockCopy(buffer, offset, temp, 0, count);
			}
			len = (uint)count;
			ret = MobileDevice.AFCFileRefWrite(phone.AFCHandle, handle, temp, len);
		}

		/// <summary>
		/// Sets the position within the current stream
		/// </summary>
		/// <param name="offset">A byte offset relative to the <c>origin</c> parameter</param>
		/// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position</param>
		/// <returns>The new position within the stream</returns>
		public override long Seek(long offset, SeekOrigin origin) {
			int ret;

			ret = MobileDevice.AFCFileRefSeek(phone.AFCHandle, handle, (uint)offset, 0);
			Console.WriteLine("ret = {0}", ret);
			return offset;
		}

		/// <summary>
		/// Clears all buffers for this stream and causes any buffered data to be written to the underlying device. 
		/// </summary>
		public override void Flush() {
			MobileDevice.AFCFlushData(phone.AFCHandle, handle);
		}
		#endregion	// Public Methods

		#region Static Methods
		/// <summary>
		/// Opens an iPhoneFile stream on the specified path
		/// </summary>
		/// <param name="phone">A valid iPhone object</param>
		/// <param name="path">The file to open</param>
		/// <param name="openmode">A <see cref="FileAccess"/> value that specifies the operations that can be performed on the file</param>
		/// <returns></returns>
		public static iPhoneFile Open(iPhone phone, string path, FileAccess openmode) {
			OpenMode	mode;
			int			ret;
			long		handle;
			string		full_path;

			mode = OpenMode.None;
			switch(openmode) {
				case FileAccess.Read: mode = OpenMode.Read; break;
				case FileAccess.Write: mode = OpenMode.Write; break;
				case FileAccess.ReadWrite: throw new NotImplementedException("Read+Write not (yet) implemented");
			}

			full_path = phone.FullPath(phone.GetCurrentDirectory(), path);
			ret = MobileDevice.AFCFileRefOpen(phone.AFCHandle, full_path, (int)mode, 0, out handle);
			if (ret != 0) {
				throw new IOException("AFCFileRefOpen failed with error " + ret.ToString());
			}
            fileName = path;
			return new iPhoneFile(phone, handle, mode);
		}

		/// <summary>
		/// Opens a file for reading
		/// </summary>
		/// <param name="phone">A valid iPhone object</param>
		/// <param name="path">The file to be opened for reading</param>
		/// <returns>An unshared <c>iPhoneFile</c> object on the specified path with Write access. </returns>
		public static iPhoneFile OpenRead(iPhone phone, string path) {
            iPhoneFile retval;
            try {
                retval = iPhoneFile.Open(phone, path, FileAccess.Read);
            }
            catch ( Exception err ) {
                throw new IOException(err.Message);
            }
            return retval;
		}

		/// <summary>
		/// Opens a file for writing
		/// </summary>
		/// <param name="phone">A valid iPhone object</param>
		/// <param name="path">The file to be opened for writing</param>
		/// <returns>An unshared <c>iPhoneFile</c> object on the specified path with Write access. </returns>
		public static iPhoneFile OpenWrite(iPhone phone, string path) {
			return iPhoneFile.Open(phone, path, FileAccess.Write);
		}
		#endregion	// Static Methods
	}
}
