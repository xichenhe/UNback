using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Spinning : MonoBehaviour
{
    public static Spinning Instance;

    public static event System.Action<bool> SpinningEvent;
    public Transform spinningTrans;
    public bool startSpinning;
    public bool isSpinning;
    public float SpinSpeed
    {
        get { return spinSpeed; }
        set
        {
            if (spinSpeed != value)
            {
                spinSpeedSetForEdgefinding?.Invoke(value >= 900 && value <= 1100);
                spinSpeedSetForCenterDrilling?.Invoke(value >= 600 && value <= 1000);
            }
            spinSpeed = value;
        }
    }
    public float spinSpeed;
    public UnityEvent<bool> spinSpeedSetForEdgefinding;
    public UnityEvent<bool> spinSpeedSetForCenterDrilling;
    public float speedMultiplier;

    public GrabbableInventoryObject snappedToChuck;
    public SnapZone chuckSnapZone;

    public AudioSource startSource, loopSource, endSource;
    public float delayStartFadeOut, startFadeOutSpeed;
    public float loopFadeInSpeed, loopFadeOutSpeed;
    public float endFadeInSpeed;

    public float maxVolume;

    public float minPitch, maxPitch, minPitchSpeed, maxPitchSpeed;
    Vector3 startRot;

    public GameObject dialOn;
    public GameObject dialOff;
    public UnityEvent dialTurnedOnEvent;
    public UnityEvent dialTurnedOffEvent;
    public UnityEvent dialCheckedEvent;

    bool isLocked;
    public bool IsLocked
    {
        get { return isLocked; }
        set
        {
            isLocked = value;
            isLockedEvent?.Invoke(isLocked);
        }
    }

    public UnityEvent<bool> isLockedEvent;

    // Start is called before the first frame update
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void Start()
    {
        SpinningEventCall(isSpinning);

        if(startSpinning) 
            CycleSpin();
        //startRot = dial.transform.rotation.eulerAngles;
        //dialOff.SetActive(true);
        //dialOn.SetActive(false);

        //CycleSpin();
        snappedToChuck?.RemoveIfSnapped();
    }

    // Update is called once per frame
    void Update()
    {
        if (isSpinning)
        {


            spinningTrans.Rotate(0f, 0f, -SpinSpeed * speedMultiplier * Time.deltaTime);
            //Debug.Log(Mathf.Clamp(spinSpeed - minPitchSpeed, 0, (maxPitchSpeed - minPitchSpeed)));
            float percentPitch = Mathf.Clamp(SpinSpeed - minPitchSpeed, 0, (maxPitchSpeed - minPitchSpeed)) / (maxPitchSpeed - minPitchSpeed);
            //Debug.Log("Percent pitch is: " + percentPitch);
            loopSource.pitch = minPitch + percentPitch * (maxPitch - minPitch);
        }
    }

    public void SpinningEventCall(bool isSpinningBool)
    {
        isSpinning = isSpinningBool;
        SpinningEvent?.Invoke(isSpinning);
    }

    public void CycleSpin()
    {
        Debug.Log("Cycling spin.");
        SpinningEventCall(!isSpinning);
        if (isSpinning)
        {
            //dial.transform.rotation = Quaternion.Euler(startRot + new Vector3(0f, -30f, 0f));
            dialOn.SetActive(true);
            dialOff.SetActive(false);
            StartCoroutine(StartSoundFadeAfterFloat());
            StartCoroutine(FadeInLoop());

            dialTurnedOnEvent?.Invoke();
        }
        else
        {
            //dial.transform.rotation = Quaternion.Euler(startRot);
            dialOff.SetActive(true);
            dialOn.SetActive(false);
            StartCoroutine(FadeOutLoop());
            StartCoroutine(FadeInEndSound());

            dialTurnedOffEvent?.Invoke();
        }
    }

    public void EmergencyStop()
    {
        if (isSpinning)
        {
            CycleSpin();
        }
    }


    IEnumerator StartSoundFadeAfterFloat()
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

    IEnumerator FadeInLoop()
    {
        loopSource.volume = 0f;
        loopSource.Play();
        for (float i = 0; i < maxVolume; i += Time.deltaTime * loopFadeInSpeed)
        {
            loopSource.volume = Mathf.Min(maxVolume, i);
            yield return null;
        }

    }
    IEnumerator FadeOutLoop()
    {
        loopSource.volume = maxVolume;
        for (float i = maxVolume; i > 0; i -= Time.deltaTime * loopFadeOutSpeed)
        {
            loopSource.volume = Mathf.Max(0, i);
            yield return null;
        }
        loopSource.Stop();
    }

    IEnumerator FadeInEndSound()
    {
        endSource.volume = 0;
        endSource.Play();
        for (float i = 0f; i < maxVolume; i += Time.deltaTime * endFadeInSpeed)
        {
            endSource.volume = Mathf.Min(maxVolume, i);
            yield return null;
        }
    }
    public void CheckDial()
    {
        dialCheckedEvent?.Invoke();
    }
}
