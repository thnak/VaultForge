namespace BusinessModels.Utils;

public static class EnumerableExtensions
{
    public static IEnumerable<T> WhereWithCancellation<T>(this IEnumerable<T> source, Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        foreach (var item in source.TakeWhile(_ => !cancellationToken.IsCancellationRequested))
        {
            if (predicate(item))
            {
                yield return item;
            }
        }
    }
}