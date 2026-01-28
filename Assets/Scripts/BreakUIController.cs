using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;



public class BreakUIController : MonoBehaviour
{
    public TrialManager trialManager;

    [SerializeField] private GameObject breakPanel;         //  parent panel 
    [SerializeField] private TextMeshProUGUI breakMessageText; // Text for "Break for X seconds"
    [SerializeField] private TextMeshProUGUI countdownText;    // Text for countdown numbers

    [SerializeField] private TextMeshProUGUI trialCountText;    // Text for current trial number
    [SerializeField] private TextMeshProUGUI scoreText;         // Text for score 

    public float totalBreakDuration;        // includes minimum break time + optional break time from the participant (in seconds)
    [SerializeField] private EEGMarkerPatterns eeg;

    private InputAction continueAction;     
    private bool spacePressed;

    private void Awake()
    {
        // Setup InputAction for spacebar
        continueAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/space");
        continueAction.performed += ctx => spacePressed = true;
        continueAction.Enable();
    }

    private void Start()
    {
        breakPanel.SetActive(false);
    }

    // Shows trial statistics () and a countdown for participant
    // After the minimum break time, participant can press the spacebar to continue to next trial
    // Called from TrialManager.cs
    // Is a coroutine 
    public IEnumerator ShowBreakUI(int breakSeconds, int trialNum, int totalTrials, int score, int totalPossibleScore)
    {
        float breakStartTime = Time.time;
        spacePressed = false;

        breakPanel.SetActive(true);

        Debug.LogError($"Break Start for {breakSeconds} seconds");
        Debug.LogError($"Trial {trialNum} / {totalTrials}");
        Debug.LogError($"Score: {score} / {totalPossibleScore}");

        EventLogger_CSVWriter.Log($"Break Start for {breakSeconds} seconds");
        breakMessageText.text = $"Break for {breakSeconds} seconds";

        trialCountText.text = $"Trial: {trialNum} / {totalTrials}";
        scoreText.text = $"Score: {score} / {totalPossibleScore}";

        for (int i = breakSeconds; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        // Done with minimum break
        countdownText.text = "Tell the researcher when you are ready\n to continue. Take as long as you need.";

        yield return new WaitForEndOfFrame(); // prevent held-space skip

        // Waits until space is pressed to end break
        while (!spacePressed)
        {
            yield return null;
        }

        // NOTE: to get here, space must have been pressed

        //gets break statistics
        float breakEndTime = Time.time;
        totalBreakDuration = breakEndTime - breakStartTime;

        trialManager.onBreak = false;
        breakPanel.SetActive(false);
        EventLogger_CSVWriter.Log("Break Over");
        eeg?.MarkBlockStart();

    }

    // Called from TrialManager.cs when experiment is complete
    public void ShowExperimentComplete(int trialNum, int totalTrials, int score, int totalPossibleScore)
    {
        breakPanel.SetActive(true);
        breakMessageText.text = "Congrats, the experiment is complete!";
        countdownText.text = "";
        trialCountText.text = $"Trial: {trialNum} / {totalTrials}";
        scoreText.text = $"Score: {score} / {totalPossibleScore}";
    }

    private void OnDestroy()
    {
        continueAction.Disable();
    }

}
