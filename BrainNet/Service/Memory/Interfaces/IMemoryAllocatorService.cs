using System.Buffers;

namespace BrainNet.Service.Memory.Interfaces;

public interface IMemoryAllocatorService : IDisposable
{
    public IMemoryOwner<TItem> Allocate<TItem>(int length, bool clean = false);
}