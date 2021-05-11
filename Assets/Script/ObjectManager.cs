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
        DontDestroyOnLoad(gameObject);
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
        }
    }
}