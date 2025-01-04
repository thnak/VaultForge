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

    public static MemoryTensorOwner<float> AllocateTensor(this IMemoryAllocatorService allocator, TensorShape shape, bool clean = false)
    {
        var memory = allocator.Allocate(shape.Length, clean);
        return new MemoryTensorOwner<float>(memory, shape.Dimensions);
    }

    public static OrtValue CreateOrtValue(this MemoryTensor<float> tensor)
    {
        return CreateOrtValue(tensor.Buffer, tensor.Dimensions64);
    }

    public static OrtValue CreateOrtValue(this Memory<float> buffer, long[] shape)
    {
        return OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, buffer, shape);
    }

    public static OrtValue CreateOrtValue(this float[] buffer, long[] shape)
    {
        return OrtValue.CreateTensorValueFromMemory(OrtMemoryInfo.DefaultInstance, buffer.AsMemory(), shape);
    }
}