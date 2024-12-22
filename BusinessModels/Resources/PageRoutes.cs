namespace BusinessModels.Resources;

public static class PageRoutes
{
    public static class Error
    {
        public const string Name = "/Error";

        public static class GeneralError
        {
            public const string Src = Name + "/error";
            public const string Title = "Error";

            public static readonly List<Dictionary<string, string>> MetaData =
            [
                new()
                {
                    { "name", "description" },
                    {
                        "content", "a general error was reported"
                    }
                }
            ];
        }

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
                    {
                        "content", "My Drive"
                    }
                }
            ];
        }

        public static class Shared
        {
            public const string Src = Name + "/shared";
            public const string Title = "Shared drive";
        }

        public static class Trash
        {
            public const string Src = Name + "/trash";
            public const string Title = "trash";
        }
    }

    public static class InternetOfThings
    {
        public const string Name = "/internet-of-things";
        public const string DeviceManagement = Name + "/device-management";
        public const string DeviceGroupManagement = Name + "/device-group-management";
        public const string SensorManagement = Name + "/sensor-management";
        
        public const string IotRecord = Name + "/iot-record";
    }
    
    public static class Account
    {
        public const string Name = "/Account";
        public const string Profile = Name + "/profile";

        public static class SignIn
        {
            public const string Src = Name + "/login";
            public const string Title = "Login to your account";

            public static readonly List<Dictionary<string, string>> MetaData =
            [
                new()
                {
                    { "name", "description" },
                    {
                        "content", "login to your account"
                    }
                }
            ];
        }


        public static class SignInError
        {
            public const string Src = Name + "/login-error";
            public const string Title = "Login to your account error";

            public static readonly List<Dictionary<string, string>> MetaData =
            [
                new()
                {
                    { "name", "description" },
                    {
                        "content", "login to your account has an error"
                    }
                }
            ];
        }


        public const string SignUp = Name + "register";
        public const string SignUpError = SignUp + "/error";

        public const string Logout = Name + "/logout";
        public const string Denied = Name + "/403";
    }

    public static class Advertisement
    {
        public const string Name = "/advertisement";

        public static class Index
        {
            public const string Src = Name + "/page";
            public const string Title = "";
            public static readonly List<Dictionary<string, string>> MetaData = [];
        }
    }

    public static class ContentCreator
    {
        public const string Name = "contentcreator";

        public static class Index
        {
            public const string Src = Name + "/page";
            public const string Title = "Content Creator";
            public static readonly List<Dictionary<string, string>> MetaData = [];
        }

        public static class Management
        {
            public const string Src = Name + "/management";
        }

        public static class Preview
        {
            public const string Src = Name + "/preview";
        }
    }
}