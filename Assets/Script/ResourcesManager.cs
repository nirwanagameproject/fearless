﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class ResourcesManager : MonoBehaviour
{
    [Tooltip("Asset/Resources/[Insert Forlder Path]")]
    [SerializeField] private string[] folderPath = null;
    private bool isDone = false;

    private void Awake()
    {
        if (!isDone)
        {
            int c = 0;

            if(folderPath==null || folderPath.Length <= 0)
            {
                Debug.Log("No Path");
            }
            else
            {
                for (int i=0;i<folderPath.Length;i++)
                {
                    List<GameObject> tmp=Resources.LoadAll(folderPath[i],typeof(GameObject)).Cast<GameObject>().ToList();

                    if(tmp==null || tmp.Count <= 0)
                    {
                        Debug.LogError("Not Found");
                    }
                    else
                    {
                        for(int n = 0; n < tmp.Count; n++)
                        {
                            gameObject.GetComponent<NetworkManager>().spawnPrefabs.Add(tmp[n]);
                            c++;
                        }
                    }
                }
            }

            isDone = true;

            Debug.Log("Rregistered " + c + " total prefabs");
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
