using System;
using System.Text;
using UnityEngine;

public enum OpCode
{
    KEEP_ALIVE = 1,
    WELCOME = 2,
    START_GAME = 3,
    MAKE_MOVE = 4,
    REMATCH = 5
}

public static class NetUtility
{
    public static void OnData(byte[] data, Server server = null)
    {
        NetMessage msg = null;
        if (data.Length == 0)
        {
            Debug.LogError("Received empty data");
            return;
        }

        var opCode = (OpCode)data[0];
        var payload = new ArraySegment<byte>(data, 1, data.Length - 1);
        var payloadString = Encoding.UTF8.GetString(payload);

        switch (opCode)
        {
            case OpCode.KEEP_ALIVE:
                msg = new NetKeepAlive(payloadString);
                break;
            case OpCode.WELCOME:
                msg = new NetWelcome(payloadString);
                break;
            case OpCode.START_GAME:
                msg = new NetStartGame(payloadString);
                break;
            case OpCode.MAKE_MOVE:
                msg = new NetMakeMove(payloadString);
                break;
            case OpCode.REMATCH:
                msg = new NetRematch(payloadString);
                break;
            default:
                Debug.LogError("Message received had no OpCode");
                break;
        }

        if (server != null)
            msg?.ReceivedOnServer();
        else
            msg?.ReceivedOnClient();
    }

    // Net messages
    public static Action<NetMessage> C_KEEP_ALIVE;
    public static Action<NetMessage> C_WELCOME;
    public static Action<NetMessage> C_START_GAME;
    public static Action<NetMessage> C_MAKE_MOVE;
    public static Action<NetMessage> C_REMATCH;

    public static Action<NetMessage> S_KEEP_ALIVE;
    public static Action<NetMessage> S_WELCOME;
    public static Action<NetMessage> S_START_GAME;
    public static Action<NetMessage> S_MAKE_MOVE;
    public static Action<NetMessage> S_REMATCH;
}