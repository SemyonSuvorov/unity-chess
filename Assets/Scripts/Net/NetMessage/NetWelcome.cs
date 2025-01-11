using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetWelcome : NetMessage
{
    public int AssignedTeam { get; set; }

    public NetWelcome(string data)
    {
        if (int.TryParse(data, out int team))
        {
            AssignedTeam = team;
        }
        else
        {
            Debug.LogError("Failed to parse AssignedTeam from data: " + data);
        }
    }
    public override string Serialize() => AssignedTeam.ToString();

    public override void ReceivedOnServer()
    {
        Debug.Log("Welcome received on server with team: " + AssignedTeam);
        NetUtility.S_WELCOME?.Invoke(this);
    }

    public override void ReceivedOnClient()
    {
        Debug.Log("Welcome received on client with team: " + AssignedTeam);
        NetUtility.C_WELCOME?.Invoke(this);
    }
}