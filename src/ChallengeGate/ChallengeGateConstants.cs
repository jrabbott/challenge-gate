using System.Diagnostics.CodeAnalysis;

namespace ChallengeGate;

[ExcludeFromCodeCoverage]
internal static class ChallengeGateConstants
{
    public const string DataProtectionPurpose = "ChallengeGate.Auth";
    public const string CookieValuePrefix = "Authorized";
    public const string RouteName = "ChallengeGate";
    public const string ControllerName = "Challenge";
    public const string ActionName = "Index";
    
    public static class Defaults
    {
        public const string CookieName = ".ChallengeGate.Auth";
        public const string ChallengePath = "/challenge";
        public const string Layout = "_Layout";
        public const string Title = "Enter password";
        public const string TokenQueryParamName = "token";
    }

    public static class ErrorMessages
    {
        public const string PasswordRequired = "Enter the password";
        public const string PasswordIncorrect = "The password you entered is incorrect";
    }
}
