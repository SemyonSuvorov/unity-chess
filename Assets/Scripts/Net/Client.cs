using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class Client : MonoBehaviour
{
    public static Client Instance { get; private set; }

    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;
    private bool isActive = false;

    public Action connectionDropped;

    private void Awake()
    {
        Instance = this;
    }

    public void Init(string ip, ushort port)
    {
        udpClient = new UdpClient();
        serverEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        Debug.Log("Attempting to connect to server at " + ip + ":" + port);
        isActive = true;

        RegisterToEvent();
    }

    public void Shutdown()
    {
        if (isActive)
        {
            UnergisterToEvent();
            udpClient.Close();
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
            if (udpClient.Available > 0)
            {
                var receivedData = udpClient.Receive(ref serverEndPoint);
                HandleMessage(Encoding.UTF8.GetString(receivedData));
            }
        }
        catch (Exception e)
        {
            Debug.Log("Error in receiving data: " + e.Message);
            connectionDropped?.Invoke();
            Shutdown();
        }
    }

    private void HandleMessage(string message)
    {
        Debug.Log("Message from server: " + message);
    }

    public void SendToServer(string message)
    {
        if (!isActive) return;

        try
        {
            var data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, serverEndPoint);
        }
        catch (Exception e)
        {
            Debug.Log("Error sending data: " + e.Message);
        }
    }

    private void RegisterToEvent()
    {
        // В будущем переделаем с использованием более высокого уровня
    }

    private void UnergisterToEvent()
    {
        // В будущем переделаем с использованием более высокого уровня
    }
}