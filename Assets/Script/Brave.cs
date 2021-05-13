using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        go.transform.localScale = new Vector3(200,200,200);
    }
}
