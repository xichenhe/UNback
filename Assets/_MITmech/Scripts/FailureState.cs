using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class FailureState : MonoBehaviour
{
    public static FailureState Instance;
    private int consecutiveWarnCount = 0;
    private float lastWarnTime = -999f;


    [Header("Manager References")]
    public GameObject instructionCanvas;
    public bool popout = true;

    [Header("Warning State Manager")]
    [Tooltip("Text Mesh Pro (TMP) gameObject which is used as variable text for 'Warning State' UI")]
    public GameObject warningCanvas;
    public TextMeshProUGUI uiWarningText;
    public TextMeshProUGUI warningText;

    [Header("Failure State Manager")]
    [Tooltip("Text Mesh Pro (TMP) gameObject which is used as variable text for 'Failure State' UI")]
    public GameObject failureCanvas;
    public TextMeshProUGUI uiFailureText;
    public TextMeshProUGUI failureReasonText;

    // ============= NEW: Error tracking =============
    private int consecutiveErrorCount = 0;
    private float lastErrorTime = 0f;
    // ===============================================

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
        failureCanvas.SetActive(false);
        warningCanvas.SetActive(false);
    }

    public void SystemFailure(string errorMessage)
    {
        DismissWarning();
        Debug.Log("Failure: " + errorMessage);
        failureReasonText.text = errorMessage;
        failureCanvas.SetActive(true);

        if (Spinning.Instance.isSpinning)
        {
            Spinning.Instance.CycleSpin();
        }

        uiFailureText.text = errorMessage;

        // ============= LOGGING ADDED HERE =============
        // Track consecutive errors
        float timeSinceLast = Time.realtimeSinceStartup - lastErrorTime;
        consecutiveErrorCount = (timeSinceLast < 15f) ? consecutiveErrorCount + 1 : 1;
        lastErrorTime = Time.realtimeSinceStartup;

        EventLogger.Instance?.Log(
            "error",
            "system_failure",
            new Dictionary<string, object> {
        { "message",            errorMessage },
        { "consecutive_errors", consecutiveErrorCount },
        { "time_since_last",    timeSinceLast }
            }
        );
    }


    public void DisplayWarning(string warningMessage)
    {
        Debug.Log("Warning: " + warningMessage);
        warningCanvas.SetActive(true);
        warningText.text = warningMessage;
        uiWarningText.text = warningMessage;

        //================================================LOGGING ADDED HERE===================================
        float timeSinceLastW = Time.realtimeSinceStartup - lastWarnTime;
        consecutiveWarnCount = (timeSinceLastW < 15f) ? consecutiveWarnCount + 1 : 1;
        lastWarnTime = Time.realtimeSinceStartup;

        EventLogger.Instance?.Log(
            "warning",
            "warning",
            new Dictionary<string, object> {
        { "message",               warningMessage },
        { "consecutive_warnings",  consecutiveWarnCount },
        { "time_since_last",       timeSinceLastW }
            }
        );
    }


    public void DismissWarning()
    {
        warningCanvas.SetActive(false);
    }

    public void RestartScene()
    {
        // ============= LOGGING ADDED HERE =============
        EventLogger.Instance?.Log("scene_restart", "user_restarted_scene");
        // ============================================

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void TurnOffScreenDebug()
    {
        failureCanvas.SetActive(false);

        // ============= LOGGING ADDED HERE =============
        EventLogger.Instance?.Log("error_dismissed", "user_dismissed_error");
        // ============================================
    }

    void Update()
    {

    }
}