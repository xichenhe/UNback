using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class GenerateRandom : MonoBehaviour
{

    public Sprite[] objNeedFix;
    public Sprite[] objFixed;
    public Sprite[] objCorrect;

    public bool needFixAvaliable; // there are objects which may violate lab safety rules
    public bool fixByChange; // objects are fixed via changing itself
    public bool fixByRemove; // objects are fixed via removing
    public bool fixByExchange; // objects are fixed via using a different object

    /*public Sprite[][] dictKey;
    public List<UnityEngine.UI.Button>[] dictValue;

    private Dictionary<Sprite[], List<UnityEngine.UI.Button>> buttonDict; // determines the displayed button based on current sprite
    */
    public List<UnityEngine.UI.Button> buttonNeedFix;
    public List<UnityEngine.UI.Button> buttonFixed;
    public List<UnityEngine.UI.Button> buttonCorrect;
    private List<UnityEngine.UI.Button> buttonList;

    private int incorrectSpriteIndex = 0;
    private int correctSpriteIndex = 0;

    public bool isFixed = true;

    public GameObject buttonAnchors;
    public bool VR;

    // Start is called before the first frame update
    void Start()
    {/*
        for (int i = 0; i < dictKey.Length; i++) {
            buttonDict.Add(dictKey[i], dictValue[i]);
        }*/
    }

    public void GenerateIndex()
    {
        if (needFixAvaliable)
        {
            incorrectSpriteIndex = Random.Range(0, objNeedFix.Length);
        }
        if (!fixByRemove)
        {
            correctSpriteIndex = Random.Range(0, objCorrect.Length);
        }
    }

    public void GenerateCorrect()
    {
        isFixed = true;
        buttonList = buttonCorrect;
        // if object is fixed by remove, generate nothing
        if (!fixByRemove)
        {
            if (VR) {
                GetComponent<UnityEngine.UI.Image>().sprite = objCorrect[correctSpriteIndex];
                Debug.Log("Generating " + objCorrect[correctSpriteIndex].name);
            }
            else
            {
                
                Destroy(GetComponent<Collider2D>());
                GetComponent<SpriteRenderer>().sprite = objCorrect[correctSpriteIndex];
                Debug.Log("Generating " + objCorrect[correctSpriteIndex].name);
                gameObject.AddComponent<PolygonCollider2D>();
            }
        }
        else
        {
            if (VR) {
                GetComponent<UnityEngine.UI.Image>().enabled = false;
            }
            else
            {
                GetComponent<SpriteRenderer>().sprite = null;
                Destroy(GetComponent<Collider2D>());
            }
        }

    }

    public void GenerateIncorrect()
    {
        isFixed = false;
        buttonList = buttonNeedFix;
        if (needFixAvaliable)
        {
            if (VR) {
                GetComponent<UnityEngine.UI.Image>().sprite = objNeedFix[incorrectSpriteIndex];
            }
            else
            {
                Destroy(GetComponent<Collider2D>());
                GetComponent<SpriteRenderer>().sprite = objNeedFix[incorrectSpriteIndex];
                gameObject.AddComponent<PolygonCollider2D>();
            }
        }
        else
        {
            Debug.Log("Attempting to generate something wrong :(");
        }
    }

    public void GenerateFixed()
    {
        isFixed = true;
        buttonList = buttonFixed;
        if (VR)
        { 
            GetComponent<UnityEngine.UI.Image>().sprite = objFixed[incorrectSpriteIndex]; 
        }
        else
        {
            Destroy(GetComponent<Collider2D>());
            GetComponent<SpriteRenderer>().sprite = objFixed[incorrectSpriteIndex];
            gameObject.AddComponent<PolygonCollider2D>();
        }
    }

    public void OnMouseDown()
    {
        Debug.Log("clicked " + name);
        if (buttonList != null)
        {
            Debug.Log("button list count: " + buttonList.Count);
            UpdateButtons(new List<UnityEngine.UI.Button>(buttonList));
        }


        // enable the menu to edit current object
        //temp solution to distinguish between buttons

        //Sprite sp = GetComponent<SpriteRenderer>().sprite; // Use For GameObject
        //Sprite sp = GetComponent<Image>().sprite; // Use For UI

        /*
        // Display Random Buttons, atleast one is the correct option
        foreach (var s in buttonDict)
        {
            if (s.Key.Contains(sp)) {
                //update the buttons with the current set in random order
                UpdateButtons(s.Value);
                break;
            }
        }*/

    }

    private void UpdateButtons(List<UnityEngine.UI.Button> b)
    {
        int totalButton = b.Count;
        for (int i = 0; i < totalButton; i++)
        {
            UnityEngine.UI.Button button = b[i];
            button.gameObject.SetActive(true);
            // move button to correct position
            //button.GetComponent<RectTransform>().anchoredPosition = buttonAnchors.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition;
        }
    }

    public void Sparkle()
    {


    }
}
