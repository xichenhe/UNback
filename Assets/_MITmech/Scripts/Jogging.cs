using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Jogging : MonoBehaviour
{
    public static Jogging Instance;

    private void Awake()
    {
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    public bool isPositive;
    public float jogXSpeed, jogYSpeed, jogZSpeed;
    public MouseRotate xRot, yRot, millHeadRot;
    public Text posNegText, otherPosNegText;
    public CoordinateDisplay coordDisp;

    public AudioSource startXYSource, loopXYSource, endXYSource, startZSource, loopZSource, endZSource;
    public float delayStartFadeOut, startFadeOutSpeed;
    public float loopFadeInSpeed, loopFadeOutSpeed;
    public float endFadeInSpeed;
    public float maxVolume;
    bool xySoundActive;
    bool zSoundActive;

    public bool isCurrentJogging;
    float timeSinceLastJog;
    // Start is called before the first frame update
    void Start()
    {
        isPositive = true;
        posNegText.text = "+";
        timeSinceLastJog = 1f;
        isCurrentJogging = false;
    }

    public void CyclePosNeg()
    {
        if (coordDisp.isJogging)
        {
            isPositive = !isPositive;
            if (isPositive)
            {
                Debug.Log("Is Positive!");
                posNegText.text = "+";
                otherPosNegText.text = "+";
            }
            else
            {
                Debug.Log("Is negative!");
                posNegText.text = "-";
                otherPosNegText.text = "-";
            }
        }
    }

    public void JogX()
    {
        if (isPositive)
        {
            xRot.Jog(jogXSpeed * Time.deltaTime);
        }
        else
        {
            xRot.Jog(-jogXSpeed * Time.deltaTime);
        }

        timeSinceLastJog = 0f;
    }

    public void JogY()
    {
        if (isPositive)
        {
            yRot.Jog(jogYSpeed * Time.deltaTime);
        }
        else
        {
            yRot.Jog(-jogYSpeed * Time.deltaTime);
        }

        timeSinceLastJog = 0f;
    }

    public void JogZ()
    {
        if (isPositive)
        {
            millHeadRot.Jog(jogZSpeed * Time.deltaTime);
        }
        else
        {
            millHeadRot.Jog(-jogZSpeed * Time.deltaTime);
        }
        timeSinceLastJog = 0f;
    }

    public void StartXYSounds()
    {
        if (coordDisp.isJogging)
        {
            xySoundActive = true;
            StopAllCoroutines();
            StartCoroutine(StartSoundFadeAfterFloat(startXYSource));
            StartCoroutine(FadeInLoop(loopXYSource));
        }
    }

    public void StartZSounds()
    {
        if (coordDisp.isJogging)
        {
            zSoundActive = true;
            StopAllCoroutines();
            StartCoroutine(StartSoundFadeAfterFloat(startZSource));
            StartCoroutine(FadeInLoop(loopZSource));
        }

    }

    public void EndXYSounds()
    {
        if (coordDisp.isJogging && xySoundActive)
        {
            xySoundActive = false;
            StopAllCoroutines();
            StartCoroutine(FadeOutLoop(loopXYSource));
            StartCoroutine(FadeInEndSound(endXYSource));
        }

    }

    public void EndZSounds()
    {
        if(coordDisp.isJogging && zSoundActive)
        {
            zSoundActive = false;
            StopAllCoroutines();
            StartCoroutine(FadeOutLoop(loopZSource));
            StartCoroutine(FadeInEndSound(endZSource));
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(timeSinceLastJog < .1f)
        {
            timeSinceLastJog += Time.deltaTime;
            if(timeSinceLastJog > .1f)
            {
                isCurrentJogging = false;
            }
            else
            {
                isCurrentJogging = true;
            }
        }
    }
    IEnumerator StartSoundFadeAfterFloat(AudioSource startSource)
    {
        startSource.volume = maxVolume;
        startSource.Play();
        yield return new WaitForSeconds(delayStartFadeOut);
        for (float i = maxVolume; i > 0; i -= Time.deltaTime * startFadeOutSpeed)
        {
            startSource.volume = Mathf.Max(0f, i);
            yield return null;
        }
        startSource.Stop();
    }

    IEnumerator FadeInLoop(AudioSource loopSource)
    {
        loopSource.volume = 0f;
        loopSource.Play();
        for (float i = 0; i < maxVolume; i += Time.deltaTime * loopFadeInSpeed)
        {
            loopSource.volume = Mathf.Min(maxVolume, i);
            yield return null;
        }

    }
    IEnumerator FadeOutLoop(AudioSource loopSource)
    {
        loopSource.volume = maxVolume;
        for (float i = maxVolume; i > 0; i -= Time.deltaTime * loopFadeOutSpeed)
        {
            loopSource.volume = Mathf.Max(0, i);
            yield return null;
        }
        loopSource.Stop();
    }

    IEnumerator FadeInEndSound(AudioSource endSource)
    {
        endSource.volume = 0;
        endSource.Play();
        for (float i = 0f; i < maxVolume; i += Time.deltaTime * endFadeInSpeed)
        {
            endSource.volume = Mathf.Min(maxVolume, i);
            yield return null;
        }
    }

    IEnumerator IsCurrentlyJogging()
    {
        isCurrentJogging = true;
        yield return new WaitForSeconds(.1f);
        isCurrentJogging = false;
    }
}
