using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField]NavMeshAgent navMesh;
    public bool mulaiIkuti;
    private int mulai;

    private void Start()
    {
        mulaiIkuti = false;
        mulai = 0;
        gameObject.SetActive(false);
    }
    // Update is called once per frame
    void Update()
    {
        if (mulaiIkuti)
        {
            if (mulai > 0 && Player.localTransformPlayer != null)
            {
                navMesh.SetDestination(Player.localTransformPlayer.position);
            }
            if(mulai > 0)
            {
                return;
            }
            else
            {
                if (mulai == 0)
                {
                    MulaiSpawn();
                }
            }
        }
    }
    private void MulaiSpawn()
    {
        gameObject.SetActive(true);
        mulai++;
    }
}
