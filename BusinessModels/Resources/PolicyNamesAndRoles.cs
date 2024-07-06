namespace BusinessModels.Resources;

public static class PolicyNamesAndRoles
{
    public const string Over18 = "Over18";
    public const string Over14 = "Over14";
    public const string Over7 = "Over7";


    public static class System
    {
        public const string Name = "System";
        public const string Roles = $"{Name},Admin,{Over7},{Over14},{Over18}";
    }

    public static class Admin
    {
        public const string Name = "Admin";
        public const string Roles = $"Admin,{Over7},{Over14},{Over18}";
    }

    public static class LimitRate
    {
        public const string Fixed = "Fixed";
        public const string Sliding = "Sliding";
        public const string Token = "Token";
        public const string Concurrency = "Concurrency";
    }
}