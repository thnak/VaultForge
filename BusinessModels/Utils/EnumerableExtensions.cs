using System.Numerics;

namespace BusinessModels.Utils;

public static class EnumerableExtensions
{
    public static void Shuffle<T>(this T[] array)
    {
        Random rng = new Random();
        int n = array.Length;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            // Swap array[i] with array[j]
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    public static void Shuffle<T>(this List<T> array)
    {
        Random rng = new Random();
        int n = array.Count;
        for (int i = n - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            // Swap array[i] with array[j]
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

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

    public static List<List<T>> PackElementsIntoContainers<T>(this List<T> elementWidths, T containerWidth) where T : struct, INumber<T>
    {
        List<T> sortedElements = elementWidths.OrderByDescending(w => w).ToList();

        List<List<T>> containers = new List<List<T>>();

        foreach (var element in sortedElements)
        {
            bool placed = false;

            foreach (var container in containers)
            {
                if (container.CustomSum() + element <= containerWidth)
                {
                    container.Add(element);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                containers.Add([element]);
            }
        }

        return containers;
    }

    public static List<List<T>> PackElementsIntoContainers<T, TWidth>(this List<T> elements, TWidth containerWidth, Func<T, TWidth> widthSelector) where T : class where TWidth : struct, INumber<TWidth>
    {
        List<T> sortedElements = elements.OrderByDescending(widthSelector).ToList();

        List<List<T>> containers = new List<List<T>>();

        foreach (var element in sortedElements)
        {
            bool placed = false;

            foreach (var container in containers)
            {
                if (container.Sum(widthSelector) + widthSelector(element) <= containerWidth)
                {
                    container.Add(element);
                    placed = true;
                    break;
                }
            }

            if (!placed)
            {
                containers.Add([element]);
            }
        }

        return containers;
    }

    // Extension method to sum elements based on a selector
    private static TWidth Sum<T, TWidth>(this IEnumerable<T> source, Func<T, TWidth> selector) where TWidth : struct, INumber<TWidth>
    {
        TWidth sum = TWidth.Zero;
        foreach (var item in source)
        {
            sum += selector(item);
        }

        return sum;
    }

    private static T CustomSum<T>(this IEnumerable<T> source) where T : struct, INumber<T>
    {
        T sum = T.Zero;
        foreach (var value in source)
        {
            sum += value;
        }

        return sum;
    }
}