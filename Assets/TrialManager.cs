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

    // Generic trial info
    public int totalTrials;                             // # of total trials
    public int ghostUpdateInterval;                     // how often to update ghost cube's index
    public float targetTrialPercentage = 0.2f;          // percentage of trials that will have ghost cube

    public bool isExperimentComplete = false;

    // Break info
    public bool onBreak = false;
    public int breakInterval;
    [SerializeField] private BreakUIController breakUI; 
    public int minimumBreakTime;               
    public bool tookBreak;

    // Statistics to Keep Track of
    public int currentTrial = 0;
    public int ghostCubeIndex = -1;
    public int score;
    public int totalPossibleScore;
    public int currentTargetCubeIndex;
    public float TrialStartTime;

    // Setup
    private List<int> targetTrialIndices = new List<int>();
    private System.Random rng = new System.Random();

    private void Start()
    {
        TrialLogger_CSVWriter.Init();
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

    // called in planeTrigger.cs as a coroutine
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

            yield return breakUI.ShowBreakUI(minimumBreakTime, currentTrial, totalTrials, score, totalPossibleScore);
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
        bool isTargetTrial = targetTrialIndices.Contains(currentTrial);

        if (isTargetTrial)
        {
            currentTargetCubeIndex = ghostCubeIndex;
        }
        else
        {
            // Choose a non-ghost cube
            List<int> options = Enumerable.Range(0, cubes.Count).Where(i => i != ghostCubeIndex).ToList();
            currentTargetCubeIndex = options[rng.Next(options.Count)];
        }

        EventLogger_CSVWriter.Log($"Trial {currentTrial + 1} Begins");

        // sets trial info
        TrialStartTime = Time.time;

        HighlightCube(currentTargetCubeIndex);
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
    public void ClearCubeHighlight()
    {
        foreach (var cube in cubes)
        {
            cube.GetComponent<Renderer>().material.color = defaultColor;
        }
    }

}
