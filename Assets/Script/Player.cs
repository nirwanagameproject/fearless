using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Mirror;

public class Notification
{
    public string indexPlayer { get; set; }

}

public class Player : NetworkBehaviour
{
    public Camera MainCamera;
    public static Player localPlayer;
    public static Transform localTransformPlayer;
    public NetworkMatchChecker networkMatchChecker;
    GameObject pivot;
    public GameObject playerLobbyUI;
    [SerializeField] public NavMeshAgent navigasi;
    [SerializeField] private Vector3 movement = new Vector3();
    [SyncVar] public string MatchID;
    [SyncVar] public int playerIndex;
    public string direction;
    public string directionRot;
    public float turn;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float maxTurnSpeed = 150f;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            localPlayer = this;
            localTransformPlayer = transform;
        }
        else
        {
            playerLobbyUI = UILobby.instance.spawnPlayerPrefab(this);
        }

        direction = "idle";
        directionRot = "idle";
        gameObject.tag = "Player";
    }

    public void SpawnToPoint(int _playerIndex)
    {
        transform.eulerAngles = new Vector3(0, 90, 0);
        transform.position = GameObject.Find("PlayersSpawn").transform.Find("Spawn" + _playerIndex).transform.position;

        GetComponent<Rigidbody>().position = transform.position;
        GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
        GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("Client stop");
        ClientDisconnect();
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("Client stop from server");
        ServerDisconnect();
    }

    // Start is called before the first frame update
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
        UILobby.instance.HostSuccess(_success,_matchId,_playerIndex);
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
        UILobby.instance.JoinSuccess(_success,_matchId,_playerIndex);
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
        UILobby.instance.SearchSuccess(_success, _matchId,_playerIndex);
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
        //SceneManager.LoadScene(2,LoadSceneMode.Additive);
    }

    /*
     Disconnect Match
     */
    
    public void DisconnectGame()
    {
        CmdDisconnectGame();
    }
    [Command]
    public void CmdDisconnectGame()
    {
        ServerDisconnect();
    }
    public void ServerDisconnect()
    {
        MatchMaker.instance.PlayerDisconnected(this, MatchID);
        networkMatchChecker.matchId = string.Empty.ToGuid();
        RpcDisconnectGame();
    }
    [ClientRpc]
    public void RpcDisconnectGame()
    {
        ClientDisconnect();
    }
    public void ClientDisconnect()
    {
        if (playerLobbyUI != null)
        {
            for(int i=0;i< GameObject.FindGameObjectsWithTag("Player").Length; i++)
            {
                if(GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>().playerIndex > playerIndex)
                {
                    GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>().playerIndex -= 1;
                    GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>().playerLobbyUI.GetComponent<UIPlayer>().setPlayer(GameObject.FindGameObjectsWithTag("Player")[i].GetComponent<Player>());
                }
            }
            Destroy(playerLobbyUI);
        }
    }
    [Command]
    public void Putus(string _matchid,int _indexPlayer)
    {
        TargetPutus(MatchMaker.instance.matches[0].players,1);
    }
    [TargetRpc]
    public void TargetPutus(SyncListGameObject players, int playerIndex)
    {
        Debug.Log("Jumlah Player : "+players.Count);
    }
    [Client]
    // Update is called once per frame
    void Update()
    {
        if (SceneManager.GetActiveScene().name== "Gameplay")
        {
            GameObject.Find("pivot").transform.position = transform.position;
            GameObject.Find("pivot").transform.parent = transform;
            pivot = GameObject.Find("pivot").gameObject;
        }
        else
        {
            return;
        }
        if (!hasAuthority) { return; }
        if (isLocalPlayer)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                CmdMoveUp();
            }
            else if (Input.GetKeyUp(KeyCode.W))
            {
                CmdMoveRelease();
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                CmdMoveLeft();
            }
            else if (Input.GetKeyUp(KeyCode.A))
            {
                CmdMoveReleaseRot();
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                CmdMoveDown();
            }
            else if (Input.GetKeyUp(KeyCode.S))
            {
                CmdMoveRelease();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                CmdMoveRight();
            }
            else if (Input.GetKeyUp(KeyCode.D))
            {
                CmdMoveReleaseRot();
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                CmdMoveRotUp();
            }
            else if (Input.GetKeyUp(KeyCode.Q))
            {
                CmdMoveReleaseRot();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                CmdMoveRotDown();
            }
            else if (Input.GetKeyUp(KeyCode.E))
            {
                CmdMoveReleaseRot();
            }
            else if (Input.GetKeyUp(KeyCode.U))
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

            if (direction == "up")
            {
                float distance = moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX,0,jalanZ));
                //transform.position += new Vector3(jalanX, 0, jalanZ) * distance;
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            else if (direction == "down")
            {
                float distance = -moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX, 0, jalanZ));
                //transform.position -= new Vector3(jalanX, 0, jalanZ) * distance;
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            if (directionRot == "uprot")
            {
                pivot.transform.Rotate(Vector3.right * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            else if (directionRot == "downrot")
            {
                pivot.transform.Rotate(Vector3.left * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            else if (directionRot == "left")
            {
                transform.Rotate(Vector3.down * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            else if (directionRot == "right")
            {
                transform.Rotate(Vector3.up * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }

            if (isLocalPlayer)
            {
                if (pivot != null)
                {
                    float desireYAngle = transform.eulerAngles.y;
                    float desireXAngle = pivot.transform.eulerAngles.x;
                    Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
                    Camera.main.transform.rotation = Quaternion.Euler(desireXAngle, desireYAngle, 0);
                }
            }
        }
    }
    [Command]
    public void CmdCloseInspector()
    {
        TargetCloseInspect();
    }

    [TargetRpc]
    public void TargetCloseInspect()
    {
        StartCoroutine(SpawnGhost());
        GameObject.Find("Inspector View").transform.Find("Camera").gameObject.SetActive(false);
    }

    public IEnumerator SpawnGhost()
    {
        yield return new WaitForSeconds(5);
        Debug.Log("Spawn Ghost");
        GameObject.Find("map").transform.Find("humanBody").gameObject.SetActive(true);
        GameObject.Find("map").transform.Find("humanBody").GetComponent<FollowPlayer>().mulaiIkuti = true; 
    }


    [Command]
    public void CmdInspect()
    {
        TargetInspect();
    }


    [TargetRpc]
    public void TargetInspect()
    {
        GameObject.Find("map").transform.Find("Pintu").transform.localEulerAngles = new Vector3(0,-90,0);
        GameObject.Find("Inspector View").transform.Find("Camera").gameObject.SetActive(true);
        
    }

    [Command]
    public void CmdMessage(int _indexPlayer, Player target)
    {
        target?.TargetMessage(_indexPlayer);
    }

    [TargetRpc]
    public void TargetMessage(int _indexPlayer)
    {
        Debug.Log("Send from"+_indexPlayer);
        //target.Send<Notification>(msg);
    }

    public void MessageSend(NetworkConnection conn,Notification msg)
    {
        Debug.Log("Message from player" +msg.indexPlayer);
    }

    [Command]
    private void CmdMoveRotDown()
    {
        RpcMoveRotDown();
    }

    [ClientRpc]
    private void RpcMoveRotDown()
    {
        directionRot = "downrot";
    }

    [Command]
    private void CmdMoveRotUp()
    {
        RpcMoveRotUp();
    }

    [ClientRpc]
    private void RpcMoveRotUp()
    {
        directionRot = "uprot";
    }

    [Command]
    private void CmdMoveUp()
    {
        RpcMoveUp();
    }

    [ClientRpc]
    private void RpcMoveUp()
    {
        direction = "up";
    }

    [Command]
    private void CmdMoveRelease()
    {
        RpcMoveRelease();
    }

    [Command]
    private void CmdMoveReleaseRot()
    {
        RpcMoveReleaseRot();
    }

    [ClientRpc]
    private void RpcMoveReleaseRot()
    {
        directionRot = "nothing";
    }

    [ClientRpc]
    private void RpcMoveRelease()
    {
        direction = "nothing";
    }

    [Command]
    private void CmdMoveLeft()
    {
        RpcMoveLeft();
    }

    [ClientRpc]
    private void RpcMoveLeft()
    {
        directionRot = "left";
    }


    [Command]
    private void CmdMoveRight()
    {
        RpcMoveRight();
    }

    [ClientRpc]
    private void RpcMoveRight()
    {
        directionRot = "right";
    }

    [Command]
    private void CmdMoveDown()
    {
        RpcMoveDown();
    }

    [ClientRpc]
    private void RpcMoveDown()
    {
        direction = "down";
    }
}
