using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mechanic : MonoBehaviour
{
    public void Mulai()
    {
        Debug.Log("Mulai Mechanic!");
    }
    public void changeColor(GameObject go)
    {
        go.GetComponent<Renderer>().material.color = new Color(0, 66, 255);
    }
}
