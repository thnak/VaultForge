namespace BusinessModels.Resources;

public static class CookieNames
{
    public const string Name = "WhatupHomie";
    public const string AuthenticationType = Name + "IG8bjI4VZU";
#if DEBUG
    public const string Domain = "localhost";
#else
    public const string Domain = "thnakdevserver.ddns.net";

#endif
    public const string AuthorizeCookie = Name + "Cookie";
    public const string JwtTokenName = Name + "Jwt";
    public const string Session = Name + "Session";
    public const string Culture = Name + "Culture";
    public const string Antiforgery = Name + "Antiforgery";
}