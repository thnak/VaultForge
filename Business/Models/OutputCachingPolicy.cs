namespace Business.Models;

public static class OutputCachingPolicy
{
    public static readonly TimeSpan Expire10 = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan Expire20 = TimeSpan.FromSeconds(20);
    public static readonly TimeSpan Expire30 = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan Expire40 = TimeSpan.FromSeconds(40);
    public static readonly TimeSpan Expire50 = TimeSpan.FromSeconds(50);
    public static readonly TimeSpan Expire60 = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan Expire120 = TimeSpan.FromSeconds(120);
    public static readonly TimeSpan Expire240 = TimeSpan.FromSeconds(240);
}