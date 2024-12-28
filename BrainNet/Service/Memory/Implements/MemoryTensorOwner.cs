using System.Buffers;
using BrainNet.Models.Vector;

namespace BrainNet.Service.Memory.Implements;

internal class MemoryTensorOwner<T>(IMemoryOwner<T> owner, int[] dimensions) : IDisposable where T : unmanaged
{
    public MemoryTensor<T> Tensor { get; } = new(owner.Memory, dimensions);

    ~MemoryTensorOwner() => Dispose();

    public void Dispose()
    {
        owner.Dispose();
    }
}