// using UnityEngine;
// using UnityEngine.InputSystem;
// using UnityEngine.UI;


// /// <summary>
// /// This script runs the "Calibration Phase" at the beginning of the game instance.
// /// Gives the user time to calibrate table via right controller and manually position the cubes on the table 
// /// For experiment, the experimenter should be doing this before the acutal participant gets in the room
// /// To end "Calibration Phase", ues the SPACE bar. 
// /// </summary>
// public class CaliPhase : MonoBehaviour
// {
//     public PlaneTrigger planeTrigger;
    
//     public GameObject calibrationUI;
//     private InputAction spaceAct;

//     private bool waiting = true;

//     private void Awake()
//     {
//         if (calibrationUI) calibrationUI.SetActive(true);

//         spaceAct = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/space");
//         spaceAct.performed += _ => OnSpacePressed();
//         spaceAct.Enable();
//         // if (calibrationUI) calibrationUI.SetActive(true);

//     }
//     private void Start()
//     {
//         if (calibrationUI) calibrationUI.SetActive(true);
//         EventLogger_CSVWriter.Log("Calibration Phase Begun");

//     }
    
//     private void OnDestroy()
//     {
//         spaceAct.Disable();
//     }
   
//     private void OnSpacePressed()
//         {
//             if (!waiting) return;

//             // resets flags to start trials
//             if (planeTrigger)
//             {
//                 planeTrigger.ResetCubeFlag();
//                 planeTrigger.ResetPlaneFlag();
//             }

//             // hide UI
            
//             if (calibrationUI) calibrationUI.SetActive(false);
//             EventLogger_CSVWriter.Log("Calibration Phase Over");

//             waiting = false;
//             Debug.LogError("[CaliPhase] Calibration complete. Systems enabled.");
//         }
// }
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

    private void Start()
    {
        EventLogger_CSVWriter.Log("Calibration Phase Begun");
        StartCoroutine(RestThenShowCalibration());

    }

    private System.Collections.IEnumerator RestThenShowCalibration()
    {
        Debug.LogError("IN REST THEN SHOW CALI PHASE");
        int t = Mathf.Max(0, preCalibrationRestSeconds);
        eeg?.BaselinePhaseStart();

        // Countdown on the SAME UI
        while (t > 0)
        {
            if (calibrationLabel)
                //calibrationLabel.text = $"";
                Debug.LogError($"Please rest to create a baseline for the EEG\n{t / 60:D2}:{t % 60:D2}");
                calibrationLabel.text = "";     // no text/countdown
            yield return new WaitForSeconds(1f);
            t--;
        }
        eeg?.BaselinePhaseEnd();


        
        if (calibrationLabel)
            calibrationLabel.text = "Calibrate table now.\nPress SPACE when finished.";

        waiting = true; // now SPACE will end the calibration phase
    }

    private void OnDestroy()
    {
        spaceAct.Disable();
    }

    private void OnSpacePressed()
    {
        if (!waiting) return;

        if (planeTrigger)
        {
            planeTrigger.ResetCubeFlag();
            planeTrigger.ResetPlaneFlagFirst();
        }

        if (calibrationUI) calibrationUI.SetActive(false);
        EventLogger_CSVWriter.Log("Calibration Phase Over");
        eeg?.MarkExperimentStart(); // since calibration phase is over, we can send start experiement markers


        waiting = false;
        Debug.LogError("[CaliPhase] Calibration complete.");
    }
}
