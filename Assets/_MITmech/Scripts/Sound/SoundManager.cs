using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance;
    public AudioSource audioSource;
    public AudioClip success;
    private void Start()
    {
        instance = this;
    }
    public void PlaySuccessAudio()
    {
        audioSource.clip = success;
        audioSource.Play();
    }
}
