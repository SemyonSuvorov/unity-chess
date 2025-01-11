using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Server : MonoBehaviour
{
    public static Server Instance { get; private set; }

    private UdpClient udpServer;
    private IPEndPoint clientEndPoint;
    private bool isActive = false;

    private const float keepAliveTickRate = 20.0f;
    private float lastKeepAlive;

    public Action connectionDropped;

    private void Awake()
    {
        Instance = this;
    }

    public void Init(ushort port)
    {
        udpServer = new UdpClient(port);
        Debug.Log("Server started on port " + port);
        isActive = true;
    }

    public void Shutdown()
    {
        if (isActive)
        {
            udpServer.Close();
            isActive = false;
        }
    }

    private void OnDestroy()
    {
        Shutdown();
    }

    private void Update()
    {
        if (!isActive) return;

        try
        {
            while (udpServer.Available > 0)
            {
                var receivedData = udpServer.Receive(ref clientEndPoint);
                HandleMessage(Encoding.UTF8.GetString(receivedData));
            }

            KeepAlive();
        }
        catch (Exception e)
        {
            Debug.Log("Error in receiving data: " + e.Message);
        }
    }

    private void KeepAlive()
    {
        if (Time.time - lastKeepAlive > keepAliveTickRate)
        {
            lastKeepAlive = Time.time;
            Broadcast("KEEP_ALIVE");
        }
    }

    private void HandleMessage(string message)
    {
        Debug.Log("Message from client: " + message);
    }

    public void Broadcast(string message)
    {
        if (!isActive || clientEndPoint == null) return;

        try
        {
            var data = Encoding.UTF8.GetBytes(message);
            udpServer.Send(data, data.Length, clientEndPoint);
        }
        catch (Exception e)
        {
            Debug.Log("Error broadcasting data: " + e.Message);
        }
    }
    public void SendToClient(string message)
    {
        Broadcast(message);
    }
}
