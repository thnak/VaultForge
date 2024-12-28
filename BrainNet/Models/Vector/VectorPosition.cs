using System.Numerics;

namespace BrainNet.Models.Vector;

/// <summary>
/// https://github.com/dme-compunet/YoloSharp/blob/1440383c608ade905866037650dbbbd8237e2b63/Source/YoloSharp/Base/Vector.cs#L3
/// </summary>
/// <param name="x"></param>
/// <param name="y"></param>
/// <typeparam name="T"></typeparam>
public readonly struct VectorPosition<T>(T x, T y) where T : INumber<T>
{
    public T X => x;

    public T Y => y;

    public override string ToString() => $"X = {x}, Y = {y}";

    public static implicit operator VectorPosition<T>(ValueTuple<T, T> tuple) => new(tuple.Item1, tuple.Item2);
}