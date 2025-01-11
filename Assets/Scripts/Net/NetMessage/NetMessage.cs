public abstract class NetMessage
{
    public abstract string Serialize();
    public abstract void ReceivedOnServer();
    public abstract void ReceivedOnClient();
}