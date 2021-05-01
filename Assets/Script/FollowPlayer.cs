using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : MonoBehaviour
{
    [SerializeField]NavMeshAgent navMesh;
    // Update is called once per frame
    void Update()
    {
        if (Player.localTransformPlayer != null)
        {
            navMesh.SetDestination(Player.localTransformPlayer.position);
        }
    }
}
