using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;

/*
 * MainMenu Manager
 * - berisi fungsi-fungsi untuk button-button play game,option,credits dan quit
 */

public class AutoConnect : MonoBehaviour
{
    [Header("GameObject Setting")]
    [SerializeField] GameObject buttonPlay;
    [SerializeField] GameObject buttonOption;
    [SerializeField] GameObject buttonCredits;
    [SerializeField] GameObject buttonQuit;
    [SerializeField] NetworkManager networkManager;

    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonPlay);
        Cursor.lockState = CursorLockMode.None;
    }

    // fungsi untuk play game button
    public void JoinLobby()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonPlay);

        networkManager.networkAddress = "localhost";
        networkManager.StartClient();
    }

    //fungsi untuk quit button
    public void Quit()
    {
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(buttonQuit);

        Application.Quit();
    }
}
