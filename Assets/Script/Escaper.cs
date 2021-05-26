using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * Kontrol Escaper 
 * - berisi fungsi untuk memilih dan merubah model escaper
 */

public class Escaper
{
    public void Mulai()
    {
        Debug.Log("Mulai Escaper!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(0, 188, 25);
        go.GetComponent<MeshFilter>().sharedMesh = Resources.LoadAll<Mesh>("Models/Player/squidward")[0];
        go.GetComponent<Renderer>().material = Resources.Load<Material>("Models/Player/squidward");
        go.transform.localScale = new Vector3(200, 200, 200);
    }
}
