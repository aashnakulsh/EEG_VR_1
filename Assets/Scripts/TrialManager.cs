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


    private void Start()
    {
        TrialLogger_CSVWriter.Init();
        EventLogger_CSVWriter.Init();

        Debug.Log($"Trials: {totalTrials}, Targets: {Mathf.RoundToInt(totalTrials * targetTrialPercentage)}, " +
        $"minGap: {minTargetGap}, maxGap: {maxTargetGap}, n: {n}, noGhostStart: {noGhostStartTrials}");

        trialSequence = GenerateTrials();
        // trialSequence = GenerateTrialsDummy();
        TrialSeqValidator.Validate(trialSequence, this);
        TrialSeqValidator.printTrialSeq(trialSequence);
        // Debug.LogError(trialSequence);

        // PickNewGhostCube();
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
        if ((currentTrial + n) <= totalTrials)
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
    //     int totalTargets = Mathf.RoundToInt(totalTrials * targetTrialPercentage);
    //     int totalNonTargets = totalTrials - totalTargets;

    //     // --- Stage 1: choose target positions ---
    //     bool[] isTargetSeq = new bool[totalTrials];
    //     List<int> targetPositions = new List<int>();

    //     bool PlaceTargets(int trial, int targetsLeft)
    //     {
    //         if (trial == totalTrials) return targetsLeft == 0;
    //         if (targetsLeft < 0) return false;
    //         int slotsLeft = totalTrials - trial;
    //         if (targetsLeft > slotsLeft) return false;

    //         // no targets in first noGhostStartTrials
    //         if (trial < noGhostStartTrials)
    //         {
    //             isTargetSeq[trial] = false;
    //             return PlaceTargets(trial + 1, targetsLeft);
    //         }

    //         // must be target if we are out of slots
    //         if (targetsLeft == slotsLeft)
    //         {
    //             // enforce <= 2 consecutive and gap range
    //             if (targetPositions.Count > 0)
    //             {
    //                 int gap = trial - targetPositions.Last();
    //                 if (gap < minTargetGap || gap > maxTargetGap) return false;
    //                 if (targetPositions.Count >= 2 &&
    //                     trial - targetPositions[^2] == 1 &&
    //                     trial - targetPositions[^1] == 1) return false;
    //             }
    //             isTargetSeq[trial] = true;
    //             targetPositions.Add(trial);
    //             if (PlaceTargets(trial + 1, targetsLeft - 1)) return true;
    //             targetPositions.RemoveAt(targetPositions.Count - 1);
    //             return false;
    //         }

    //         // branch: try non-target first sometimes
    //         List<bool> order = UnityEngine.Random.value < 0.5f ? 
    //             new List<bool>{false,true} : new List<bool>{true,false};

    //         foreach (bool asTarget in order)
    //         {
    //             if (asTarget)
    //             {
    //                 if (targetsLeft == 0) continue;
    //                 if (targetPositions.Count > 0)
    //                 {
    //                     int gap = trial - targetPositions.Last();
    //                     if (gap < minTargetGap || gap > maxTargetGap) continue;
    //                 }
    //                 // ≤2 consecutive
    //                 if (targetPositions.Count >= 2 &&
    //                     trial - targetPositions[^2] == 1 &&
    //                     trial - targetPositions[^1] == 1) continue;

    //                 isTargetSeq[trial] = true;
    //                 targetPositions.Add(trial);
    //                 if (PlaceTargets(trial + 1, targetsLeft - 1)) return true;
    //                 targetPositions.RemoveAt(targetPositions.Count - 1);
    //             }
    //             else
    //             {
    //                 isTargetSeq[trial] = false;
    //                 if (PlaceTargets(trial + 1, targetsLeft)) return true;
    //             }
    //         }
    //         return false;
    //     }

    //     if (!PlaceTargets(0, totalTargets))
    //         Debug.LogError("Failed to place targets with spacing/backtracking!");

    //     // --- Stage 2: assign cubes to slots ---
    //     int baseTargetsPerCube = totalTargets / numCubes;
    //     int baseNonTargetsPerCube = totalNonTargets / numCubes;
    //     int[] remTargets = Enumerable.Repeat(baseTargetsPerCube, numCubes).ToArray();
    //     int[] remNonTargets = Enumerable.Repeat(baseNonTargetsPerCube, numCubes).ToArray();

    //     // distribute remainders
    //     for (int i = 0; i < totalTargets % numCubes; i++) remTargets[i]++;
    //     for (int i = 0; i < totalNonTargets % numCubes; i++) remNonTargets[i]++;

    //     int[] assignment = new int[totalTrials];
    //     int[] lastTargetIdx = Enumerable.Repeat(-9999, numCubes).ToArray();
    //     int[] lastAnyHighlight = Enumerable.Repeat(-9999, numCubes).ToArray();

    //     int[] blockedUntil = Enumerable.Repeat(-9999, numCubes).ToArray();

    //     bool AssignCube(int trial)
    //     {
    //         if (trial == totalTrials) return true;
    //         bool isT = isTargetSeq[trial];

    //         List<int> candidates = new List<int>();
    //         for (int c = 0; c < numCubes; c++)
    //         {
    //             if (isT && remTargets[c] <= 0) continue;
    //             if (!isT && remNonTargets[c] <= 0) continue;

    //             // Rule: cube can't be used before it's unblocked
    //             if (trial < blockedUntil[c]) continue;

    //             // Rule: for targets, also forbid if highlighted recently (lookback)
    //             if (isT && trial - lastAnyHighlight[c] < n) continue;

    //             candidates.Add(c);
    //         }
    //         if (candidates.Count == 0) return false;
    //         Shuffle(candidates);

    //         foreach (int cand in candidates)
    //         {
    //             assignment[trial] = cand;
    //             int prevT = lastTargetIdx[cand];
    //             int prevH = lastAnyHighlight[cand];
    //             int prevBlocked = blockedUntil[cand];

    //             if (isT) remTargets[cand]--; else remNonTargets[cand]--;
    //             if (isT)
    //             {
    //                 lastTargetIdx[cand] = trial;
    //                 // Block this cube for the next n trials
    //                 blockedUntil[cand] = trial + n + 1;
    //             }
    //             lastAnyHighlight[cand] = trial;

    //             if (AssignCube(trial + 1)) return true;

    //             // backtrack
    //             if (isT) remTargets[cand]++; else remNonTargets[cand]++;
    //             lastTargetIdx[cand] = prevT;
    //             lastAnyHighlight[cand] = prevH;
    //             blockedUntil[cand] = prevBlocked;
    //         }
    //         return false;
    //     }


    //     if (!AssignCube(0))
    //         Debug.LogError("Failed cube assignment with backtracking!");

    //     // --- Build result ---
    //     var result = new List<(int,bool)>(totalTrials);
    //     for (int i = 0; i < totalTrials; i++)
    //         result.Add((assignment[i], isTargetSeq[i]));
    //     return result;
    // }


    //     private List<(int cubeIdx, bool isTarget)> GenerateTrials()
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
    //     int lastGhostCube = -1;

    //     System.Random rnd = new System.Random();

    //     bool Backtrack(int t, int targetsLeft, int nonTargetsLeft, int lastTargetTrial, int consecutiveTargets, int prevGhostCube)
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
    //             // reject >2 consecutive targets
    //             if (isTarget && consecutiveTargets >= 2) continue;

    //             // candidate cubes
    //             var candidates = new List<int>();
    //             for (int c = 0; c < numCubes; c++)
    //             {
    //                 // Rule: must satisfy cooldown block
    //                 if (t < blockedUntil[c]) continue;

    //                 if (isTarget)
    //                 {
    //                     if (targetQuota[c] <= 0) continue;

    //                     // must not have been highlighted in last n trials
    //                     if (lastHighlight[c] >= t - n) continue;

    //                     // --- Check previous ghost cube rule (validator Rule1) ---
    //                     if (prevGhostCube != -1)
    //                     {
    //                         // ensure prevGhostCube not highlighted in last n trials before this target
    //                         for (int j = Mathf.Max(0, t - n); j < t; j++)
    //                         {
    //                             if (seq[j].cubeIdx == prevGhostCube)
    //                             {
    //                                 goto skipCandidate; // fails rule
    //                             }
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
    //                     prevGhostCube))
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

    //     bool ok = Backtrack(0, K, NT, -1, 0, lastGhostCube);
    //     if (!ok)
    //     {
    //         Debug.LogError("GenerateTrials: backtracking failed (parameters may be infeasible).");
    //     }

    //     return seq.ToList();
    // }



private List<(int cubeIdx, bool isTarget)> GenerateTrials()
{
    UnityEngine.Random.InitState(System.Environment.TickCount);

    int numCubes = cubes.Count;
    int T = totalTrials;
    int K = Mathf.RoundToInt(T * targetTrialPercentage); // # targets
    int NT = T - K;

    if (numCubes <= 0 || T <= 0)
    {
        Debug.LogError("GenerateTrials: invalid configuration");
        return new List<(int, bool)>();
    }

    // quotas per cube (balanced ±1)
    int[] targetQuota = Enumerable.Repeat(K / numCubes, numCubes).ToArray();
    int[] nonTargetQuota = Enumerable.Repeat(NT / numCubes, numCubes).ToArray();
    for (int i = 0; i < K % numCubes; i++) targetQuota[i]++;
    for (int i = 0; i < NT % numCubes; i++) nonTargetQuota[i]++;

    var seq = new (int cubeIdx, bool isTarget)[T];

    // state trackers
    int[] lastHighlight = Enumerable.Repeat(-9999, numCubes).ToArray();
    int[] blockedUntil = Enumerable.Repeat(-9999, numCubes).ToArray();

    System.Random rnd = new System.Random();

    bool Backtrack(
        int t,
        int targetsLeft,
        int nonTargetsLeft,
        int lastTargetTrial,
        int consecutiveTargets,
        int prevGhostCube,
        int? lastTargetCube,
        int sameCubeTargetRun)
    {
        if (t == T)
        {
            return targetsLeft == 0 && nonTargetsLeft == 0;
        }

        // no targets allowed in first noGhostStartTrials
        bool canBeTargetHere = (t >= noGhostStartTrials) && (targetsLeft > 0);

        // enforce spacing between targets
        if (lastTargetTrial >= 0)
        {
            int gap = t - lastTargetTrial;
            if (gap < minTargetGap) canBeTargetHere = false;
            if (gap > maxTargetGap && targetsLeft > 0) return false; // must place target earlier
        }

        // must place target if not enough slots left
        if (targetsLeft > (T - t)) canBeTargetHere = false;

        // order of trying: randomized
        var choices = new List<bool>();
        if (canBeTargetHere) choices.Add(true);
        if (nonTargetsLeft > 0) choices.Add(false);
        choices = choices.OrderBy(_ => rnd.Next()).ToList();

        foreach (var isTarget in choices)
        {
            // reject >2 consecutive targets (any cube)
            if (isTarget && consecutiveTargets >= 2) continue;

            // candidate cubes
            var candidates = new List<int>();
            for (int c = 0; c < numCubes; c++)
            {
                // always enforce block window
                if (t < blockedUntil[c]) continue;

                if (isTarget)
                {
                    if (targetQuota[c] <= 0) continue;

                    // must not have been highlighted in last n trials
                    if (lastHighlight[c] >= t - n) continue;

                    // --- Check previous ghost cube rule ---
                    if (prevGhostCube != -1)
                    {
                        for (int j = Mathf.Max(0, t - n); j < t; j++)
                        {
                            if (seq[j].cubeIdx == prevGhostCube)
                                goto skipCandidate; // fails rule
                        }
                    }
                }
                else
                {
                    if (nonTargetQuota[c] <= 0) continue;
                }

                candidates.Add(c);
            skipCandidate:;
            }

            candidates = candidates.OrderBy(_ => rnd.Next()).ToList();
            foreach (int c in candidates)
            {
                // compute new run count for same cube target rule
                int newSameCubeRun = sameCubeTargetRun;
                int? newLastTargetCube = lastTargetCube;
                if (isTarget)
                {
                    if (lastTargetCube.HasValue && lastTargetCube.Value == c)
                        newSameCubeRun++;
                    else
                    {
                        newSameCubeRun = 1;
                        newLastTargetCube = c;
                    }

                    // reject if >2 same-cube targets consecutively
                    if (newSameCubeRun > 2)
                        continue;
                }
                else
                {
                    // optionally reset run on non-targets
                    // newSameCubeRun = 0; newLastTargetCube = null;
                }

                // commit
                seq[t] = (c, isTarget);
                int oldLast = lastHighlight[c];
                int oldBlocked = blockedUntil[c];
                int oldPrevGhost = prevGhostCube;

                if (isTarget)
                {
                    targetQuota[c]--;
                    lastHighlight[c] = t;
                    blockedUntil[c] = t + n + 1; // block this cube for next n trials
                    prevGhostCube = c; // new ghost cube
                }
                else
                {
                    nonTargetQuota[c]--;
                    lastHighlight[c] = t;
                }

                if (Backtrack(
                    t + 1,
                    targetsLeft - (isTarget ? 1 : 0),
                    nonTargetsLeft - (isTarget ? 0 : 1),
                    isTarget ? t : lastTargetTrial,
                    isTarget ? consecutiveTargets + 1 : 0,
                    prevGhostCube,
                    newLastTargetCube,
                    newSameCubeRun))
                {
                    return true;
                }

                // backtrack
                if (isTarget) targetQuota[c]++; else nonTargetQuota[c]++;
                lastHighlight[c] = oldLast;
                blockedUntil[c] = oldBlocked;
                prevGhostCube = oldPrevGhost;
            }
        }

        return false;
    }

    bool ok = Backtrack(0, K, NT, -1, 0, -1, null, 0);
    if (!ok)
    {
        Debug.LogError("GenerateTrials: backtracking failed (parameters may be infeasible).");
    }

    return seq.ToList();
}

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
    // private void Shuffle<T>(List<T> list)
    // {
    //     for (int i = list.Count - 1; i > 0; i--)
    //     {
    //         int j = UnityEngine.Random.Range(0, i + 1);
    //         (list[i], list[j]) = (list[j], list[i]);
    //     }
    // }

    // /// <summary>
    // /// Decides whether this trial should be a target based on remaining quota and spacing
    // /// </summary>
    // private bool ShouldBeTarget(int trial, int noGhostStartTrials, int totalTargets, int lastTargetPos, int p, int q, int numTrials, List<(int, bool)> result)
    // {
    //     if (trial < noGhostStartTrials) return false;

    //     // int usedTargets = result.Count(r => r.isTarget);

    //     int usedTargets = 0;

    //     foreach (var r in result)
    //     {
    //         var (targetCubeIdx, isTarget) = r;
    //         // r.Item2 is the boolean indicating whether it is a target
    //         if (isTarget)
    //         {
    //             usedTargets++;
    //         }
    //     }

    //     int targetsRemaining = totalTargets - usedTargets;
    //     int trialsRemaining = numTrials - trial;

    //     // Force target if we're running out of trials
    //     if (targetsRemaining >= trialsRemaining) return true;

    //     int gap = trial - lastTargetPos;
    //     if (gap < p) return false;
    //     if (gap >= q) return true;

    //     // Otherwise probabilistic based on remaining quota
    //     return UnityEngine.Random.value < (float)targetsRemaining / trialsRemaining;
    // }

    // // /// <summary>
    // // /// Fisher-Yates shuffle to randomize a list.
    // // /// </summary>
    // private void shuffle<T>(List<T> list)
    // {
    //     for (int i = list.Count - 1; i > 0; i--)
    //     {
    //         int j = UnityEngine.Random.Range(0, i + 1);
    //         (list[i], list[j]) = (list[j], list[i]);
    //     }
    // }


}
