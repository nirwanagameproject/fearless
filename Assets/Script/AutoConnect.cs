using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;

public class AutoConnect : MonoBehaviour
{
    [SerializeField] GameObject buttonPlay;
    [SerializeField] GameObject buttonOption;
    [SerializeField] GameObject buttonCredits;
    [SerializeField] GameObject buttonQuit;
    [SerializeField] NetworkManager networkManager;
    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonPlay);
    }
    // Start is called before the first frame update
    public void JoinLobby()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonPlay);

        networkManager.networkAddress = "localhost";
        networkManager.StartClient();
    }

    public void Quit()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonQuit);

        Application.Quit();
    }
}
