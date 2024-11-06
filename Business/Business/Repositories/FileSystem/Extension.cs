namespace Business.Business.Repositories.FileSystem;

public static class Extension
{
    public static string GetVectorName(this string name, string provider)
    {
        return $"{provider};{name}";
    }
}