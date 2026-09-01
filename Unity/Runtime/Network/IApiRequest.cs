namespace Portfolio.Game.Network
{
    public interface IApiRequest<TResponse>
    {
        string Endpoint { get; }
        AuthType AuthType { get; }
    }
}
