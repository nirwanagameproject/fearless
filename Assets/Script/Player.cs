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
    [SerializeField] public NavMeshAgent navigasi;
    [SerializeField] private Vector3 movement = new Vector3();
    [SyncVar] public string MatchID;
    [SyncVar] public int playerIndex;
    public string direction;
    public float turn;

    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float maxTurnSpeed = 150f;

    // Start is called before the first frame update
    void Start()
    {
        if (isLocalPlayer)
        {
            localPlayer = this;
            localTransformPlayer = transform;
        }
        else
        {
            UILobby.instance.spawnPlayerPrefab(this);
        }
        transform.eulerAngles = new Vector3(0,90,0);
        DontDestroyOnLoad(this);
        direction = "idle";
        networkMatchChecker = GetComponent<NetworkMatchChecker>();
    }
    /*
     Host Game
     */
    public void HostGame()
    {
        string matchId = MatchMaker.getRandomMatchId();
        CmdHostGame(matchId);
    }
    [Command]
    void CmdHostGame(string matchId)
    {
        MatchID = matchId;
        if (MatchMaker.instance.HostGame(matchId, gameObject,out playerIndex))
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
        if (!hasAuthority) { return; }
        if (Input.GetKeyDown(KeyCode.W)) {
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
            CmdMoveRelease();
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
            CmdMoveRelease();
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            CmdMoveRotUp();
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            CmdMoveRelease();
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            CmdMoveRotDown();
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            CmdMoveRelease();
        }
        else if (Input.GetKeyUp(KeyCode.U))
        {
            GameObject[] gos = (GameObject[])FindObjectsOfType(typeof(GameObject));
            for (int i = 0; i < gos.Length; i++)
            {
                if (gos[i].name.Contains("Player"))
                {
                    if (gos[i].name != "Player " + playerIndex) {
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
        }
        else if (direction == "uprot")
        {
            pivot.transform.Rotate(Vector3.right * maxTurnSpeed * Time.deltaTime);
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
        }
        else if (direction == "downrot")
        {
            pivot.transform.Rotate(Vector3.left * maxTurnSpeed * Time.deltaTime);
        }
        else if (direction == "left")
        {
            transform.Rotate(Vector3.down * maxTurnSpeed * Time.deltaTime);
        }
        else if (direction == "right")
        {
            transform.Rotate(Vector3.up * maxTurnSpeed * Time.deltaTime);
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
    [Command]
    public void CmdCloseInspector()
    {
        TargetCloseInspect();
    }

    [TargetRpc]
    public void TargetCloseInspect()
    {
        GameObject.Find("Inspector View").transform.Find("Camera").gameObject.SetActive(false);
    }

    [Command]
    public void CmdInspect()
    {
        TargetInspect();
    }


    [TargetRpc]
    public void TargetInspect()
    {
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
        direction = "downrot";
    }

    [Command]
    private void CmdMoveRotUp()
    {
        RpcMoveRotUp();
    }

    [ClientRpc]
    private void RpcMoveRotUp()
    {
        direction = "uprot";
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
        direction = "left";
    }


    [Command]
    private void CmdMoveRight()
    {
        RpcMoveRight();
    }

    [ClientRpc]
    private void RpcMoveRight()
    {
        direction = "right";
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
