using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * Kontrol Mechanic 
 * - berisi fungsi untuk memilih dan merubah model mechanic
 */

public class Mechanic
{
    public void Mulai()
    {
        Debug.Log("Mulai Mechanic!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(0, 66, 255);
        go.GetComponent<MeshFilter>().sharedMesh = Resources.LoadAll<Mesh>("Models/Player/mr crab")[0];
        go.GetComponent<Renderer>().material = Resources.Load<Material>("Models/Player/mr crab");
        go.transform.localScale = new Vector3(200, 200, 200);
    }
}
