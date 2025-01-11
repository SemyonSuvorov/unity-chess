using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetRematch : NetMessage
{
    public int TeamId { get; set; }
    public byte WantRematch { get; set; }
    public NetRematch()
    {
        
    }

    public NetRematch(string data)
    {
        var parts = data.Split(',');
        if (parts.Length == 2 &&
            int.TryParse(parts[0], out int teamId) &&
            byte.TryParse(parts[1], out byte wantRematch))
        {
            TeamId = teamId;
            WantRematch = wantRematch;
        }
        else
        {
            Debug.LogError("Failed to parse NetRematch from data: " + data);
        }
    }
    public override string Serialize() => $"{TeamId},{WantRematch}";

    public override void ReceivedOnServer()
    {
        Debug.Log($"Rematch received on server: TeamId={TeamId}, WantRematch={WantRematch}");
        NetUtility.S_REMATCH?.Invoke(this);
    }

    public override void ReceivedOnClient()
    {
        Debug.Log($"Rematch received on client: TeamId={TeamId}, WantRematch={WantRematch}");
        NetUtility.C_REMATCH?.Invoke(this);
    }
}