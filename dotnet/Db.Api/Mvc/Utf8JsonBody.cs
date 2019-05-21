// Implementation taken from `JsonDocument.Parse.cs`

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Db.Api.Mvc
{
    class Utf8JsonBody : IMemoryOwner<byte>
    {
        private static ReadOnlySpan<byte> Utf8Bom => new byte[] {0xEF, 0xBB, 0xBF};
        private const int UnseekableStreamInitialRentSize = 4096;

        private ArraySegment<byte> _segment;

        public Memory<byte> Memory => _segment.AsMemory();

        public static async Task<Utf8JsonBody>
            ReadToEndAsync(
                Stream stream,
                CancellationToken cancellationToken)
        {
            var written = 0;
            byte[] rented = null;

            try
            {
                // Save the length to a local to be reused across awaits.
                var utf8BomLength = Utf8Bom.Length;

                if (stream.CanSeek)
                {
                    // Ask for 1 more than the length to avoid resizing later,
                    // which is unnecessary in the common case where the stream length doesn't change.
                    var expectedLength = Math.Max(utf8BomLength, stream.Length - stream.Position) + 1;
                    rented = ArrayPool<byte>.Shared.Rent(checked((int) expectedLength));
                }
                else
                {
                    rented = ArrayPool<byte>.Shared.Rent(UnseekableStreamInitialRentSize);
                }

                int lastRead;

                // Read up to 3 bytes to see if it's the UTF-8 BOM
                do
                {
                    // No need for checking for growth, the minimal rent sizes both guarantee it'll fit.
                    Debug.Assert(rented.Length >= Utf8Bom.Length);

                    lastRead = await stream.ReadAsync(
                        rented,
                        written,
                        utf8BomLength - written,
                        cancellationToken).ConfigureAwait(false);

                    written += lastRead;
                } while (lastRead > 0 && written < utf8BomLength);

                // If we have 3 bytes, and they're the BOM, reset the write position to 0.
                if (written == utf8BomLength &&
                    Utf8Bom.SequenceEqual(rented.AsSpan(0, utf8BomLength)))
                    written = 0;

                do
                {
                    if (rented.Length == written)
                    {
                        var toReturn = rented;
                        rented = ArrayPool<byte>.Shared.Rent(toReturn.Length * 2);
                        Buffer.BlockCopy(toReturn, 0, rented, 0, toReturn.Length);
                        // Holds document content, clear it.
                        ArrayPool<byte>.Shared.Return(toReturn, true);
                    }

                    lastRead = await stream.ReadAsync(
                        rented,
                        written,
                        rented.Length - written,
                        cancellationToken).ConfigureAwait(false);

                    written += lastRead;
                } while (lastRead > 0);

                return new Utf8JsonBody
                {
                    _segment = new ArraySegment<byte>(rented, 0, written)
                };
            }
            catch
            {
                if (rented == null) throw;

                // Holds document content, clear it before returning it.
                rented.AsSpan(0, written).Clear();
                ArrayPool<byte>.Shared.Return(rented);

                throw;
            }
        }

        public void Dispose()
        {
            if (_segment != null)
            {
                _segment.AsSpan().Clear();
                ArrayPool<byte>.Shared.Return(_segment.Array);
                _segment = null;
            }
        }
    }
}