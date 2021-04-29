using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    [SerializeField] Material selectMaterial;
    [SerializeField] Material dafaultMaterial;

    private Transform _selection;
    public string selectTag = "Selectable";
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(_selection != null)
        {
            var renderSelection = _selection.GetComponent<Renderer>();
            renderSelection.material = dafaultMaterial;
            _selection = null;
        }
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray,out hit))
        {
            var selection = hit.transform;
            if (selection.CompareTag(selectTag))
            {
                var selectionRendered = selection.GetComponent<Renderer>();
                dafaultMaterial = selectionRendered.material;
                if (selectionRendered != null)
                {
                    selectionRendered.material = selectMaterial;
                }
                _selection = selection;
                if (Input.GetMouseButtonDown(0))
                {
                    Player.localPlayer.CmdInspect();
                }
            }
        }
        if (GameObject.Find("Inspector View").transform.Find("Camera").gameObject.active)
        {
            if (Input.GetMouseButtonDown(1))
            {
                Player.localPlayer.CmdCloseInspector();
            }
        }
    }
}
