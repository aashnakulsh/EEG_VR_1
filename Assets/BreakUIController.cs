using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;



public class BreakUIController : MonoBehaviour
{
    public TrialManager trialManager;

    [SerializeField] private GameObject breakPanel;         // The parent panel GameObject
    [SerializeField] private TextMeshProUGUI breakMessageText; // Text for "Break for X seconds"
    [SerializeField] private TextMeshProUGUI countdownText;    // Text for countdown numbers

    [SerializeField] private TextMeshProUGUI trialCountText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public float totalBreakDuration;


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
    public IEnumerator ShowBreakUI(int breakSeconds, int trialNum, int totalTrials, int score, int totalPossibleScore)
    {
        float breakStartTime = Time.time;
        spacePressed = false;

        breakPanel.SetActive(true);

        Debug.LogError($"Break Start for {breakSeconds} seconds");
        Debug.LogError($"Trial {trialNum} / {totalTrials}");
        Debug.LogError( $"Score: {score} / {totalPossibleScore}");

        EventLogger_CSVWriter.Log($"Break Start for {breakSeconds} seconds");
        breakMessageText.text = $"Break for {breakSeconds} seconds";

        trialCountText.text = $"Trial {trialNum} / {totalTrials}";
        scoreText.text = $"Score: {score} / {totalPossibleScore}";

        for (int i = breakSeconds; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        // Done with minimum break
        countdownText.text = "Press SPACE when ready";
        // progressBar.interactable = false; // just in case

        yield return new WaitForEndOfFrame(); // prevent held-space skip

        // // Wait until space is pressed
        // while (!Input.GetKeyDown(KeyCode.Space))
        // {
        //     yield return null;
        // }

        // Wait until space is pressed using the new system
        while (!spacePressed)
        {
            yield return null;
        }

        float breakEndTime = Time.time;
        totalBreakDuration = breakEndTime - breakStartTime;

        trialManager.onBreak = false;
        breakPanel.SetActive(false);
        EventLogger_CSVWriter.Log("Break Over");
    }

    public void ShowExperimentComplete()
    {
        breakPanel.SetActive(true);
        breakMessageText.text = "Experiment Complete";
        countdownText.text = "";
    }

    private void OnDestroy()
    {
        continueAction.Disable();
    }

}
