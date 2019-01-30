using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Mahzen.Core
{
    /// <summary>
    /// The Protocol Writer that writes message protocols to an output stream.
    /// </summary>
    public class MessageProtocolWriter : ProtocolWriter, IDisposable
    {
        //binarywriter will block writes, so we must use faster stream to write then flush this memory stream to output stream.
        private readonly MemoryStream _internalBuffer = new MemoryStream();
        private readonly BinaryWriter _binaryWriter;
        private readonly Stream _outputStream;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputStream"></param>
        public MessageProtocolWriter(Stream outputStream)
        {
            _binaryWriter = new BinaryWriter(_internalBuffer, Encoding.UTF8);
            _outputStream = outputStream;
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _internalBuffer?.Dispose();
                    _binaryWriter?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MessageProtocolWriter() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        /// <inheritdoc />
        protected override void HandleWrite(MessageProtocolObject protocolObject)
        {
            _binaryWriter.Write(protocolObject);
        }

        /// <summary>
        /// Write all internal buffer to the output stream, and clears the internal buffer.
        /// </summary>
        /// <returns></returns>
        public async Task FlushAsync()
        {
            _internalBuffer.Position = 0;
            await _internalBuffer.CopyToAsync(_outputStream).ConfigureAwait(false);
            _internalBuffer.SetLength(0);
        }

        /// <summary>
        /// Write all internal buffer to the output stream, and clears the internal buffer.
        /// </summary>
        public void Flush()
        {
            _internalBuffer.Position = 0;
            _internalBuffer.CopyTo(_outputStream);
            _internalBuffer.SetLength(0);
        }
    }
}
