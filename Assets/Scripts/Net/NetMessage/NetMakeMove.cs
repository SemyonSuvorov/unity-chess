using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

public class NetMakeMove : NetMessage
{
    public int OriginalX { get; set; }
    public int OriginalY { get; set; }
    public int DestinationX { get; set; }
    public int DestinationY { get; set; }
    public int TeamId { get; set; }

    public NetMakeMove()
    {
        
    }
    public NetMakeMove(string data)
    {
        var parts = data.Split(',');
        if (parts.Length == 5 &&
            int.TryParse(parts[0], out int originalX) &&
            int.TryParse(parts[1], out int originalY) &&
            int.TryParse(parts[2], out int destinationX) &&
            int.TryParse(parts[3], out int destinationY) &&
            int.TryParse(parts[4], out int teamId))
        {
            OriginalX = originalX;
            OriginalY = originalY;
            DestinationX = destinationX;
            DestinationY = destinationY;
            TeamId = teamId;
        }
        else
        {
            Debug.LogError("Failed to parse NetMakeMove from data: " + data);
        }
    }
     public override string Serialize() => $"{OriginalX},{OriginalY},{DestinationX},{DestinationY},{TeamId}";

    public override void ReceivedOnServer()
    {
        Debug.Log($"MakeMove received on server: ({OriginalX}, {OriginalY}) -> ({DestinationX}, {DestinationY}), TeamId={TeamId}");
        NetUtility.S_MAKE_MOVE?.Invoke(this);
    }

    public override void ReceivedOnClient()
    {
        Debug.Log($"MakeMove received on client: ({OriginalX}, {OriginalY}) -> ({DestinationX}, {DestinationY}), TeamId={TeamId}");
        NetUtility.C_MAKE_MOVE?.Invoke(this);
    }
}
