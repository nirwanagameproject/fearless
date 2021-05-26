using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Item Inisialisasi
 * - mengubah nama item
 * - berisi untuk memindahkan parent setiap item ke ItemSpawn
 */

public class ObjectInit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        if (name.Contains("(Clone)"))
        {
            name = name.Replace("(Clone)", "");
            name = name + "_" + Player.localPlayer.MatchID;
            transform.parent = GameObject.Find("ItemSpawn").transform;
        }
        DontDestroyOnLoad(this);
    }
}
