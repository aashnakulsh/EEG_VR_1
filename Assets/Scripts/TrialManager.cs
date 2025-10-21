using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using Debug = UnityEngine.Debug;

public class TrialManager : MonoBehaviour
{
    public bool debugMode = false;
    //Cube info
    public List<GameObject> cubes;
    public Color highlightColor;
    public Color defaultColor;
    public Color ghostColor = Color.green;

    // Generic trial info
    public int noGhostStartTrials;
    public int minTargetGap;
    public int maxTargetGap;
    public int n;
    public int totalTrials;                             // # of total trials
    // public int ghostUpdateInterval;                     // how often to update ghost cube's Idx
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
    public int ghostCubeIdx = -1;
    public int score;
    public int totalPossibleScore;
    public int currTargetCubeIdx = -1;
    public float TrialStartTime;


    // Setup
    private List<int> targetTrialIndices = new List<int>();
    private System.Random rng = new System.Random();
    private List<(int cubeIndex, bool isTarget)> trialSequence;

    [Serializable]
    public class Trial
    {
        public int cubeIndex;
        public bool isTarget;
    }

    [Serializable]
    public class TrialList
    {
        public List<Trial> trials;
    }


    [Header("Python Settings")]
    public string pythonPath = "python";  // or full path if needed
    public string scriptPath = "Scripts/trial_generator_ortools.py";
    public int maxTimeSeconds = 300;
    public List<Trial> generatedTrials = new List<Trial>();
        
    private void Start()
    {
        TrialLogger_CSVWriter.Init();
        EventLogger_CSVWriter.Init();

        Debug.Log($"Trials: {totalTrials}, Targets: {Mathf.RoundToInt(totalTrials * targetTrialPercentage)}, " +
        $"minGap: {minTargetGap}, maxGap: {maxTargetGap}, n: {n}, noGhostStart: {noGhostStartTrials}");

        // commented out for debugging:
        // LoadTrialsFromPython();
        // trialSequence = generatedTrials.Select(t => (t.cubeIndex, t.isTarget)).ToList();

        // commenet out when NOT debugging:
        trialSequence = GenerateTrialsDummy();
        
        TrialSeqValidator.Validate(trialSequence, this);
        TrialSeqValidator.printTrialSeq(trialSequence);
    }
    public void LoadTrialsFromPython()
    {
        // build args with quoted script path (safer if paths contain spaces)
        string args = string.Join(" ", new string[]
        {
            $"\"{scriptPath}\"",
            $"--num_cubes {cubes.Count}",
            $"--total_trials {totalTrials}",
            $"--target_ratio {targetTrialPercentage}",
            $"--no_ghost_start_trials {noGhostStartTrials}",
            $"--min_target_gap {minTargetGap}",
            $"--max_target_gap {maxTargetGap}",
            $"--n {n}",
            $"--max_time_seconds {maxTimeSeconds}"
        });

        ProcessStartInfo psi = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Application.dataPath
        };

        try
        {
            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string errors = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // ALWAYS log raw output & errors for debugging
                Debug.Log($"[Python STDOUT raw]:\n{output}");
                if (!string.IsNullOrEmpty(errors))
                    Debug.LogWarning($"[Python STDERR]:\n{errors}");

                // quick guard: empty output -> fallback
                if (string.IsNullOrWhiteSpace(output))
                {
                    Debug.LogError("Python produced no output on stdout. Falling back to dummy generator.");
                    generatedTrials = GenerateTrialsDummy().Select(t => new Trial { cubeIndex = t.cubeIdx, isTarget = t.isTarget }).ToList();
                    return;
                }

                // remove BOM if present
                if (output.Length > 0 && output[0] == '\uFEFF')
                    output = output.Substring(1);

                // Try to extract the first JSON array found (from first '[' to last ']')
                int firstBracket = output.IndexOf('[');
                int lastBracket = output.LastIndexOf(']');
                string jsonArrayCandidate = null;
                if (firstBracket != -1 && lastBracket != -1 && lastBracket > firstBracket)
                {
                    jsonArrayCandidate = output.Substring(firstBracket, lastBracket - firstBracket + 1).Trim();
                    Debug.Log($"[Python JSON candidate extracted]:\n{jsonArrayCandidate}");
                }
                else
                {
                    Debug.LogWarning("Could not locate JSON array brackets in Python output. Attempting to use entire stdout as JSON.");
                    jsonArrayCandidate = output.Trim();
                }

                // Wrap as {"trials": <array>} because JsonUtility cannot parse a raw list
                string wrapped = "{\"trials\":" + jsonArrayCandidate + "}";

                try
                {
                    TrialList wrapper = JsonUtility.FromJson<TrialList>(wrapped);
                    if (wrapper == null || wrapper.trials == null)
                    {
                        Debug.LogError("JsonUtility.FromJson returned null wrapper or null trials. Falling back to dummy generator.");
                        generatedTrials = GenerateTrialsDummy().Select(t => new Trial { cubeIndex = t.cubeIdx, isTarget = t.isTarget }).ToList();
                    }
                    else
                    {
                        generatedTrials = wrapper.trials;
                        Debug.Log($"✅ Loaded {generatedTrials.Count} trials from Python");
                    }
                }
                catch (Exception jsonEx)
                {
                    Debug.LogError($"❌ JSON parse exception: {jsonEx.Message}\nFalling back to dummy generator.");
                    Debug.LogError($"[Wrapped JSON that failed to parse]:\n{wrapped}");
                    generatedTrials = GenerateTrialsDummy().Select(t => new Trial { cubeIndex = t.cubeIdx, isTarget = t.isTarget }).ToList();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ Failed to run Python: {e.Message}");
            // fallback to dummy generator
            generatedTrials = GenerateTrialsDummy().Select(t => new Trial { cubeIndex = t.cubeIdx, isTarget = t.isTarget }).ToList();
        }
    }


    // called in planeTrigger.cs as a coroutine
    public IEnumerator StartNextTrial()
    {
        tookBreak = false;
        if ((currentTrial != 0) && (currentTrial != totalTrials) && (currentTrial % breakInterval == 0))
        {
            // Runs break
            Debug.LogWarning("START BREAK");
            onBreak = true;
            tookBreak = true;

            yield return breakUI.ShowBreakUI(minimumBreakTime, currentTrial, totalTrials, score, totalPossibleScore);
        }
        if (currentTrial >= totalTrials)
        {
            // Experiment is complete
            Debug.LogError("Experiment complete");
            EventLogger_CSVWriter.Log("Experiment Complete");
            isExperimentComplete = true;

            breakUI.ShowExperimentComplete(currentTrial, totalTrials, score, totalPossibleScore);
            yield break;
        }

        var (nextCubeIdx, isTargetTrial) = trialSequence[currentTrial];
        currTargetCubeIdx = nextCubeIdx;

        var nextCubeIdxN = -1;
        var isTargetTrialN = false;

        // check within the next n trials, if a cube is ghost -> debug.log which index + in how many trials (trials first then index)
        if ((currentTrial + n) < totalTrials)
        {
            (nextCubeIdxN, isTargetTrialN) = trialSequence[currentTrial + n];
        }
        else
        {
            (nextCubeIdxN, isTargetTrialN) = (-1, false);
        }

        if (isTargetTrial)
        {
            ghostCubeIdx = currTargetCubeIdx;
            EventLogger_CSVWriter.Log($"Ghost Cube to index: {ghostCubeIdx}");
        }

        if (isTargetTrialN) Debug.LogError($"🔄🔄🔄 Switch ghost cube to INDEX {nextCubeIdxN} in {n} trials!");

        // When cube is highlighted, next trial begins
        HighlightCube(currTargetCubeIdx);
        EventLogger_CSVWriter.Log($"Trial {currentTrial + 1} Begins");
        TrialStartTime = Time.time;
        currentTrial++;
    }

        // Change ghost cube at set intervals
        // if (currentTrial % ghostUpdateInterval == 0 && currentTrial != 0)
        // {
        //     PickNewGhostCube();
        // }

        // if it's a target trial, determines highlights ghost cube
        // bool isTargetTrial = targetTrialIndices.Contains(currentTrial);
        // if (isTargetTrial)
        // {
        //     currentTargetCubeIdx = ghostCubeIdx;
        // }
        // // not a target trial, so determines a non-ghost cube to highlight
        // else
        // {
        //     // Choose a non-ghost cube
        //     List<int> options = Enumerable.Range(0, cubes.Count).Where(i => i != ghostCubeIdx).ToList();
        //     currentTargetCubeIdx = options[rng.Next(options.Count)];
        // }
    private void HighlightCube(int idx)
    {
        for (int i = 0; i < cubes.Count; i++)
        {
            Renderer rend = cubes[i].GetComponent<Renderer>();

            if (i == idx)
            {
                // This cube is the one to touch in this trial
                rend.material.color = highlightColor;
            }
            // this else if case can highlight the ghost cube a different color; is used for debugging
            else if (i == ghostCubeIdx)
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
        EventLogger_CSVWriter.Log($"Cube Highlighted: {idx}");
        Debug.LogWarning($"Trial {currentTrial + 1}/{totalTrials}: Highlighting cube {idx}. Target Trial? {idx == ghostCubeIdx}");
    }

    // Clears the highlight from each cube and "rests" them
    // Called in CubeTrigger.cs
    public void ClearCubeHighlight()
    {
        foreach (var cube in cubes)
        {
            cube.GetComponent<Renderer>().material.color = defaultColor;
        }
    }


// private List<(int cubeIdx, bool isTarget)> GenerateTrials()
// {
//     UnityEngine.Random.InitState(System.Environment.TickCount);

//     int numCubes = cubes.Count;
//     int T = totalTrials;
//     int K = Mathf.RoundToInt(T * targetTrialPercentage); // # targets
//     int NT = T - K;

//     if (numCubes <= 0 || T <= 0)
//     {
//         Debug.LogError("GenerateTrials: invalid configuration");
//         return new List<(int, bool)>();
//     }

//     // quotas per cube (balanced ±1)
//     int[] targetQuota = Enumerable.Repeat(K / numCubes, numCubes).ToArray();
//     int[] nonTargetQuota = Enumerable.Repeat(NT / numCubes, numCubes).ToArray();
//     for (int i = 0; i < K % numCubes; i++) targetQuota[i]++;
//     for (int i = 0; i < NT % numCubes; i++) nonTargetQuota[i]++;

//     var seq = new (int cubeIdx, bool isTarget)[T];

//     // state trackers
//     int[] lastHighlight = Enumerable.Repeat(-9999, numCubes).ToArray();
//     int[] blockedUntil = Enumerable.Repeat(-9999, numCubes).ToArray();

//     System.Random rnd = new System.Random();

//     bool Backtrack(
//         int t,
//         int targetsLeft,
//         int nonTargetsLeft,
//         int lastTargetTrial,
//         int consecutiveTargets,
//         int prevGhostCube,
//         int? lastTargetCube,
//         int sameCubeTargetRun)
//     {
//         if (t == T)
//         {
//             return targetsLeft == 0 && nonTargetsLeft == 0;
//         }

//         // no targets allowed in first noGhostStartTrials
//         bool canBeTargetHere = (t >= noGhostStartTrials) && (targetsLeft > 0);

//         // enforce spacing between targets
//         if (lastTargetTrial >= 0)
//         {
//             int gap = t - lastTargetTrial;
//             if (gap < minTargetGap) canBeTargetHere = false;
//             if (gap > maxTargetGap && targetsLeft > 0) return false; // must place target earlier
//         }

//         // must place target if not enough slots left
//         if (targetsLeft > (T - t)) canBeTargetHere = false;

//         // order of trying: randomized
//         var choices = new List<bool>();
//         if (canBeTargetHere) choices.Add(true);
//         if (nonTargetsLeft > 0) choices.Add(false);
//         choices = choices.OrderBy(_ => rnd.Next()).ToList();

//         foreach (var isTarget in choices)
//         {
//             // reject >2 consecutive targets (any cube)
//             if (isTarget && consecutiveTargets >= 2) continue;

//             // candidate cubes
//             var candidates = new List<int>();
//             for (int c = 0; c < numCubes; c++)
//             {
//                 // always enforce block window
//                 if (t < blockedUntil[c]) continue;

//                 if (isTarget)
//                 {
//                     if (targetQuota[c] <= 0) continue;

//                     // must not have been highlighted in last n trials
//                     if (lastHighlight[c] >= t - n) continue;

//                     // --- Check previous ghost cube rule ---
//                     if (prevGhostCube != -1)
//                     {
//                         for (int j = Mathf.Max(0, t - n); j < t; j++)
//                         {
//                             if (seq[j].cubeIdx == prevGhostCube)
//                                 goto skipCandidate; // fails rule
//                         }
//                     }
//                 }
//                 else
//                 {
//                     if (nonTargetQuota[c] <= 0) continue;
//                 }

//                 candidates.Add(c);
//             skipCandidate:;
//             }

//             candidates = candidates.OrderBy(_ => rnd.Next()).ToList();
//             foreach (int c in candidates)
//             {
//                 // compute new run count for same cube target rule
//                 int newSameCubeRun = sameCubeTargetRun;
//                 int? newLastTargetCube = lastTargetCube;
//                 if (isTarget)
//                 {
//                     if (lastTargetCube.HasValue && lastTargetCube.Value == c)
//                         newSameCubeRun++;
//                     else
//                     {
//                         newSameCubeRun = 1;
//                         newLastTargetCube = c;
//                     }

//                     // reject if >2 same-cube targets consecutively
//                     if (newSameCubeRun > 2)
//                         continue;
//                 }
//                 else
//                 {
//                     // optionally reset run on non-targets
//                     // newSameCubeRun = 0; newLastTargetCube = null;
//                 }

//                 // commit
//                 seq[t] = (c, isTarget);
//                 int oldLast = lastHighlight[c];
//                 int oldBlocked = blockedUntil[c];
//                 int oldPrevGhost = prevGhostCube;

//                 if (isTarget)
//                 {
//                     targetQuota[c]--;
//                     lastHighlight[c] = t;
//                     blockedUntil[c] = t + n + 1; // block this cube for next n trials
//                     prevGhostCube = c; // new ghost cube
//                 }
//                 else
//                 {
//                     nonTargetQuota[c]--;
//                     lastHighlight[c] = t;
//                 }

//                 if (Backtrack(
//                     t + 1,
//                     targetsLeft - (isTarget ? 1 : 0),
//                     nonTargetsLeft - (isTarget ? 0 : 1),
//                     isTarget ? t : lastTargetTrial,
//                     isTarget ? consecutiveTargets + 1 : 0,
//                     prevGhostCube,
//                     newLastTargetCube,
//                     newSameCubeRun))
//                 {
//                     return true;
//                 }

//                 // backtrack
//                 if (isTarget) targetQuota[c]++; else nonTargetQuota[c]++;
//                 lastHighlight[c] = oldLast;
//                 blockedUntil[c] = oldBlocked;
//                 prevGhostCube = oldPrevGhost;
//             }
//         }

//         return false;
//     }

//     bool ok = Backtrack(0, K, NT, -1, 0, -1, null, 0);
//     if (!ok)
//     {
//         Debug.LogError("GenerateTrials: backtracking failed (parameters may be infeasible).");
//     }

//     return seq.ToList();
// }

    private List<(int cubeIdx, bool isTarget)> GenerateTrialsDummy()
    {
        Debug.LogWarning("⚠️ Using dummy trial generator!");

        // NOTE: hardcoding 5 cubes here
        int iterations = totalTrials / 5;

        // Just 10 trials: alternating between cube 0 and cube 1
        var dummy = new List<(int, bool)>();
        for (int i = 0; i < 20; i++)
        {
            int cube = i % cubes.Count;   // cycle through cubes
            bool isTarget = i % 2 == 0; // every other trial is a target
            dummy.Add((cube, isTarget));
        }

        Debug.Log($"Generated {dummy.Count} dummy trials.");
        return dummy;
    }


}
