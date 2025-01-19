using System.Buffers;
using BrainNet.Service.Memory.Interfaces;

namespace BrainNet.Service.Memory.Implements;

internal sealed class MemoryAllocatorService : IMemoryAllocatorService
{
    #region ArrayMemoryPoolBuffer<T>

    private sealed class ArrayMemoryPoolBuffer<T> : IMemoryOwner<T>
    {
        private readonly ArrayPool<T> _pool;
        private readonly T[] _buffer;

        public Memory<T> Memory => _buffer.AsMemory();

        public ArrayMemoryPoolBuffer(ArrayPool<T> pool, int length, bool clean)
        {
            _pool = pool;
            _buffer = _pool.Rent(length);

            if (clean)
            {
                Array.Clear(_buffer, 0, length);
            }
        }

        ~ArrayMemoryPoolBuffer() => Dispose();

        public void Dispose()
        {
            _pool.Return(_buffer, clearArray: true);
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

    public IMemoryOwner<float> Allocate(int length, bool clean = false)
    {
        return new ArrayMemoryPoolBuffer<float>(_floatPool, length, clean);
    }

    public void Dispose()
    {
        //
    }
}