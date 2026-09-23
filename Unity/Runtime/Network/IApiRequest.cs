namespace Portfolio.Game.Network
{
    public interface IApiRequestPayload
    {
        string ToJson();
    }

    public interface IApiRequest<TResponse>
    {
        string Endpoint { get; }
        AuthType AuthType { get; }
    }
}
