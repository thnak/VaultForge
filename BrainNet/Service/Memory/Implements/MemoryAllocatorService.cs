using System.Buffers;
using BrainNet.Service.Memory.Interfaces;

namespace BrainNet.Service.Memory.Implements;

internal class MemoryAllocatorService : IMemoryAllocatorService
{
    #region ArrayMemoryPoolBuffer<T>

    private class ArrayMemoryPoolBuffer<T> : IMemoryOwner<T>
    {
        private readonly int _length;

        private readonly T[] _buffer;

        public Memory<T> Memory
        {
            get
            {
#if DEBUG
                ObjectDisposedException.ThrowIf(_buffer == null, this);
#endif
                return new Memory<T>(_buffer, 0, _length);
            }
        }

        public ArrayMemoryPoolBuffer(int length, bool clean)
        {
            _buffer = ArrayPool<T>.Shared.Rent(length);

            if (clean)
            {
                Array.Clear(_buffer, 0, length);
            }

            _length = length;
        }

        ~ArrayMemoryPoolBuffer() => Dispose();

        public void Dispose()
        {
            ArrayPool<T>.Shared.Return(_buffer);
        }
    }

    #endregion

    public IMemoryOwner<T> Allocate<T>(int length, bool clean = false)
    {
        return new ArrayMemoryPoolBuffer<T>(length, clean);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}