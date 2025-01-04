using System.Buffers;
using BrainNet.Service.Memory.Interfaces;

namespace BrainNet.Service.Memory.Implements;

internal sealed class MemoryAllocatorService : IMemoryAllocatorService
{
    #region ArrayMemoryPoolBuffer<T>

    private sealed class ArrayMemoryPoolBuffer<T> : IMemoryOwner<T>
    {
        private readonly ArrayPool<T> _pool;
        private readonly int _length;
        private T[]? _buffer;
        private bool _disposed;

        public Memory<T> Memory
        {
            get
            {
#if DEBUG
                if (_disposed) throw new ObjectDisposedException(nameof(ArrayMemoryPoolBuffer<T>));
#endif
                return new Memory<T>(_buffer!, 0, _length);
            }
        }

        public ArrayMemoryPoolBuffer(ArrayPool<T> pool, int length, bool clean)
        {
            _pool = pool;
            _buffer = _pool.Rent(length);

            if (clean)
            {
                Array.Clear(_buffer, 0, length);
            }

            _length = length;
        }

        ~ArrayMemoryPoolBuffer() => Dispose();

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_buffer != null)
                {
                    _pool.Return(_buffer);
                    _buffer = null;
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }
    }

    #endregion

    private readonly ArrayPool<float> _floatPool;

    public MemoryAllocatorService(int maxArrayLength = 1024, int maxArraysPerBucket = 50)
    {
        _floatPool = ArrayPool<float>.Create(maxArrayLength, maxArraysPerBucket);
    }

    public MemoryAllocatorService()
    {
        _floatPool = ArrayPool<float>.Create();
    }

    public IMemoryOwner<T> Allocate<T>(int length, bool clean = false)
    {
        return new ArrayMemoryPoolBuffer<T>((ArrayPool<T>)(object)_floatPool, length, clean);
    }

    public void Dispose()
    {
        // If other resources are added later, release them here.
        // GC.SuppressFinalize(this);
    }
}