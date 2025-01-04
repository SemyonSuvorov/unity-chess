using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance {get; set;}
    public Server server;
    public Client client;

    [SerializeField] private Animator menuAnimatior;
    [SerializeField] private TMP_InputField addressInput;

    private void Awake() {

        Instance = this;
    }

    //BUTTONS
    public void OnLocalGameButton() {
        menuAnimatior.SetTrigger("InGameMenu");
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
    }
    public void OnOnlineGameButton() {
        menuAnimatior.SetTrigger("OnlineMenu");
    }
    public void OnOnlineHostButton() {
        server.Init(8007);
        client.Init("127.0.0.1", 8007);
        menuAnimatior.SetTrigger("HostMenu");
    }
    public void OnOnlineConnectButton() {
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
}