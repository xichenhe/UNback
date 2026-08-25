using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitchTrigger : MonoBehaviour
{
    public static CameraSwitchTrigger Instance;

    public enum CameraView { HomeScreen, TableView, QuillView, ScreenView, BlockCloseUp, BlockSideView}
    public CameraView currentCameraView;

    public GameObject[] buttonList;
    Animator anim;

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
        anim = GetComponent<Animator>();
    }

    private void Start()
    {

        ChangeToHomeScreen();
    }

    public void ChangeToHomeScreen()
    {
        anim.Play("FullViewHome");
        setButton(0);
        currentCameraView = CameraView.HomeScreen;
    }

    public void ChangeToTableView()
    {
        anim.Play("TableCloseUp");
        setButton(0);
        currentCameraView = CameraView.TableView;
    }

    public void ChangeToQuillView()
    {
        anim.Play("QuillCloseUp");
        setButton(0);
        currentCameraView = CameraView.QuillView;
    }

    public void ChangeToScreenView()
    {
        anim.Play("ScreenCloseUp");
        setButton(2);
        currentCameraView = CameraView.ScreenView;
    }

    public void ChangeToBlockCloseUp()
    {
        anim.Play("BlockCloseUp");
        setButton(0);
        currentCameraView = CameraView.BlockCloseUp;
    }

    public void ChangeToBlockSideView()
    {
        anim.Play("BlockSideView");
        setButton(1);
        currentCameraView = CameraView.BlockSideView;
    }

    private void setButton(int i) {  // 0 - move, 1- swtich, 2 - none
        if (i == 0)
        {
            buttonList[0].SetActive(true);
            buttonList[1].SetActive(false);
        }
        else if (i == 1)
        {
            buttonList[1].SetActive(true);
            buttonList[0].SetActive(false);
        }
        else
        {
            buttonList[1].SetActive(false);
            buttonList[0].SetActive(false);
        }
    }
}
