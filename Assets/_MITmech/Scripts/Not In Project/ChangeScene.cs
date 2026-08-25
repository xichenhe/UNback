using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class ChangeScene : MonoBehaviour
{
    int sceneNumber;
    public void changeScene(int sceneNumber)
    {
        SceneManager.LoadScene(sceneNumber);
    }
}
