using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


/// <summary>
/// This script runs the "Calibration Phase" at the beginning of the game instance.
/// Gives the user time to calibrate table via right controller and manually position the cubes on the table 
/// For experiment, the experimenter should be doing this before the acutal participant gets in the room
/// To end "Calibration Phase", ues the SPACE bar. 
/// </summary>
public class CaliPhase : MonoBehaviour
{
    public PlaneTrigger planeTrigger;
    public GameObject calibrationUI;
    private InputAction spaceAct;

    private bool waiting = true;

    private void Awake()
    {
        if (calibrationUI) calibrationUI.SetActive(true);

        spaceAct = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/space");
        spaceAct.performed += _ => OnSpacePressed();
        spaceAct.Enable();
        // if (calibrationUI) calibrationUI.SetActive(true);

    }
    private void Start()
    {
        if (calibrationUI) calibrationUI.SetActive(true);
    }
    
    private void OnDestroy()
    {
        spaceAct.Disable();
    }
   
    private void OnSpacePressed()
        {
            if (!waiting) return;

            // resets flags to start trials
            if (planeTrigger)
            {
                planeTrigger.ResetCubeFlag();
                planeTrigger.ResetPlaneFlag();
            }

            // hide UI
            if (calibrationUI) calibrationUI.SetActive(false);

            waiting = false;
            Debug.LogError("[CaliPhase] Calibration complete. Systems enabled.");
        }
}
