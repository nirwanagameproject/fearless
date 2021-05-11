using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Indigo : MonoBehaviour
{
    public void Mulai()
    {
        Debug.Log("Mulai Indigo!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(196, 0, 255);
    }
}