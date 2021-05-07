using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using System.Security.Cryptography;
using System.Text;

[System.Serializable]
public class Match
{
    public string matchId;
    public SyncListGameObject players = new SyncListGameObject();
    public bool publicMatch;
    public bool inMatch;
    public bool matchFull;

    public Match(string matchId, GameObject player)
    {
        this.matchId = matchId;
        players.Add(player);
    }
    public Match() { }
}
[System.Serializable]
public class SyncListGameObject : SyncList<GameObject> { }

[System.Serializable]
public class SyncListMatch : SyncList<Match> { }

public class MatchMaker : NetworkBehaviour
{
    
    public static MatchMaker instance;

    public SyncListMatch matches = new SyncListMatch();
    public SyncListString matchIDs = new SyncListString();

    [SerializeField] GameObject turnManagerPrefab;

    void Start()
    {
        Debug.Log("match ok");
        instance = this;
    }
    public static string getRandomMatchId()
    {
        string _id = string.Empty;
        for (int i = 0; i < 5; i++)
        {
            int random = UnityEngine.Random.Range(0,36);
            if(random < 26)
            {
                _id += (char)(random + 65);
            }
            else
            {
                _id += (random - 26).ToString();
            }
        }

        return _id;
    }

    public bool HostGame(string _matchId,GameObject _player,bool publicMatch,out int playerIndex)
    {
        playerIndex = -1;
        if (!matchIDs.Contains(_matchId))
        {
            matchIDs.Add(_matchId);
            Match matchBaru = new Match(_matchId, _player);
            matchBaru.publicMatch = publicMatch;
            matches.Add(matchBaru);
            Debug.Log("Match generated");
            playerIndex = 1;
            return true;
        }
        else
        {
            Debug.Log("Match already exist");
            return false;
        }
    }

    public bool SearchGame(GameObject _player,out int playerIndex,out string matchId)
    {
        playerIndex = -1;
        matchId = String.Empty;

        for (int i=0;i<matches.Count;i++)
        {
            if(matches[i].publicMatch && !matches[i].matchFull && !matches[i].inMatch)
            {
                matchId = matches[i].matchId;
                if (JoinGame(matchId, _player, out playerIndex))
                {
                    return true;
                }
            }
        }

        return false;
    }

    public bool JoinGame(string _matchId, GameObject _player, out int playerIndex)
    {
        playerIndex = -1;
        if (matchIDs.Contains(_matchId))
        {
            for(int i = 0; i < matches.Count; i++)
            {
                if(matches[i].matchId == _matchId)
                {
                    matches[i].players.Add(_player);
                    playerIndex = matches[i].players.Count;
                    break;
                }
            }
            Debug.Log("Joined Match"+playerIndex);
            return true;
        }
        else
        {
            Debug.Log("Match not exist");
            return false;
        }
    }

    public void BeginGame(string _matchId)
    {
        GameObject newTurnManager = Instantiate(turnManagerPrefab);
        NetworkServer.Spawn(newTurnManager);
        newTurnManager.GetComponent<NetworkMatchChecker>().matchId = _matchId.ToGuid();
        TurnManager turnManager = newTurnManager.GetComponent<TurnManager>();

        for(int i = 0; i < matches.Count; i++)
        {
            if (matches[i].matchId == _matchId)
            {
                foreach(var player in matches[i].players)
                {
                    Player _player = player.GetComponent<Player>();
                    turnManager.AddPlayer(_player);
                    _player.StartGame();
                    matches[i].inMatch = true;
                }
                break;
            }
        }
    }

    public void PlayerDisconnected(Player _player,string _matchId)
    {
        for (int i=0;i<matches.Count;i++)
        {
            if (matches[i].matchId == _matchId)
            {
                var playerIndex = matches[i].players.IndexOf(_player.gameObject);
                matches[i].players.RemoveAt(playerIndex);
                for(int j = 0; j < matches[i].players.Count; j++)
                {
                    if(matches[i].players[j].GetComponent<Player>().playerIndex > _player.playerIndex)
                    {
                        matches[i].players[j].GetComponent<Player>().playerIndex -= 1;
                    }
                }
                Debug.Log("Player disconnect from lobby");
                if(matches[i].players.Count == 0)
                {
                    matches.RemoveAt(i);
                    matchIDs.Remove(_matchId);
                    for (int j = 0; j < ObjectManager.instance.prefabSpawnObject.Count; j++)
                    {
                        Destroy(GameObject.Find(ObjectManager.instance.prefabSpawnObject[j].name +"_"+ _matchId).gameObject);
                    }
                }

                break;
            }
        }
    }
}
public static class MatchExtensions
{
    public static Guid ToGuid(this string id)
    {
        MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
        byte[] inputBytes = Encoding.Default.GetBytes(id);
        byte[] hashBytes = provider.ComputeHash(inputBytes);

        return new Guid(hashBytes);
    }
}