﻿using System.Collections;
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
    [SyncVar] public string interaksi;
    [SyncVar] public int test = 0;
    public string direction;
    public string direction2;
    public string direction3;
    public string direction4;
    public string directionRot;
    public string directionRot2;
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
        direction2 = "idle";
        directionRot = "idle";
        directionRot2 = "idle";
        gameObject.tag = "Player";
        name = "Player "+playerIndex;
        transform.parent = GameObject.Find("PlayersSpawn").transform;
    }

    public void SpawnToPoint()
    {
        transform.eulerAngles = new Vector3(0, 90, 0);
        transform.position = GameObject.Find("PlayersSpawn").transform.Find("Spawn" + playerIndex).transform.position;

        GetComponent<Rigidbody>().position = transform.position;
        GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
        GetComponent<Rigidbody>().velocity = new Vector3(0, 0, 0);
        GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0, 0);
        if (playerIndex == 1)
        {
            CmdSpawnObjects(MatchID);
        }
    }

    [Command]
    public void CmdSpawnObjects(string _matchId)
    {
        ObjectManager.instance.SpawnObject(_matchId);
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
    // Update is called once per frame
    void Update()
    {

        if (!(SceneManager.GetActiveScene().name == "Gameplay"))
        {
            return;
        }
        if (!hasAuthority) { return; }
        if (isLocalPlayer)
        {
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
            if (Input.GetAxis("Mouse X") > 0)
            {
                CmdMoveRight();
            }
            if (Input.GetAxis("Mouse X") < 0)
            {
                CmdMoveLeft();
            }
            if (Input.GetAxis("Mouse Y") > 0)
            {
                CmdMoveRotUp();
            }
            if (Input.GetAxis("Mouse Y") < 0)
            {
                CmdMoveRotDown();
            }
            if (Input.GetAxis("Mouse Y") == 0)
            {
                CmdMoveReleaseRot2();
            }
            if(Input.GetAxis("Mouse X") == 0)
            {
                CmdMoveReleaseRot();
            }

            if (direction2 == "left")
            {
                float distance = moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y-90;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX, 0, jalanZ));
                //transform.position += new Vector3(jalanX, 0, jalanZ) * distance;
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            if (direction3 == "right")
            {
                float distance = moveSpeed * Time.deltaTime;
                float sudut = transform.localEulerAngles.y + 90;
                var angleOfSineInDegrees = Mathf.Sin((sudut * Mathf.PI) / 180);
                float angleOfCosInDegrees = Mathf.Cos((sudut * Mathf.PI) / 180);
                float jalanX = angleOfSineInDegrees * distance;
                float jalanZ = angleOfCosInDegrees * distance;
                navigasi.Move(new Vector3(jalanX, 0, jalanZ));
                //transform.position += new Vector3(jalanX, 0, jalanZ) * distance;
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
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
            if (direction4 == "down")
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
            if (directionRot2 == "uprot")
            {
                /*pivot.transform.Rotate(Vector3.right * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);*/
                pivot.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y"), 0, 0) * Time.deltaTime * -maxTurnSpeed);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);

            }
            if (directionRot2 == "downrot")
            {
                /*pivot.transform.Rotate(Vector3.left * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);*/
                pivot.transform.Rotate(new Vector3(Input.GetAxis("Mouse Y"), 0, 0) * Time.deltaTime * -maxTurnSpeed);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            if (directionRot == "left")
            {
                /*transform.Rotate(Vector3.down * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
                */
                transform.Rotate(new Vector3(0, Input.GetAxis("Mouse X"), 0) * Time.deltaTime * maxTurnSpeed);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
            }
            if (directionRot == "right")
            {
                /*transform.Rotate(Vector3.up * maxTurnSpeed * Time.deltaTime);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);
                */
                transform.Rotate(new Vector3(0,Input.GetAxis("Mouse X"),0) * Time.deltaTime * maxTurnSpeed);
                GetComponent<Rigidbody>().velocity = Vector3.zero;
                GetComponent<Rigidbody>().rotation = Quaternion.EulerAngles(0, 0, 0);

            }

            pivot = transform.Find("pivot").gameObject;

            float desireYAngle = transform.eulerAngles.y;
            float desireXAngle = pivot.transform.eulerAngles.x;
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z);
            Camera.main.transform.rotation = Quaternion.Euler(desireXAngle, desireYAngle, 0);
            transform.Find("Flashlight").rotation = Quaternion.Euler(desireXAngle, desireYAngle, 0);
            
        }
    }

    [Command]
    public void CmdCloseInspector(string item)
    {
        TargetCloseInspect(item);
    }

    [TargetRpc]
    public void TargetCloseInspect(string item)
    {
        Cursor.lockState = CursorLockMode.Locked;
        StartCoroutine(SpawnGhost());
        Destroy(GameObject.Find("Inspector View").transform.Find("Cube").transform.Find(item).gameObject);
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
    public void CmdInspect(string item)
    {
        GameObject.Find("Pintu"+"_"+MatchID).transform.localEulerAngles = new Vector3(0, -180, 0);
        TargetInspect(item.Replace("_"+MatchID,""));
    }


    [TargetRpc]
    public void TargetInspect(string item)
    {
        Cursor.lockState = CursorLockMode.None;
        GameObject.Find("Inspector View").transform.Find("Camera").gameObject.SetActive(true);

        GameObject go = Instantiate(Resources.Load<GameObject>("Prefab/Map1/" + item), new Vector3(0, 0, 0),Quaternion.EulerAngles(0,0,0));
        go.name = item;
        go.tag = "Untagged";
        go.layer = LayerMask.NameToLayer("SecondCamera");
        for(int i=0;i< go.transform.GetChildCount(); i++)
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

    [Command]
    public void CmdMessage(int _indexPlayer, Player target)
    {
        test = 1;
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
        directionRot2 = "downrot";
    }

    [Command]
    private void CmdMoveRotUp()
    {
        RpcMoveRotUp();
    }

    [ClientRpc]
    private void RpcMoveRotUp()
    {
        directionRot2 = "uprot";
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

    [Command]
    private void CmdMoveReleaseRot2()
    {
        RpcMoveReleaseRot2();
    }

    [ClientRpc]
    private void RpcMoveReleaseRot2()
    {
        directionRot2 = "nothing";
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
        direction4 = "down";
    }

    [Command]
    private void CmdMoveLeftSide()
    {
        RpcMoveLeftSide();
    }

    [ClientRpc]
    private void RpcMoveLeftSide()
    {
        direction2 = "left";
    }

    [Command]
    private void CmdMoveRightSide()
    {
        RpcMoveRightSide();
    }

    [ClientRpc]
    private void RpcMoveRightSide()
    {
        direction3 = "right";
    }

    [Command]
    private void CmdMoveRelease2()
    {
        RpcMoveRelease2();
    }

    [ClientRpc]
    private void RpcMoveRelease2()
    {
        direction2 = "nothing";
    }
    [Command]
    private void CmdMoveRelease3()
    {
        RpcMoveRelease3();
    }

    [ClientRpc]
    private void RpcMoveRelease3()
    {
        direction3 = "nothing";
    }
    [Command]
    private void CmdMoveRelease4()
    {
        RpcMoveRelease4();
    }

    [ClientRpc]
    private void RpcMoveRelease4()
    {
        direction4 = "nothing";
    }
}
