namespace XREngine.Data.Server
{
    public class ServerResponseObject(EServerResponseCode code)
    {
        public EServerResponseCode Code { get; } = code;
    }
}
