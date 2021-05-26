using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/*
 * AI Hantu
 * - berisi fungsi untuk mengikuti pemain
 */

public class FollowPlayer : MonoBehaviour
{
    [SerializeField]NavMeshAgent navMesh;
    public bool mulaiIkuti;
    private int mulai;

    //fungsi dijalakan awal saja
    private void Start()
    {
        //inisialisasi mulaiIkuti dan mulai
        mulaiIkuti = false;
        mulai = 0;

        //mehilangkan gameObject hantu
        gameObject.SetActive(false);
    }
    // fungsi dijalankan per frame
    void Update()
    {
        if (mulaiIkuti)
        {
            //fungsi untuk mengikuti player pertama
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

    //menampilkan hantu
    private void MulaiSpawn()
    {
        gameObject.SetActive(true);
        mulai++;
    }
}
