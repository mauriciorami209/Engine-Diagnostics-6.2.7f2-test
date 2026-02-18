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
        // UnityServices.InitializeAsync() is required to initialize the
        // Cloud Diagnostics pipeline before setting any metadata
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }

        // Set up metadata AFTER Unity Services is initialized
        SetupMetadata();

        // Assigning button listeners
        quitButton.onClick.AddListener(QuitApp);
        throwExceptionButton.onClick.AddListener(ThrowException);
        forceCrashButton.onClick.AddListener(ForceCrash);
        ooRErrorButton.onClick.AddListener(OORError);
        forceCrashAccessViolationButton.onClick.AddListener(ForceCrashAccessViolation);
        nullReferenceErrorButton.onClick.AddListener(NullReferenceTest);
    }

    private void SetupMetadata()
    {
        Debug.Log("=== Setting up CrashReportHandler metadata ===");
        try
        {
            // Using Method 1: CrashReportHandler.SetUserMetadata
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
        SetupMetadata();
        throw new Exception("Test Exception");
    }

    public void ForceCrash()
    {
        SetupMetadata();
        Utils.ForceCrash(ForcedCrashCategory.FatalError);
    }

    public void ForceCrashAccessViolation()
    {
        SetupMetadata();
        Utils.ForceCrash(ForcedCrashCategory.AccessViolation);
    }

    public void OORError()
    {
        SetupMetadata();
        int[] testArray = new int[1];
        int goingToFail = testArray[2]; // Triggers IndexOutOfRangeException
    }

    public void NullReferenceTest()
    {
        SetupMetadata();
        // Ensure 'leaveEmpty' is not assigned in the inspector for this test
        string willFail = leaveEmpty.name;
    }
}
