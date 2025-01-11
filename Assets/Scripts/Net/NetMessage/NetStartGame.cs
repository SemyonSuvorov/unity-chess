using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetStartGame : NetMessage
{
    private string payload;

    public NetStartGame(string data)
    {
        payload = data;
    }
    public override string Serialize() => payload;

    public override void ReceivedOnServer()
    {
        Debug.Log("StartGame received on server: " + payload);
        NetUtility.S_START_GAME?.Invoke(this);
    }

    public override void ReceivedOnClient()
    {
        Debug.Log("StartGame received on client: " + payload);
        NetUtility.C_START_GAME?.Invoke(this);
    }
}