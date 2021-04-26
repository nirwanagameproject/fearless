using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameInit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameObject[] gos = (GameObject[])FindObjectsOfType(typeof(GameObject));
        for (int i = 0; i < gos.Length; i++)
        {
            if (gos[i].name.Contains("Player"))
            {
                gos[i].transform.parent = GameObject.Find("PlayersSpawn").gameObject.transform;
            }
        }
    }
}
