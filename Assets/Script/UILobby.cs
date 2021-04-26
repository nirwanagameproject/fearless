using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILobby : MonoBehaviour
{

    
    public static UILobby instance;

    [Header("Host Join")]
    [SerializeField] InputField joinInput;
    [SerializeField] Button joinButton;
    [SerializeField] Button hostButton;
    [SerializeField] Canvas lobbyCanvas;

    [Header("Lobby")]
    [SerializeField] Transform UIPlayerParrent;
    [SerializeField] GameObject UIPlayerPrefab;

    [SerializeField] Text matchIDText;
    [SerializeField] GameObject beginGameButton;
    void Start()
    {
        instance = this;
    }

    public void Host()
    {
        joinButton.interactable = false;
        joinInput.interactable = false;
        hostButton.interactable = false;

        Player.localPlayer.HostGame();
    }
    public void HostSuccess(bool success, string _matchID)
    {
        if (success)
        {
            lobbyCanvas.enabled = true;
            spawnPlayerPrefab(Player.localPlayer);
            beginGameButton.SetActive(true);
            matchIDText.text = _matchID;
        }
        else
        {
            joinButton.interactable = true;
            joinInput.interactable = true;
            hostButton.interactable = true;
            
        }
    }

    public void Join()
    {
        joinButton.interactable = false;
        joinInput.interactable = false;
        hostButton.interactable = false;

        Player.localPlayer.JoinGame(joinInput.text.ToUpper());
    }
    public void JoinSuccess(bool success,string _matchID)
    {
        if (success)
        {
            lobbyCanvas.enabled = true;
            spawnPlayerPrefab(Player.localPlayer);
            matchIDText.text = _matchID;
        }
        else
        {
            joinButton.interactable = true;
            joinInput.interactable = true;
            hostButton.interactable = true;

        }
    }
    public void BeginGame()
    {
        Player.localPlayer.BeginGame();
    }
    public void spawnPlayerPrefab(Player player)
    {
        GameObject newUIPlayer = Instantiate(UIPlayerPrefab,UIPlayerParrent);
        newUIPlayer.GetComponent<UIPlayer>().setPlayer(player);
        newUIPlayer.transform.SetSiblingIndex(player.playerIndex-1);
    }
}
