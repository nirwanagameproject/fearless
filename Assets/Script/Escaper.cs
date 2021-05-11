using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Escaper : MonoBehaviour
{
    public void Mulai()
    {
        Debug.Log("Mulai Escaper!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(0, 188, 25);
    }
}
