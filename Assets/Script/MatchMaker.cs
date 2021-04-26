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

    public bool HostGame(string _matchId,GameObject _player,out int playerIndex)
    {
        playerIndex = -1;
        if (!matchIDs.Contains(_matchId))
        {
            matchIDs.Add(_matchId);
            matches.Add(new Match(_matchId, _player));
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