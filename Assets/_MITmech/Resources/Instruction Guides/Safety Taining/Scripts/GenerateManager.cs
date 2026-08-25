using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GenerateManager : MonoBehaviour
{
    public static GenerateManager Instance;
    public List<GenerateRandom> genList;
    public int minFixes;
    public int maxFixes;

    public Sprite[] avatarList;

    // temp solution
    public GenerateRandom[] allCorrectList; // a list of generate randoms where the objects cannot be fixed
    public bool VR;

    private int fixesLeft; // how many fixes are still needed 

    private void Awake()
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

    void Start()
    {
        if (maxFixes > genList.Count + 1)
        {
            maxFixes = genList.Count + 1;
        }
        if (minFixes < 1)
        {
            minFixes = 1;
        }
        GenerateAvatar();
    }

    public void GenerateAvatar()
    {
        // first generate an avatar
        if (VR)
        {
            GetComponent<UnityEngine.UI.Image>().sprite = avatarList[Random.Range(0, avatarList.Length)]; // Use For UI
        }
        else {
            GetComponent<SpriteRenderer>().sprite = avatarList[Random.Range(0, avatarList.Length)]; // Use For GameObject
        }
           
        //GetComponent<Image>().sprite = avatarList[Random.Range(0, avatarList.Length)]; // Use For UI


        int numFixes = Random.Range(minFixes, maxFixes + 1);
        fixesLeft = numFixes;

        Debug.Log("Generating Avatar With Fixes #: " + numFixes);

        // randomly generate objects that needs to be fixed
        for (int i = 0; i < numFixes; i++)
        {
            int j = Random.Range(0, genList.Count);

            Debug.Log("Generating Incorrect " + genList[j].gameObject.name);
            genList[j].GenerateIndex();
            genList[j].GenerateIncorrect();
            genList.RemoveAt(j);
        }

        // generate the objects that does not need to be fixed
        for (int i = 0; i < genList.Count; i++)
        {
            Debug.Log("Generating Correct " + genList[i].gameObject.name);
            genList[i].GenerateIndex();
            genList[i].GenerateCorrect();
        }

        for (int i = 0; i < allCorrectList.Length; i++)
        {
            Debug.Log("Generating Correct " + allCorrectList[i].gameObject.name);
            allCorrectList[i].GenerateIndex();
            allCorrectList[i].GenerateCorrect();
        }
    }
  
}
