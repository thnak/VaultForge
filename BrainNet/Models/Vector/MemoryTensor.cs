﻿using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BrainNet.Models.Vector;

/// <summary>
/// https://github.com/dme-compunet/YoloSharp/blob/1440383c608ade905866037650dbbbd8237e2b63/Source/YoloSharp/Memory/MemoryTensor.cs#L3
/// </summary>
/// <typeparam name="T"></typeparam>
public class MemoryTensor<T> where T : unmanaged
{
    private readonly Memory<T> _buffer;

    public int[] Strides { get; }

    public int[] Dimensions { get; }

    public long[] Dimensions64 { get; }

    public Span<T> Span => _buffer.Span;

    public Memory<T> Buffer => _buffer;

    public T this[int index0, int index1, int index2]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Span[GetIndex(index0, index1, index2)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Span[GetIndex(index0, index1, index2)] = value;
    }

    public T this[int index0, int index1, int index2, int index3]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Span[GetIndex(index0, index1, index2, index3)];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Span[GetIndex(index0, index1, index2, index3)] = value;
    }

    public MemoryTensor(Memory<T> buffer, int[] dimensions)
    {
        Strides = GetStrides(dimensions);
        Dimensions = dimensions;
        Dimensions64 = [.. dimensions.Select(x => (long)x)];
        
        _buffer = buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetIndex(int index0, int index1, int index2)
    {
        Debug.Assert(Strides.Length == 3);

        return (Strides[0] * index0)
               + (Strides[1] * index1)
               + (Strides[2] * index2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetIndex(int index0, int index1, int index2, int index3)
    {
        Debug.Assert(Strides.Length == 4);

        return (Strides[0] * index0)
               + (Strides[1] * index1)
               + (Strides[2] * index2)
               + (Strides[3] * index3);
    }

    private static int[] GetStrides(ReadOnlySpan<int> dimensions)
    {
        if (dimensions.Length == 0)
        {
            return [];
        }

        var strides = new int[dimensions.Length];
        var stride = 1;

        for (var i = strides.Length - 1; i >= 0; i--)
        {
            strides[i] = stride;

            stride *= dimensions[i];
        }

        return strides;
    }
}