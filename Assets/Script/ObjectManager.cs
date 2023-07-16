using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*
 * Item Manager
 * - berisi fungsi untuk meletakan item-item sesuai posisinya
 * - berisi fungsi untuk mengabaikan collision dengan item beda match id
 */

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

    //spawn item-item diserver
    public void SpawnObject(string _matchId)
    {
        for(int i = 0; i < prefabSpawnObject.Count; i++)
        {
            //membuat item diserver sesuai list GameObject di prefabSpawnObject
            GameObject newTurnManager = Instantiate(prefabSpawnObject[i],prefabPointSpawnObject[i], Quaternion.EulerAngles(prefabRotSpawnObject[i]));
            newTurnManager.transform.localPosition = prefabPointSpawnObject[i];
            newTurnManager.transform.eulerAngles = (prefabRotSpawnObject[i]);
            newTurnManager.name = newTurnManager.name.Replace("(Clone)", "");
            newTurnManager.name = newTurnManager.name +"_"+_matchId;

            //spawn item disemua client-clint
            NetworkServer.Spawn(newTurnManager);
            newTurnManager.GetComponent<NetworkMatch>().matchId = _matchId.ToGuid();

            //menambahkan item-item ke list item di MatchMaker
            for (int k = 0; k < MatchMaker.instance.matchku.matches.Count; k++)
            {
                if (MatchMaker.instance.matchku.matches[k].matchId == _matchId)
                {
                    MatchMaker.instance.matchku.matches[k].items.Add(newTurnManager);
                    break;
                }
            }

            //mengabaikan collision item dengan item lain yang berbeda matchId di MatchMaker
            for (int k = 0; k < MatchMaker.instance.matchku.matches.Count; k++)
            {
                if (MatchMaker.instance.matchku.matches[k].matchId != _matchId)
                {
                    for (int j = 0; j < MatchMaker.instance.matchku.matches[k].players.Count; j++)
                    {
                        Transform[] allChildren = newTurnManager.GetComponentsInChildren<Transform>();
                        foreach (Transform child in allChildren)
                        {
                            if(child.GetComponent<Collider>()!=null)
                            Physics.IgnoreCollision(child.GetComponent<Collider>(), MatchMaker.instance.matchku.matches[k].players[j].GetComponent<Collider>());

                            Transform[] allChildren2 = child.GetComponentsInChildren<Transform>();
                            foreach (Transform child2 in allChildren2)
                            {
                                if (child2.GetComponent<Collider>() != null)
                                    Physics.IgnoreCollision(child2.GetComponent<Collider>(), MatchMaker.instance.matchku.matches[k].players[j].GetComponent<Collider>());
                            }
                        }
                    }
                }
            }
        }

    }
}