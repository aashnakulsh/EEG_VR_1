using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TrialManager : MonoBehaviour
{
    //Cube info
    public List<GameObject> cubes;
    public Color highlightColor;
    public Color defaultColor;
    public Color ghostColor = Color.green;
    public bool onBreak = false;

    // Generic trial info
    public int totalTrials;
    public int ghostUpdateInterval;
    public float targetTrialPercentage = 0.2f;

    public bool isExperimentComplete = false;

    // Break info
    public int breakInterval;
    [SerializeField] private BreakUIController breakUI;  // drag the BreakUIController GameObject in inspector
    public int breakTime;
    public bool tookBreak;

    // Statistics to Keep Track of
    private int currentTrial = 0;
    public int ghostCubeIndex = -1;
    private List<int> targetTrialIndices = new List<int>();

    private System.Random rng = new System.Random();

    public TrialLogger trialLogger;
    // public EventLogger eventLogger;

    public int score;
    public int totalPossibleScore;


    public int CurrentTrialNumber => currentTrial;
    public int CurrentTargetCubeIndex { get; private set; }
    public float TrialStartTime { get; private set; }

    private void SetTrialInfo(int targetIndex)
    {
        CurrentTargetCubeIndex = targetIndex;
        TrialStartTime = Time.time;
    }
    //end of test


    private void Start()
    {
        trialLogger.Init();
        EventLogger_CSVWriter.Init();
        GenerateTargetTrials();
        PickNewGhostCube(); // initial ghost
    }

    private void GenerateTargetTrials()
    {
        int numTargetTrials = Mathf.RoundToInt(totalTrials * targetTrialPercentage);
        HashSet<int> chosen = new HashSet<int>();
        while (chosen.Count < numTargetTrials)
        {
            chosen.Add(rng.Next(totalTrials));
        }
        targetTrialIndices = chosen.OrderBy(i => i).ToList();
    }

    private void PickNewGhostCube()
    {
        int newGhost;
        do
        {
            newGhost = rng.Next(cubes.Count);
        } while (newGhost == ghostCubeIndex); // Avoid repeats

        ghostCubeIndex = newGhost;

        EventLogger_CSVWriter.Log($"Ghost Cube Picked: {ghostCubeIndex}");
        Debug.LogError($"🔄🔄🔄 New ghost cube is at index {ghostCubeIndex}.");
    }

    // called in planetrigger script
    public IEnumerator StartNextTrial()
    {
        tookBreak = false;
        if ((currentTrial != 0) && (currentTrial != totalTrials) && (currentTrial % breakInterval == 0))
        {
            // StartCoroutine(runBreak());
            // Run break!
            Debug.LogWarning("START BREAK");
            // EventLogger_CSVWriter.Log("Start Break"); IS DONE IN 
            onBreak = true;
            tookBreak = true;

            yield return breakUI.ShowBreakUI(breakTime, currentTrial, totalTrials, score, totalPossibleScore);
            // yield return new WaitForSeconds(breakTime);
        }
        if (currentTrial >= totalTrials)
        {
            Debug.LogError("Experiment complete");
            EventLogger_CSVWriter.Log("Experiment Complete");
            isExperimentComplete = true;

            breakUI.ShowExperimentComplete();
            yield break;
        }



        // Change ghost cube if needed
        if (currentTrial % ghostUpdateInterval == 0 && currentTrial != 0)
        {
            PickNewGhostCube();
        }

        // Determine which cube to highlight
        int cubeIndexToHighlight;
        bool isTargetTrial = targetTrialIndices.Contains(currentTrial);

        if (isTargetTrial)
        {
            cubeIndexToHighlight = ghostCubeIndex;
        }
        else
        {
            // Choose a non-ghost cube
            List<int> options = Enumerable.Range(0, cubes.Count).Where(i => i != ghostCubeIndex).ToList();
            cubeIndexToHighlight = options[rng.Next(options.Count)];
        }

        EventLogger_CSVWriter.Log($"Trial {currentTrial + 1} Begins");
        SetTrialInfo(cubeIndexToHighlight);
        HighlightCube(cubeIndexToHighlight);
        currentTrial++;
    }

    private void HighlightCube(int index)
    {
        for (int i = 0; i < cubes.Count; i++)
        {
            Renderer rend = cubes[i].GetComponent<Renderer>();

            if (i == index)
            {
                // This cube is the one to touch in this trial
                rend.material.color = highlightColor;
            }
            else if (i == ghostCubeIndex)
            {
                // This cube is the ghost cube, but not the one being highlighted
                rend.material.color = ghostColor;
            }
            else
            {
                // Normal cube
                rend.material.color = defaultColor;
            }
        }
        EventLogger_CSVWriter.Log($"Cube Highlighted: {index}");
        Debug.LogWarning($"Trial {currentTrial + 1}/{totalTrials}: Highlighting cube {index}. Target Trial? {index == ghostCubeIndex}");
    }

    // Called in CubeTrigger.cs in OnTriggerEnter function
    public void ResetCubes()
    {
        foreach (var cube in cubes)
        {
            cube.GetComponent<Renderer>().material.color = defaultColor;
        }
    }

}
