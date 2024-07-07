namespace BusinessModels.Resources;

public static class CookieNames
{
#if DEBUG
    public const string Domain = "localhost";
    public const string Name = "WhatupHomie";
#else
    public const string Domain = "thnakdevserver.ddns.net";
    public const string Name = "thnakdevserver";
#endif
    public const string AuthenticationType = Name + "IG8bjI4VZU";
    public const string AuthorizeCookie = Name + "Cookie";
    public const string JwtTokenName = Name + "Jwt";
    public const string Session = Name + "Session";
    public const string Culture = Name + "Culture";
    public const string Antiforgery = Name + "Antiforgery";
}