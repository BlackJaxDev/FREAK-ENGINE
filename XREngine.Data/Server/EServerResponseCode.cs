namespace XREngine.Data.Server
{
    public enum EServerResponseCode
    {
        InternalServerError = 0,
        Success = 1,
        InvalidTokenJson = 2,
        InvalidToken = 3,
        InvalidCommand = 4,
        NoAuthority = 5,
        AuthenticationFailed = 6,
        AlreadyRegistered = 7,
    }
}
