using System;
using System.Threading;
using System.Threading.Tasks;

namespace Discord.Audio.Streams
{
    /// <summary>
    ///     Decrypts an RTP frame using libsodium.
    /// </summary>
    public class SodiumDecryptStream : AudioOutStream
    {
        private readonly AudioClient _client;
        private readonly AudioStream _next;
        private readonly byte[] _nonce;
        private readonly byte[] _buffer;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public SodiumDecryptStream(AudioStream next, IAudioClient client, int bufferSize = 4000)
        {
            _next = next;
            _client = (AudioClient)client;
            _nonce = new byte[24];
            _buffer = new byte[bufferSize];
        }

        /// <exception cref="InvalidOperationException">Received payload without an RTP header.</exception>
        /// <exception cref="OperationCanceledException">The token has had cancellation requested.</exception>
        /// <exception cref="ObjectDisposedException">The associated <see cref="T:System.Threading.CancellationTokenSource" /> has been disposed.</exception>
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancelToken)
        {
            cancelToken.ThrowIfCancellationRequested();
            if (count < 12)
                throw new InvalidOperationException("Received payload without an RTP header.");

            if (_client.SecretKey == null)
                return;

            Buffer.BlockCopy(buffer, offset, _nonce, 0, 12); //Copy RTP header to nonce
            Buffer.BlockCopy(buffer, offset, _buffer, 0, 12);
            count = SecretBox.Decrypt(buffer, offset + 12, count - 12, _buffer, 12, _nonce, _client.SecretKey);
            await _next.WriteAsync(_buffer, 0, count + 12, cancelToken).ConfigureAwait(false);
        }

        public override async Task FlushAsync(CancellationToken cancelToken)
        {
            await _next.FlushAsync(cancelToken).ConfigureAwait(false);
        }
        public override async Task ClearAsync(CancellationToken cancelToken)
        {
            await _next.ClearAsync(cancelToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _next.Dispose();
            base.Dispose(disposing);
        }
    }
}
