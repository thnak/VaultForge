using System.Numerics;
using BrainNet.Models.Vector;
using BrainNet.Service.Memory.Implements;
using BrainNet.Service.Memory.Interfaces;
using Microsoft.ML.OnnxRuntime;

namespace BrainNet.Service.Memory.Utils;

internal static class MemoryAllocatorExtensions
{
    public static MemoryTensorOwner<T> AllocateTensor<T>(this IMemoryAllocatorService allocator, TensorShape shape, bool clean = false) where T : unmanaged, INumber<T>
    {
        var memory = allocator.Allocate<T>(shape.Length, clean);

        return new MemoryTensorOwner<T>(memory, shape.Dimensions);
    }
    
    public static OrtValue CreateOrtValue(this MemoryTensor<float> tensor)
    {
        return CreateOrtValue(tensor.Buffer, tensor.Dimensions64);
    }

    private static OrtValue CreateOrtValue(this Memory<float> buffer, long[] shape)
    {
        return OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, buffer, shape);
    }
}