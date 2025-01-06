using System;
using TMPro;
using UnityEngine;

public enum cameraAngle
{
    menu = 0,
    whiteTeam = 1,
    blackTeam = 2
}

public class GameUI : MonoBehaviour
{
    public static GameUI Instance {get; set;}
    public Server server;
    public Client client;

    [SerializeField] private Animator menuAnimatior;
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private GameObject[] cameraAngles;

    public Action<bool> SETLocalGame;

    private void Awake() {

        Instance = this;
        RegisterEvents();
    }

    // Cameras
    public void ChangeCamera(cameraAngle index)
    {
        for (int i = 0; i < cameraAngles.Length; i++)
            cameraAngles[i].SetActive(false);

        cameraAngles[(int)index].SetActive(true);
    }

    //BUTTONS
    public void OnLocalGameButton() {
        menuAnimatior.SetTrigger("InGameMenu");
        SETLocalGame?.Invoke(true);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
    }
    public void OnOnlineGameButton() {
        menuAnimatior.SetTrigger("OnlineMenu");
    }
    public void OnOnlineHostButton() {
        SETLocalGame?.Invoke(false);
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        menuAnimatior.SetTrigger("HostMenu");
    }
    public void OnOnlineConnectButton() {
        SETLocalGame?.Invoke(false);
        client.Init(addressInput.text, 8007);
    }
    public void OnOnlineBackButton() {
        menuAnimatior.SetTrigger("StartMenu");
    }
    public void OnHostBackButton() {
        server.Shutdown();
        client.Shutdown();
        menuAnimatior.SetTrigger("OnlineMenu");

    }
    #region
    private void RegisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }
    private void UnergisterEvents()
    {
        NetUtility.C_START_GAME += OnStartGameClient;
    }

    private void OnStartGameClient(NetMessage msg)
    {
        // delete menuAnimator
        menuAnimatior.SetTrigger("InGameMenu");
    }
    #endregion
}