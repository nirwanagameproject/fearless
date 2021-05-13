using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indigo
{
    public void Mulai()
    {
        Debug.Log("Mulai Indigo!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(196, 0, 255);
        go.GetComponent<MeshFilter>().sharedMesh = Resources.LoadAll<Mesh>("Models/Player/spongebob")[4];
        go.GetComponent<Renderer>().material = Resources.Load<Material>("Models/Player/spongebob");
        go.transform.localScale = new Vector3(200, 200, 200);
    }
}