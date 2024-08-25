namespace BusinessModels.Resources;

public static class PageRoutes
{
    public static class Error
    {
        public const string Name = "/Error";
        public const string ErrorPage = Name + "/error-page";
        public const string Default403 = "/403";
        public const string NotFound = Name + "/404";
        public const string Default404 = "/404";
        public const string UnAuthorized = Name + "/403";
    }

    public static class Home
    {
        public static class Root
        {
            public const string Src = "/home";
            public const string Title = "Home";

            public static readonly List<Dictionary<string, string>> MetaData =
            [
                new()
                {
                    { "name", "description" },
                    { "content", "My home page" }
                }
            ];
        }
    }

    public static class Drive
    {
        public const string Name = "/drive";

        public static class Index
        {
            public const string Src = Name + "/home";
            public const string Title = "Drive";
            public static readonly List<Dictionary<string, string>> MetaData =
            [
                new()
                {
                    { "name", "description" },
                    { "content", "My Drive"
                    }
                }
            ];
        }

        public static class Shared
        {
            public const string Src = Name + "/shared";
            public const string Title = "Shared drive";

            public static readonly List<Dictionary<string, string>> MetaData =
            [
                new()
                {
                    { "name", "description" },
                    { "content", "Share drive to community"
                }
                }
            ];
        }
        
        
    }

    public static class Account
    {
        public const string Name = "/Account";
        public const string Profile = Name + "/profile";
        public const string SignIn = Name + "/login";
        public const string SignInError = SignIn + "/error";

        public const string SignUp = Name + "register";
        public const string SignUpError = SignUp + "/error";

        public const string Logout = Name + "/logout";
        public const string Denied = Name + "/403";
    }
}