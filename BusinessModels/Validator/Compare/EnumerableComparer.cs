namespace BusinessModels.Validator.Compare;

public static class EnumerableComparer
{
    public static bool Equal(this List<Dictionary<string, string>> self, List<Dictionary<string, string>> compare)
    {
        if (self.Count != compare.Count) return false;
        for (int i = 0; i < self.Count; i++)
        {
            var eleA = self[i];
            var eleB = compare[i];
            foreach (var pair in eleA)
            {
                if (eleB.TryGetValue(pair.Key, out string? value))
                {
                    if (value != pair.Value) return false;
                }
                else
                {
                    return false;
                }
            }
        }

        return true;
    }
}