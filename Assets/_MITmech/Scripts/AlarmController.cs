using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlarmController : MonoBehaviour
{
    public AudioClip warningAudio;
    public AudioClip errorAudio;

    public List<Light> lights;

    public AudioSource sirenSource;

    float time;
    public float duration;
    // Start is called before the first frame update
    void Start()
    {
        time = duration;
    }

    // Update is called once per frame
    void Update()
    {
        
        if(time <= duration)
        {
            transform.eulerAngles += new Vector3(0, 1, 0);

            time += Time.deltaTime;
        }

        else
        {
            foreach (Light light in lights)
            {
                light.enabled = false;
            }
        }
    }

    public void SoundAlarm()
    {
        time = 0;
        sirenSource.clip = errorAudio;

        duration = errorAudio.length;

        sirenSource.Play();

        foreach(Light light in lights)
        {
            light.color = Color.red;
            light.enabled = true;
        }
    }

    public void SoundWarning()
    {
        time = 0;

        sirenSource.clip = warningAudio;

        sirenSource.time = 1f;

        duration = warningAudio.length - 1f;

        sirenSource.Play();

        foreach (Light light in lights)
        {
            light.color = Color.yellow;
            light.enabled = true;
        }
    }
}
