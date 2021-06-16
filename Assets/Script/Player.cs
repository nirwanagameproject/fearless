using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Mirror;
using System;

/*
 * Kontrol Pemain
 * - berisi peletakan spawn pemain dan item
 * - pemilihan job pemain
 * - berisi kontrol keyboard dan mouse pemain
 * - penghilangan collision dengan object lain
 * - server dan client untuk button Host,Join,Begin dan Search
 * - berisi fungsi client dan server disconnect
 */

public class Player : NetworkBehaviour
{
    [Header("Object setting")]
    public static Player localPlayer;
    public static Transform localTransformPlayer;
    [SerializeField] Karakter typeObject;

    [Header("GameObject setting")]
    public Camera MainCamera;
    public NetworkMatchChecker networkMatchChecker;
    public GameObject pivot;
    public GameObject playerLobbyUI;
    [SerializeField] public NavMeshAgent navigasi;
    
    [Header("SyncVar setting")]
    [SyncVar] public string MatchID;
    [SyncVar] public int playerIndex;
    [SyncVar] public string interaksi;
    [SyncVar] public string direction;
    [SyncVar] public string direction2;
    [SyncVar] public string direction3;
    [SyncVar] public string direction4;
    [SyncVar] public string directionRot;
    [SyncVar] public string directionRot2;
    [SyncVar] public float InputMX;
    [SyncVar] public float InputMY;
    [SyncVar] public int playerType;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float maxTurnSpeed = 150f;

    // Pada saat mulai koneksi client
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (isLocalPlayer)
        {
            //inisialisasi Object Player
            localPlayer = this;
            localTransformPlayer = transform;
        }
        else
        {
            //inisialisasi GameObject UI Player di Lobby
            playerLobbyUI = UILobby.instance.spawnPlayerPrefab(this);
        }

        // inisialisasi direction dan rotasi awal
        direction = "idle";
        direction2 = "idle";
        direction3 = "idle";
        direction4 = "idle";
        directionRot = "idle";
        directionRot2 = "idle";

        // inisialiasi Player GameObject atribute
        playerType = 0;
        gameObject.tag = "Player";
        name = "Player "+playerIndex;
        transform.parent = GameObject.Find("PlayersSpawn").transform;

        // Membuat karakter baru untuk brave default
        typeObject = new Karakter(1);
    }

    // fungsi untuk meletakan posisi player
    // dipanggil di UILobby.cs pada saat sukses Join
    public void SpawnToPoint()
    {
        //memanggil fungsi untuk meletakan posisi player di server
        SpawnPlayerPoint();

        //meload semua Item di server  
        if (playerIndex == 1)
        {
            CmdSpawnObjects(MatchID);
        }
    }

    // fungsi meletakan posisi player di server
    [Command]
    public void SpawnPlayerPoint()
    {
        //mengabaikan collision dengan player lain yang sudah join
        for (int i = 0; i < MatchMaker.instance.matches.Count; i++)
        {
            if (MatchMaker.instance.matches[i].matchId != MatchID)
            {
                for (int j = 0; j < MatchMaker.instance.matches[i].players.Count; j++)
                {
                    Debug.Log("Ignore Match "+ MatchMaker.instance.matches[i].matchId +" Player "+ MatchMaker.instance.matches[i].players[j].GetComponent<Player>().playerIndex);
                    Physics.IgnoreCollision(gameObject.GetComponent<Collider>(), MatchMaker.instance.matches[i].players[j].GetComponent<Collider>());
                }
            }
        }

        //mengabaikan collision dengan item-item di matchId lain
        for (int k = 0; k < MatchMaker.instance.matches.Count; k++)
        {
            if (MatchMaker.instance.matches[k].matchId != MatchID)
            {
                for (int j = 0; j < MatchMaker.instance.matches[k].items.Count; j++)
                {
                    Transform[] allChildren = MatchMaker.instance.matches[k].items[j].GetComponentsInChildren<Transform>();
                    foreach (Transform child in allChildren)
                    {
                        if (child.GetComponent<Collider>() != null)
                        {
                            Debug.Log(child.parent.name);
                            Debug.Log(gameObject.GetComponent<Collider>().name + " Ignore " + child.GetComponent<Collider>().name);
                            Physics.IgnoreCollision(gameObject.GetComponent<Collider>(), child.GetComponent<Collider>());
                        }
                        Transform[] allChildren2 = child.GetComponentsInChildren<Transform>();
                        foreach (Transform child2 in allChildren2)
                        {
                            if (child2.GetComponent<Collider>() != null)
                            {
                                Debug.Log(gameObject.GetComponent<Collider>().name+" Ignore "+ child2.GetComponent<Collider>().name);
                                Physics.IgnoreCollision(gameObject.GetComponent<Collider>(), child2.GetComponent<Collider>());
                            }
                        }
                    }
                }
            }
        }

        //meletakan player sesuai posisi spawn
        transform.eulerAngles = new Vector3(0, 90, 0);
        transform.localPosition = GameObject.Find("PlayersSpawn").transform.Find("Spawn" + playerIndex).transform.localPosition;

        GetComponent<Rigidbody>().position = transform.position;
        GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
        GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);

    }

    //fungsi pemilihan karakter diserver
    [Command]
    public void CmdChoice(int _typePlayer)
    {
        //Jika player sudah dipilih maka fungsi selesai
        for(int i=0;i< MatchMaker.instance.matches.Count; i++)
        {
            if(MatchMaker.instance.matches[i].matchId == MatchID)
            {
                for(int j=0;j< MatchMaker.instance.matches[i].players.Count; j++)
                {
                    if(MatchMaker.instance.matches[i].players[j].GetComponent<Player>().playerType == _typePlayer)
                    {
                        Debug.Log("Player already Choice");
                        return;
                    }
                }
            }
        }

        //pemilihan job player
        playerType = _typePlayer;
        typeObject = new Karakter(_typePlayer);
        typeObject.getCharacter().changeColor(this.gameObject);

        //kembali ke client dengan job yang dipilih
        TargetChangePlayer(playerType);
    }

    //mengubah job player disemua client
    [ClientRpc]
    public void TargetChangePlayer(int _playerType)
    {
        typeObject.setCharacter(_playerType);
        typeObject.getCharacter().Mulai();
        typeObject.getCharacter().changeColor(this.gameObject);
    }

    //fungsi menampilkan item-item disetiap match di server
    [Command]
    public void CmdSpawnObjects(string _matchId)
    {
        //GameObject go = Instantiate(Resources.Load<GameObject>("Prefab/Map1/book"), new Vector3(0, 0, 0), Quaternion.EulerAngles(0, 0, 0));
        //NetworkServer.Spawn(go, connectionToClient);

        //memanggil fungsi spawn item di ObjectManager
        ObjectManager.instance.SpawnObject(_matchId);
    }

    //pada saat client disconnect
    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("Client stop");
        ClientDisconnect(0);
    }

    //pada saat server disconnect
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Client stop from server");
        ServerDisconnect(0);
    }

    // Inisialisasi awal player
    void Awake()
    {
        DontDestroyOnLoad(this);
        networkMatchChecker = GetComponent<NetworkMatchChecker>();
    }

    /*
     Host Game
     */
    public void HostGame(bool publicMatch)
    {
        string matchId = MatchMaker.getRandomMatchId();
        CmdHostGame(matchId,publicMatch);
    }
    [Command]
    void CmdHostGame(string matchId,bool publicMatch)
    {
        MatchID = matchId;
        if (MatchMaker.instance.HostGame(matchId, gameObject,publicMatch,out playerIndex))
        {
            Debug.Log("Game Hosted Successfully");
            networkMatchChecker.matchId = matchId.ToGuid();
            TargetHostGame(true,matchId,playerIndex);
            TargetHostGameAll(playerIndex);
        }
        else
        {
            Debug.Log("Game Hosted Failed");
            TargetHostGame(false, matchId,playerIndex);
            TargetHostGameAll(playerIndex);
        }
    }
    [TargetRpc]
    void TargetHostGame(bool _success, string _matchId,int _playerIndex)
    {
        playerIndex = _playerIndex;
        MatchID = _matchId;
        UILobby.instance.HostSuccess(_success,_matchId);
    }
    [ClientRpc]
    void TargetHostGameAll(int _playerIndex)
    {
        name = "Player " + playerIndex;
    }
    /*
     Join Game
     */
    public void JoinGame(string inputId)
    {
        CmdJoinGame(inputId);
    }
    [Command]
    void CmdJoinGame(string matchId)
    {
        MatchID = matchId;
        if (MatchMaker.instance.JoinGame(matchId, gameObject, out playerIndex))
        {
            Debug.Log("Game Joined Successfully");
            networkMatchChecker.matchId = matchId.ToGuid();
            TargetJoinGame(true, matchId,playerIndex);
            TargetJoinGameAll(playerIndex);
        }
        else
        {
            Debug.Log("Game Joined Failed");
            TargetJoinGame(false, matchId,playerIndex);
            TargetJoinGameAll(playerIndex);
        }
    }
    [TargetRpc]
    void TargetJoinGame(bool _success, string _matchId,int _playerIndex)
    {
        playerIndex = _playerIndex;
        MatchID = _matchId;
        UILobby.instance.JoinSuccess(_success,_matchId);
    }
    [ClientRpc]
    void TargetJoinGameAll(int _playerIndex)
    {
        name = "Player " + playerIndex;
    }
    /*
     Search Match
     */
    public void SearchGame()
    {
        CmdSearchGame();
    }
    [Command]
    void CmdSearchGame()
    {
        if (MatchMaker.instance.SearchGame(gameObject, out playerIndex,out MatchID))
        {
            Debug.Log("Game Search Successfully");
            networkMatchChecker.matchId = MatchID.ToGuid();
            TargetSearchGame(true, MatchID, playerIndex);
            TargetJoinGameAll(playerIndex);
        }
        else
        {
            Debug.Log("Game Search Failed");
            TargetSearchGame(false, MatchID, playerIndex);
            TargetJoinGameAll(playerIndex);
        }
    }
    [TargetRpc]
    public void TargetSearchGame(bool _success, string _matchId, int _playerIndex)
    {
        playerIndex = _playerIndex;
        MatchID = _matchId;
        UILobby.instance.SearchSuccess(_success, _matchId);
    }
    /*
     Begin Game
     */
    public void BeginGame()
    {
        CmdBeginGame();
    }
    [Command]
    void CmdBeginGame()
    {
        for (int i = 0; i < MatchMaker.instance.matches.Count; i++)
        {
            if (MatchMaker.instance.matches[i].matchId == MatchID)
            {
                for (int j = 0; j < MatchMaker.instance.matches[i].players.Count; j++)
                {
                    if (MatchMaker.instance.matches[i].players[j].GetComponent<Player>().playerType == 0)
                    {
                        Debug.Log("Please choice player");
                        return;
                    }
                }
            }
        }
        MatchMaker.instance.BeginGame(MatchID);
        Debug.Log("Game Begin"+ MatchID);
        
        
    }

    public void StartGame()
    {
        TargetBeginGame();
    }
    [TargetRpc]
    void TargetBeginGame()
    {
        GameObject.Find("NetworkManager").GetComponent<NetworkManager>().onlineScene = "Gameplay";
        GameObject.Find("NetworkManager").GetComponent<NetworkManager>().ServerChangeScene("Gameplay");
        Cursor.lockState = CursorLockMode.Locked;
        //SceneManager.LoadScene(2,LoadSceneMode.Additive);
    }

    /*
     Disconnect Match
     */
    
    public void DisconnectGame(int lobbyScene)
    {
        CmdDisconnectGame(lobbyScene);
    }
    [Command]
    public void CmdDisconnectGame(int lobbyScene)
    {
        ServerDisconnect(lobbyScene);
    }
    public void ServerDisconnect(int lobbyScene)
    {
        MatchMaker.instance.PlayerDisconnected(this, MatchID);
        networkMatchChecker.matchId = string.Empty.ToGuid();
        RpcDisconnectGame(lobbyScene);
    }
    [ClientRpc]
    public void RpcDisconnectGame(int lobbyScene)
    {
        ClientDisconnect(lobbyScene);
    }
    public void ClientDisconnect(int lobbyScene)
    {
        if (SceneManager.GetActiveScene().name != "Gameplay")
        {
            if (playerLobbyUI != null)
            {
                for (int i = 0; i < GameObject.FindGameObjectsWithTag("Player").Length; i++)
                {
                    if (GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>().playerIndex > playerIndex)
                    {
                        GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>().playerIndex -= 1;
                        GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>().playerLobbyUI.GetComponent<UIPlayer>().setPlayer(GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>());
                    }
                }
                Destroy(playerLobbyUI);
            }
            if (lobbyScene == 0)
            {
                Destroy(GameObject.Find("ItemSpawn").gameObject);
                Destroy(GameObject.Find("PlayersSpawn").gameObject);
                Destroy(GameObject.Find("ObjectSpawn").gameObject);
            }
        }
    }

    // fungsi dijalankan per frame
    void Update()
    {
        //dijalankan di server
        if (isServer)
        {
            //inisialisasi GameObject pivot
            pivot = transform.Find("pivot").gameObject;

            //jika direction ke jalan kiri
            if (direction2 == "left")
            {
                float distance = moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y - 90;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX, 0, jalanZ));
            }

            //jika direction ke jalan kanan
            if (direction3 == "right")
            {
                float distance = moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y + 90;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX, 0, jalanZ));
            }

            //jika direction ke maju
            if (direction == "up")
            {
                float distance = moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX, 0, jalanZ));
            }

            //jika direction ke mundur
            if (direction4 == "down")
            {
                float distance = -moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX, 0, jalanZ));
            }

            //jika direction ke rotasi ke atas
            if (directionRot2 == "uprot")
            {
                pivot.transform.Rotate(new Vector3(InputMY, 0, 0) * Time.deltaTime * -maxTurnSpeed * 2f);
            }

            //jika direction ke rotasi ke bawah
            if (directionRot2 == "downrot")
            {
                pivot.transform.Rotate(new Vector3(InputMY, 0, 0) * Time.deltaTime * -maxTurnSpeed * 2f);
            }

            //jika direction ke rotasi ke kiri
            if (directionRot == "left")
            {
                transform.Rotate(new Vector3(0, InputMX, 0) * Time.deltaTime * maxTurnSpeed * 2f);
            }

            //jika direction ke rotasi ke kanan
            if (directionRot == "right")
            {
                transform.Rotate(new Vector3(0, InputMX, 0) * Time.deltaTime * maxTurnSpeed * 2f);
            
            }

            //memindahkan posisi kamera dan flashlight sesuai posisi player
            float desireYAngle = transform.eulerAngles.y;
            float desireXAngle = pivot.transform.eulerAngles.x;
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
            Camera.main.transform.rotation = Quaternion.Euler(desireXAngle, desireYAngle, 0);
            transform.Find("Flashlight").rotation = Quaternion.Euler(desireXAngle, desireYAngle, 0);
        }

        //Jika scene aktif bukan gameplay maka selesai.
        if (!(SceneManager.GetActiveScene().name == "Gameplay"))
        {
            return;
        }

        //Jika player tidak punya autoritas maka keluar
        if (!hasAuthority) { return; }

        // dijalankan diclient
        if (isLocalPlayer)
        {
            //Input keyboard player menjalankan fungsi ke server
            if (Input.GetKeyUp(KeyCode.A))
            {
                CmdMoveRelease2();
            }
            if (Input.GetKeyUp(KeyCode.D))
            {
                CmdMoveRelease3();
            }
            if (Input.GetKeyUp(KeyCode.W))
            {
                CmdMoveRelease();
            }
            if (Input.GetKeyUp(KeyCode.S))
            {
                CmdMoveRelease4();
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                CmdMoveUp();
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                CmdMoveLeftSide();
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                CmdMoveDown();
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                CmdMoveRightSide();
            }


            if (Input.GetKeyUp(KeyCode.U))
            {
                GameObject[] gos = (GameObject[])FindObjectsOfType(typeof(GameObject));
                for (int i = 0; i < gos.Length; i++)
                {
                    if (gos[i].name.Contains("Player"))
                    {
                        if (gos[i].name != "Player " + playerIndex)
                        {
                            CmdMessage(playerIndex, gos[i].GetComponent<Player>());
                        }
                    }
                }

            }

            //Input fungsi mouse menjalankan fungsi ke server
            if (Input.GetAxis("Mouse X") > 0)
            {
                CmdMoveRight(Input.GetAxis("Mouse X"));
            }
            if (Input.GetAxis("Mouse X") < 0)
            {
                CmdMoveLeft(Input.GetAxis("Mouse X"));
            }
            if (Input.GetAxis("Mouse Y") > 0)
            {
                CmdMoveRotUp(Input.GetAxis("Mouse Y"));
            }
            if (Input.GetAxis("Mouse Y") < 0)
            {
                CmdMoveRotDown(Input.GetAxis("Mouse Y"));
            }
            if (Input.GetAxis("Mouse X") == 0)
            {
                CmdMoveReleaseRot();
            }
            if (Input.GetAxis("Mouse Y") == 0)
            {
                CmdMoveReleaseRot2();
            }

            //inisialisasi GameObject pivot
            pivot = transform.Find("pivot").gameObject;

            //memindahkan posisi kamera sesuai player
            float desireYAngle = transform.eulerAngles.y;
            float desireXAngle = pivot.transform.eulerAngles.x;
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y + 3f, transform.position.z);
            Camera.main.transform.rotation = Quaternion.Euler(desireXAngle, desireYAngle, 0);
            
        }
    }

    //fungsi inspeksi item diserver
    [Command]
    public void CmdInspect(string item)
    {
        GameObject.Find("Pintu" + "_" + MatchID).transform.localEulerAngles = new Vector3(0, -180, 0);
        TargetInspect(item.Replace("_" + MatchID, ""));
    }

    //fungsi inspeksi item kembali ke client
    [TargetRpc]
    public void TargetInspect(string item)
    {
        //mengaktifkan cursor mouse
        Cursor.lockState = CursorLockMode.None;

        //mengaktifkan kamera ke 2 untuk inspect item
        GameObject.Find("Inspector View").transform.Find("Camera").gameObject.SetActive(true);

        //membuat objek untuk diinspect
        GameObject go = Instantiate(Resources.Load<GameObject>("Prefab/Map1/" + item), new Vector3(0, 0, 0), Quaternion.EulerAngles(0, 0, 0));
        go.name = item;
        go.tag = "Untagged";
        go.layer = LayerMask.NameToLayer("SecondCamera");
        for (int i = 0; i < go.transform.GetChildCount(); i++)
        {
            go.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("SecondCamera");
            go.transform.GetChild(i).gameObject.tag = "Untagged";
        }
        go.transform.parent = GameObject.Find("Inspector View").transform.Find("Cube").transform;
        go.transform.localPosition = new Vector3(0, 0, 0);
        go.transform.rotation = Quaternion.EulerAngles(0, 0, 0);
        go.transform.localScale = new Vector3(1, 1, 1);
        interaksi = item;
    }

    //fungsi menutup inspeksi item diserver
    [Command]
    public void CmdCloseInspector(string item)
    {
        TargetCloseInspect(item);
    }

    //fungsi menutup inspeksi item kembali ke client
    [TargetRpc]
    public void TargetCloseInspect(string item)
    {
        //menghilangkan cursor mouse
        Cursor.lockState = CursorLockMode.Locked;

        //memangil fungsi untuk menampilkan hantu
        StartCoroutine(SpawnGhost());

        //menghapus item
        Destroy(GameObject.Find("Inspector View").transform.Find("Cube").transform.Find(item).gameObject);

        //menghilangkan kamera ke-2 untuk inspeksi
        GameObject.Find("Inspector View").transform.Find("Camera").gameObject.SetActive(false);
    }

    //fungsi menampilkan hantu
    public IEnumerator SpawnGhost()
    {
        yield return new WaitForSeconds(5);
        Debug.Log("Spawn Ghost");
        GameObject.Find("map").transform.Find("humanBody").gameObject.SetActive(true);
        GameObject.Find("map").transform.Find("humanBody").GetComponent<FollowPlayer>().mulaiIkuti = true; 
    }

    //fungsi mengirim pesan diserver
    [Command]
    public void CmdMessage(int _indexPlayer, Player target)
    {
        target?.TargetMessage(_indexPlayer);
    }

    //fungsi menerima pesan kembali ke client
    [TargetRpc]
    public void TargetMessage(int _indexPlayer)
    {
        Debug.Log("Send from"+_indexPlayer);
        //target.Send<Notification>(msg);
    }

    /*
     * fungsi pergerakan mouse untuk rotasi
     */
    [Command]
    private void CmdMoveRotDown(float InputMouseY)
    {
        InputMY = InputMouseY;
        directionRot2 = "downrot";
    }

    [Command]
    private void CmdMoveRotUp(float InputMouseY)
    {
        InputMY = InputMouseY;
        directionRot2 = "uprot";
    }

    [Command]
    private void CmdMoveLeft(float InputMouseX)
    {
        InputMX = InputMouseX;
        directionRot = "left";
    }


    [Command]
    private void CmdMoveRight(float InputMouseX)
    {
        InputMX = InputMouseX;
        directionRot = "right";

    }

    [Command]
    private void CmdMoveReleaseRot()
    {
        InputMX = 0;
        directionRot = "nothing";
    }

    [Command]
    private void CmdMoveReleaseRot2()
    {
        InputMY = 0;
        directionRot2 = "nothing";
    }

    /*
     * fungsi berjalan di server 
     */

    [Command]
    private void CmdMoveUp()
    {
        direction = "up";
    }

    [Command]
    private void CmdMoveRelease()
    {
        direction = "nothing";
    }
    
    [Command]
    private void CmdMoveDown()
    {
        direction4 = "down";
    }

    [Command]
    private void CmdMoveLeftSide()
    {
        direction2 = "left";
    }


    [Command]
    private void CmdMoveRightSide()
    {
        direction3 = "right";
    }

    [Command]
    private void CmdMoveRelease2()
    {
        direction2 = "nothing";
    }

    [Command]
    private void CmdMoveRelease3()
    {
        direction3 = "nothing";
    }

    [Command]
    private void CmdMoveRelease4()
    {
        direction4 = "nothing";
    }

}
