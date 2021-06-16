using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Kontrol Brave 
 * - berisi fungsi untuk memilih dan merubah model brave
 */

public class Brave
{
    public void Mulai()
    {
        Debug.Log("Mulai Brave!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<MeshFilter>().sharedMesh = Resources.Load<Mesh>("Models/Player/patrick");
        go.GetComponent<Renderer>().material = Resources.Load<Material>("Models/Player/pat");
        go.transform.localScale = new Vector3(1,1,1);
        Transform amature = GameObject.Instantiate(Resources.Load<Transform>("Models/Player/brave").Find("Armature").transform);
        amature.transform.parent = go.transform;
    }
}
