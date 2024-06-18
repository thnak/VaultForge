namespace BusinessModels.Resources;

public static class PageRoutes
{
    public static class Account
    {
        public const string Name = "/Account";
        public const string Login = Name + "/login";
        public const string Logout = Name + "/logout";
        public const string Denied = Name + "/403";
    }
}