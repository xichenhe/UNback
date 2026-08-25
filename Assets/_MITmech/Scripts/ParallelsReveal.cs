using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ParallelsReveal : MonoBehaviour
{
    //New Project

    [Header("Current Status")]
    public GameObject parallelStep;
    public GameObject parallelSnapZone;
    public bool revealed = false;

    [Header("Parallels Secure")]
    public MeshRenderer parallelFront;
    public MeshRenderer parallelBack;


    [Header("Parallels Loose Variant1")]
    public GameObject parallelV1Front;
    public GameObject parallelV1Back;
    public Quaternion rotationV1Front;
    public Quaternion rotationV1Back;

    [Header("Parallels Loose Variant 2")]
    public GameObject parallelV2Front;
    public GameObject parallelV2Back;
    public Quaternion rotationV2Front;
    public Quaternion rotationV2Back;

    [Header("Status Text")]
    [Tooltip("Variable UI Text for the machine componant status")]
    public TextMeshProUGUI statusText;

    [Tooltip("Reference to the UI Image component which will change based on the status")]
    public Image statusColor; // Update the color of the statusColor based on the current state
    public Color looseColor = Color.red; // State Color
    public Color tightColor = Color.green; // State Color

    void Start() //Inital Parallel State
    {
        //Parallel Objects Status
        parallelSnapZone.SetActive(true);
        rotationV1Back = parallelV1Back.transform.rotation;
        rotationV1Front = parallelV1Front.transform.rotation;
        rotationV2Back = parallelV2Back.transform.rotation;
        rotationV2Front = parallelV2Front.transform.rotation;

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    private void OnEnable()
    {
        //Parallel Objects Status
        revealed = true;
        parallelBack.enabled = true;
        parallelFront.enabled = true;

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    private void OnDisable()
    {
        //Parallel Objects Status
        revealed = false;
        parallelBack.enabled = false;
        parallelFront.enabled = false;

        parallelV1Back.SetActive(false);
        parallelV1Front.SetActive(false);

        parallelV2Back.SetActive(false);
        parallelV2Front.SetActive(false);
        //parallelSnapZone.SetActive(false);

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    public void SlideParallel()
    {
        //Parallel Objects Status
        parallelBack.enabled = false;
        parallelFront.enabled = false;
        parallelV1Back.SetActive(true);
        parallelV1Front.SetActive(true);
        //parallelStatus.text = "Tight";

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    public void SlideSecuredParallel()
    {
        //Parallel Objects Status
        parallelBack.enabled = false;
        parallelFront.enabled = false;
        parallelV2Back.SetActive(true);
        parallelV2Front.SetActive(true);
        //parallelStatus.text = "Tight";

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    public void ReverseParallel()
    {
        //Parallel Objects Status
        parallelV1Back.transform.rotation = rotationV1Back;
        parallelV1Front.transform.rotation = rotationV1Front;
        parallelV1Back.SetActive(false);
        parallelV1Front.SetActive(false);

        parallelV2Back.transform.rotation = rotationV2Back;
        parallelV2Front.transform.rotation = rotationV2Front;
        parallelV2Back.SetActive(false);
        parallelV2Front.SetActive(false);

        parallelBack.enabled = true;
        parallelFront.enabled = true;
        parallelStep.SetActive(false);

        //Machining Componant Status
        statusText.text = "Tight"; // Update the statusText based on the current state
        statusColor.color = tightColor; // Update the color of the statusColor based on the current state
    }

    /*
    //Working
    [Header("Current Status")]
    public GameObject parallelStep;
    public GameObject parallelSnapZone;
    public bool revealed = false;

    [Header("Parallels Secure")]
    public MeshRenderer parallel1;
    public MeshRenderer parallel2;

    [Header("Parallels Loose Variant 01")]
    public GameObject parallel1Slide;
    public GameObject parallel2Slide;
    public Quaternion parallel1SlideRot, parallel2SlideRot;

    [Header("Parallels Loose Variant 02")]
    public GameObject parallel1SecureSlide;
    public GameObject parallel2SecureSlide;
    public Quaternion parallel1SecureSlideRot, parallel2SecureSlideRot;

    [Header("Status Text")]
    [Tooltip("Variable UI Text for the machine componant status")]
    public TextMeshProUGUI statusText;

    [Tooltip("Reference to the UI Image component which will change based on the status")]
    public Image statusColor;
    public Color looseColor = Color.red; // State Color
    public Color tightColor = Color.green; // State Color

    // Start is called before the first frame update
    void Start()
    {
        //Parallel Objects Status
        parallelSnapZone.SetActive(true);
        parallel1SlideRot = parallel1Slide.transform.rotation;
        parallel2SlideRot = parallel2Slide.transform.rotation;
        parallel1SecureSlideRot = parallel1SecureSlide.transform.rotation;
        parallel2SecureSlideRot = parallel2SecureSlide.transform.rotation;

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    private void OnEnable()
    {
        //Parallel Objects Status
        revealed = true;
        parallel1.enabled = true;
        parallel2.enabled = true;

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    private void OnDisable()
    {
        //Parallel Objects Status
        revealed = false;
        parallel1.enabled = false;
        parallel2.enabled = false;

        parallel1Slide.SetActive(false);
        parallel2Slide.SetActive(false);

        parallel1SecureSlide.SetActive(false);
        parallel2SecureSlide.SetActive(false);
        //parallelSnapZone.SetActive(false);

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    public void SlideParallel()
    {
        //Parallel Objects Status
        parallel1.enabled = false;
        parallel2.enabled = false;
        parallel1Slide.SetActive(true);
        parallel2Slide.SetActive(true);
        //parallelStatus.text = "Tight";

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    public void SlideSecuredParallel()
    {
        //Parallel Objects Status
        parallel1.enabled = false;
        parallel2.enabled = false;
        parallel1SecureSlide.SetActive(true);
        parallel2SecureSlide.SetActive(true);
        //parallelStatus.text = "Tight";

        //Machining Componant Status
        statusText.text = "Loose"; // Update the statusText based on the current state
        statusColor.color = looseColor; // Update the color of the statusColor based on the current state
    }

    public void ReverseParallel()
    {
        //Parallel Objects Status
        parallel1Slide.transform.rotation = parallel1SlideRot;
        parallel2Slide.transform.rotation = parallel2SlideRot;
        parallel1Slide.SetActive(false);
        parallel2Slide.SetActive(false);

        parallel1SecureSlide.transform.rotation = parallel1SecureSlideRot;
        parallel2SecureSlide.transform.rotation = parallel2SecureSlideRot;
        parallel1SecureSlide.SetActive(false);
        parallel2SecureSlide.SetActive(false);

        parallel1.enabled = true;
        parallel2.enabled = true;
        parallelStep.SetActive(false);

        //Machining Componant Status
        statusText.text = "Tight"; // Update the statusText based on the current state
        statusColor.color = tightColor; // Update the color of the statusColor based on the current state
    }

    /*
    [Header("Current Status")]

    public GameObject parallelStep;
    public GameObject parallelSnapZone;

    public bool revealed = false;

    [Header("Parallel Secure")]
    public MeshRenderer parallel1;
    public MeshRenderer parallel2;

    [Header("Parallel Loose Option 01")]
    public GameObject option01Back;
    public GameObject option01Clamp;
    public Quaternion rotationOption01Back;
    public Quaternion rotationOption01Clamp;

    [Header("Parallel Loose Option 02")]
    public GameObject option02Back;
    public GameObject option02Clamp;
    public Quaternion rotationOption02Back;
    public Quaternion otationOption02Clamp;

    
    */

}
