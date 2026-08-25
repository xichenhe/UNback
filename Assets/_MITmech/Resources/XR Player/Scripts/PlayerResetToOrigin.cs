using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerResetToOrigin : MonoBehaviour
{
    [SerializeField] GameObject player;
    private Vector3 playerStartPosistion;
    
    // Start is called before the first frame update
    void Start()
    {
        playerStartPosistion = new Vector3(player.transform.position.x, player.transform.position.y, player.transform.position.z);
    }

    [ContextMenu("ResetPlayerPosition")]
    public void ResetPlayerPosition()
    {
        player.transform.position = playerStartPosistion;
    }
}
