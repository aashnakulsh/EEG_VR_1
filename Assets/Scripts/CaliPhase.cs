using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;  

public class CaliPhase : MonoBehaviour
{
    public PlaneTrigger planeTrigger;

    public GameObject calibrationUI;
    [SerializeField] private TMP_Text calibrationLabel;  

    private InputAction spaceAct;

    [SerializeField] private int preCalibrationRestSeconds = 180; // 3 minutes
    [SerializeField] private EEGMarkerPatterns eeg;

    private bool waiting = false; // ignore SPACE during rest

    private void Awake()
    {
        if (calibrationUI) calibrationUI.SetActive(true); // show the same panel during rest

        spaceAct = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/s");
        spaceAct.performed += _ =>
        {
            OnSpacePressed();
            UnityEngine.Debug.LogError("S HAS BEEN PRESSED!");
        };
        spaceAct.Enable();

    }

    // private void Start()
    // {
    //     EventLogger_CSVWriter.Log("Calibration Phase Begun");
    //     StartCoroutine(RestThenShowCalibration());

    // }
    private void Start()
    {
        EventLogger_CSVWriter.Log("Calibration Phase Begun");

        // Calibration FIRST
        if (calibrationLabel)
            calibrationLabel.text = "Allow researcher to calibrate the table.\n Please follow their instructions.";

        waiting = true; // S ends calibration now
    }


    // private System.Collections.IEnumerator RestThenShowCalibration()
    // {
    //     Debug.LogError("IN REST THEN SHOW CALI PHASE");
    //     int t = Mathf.Max(0, preCalibrationRestSeconds);
    //     eeg?.BaselinePhaseStart();

    //     // Countdown on the SAME UI
    //     while (t > 0)
    //     {
    //         if (calibrationLabel)
    //             //calibrationLabel.text = $"";
    //             Debug.LogError($"Please rest to create a baseline for the EEG\n{t / 60:D2}:{t % 60:D2}");
    //             calibrationLabel.text = "";     // no text/countdown
    //         yield return new WaitForSeconds(1f);
    //         t--;
    //     }
    //     eeg?.BaselinePhaseEnd();


        
    //     if (calibrationLabel)
    //         calibrationLabel.text = "Calibrate table now.\nPress SPACE when finished.";

    //     waiting = true; // now SPACE will end the calibration phase
    // }

    private void OnDestroy()
    {
        spaceAct.Disable();
    }

    // private void OnSpacePressed()
    // {
    //     if (!waiting) return;

    //     if (planeTrigger)
    //     {
    //         planeTrigger.ResetCubeFlag();
    //         planeTrigger.ResetPlaneFlagFirst();
    //     }

    //     if (calibrationUI) calibrationUI.SetActive(false);
    //     EventLogger_CSVWriter.Log("Calibration Phase Over");
    //     eeg?.MarkExperimentStart(); // since calibration phase is over, we can send start experiement markers


    //     waiting = false;
    //     Debug.LogError("[CaliPhase] Calibration complete.");
    // }

    private void OnSpacePressed()
    {
        if (!waiting) return;

        // Calibration is now complete; baseline starts next
        waiting = false; // ignore more SPACE during baseline
        StartCoroutine(BaselineThenStartTrials());
    }

    private System.Collections.IEnumerator BaselineThenStartTrials()
    {
        int t = Mathf.Max(0, preCalibrationRestSeconds);

        eeg?.BaselinePhaseStart();

        while (t > 0)
        {
            if (calibrationLabel)
                calibrationLabel.text = $"Please rest to create a baseline for the EEG\n{t / 60:D2}:{t % 60:D2}";
            yield return new WaitForSeconds(1f);
            t--;
        }

        eeg?.BaselinePhaseEnd();

        // NOW enable trials (same flag reset you were doing before)
        if (planeTrigger)
        {
            planeTrigger.ResetCubeFlag();
            planeTrigger.ResetPlaneFlagFirst();
        }

        if (calibrationUI) calibrationUI.SetActive(false);
        EventLogger_CSVWriter.Log("Calibration Phase Over");
        eeg?.MarkExperimentStart();

        Debug.LogError("[CaliPhase] Calibration + baseline complete. Trials enabled.");
    }


}
