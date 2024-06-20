namespace BusinessModels.Utils;

public static class StringExtension
{
    public static string AutoReplace(this string self, IEnumerable<string> text2Replace)
    {
        int index = 0;
        foreach (var text in text2Replace)
        {
            self = self.Replace($"{index}", text);
            index++;
        }
        return self;
    }
}