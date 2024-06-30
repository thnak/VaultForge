namespace BusinessModels.Resources;

public static class PageRoutes
{
    public static class Account
    {
        public const string Name = "/Account";
        public const string Profile = Name + "/Profile";
        public const string SignIn = Name + "/login";
        public const string SignInError = SignIn + "/error";

        public const string SignUp = Name + "register";
        public const string SignUpError = SignUp + "/error";
        
        public const string Logout = Name + "/logout";
        public const string Denied = Name + "/403";
    }
}