using System;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;
using UnityEngine.CrashReportHandler;
using Unity.Services.Core;

public class TestUI : MonoBehaviour
{
    [Header("UI Controls")]
    public Button quitButton;
    public Button throwExceptionButton;
    public Button forceCrashButton;
    public Button ooRErrorButton;
    public Button forceCrashAccessViolationButton;
    public Button nullReferenceErrorButton;

    [Header("Test References")]
    public GameObject leaveEmpty;

    private async void Start()
    {
        Debug.Log("Force a new try to upload symbols" + System.Guid.NewGuid());
        Debug.Log("=== Starting TestUI Initialization ===");
        
        // 1. Disable buttons to prevent clicking before initialization finishes
        SetButtonsInteractable(false);

        try
        {
            // 2. Wait for Cloud Diagnostics pipeline to initialize completely
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized successfully");
            
            // 3. Set up metadata AFTER Unity Services is initialized
            SetupMetadata();
            
            // 4. Enable buttons safely now that initialization is done
            SetButtonsInteractable(true);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }

        // Assigning button listeners
        quitButton.onClick.AddListener(QuitApp);
        throwExceptionButton.onClick.AddListener(ThrowException);
        forceCrashButton.onClick.AddListener(ForceCrash);
        ooRErrorButton.onClick.AddListener(OORError);
        forceCrashAccessViolationButton.onClick.AddListener(ForceCrashAccessViolation);
        nullReferenceErrorButton.onClick.AddListener(NullReferenceTest);
    }

    private void SetButtonsInteractable(bool state)
    {
        if (quitButton) quitButton.interactable = state;
        if (throwExceptionButton) throwExceptionButton.interactable = state;
        if (forceCrashButton) forceCrashButton.interactable = state;
        if (ooRErrorButton) ooRErrorButton.interactable = state;
        if (forceCrashAccessViolationButton) forceCrashAccessViolationButton.interactable = state;
        if (nullReferenceErrorButton) nullReferenceErrorButton.interactable = state;
    }

    private void SetupMetadata()
    {
        Debug.Log("=== Setting up CrashReportHandler metadata ===");
        try
        {
            CrashReportHandler.SetUserMetadata("CRH_user_id", "user_12345");
            CrashReportHandler.SetUserMetadata("CRH_session_id", Guid.NewGuid().ToString());
            CrashReportHandler.SetUserMetadata("CRH_test_environment", "QA_Testing");
            CrashReportHandler.SetUserMetadata("CRH_tester_name", "Mauricio Ramirez");
            CrashReportHandler.SetUserMetadata("CRH_platform", Application.platform.ToString());
            CrashReportHandler.SetUserMetadata("CRH_unity_version", Application.unityVersion);
            CrashReportHandler.SetUserMetadata("CRH_device_model", SystemInfo.deviceModel);
            CrashReportHandler.SetUserMetadata("CRH_os", SystemInfo.operatingSystem);

            // CRITICAL: Enable exception capture to ensure metadata is sent
            CrashReportHandler.enableCaptureExceptions = true;

            Debug.Log("CrashReportHandler metadata set successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set CrashReportHandler metadata: {e.Message}");
        }
    }

    // --- Action Methods ---

    public void QuitApp() => Application.Quit();

    public void ThrowException()
    {
        try 
        {
            throw new Exception("Test Exception for Cloud Diagnostics");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void ForceCrash()
    {
        Utils.ForceCrash(ForcedCrashCategory.FatalError);
    }

    public void ForceCrashAccessViolation()
    {
        Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
    }

    public void OORError()
    {
        try
        {
            int[] testArray = new int[1];
            int goingToFail = testArray[2]; // Triggers IndexOutOfRangeException
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void NullReferenceTest()
    {
        try
        {
            // Ensure 'leaveEmpty' is not assigned in the inspector for this test
            string willFail = leaveEmpty.name;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}

