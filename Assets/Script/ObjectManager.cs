using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class ObjectManager : MonoBehaviour
{
    [SerializeField] public List<GameObject> prefabSpawnObject;
    [SerializeField] public List<Vector3> prefabPointSpawnObject;
    [SerializeField] List<Vector3> prefabRotSpawnObject;


    public static ObjectManager instance;

    private void Start()
    {
        instance = this;
    }

    public void SpawnObject(string _matchId)
    {
        for(int i = 0; i < prefabSpawnObject.Count; i++)
        {
            
            GameObject newTurnManager = Instantiate(prefabSpawnObject[i],prefabPointSpawnObject[i], Quaternion.EulerAngles(prefabRotSpawnObject[i]));
            newTurnManager.transform.localPosition = prefabPointSpawnObject[i];
            newTurnManager.transform.eulerAngles = (prefabRotSpawnObject[i]);
            newTurnManager.name = newTurnManager.name.Replace("(Clone)", "");
            newTurnManager.name = newTurnManager.name +"_"+_matchId;
            NetworkServer.Spawn(newTurnManager);
            newTurnManager.GetComponent<NetworkMatchChecker>().matchId = _matchId.ToGuid();
            for (int k = 0; k < MatchMaker.instance.matches.Count; k++)
            {
                if (MatchMaker.instance.matches[k].matchId == _matchId)
                {
                    MatchMaker.instance.matches[k].items.Add(newTurnManager);
                    break;
                }
            }

            for (int k = 0; k < MatchMaker.instance.matches.Count; k++)
            {
                if (MatchMaker.instance.matches[k].matchId != _matchId)
                {
                    for (int j = 0; j < MatchMaker.instance.matches[k].players.Count; j++)
                    {
                        Transform[] allChildren = newTurnManager.GetComponentsInChildren<Transform>();
                        foreach (Transform child in allChildren)
                        {
                            if(child.GetComponent<Collider>()!=null)
                            Physics.IgnoreCollision(child.GetComponent<Collider>(), MatchMaker.instance.matches[k].players[j].GetComponent<Collider>());

                            Transform[] allChildren2 = child.GetComponentsInChildren<Transform>();
                            foreach (Transform child2 in allChildren2)
                            {
                                if (child2.GetComponent<Collider>() != null)
                                    Physics.IgnoreCollision(child2.GetComponent<Collider>(), MatchMaker.instance.matches[k].players[j].GetComponent<Collider>());
                            }
                        }
                    }
                }
            }
        }

    }
}