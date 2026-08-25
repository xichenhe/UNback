using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MITmech.ITS
{

public class Notification : MonoBehaviour
{
    float timer;
    public float duration;
    public GameObject notificationIcon;
    public AudioSource audioSource;
    public AudioClip notifyDing;
    public AudioClip correctDing;

    public Material correctMaterial;
    public Material hintMaterial;

    public void Notify()
    {
        StopAllCoroutines();
        
        notificationIcon.SetActive(true);
        notificationIcon.GetComponent<MeshRenderer>().material = hintMaterial;

        audioSource.clip = notifyDing;
        audioSource.Play();

        StartCoroutine(StartCooldown());
    } 
    public void Complete()
    {        
        notificationIcon.SetActive(true);
        notificationIcon.GetComponent<MeshRenderer>().material = correctMaterial;
        audioSource.clip=correctDing;
        audioSource.Play();
    }

    public IEnumerator StartCooldown()
    {
        timer = 0;
        while (timer <= duration)
        {

            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        notificationIcon.SetActive(false);
    }
}

}
