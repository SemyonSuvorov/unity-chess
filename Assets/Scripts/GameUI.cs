using UnityEngine;

public class GameUI : MonoBehaviour
{
    public static GameUI Instance {get; set;}

    [SerializeField] private Animator menuAnimatior;

    private void Awake() {
        Instance = this;
    }

    //BUTTONS
    public void OnLocalGameButton() {
        menuAnimatior.SetTrigger("InGameMenu");
    }
    public void OnOnlineGameButton() {
        menuAnimatior.SetTrigger("OnlineMenu");
    }
    public void OnOnlineHostButton() {
        menuAnimatior.SetTrigger("HostMenu");

    }
    public void OnOnlineConnectButton() {
        Debug.Log("OnOnlineConnectButton");
    }
    public void OnOnlineBackButton() {
        menuAnimatior.SetTrigger("StartMenu");
    }
    public void OnHostBackButton() {
        menuAnimatior.SetTrigger("OnlineMenu");

    }
}