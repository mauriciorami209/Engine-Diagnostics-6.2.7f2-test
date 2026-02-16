using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.UI;
using UnityEngine.CrashReportHandler;
using Unity.Services.Core;

public class TestUI : MonoBehaviour
{
    public Button quitButton;
    public Button throwExceptionButton;
    public Button forceCrashButton;
    public Button ooRErrorButton;
    public Button forceCrashAccessViolationButton;
    public Button nullReferenceErrorButton;
    public GameObject leaveEmpty;

    private async void Start()
    {
        // Initialize Unity Services first (required for ExternalUserId)
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }

        // Set up all three types of metadata for testing
        SetupAllMetadataMethods();

        quitButton.onClick.AddListener(QuitApp);
        throwExceptionButton.onClick.AddListener(ThrowException);
        forceCrashButton.onClick.AddListener(ForceCrash);
        ooRErrorButton.onClick.AddListener(OORError);
        forceCrashAccessViolationButton.onClick.AddListener(ForceCrashAccessViolation);
        nullReferenceErrorButton.onClick.AddListener(NullReferenceTest);
    }

    private void SetupAllMetadataMethods()
    {
        // METHOD 1: CrashReportHandler.SetUserMetadata
        Debug.Log("=== Setting up CrashReportHandler metadata ===");
        try
        {
            CrashReportHandler.SetUserMetadata("CRH_user_id", "user_12345");
            CrashReportHandler.SetUserMetadata("CRH_session_id", System.Guid.NewGuid().ToString());
            CrashReportHandler.SetUserMetadata("CRH_test_environment", "QA_Testing");
            CrashReportHandler.SetUserMetadata("CRH_tester_name", "Mauricio Ramirez");
            CrashReportHandler.SetUserMetadata("CRH_platform", Application.platform.ToString());
            CrashReportHandler.SetUserMetadata("CRH_unity_version", Application.unityVersion);
            CrashReportHandler.SetUserMetadata("CRH_device_model", SystemInfo.deviceModel);
            CrashReportHandler.SetUserMetadata("CRH_os", SystemInfo.operatingSystem);
            Debug.Log("CrashReportHandler metadata set successfully");
            
            // Esta línea es opcional pero recomendada para asegurar la captura
            CrashReportHandler.enableCaptureExceptions = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set CrashReportHandler metadata: {e.Message}");
        }

        // METHOD 2: UnityServices.ExternalUserId
        Debug.Log("=== Setting up UnityServices.ExternalUserId ===");
        try
        {
            UnityServices.ExternalUserId = "external_user_mauricio_12345";
            Debug.Log($"UnityServices.ExternalUserId set to: {UnityServices.ExternalUserId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set ExternalUserId: {e.Message}");
        }

        // METHOD 3: Additional CrashReportHandler metadata with Custom prefix
        // Note: UserReporting.Metadata API is not available in Unity 6
        // Using CrashReportHandler for additional custom metadata instead
        Debug.Log("=== Setting up Additional Custom Metadata via CrashReportHandler ===");
        try
        {
            CrashReportHandler.SetUserMetadata("Custom_user_id", "custom_user_67890");
            CrashReportHandler.SetUserMetadata("Custom_session_type", "Diagnostic_Test_Session");
            CrashReportHandler.SetUserMetadata("Custom_test_scenario", "Metadata_Validation");
            CrashReportHandler.SetUserMetadata("Custom_tester", "Mauricio_Ramirez_Team");
            CrashReportHandler.SetUserMetadata("Custom_build_type", "Debug_Build");
            CrashReportHandler.SetUserMetadata("Custom_feature_flag", "DiagnosticsDashboard_v2");
            CrashReportHandler.SetUserMetadata("Custom_graphics_api", SystemInfo.graphicsDeviceType.ToString());
            CrashReportHandler.SetUserMetadata("Custom_memory_size", SystemInfo.systemMemorySize + "MB");
            Debug.Log("Additional custom metadata set successfully via CrashReportHandler");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to set additional custom metadata: {e.Message}");
        }

        Debug.Log("=== All metadata methods completed ===");
    }

    public void QuitApp()
    {
        Application.Quit();
    }

    public void ThrowException()
    {
        Debug.LogException(new Exception("Test Exception"));
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
        int[] testArray = new int[1];
        int goingToFail = testArray[2];
    }

    public void NullReferenceTest()
    {
        string willFail = leaveEmpty.name;
    }
}
