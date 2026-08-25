using UnityEngine;

public class UpdateTransparencyInTransparentViewQuad : MonoBehaviour
{
    void Update()
    {
        if(transform.parent.GetComponent<MeshRenderer>().enabled || !transform.parent.GetComponent<ProceduralHoleController>().isSurfaceQuadOnSideOfBlock){
            gameObject.GetComponent<MeshRenderer>().enabled = false;
        }else{
            gameObject.GetComponent<MeshRenderer>().enabled = true;
        }
    }
}
