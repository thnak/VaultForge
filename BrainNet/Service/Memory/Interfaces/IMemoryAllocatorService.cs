using System.Buffers;

namespace BrainNet.Service.Memory.Interfaces;

public interface IMemoryAllocatorService : IDisposable
{
    public IMemoryOwner<T> Allocate<T>(int length, bool clean = false);
}