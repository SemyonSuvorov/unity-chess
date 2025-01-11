using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetKeepAlive : NetMessage
{
    private string payload;

    public NetKeepAlive(string data)
    {
        payload = data;
    }
    public override string Serialize() => payload;

    public override void ReceivedOnServer()
    {
        Debug.Log("KeepAlive received on server: " + payload);
    }

    public override void ReceivedOnClient()
    {
        Debug.Log("KeepAlive received on client: " + payload);
    }
}
