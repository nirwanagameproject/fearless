using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Escaper
{
    public void Mulai()
    {
        Debug.Log("Mulai Escaper!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(0, 188, 25);
        go.transform.localScale = new Vector3(200, 200, 200);
    }
}
