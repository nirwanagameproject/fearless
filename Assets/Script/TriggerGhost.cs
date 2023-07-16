using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerGhost : MonoBehaviour
{
    public GameObject pocong;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        pocong.SetActive(true);
        StartCoroutine(EndJump());
    }

    public IEnumerator EndJump()
    {
        yield return new WaitForSeconds(2.03f);
        pocong.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
