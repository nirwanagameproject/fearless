using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class AutoConnect : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    // Start is called before the first frame update
    public void JoinLobby()
    {
        networkManager.networkAddress = "localhost";
        networkManager.StartClient();
    } 
}
