using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

/// <summary>
/// Call TrialSequenceValidator.Validate(trials, tm) after you generate trials
/// to check constraints.
/// </summary>
public static class TrialSeqValidator
{
    public static void printTrialSeq(List<(int cubeIdx, bool isTarget)> trialSeq)
    {
        string output = "TrialSequence: [ ";
        for (int i = 0; i < trialSeq.Count; i++)
        {
            var trial = trialSeq[i];
            output += $"({trial.cubeIdx}, {trial.isTarget})";
            if (i < trialSeq.Count - 1)
                output += ", ";
        }
        output += " ]";

        Debug.LogError(output);
    }
    // public static void Validate(List<(int cubeIdx, bool isTarget)> trials, TrialManager tm)
    // {
    //     int numCubes = tm.cubes.Count;
    //     if (trials == null || tm == null)
    //     {
    //         Debug.LogError("Validator: trials or TrialManager is null");
    //         return;
    //     }

    //     // 1. Total count
    //     if (trials.Count != tm.totalTrials)
    //         Debug.LogError($"Validator: Expected {tm.totalTrials} trials but got {trials.Count}");

    //     // 2. Target count matches expectation
    //     int expectedTargets = Mathf.RoundToInt(tm.totalTrials * tm.targetTrialPercentage);

    //     int actualTargets = 0;
    //     for (int i = 0; i < trials.Count; i++)
    //     {
    //         if (trials[i].isTarget)
    //         {
    //             actualTargets++;
    //         }
    //     }
    //     // Debug.LogError("expected targetssss: " + expectedTargets);
    //     // Debug.LogError("acutal targetsssss" + actualTargets);

    //     if (actualTargets != expectedTargets)
    //         Debug.LogError($"Validator: Expected {expectedTargets} targets but got {actualTargets}");

    //     // 3. No cube violates n-cooldown after a target
    //     int[] lastTargetIdx = new int[tm.cubes.Count];
    //     for (int i = 0; i < lastTargetIdx.Length; i++) lastTargetIdx[i] = -9999;

    //     for (int i = 0; i < trials.Count; i++)
    //     {
    //         var (cubeIdx, isTarget) = trials[i];
    //         if (isTarget)
    //         {
    //             if (i - lastTargetIdx[cubeIdx] < tm.n)
    //                 Debug.LogError($"Validator: Cube {cubeIdx} violates cooldown at trial {i} (gap={i - lastTargetIdx[cubeIdx]})");
    //             lastTargetIdx[cubeIdx] = i;
    //         }
    //     }

    //     // 4. No more than 2 consecutive targets
    //     int run = 0;
    //     for (int i = 0; i < trials.Count; i++)
    //     {
    //         if (trials[i].isTarget) run++; else run = 0;
    //         if (run > 2)
    //             Debug.LogError($"Validator: Too many consecutive targets at trial {i} (run={run})");
    //     }

    //     // 5. Roughly balanced across cubes
    //     int[] targetCounts = new int[numCubes];
    //     int[] nonTargetCounts = new int[numCubes];
    //     int targetCountsTotal = 0;
    //     int nonTargetCountsTotal = 0;


    //     for (int i = 0; i < trials.Count; i++)
    //     {
    //         var trial = trials[i];
    //         Debug.Log($"Trial {i + 1}: Cube {trial.cubeIdx} | Target? {trial.isTarget}");

    //         if (trial.isTarget)
    //         {
    //             targetCounts[trial.cubeIdx]++;
    //             targetCountsTotal += 1;
    //         }
    //         else
    //         {
    //             nonTargetCounts[trial.cubeIdx]++;
    //             nonTargetCountsTotal += 1;
    //         }
    //     }
    //     for (int i = 1; i < numCubes; i++)
    //     {
    //         if ((Math.Abs(targetCounts[i] - targetCounts[i - 1]) >= 2) ||
    //             (Math.Abs(nonTargetCounts[i] - nonTargetCounts[i - 1]) >= 2))
    //         {
    //             Debug.LogError("Target/Nontarget distribution on cubes is wrong: ");
    //             Debug.LogError($"Targets: [{string.Join(" ", targetCounts)}]");
    //             Debug.LogError($"Targets total: " + targetCountsTotal);
    //             Debug.LogError($"NonTargets: [{string.Join(" ", nonTargetCounts)}]");
    //             Debug.LogError($"NonTargets total: " + nonTargetCountsTotal);
    //         }
    //     }



    //     // 6. Optional: Check that first noGhostStartTrials are non-targets
    //     for (int i = 0; i < Mathf.Min(tm.noGhostStartTrials, trials.Count); i++)
    //     {
    //         if (trials[i].isTarget)
    //             Debug.LogError($"Validator: Trial {i} is a target but noGhostStartTrials={tm.noGhostStartTrials}");
    //     }

    //     Debug.LogError("Validator: Validation complete");
    // }
    public static void Validate(List<(int cubeIdx, bool isTarget)> trials, TrialManager tm)
    {
        int numCubes = tm.cubes.Count;
        // int n = tm.n + 1;
        if (trials == null || tm == null)
        {
            Debug.LogError("Validator: trials or TrialManager is null");
            return;
        }

        // 1. Total count
        if (trials.Count != tm.totalTrials)
            Debug.LogError($"Validator: Expected {tm.totalTrials} trials but got {trials.Count}");

        // 2. Target count matches expectation
        int expectedTargets = Mathf.RoundToInt(tm.totalTrials * tm.targetTrialPercentage);
        int actualTargets = 0;
        for (int i = 0; i < trials.Count; i++)
        {
            if (trials[i].isTarget) actualTargets++;
        }
        // int actualTargets = trials.Count(t => t.isTarget);
        if (actualTargets != expectedTargets)
            Debug.LogError($"Validator: Expected {expectedTargets} targets but got {actualTargets}");

        // // 3. Rule 1a (cooldown: if cube is target, must wait n trials)
        // int[] lastTargetIdx = Enumerable.Repeat(-9999, numCubes).ToArray();
        // for (int i = 0; i < trials.Count; i++)
        // {
        //     var (cubeIdx, isTarget) = trials[i];
        //     if (isTarget)
        //     {
        //         if (i - lastTargetIdx[cubeIdx] < tm.n)
        //             Debug.LogError($"Validator: Cube {cubeIdx} violates cooldown at trial {i} (gap={i - lastTargetIdx[cubeIdx]})");
        //         lastTargetIdx[cubeIdx] = i;
        //     }
        // }

        // // 4. Rule 1b (lookback: if cube is target, it was not highlighted in last n trials)
        // for (int i = 0; i < trials.Count; i++)
        // {
        //     if (trials[i].isTarget)
        //     {
        //         int cIdx = trials[i].cubeIdx;
        //         for (int j = Math.Max(0, i - tm.n+1); j < i; j++)
        //         {
        //             if (trials[j].cubeIdx == cIdx)
        //                 Debug.LogError($"Validator: Cube {cIdx} is a target at {i} but was highlighted at {j} within last {tm.n} trials");
        //         }
        //     }
        // }


        //separate


        // Rule 1 (cooldown/lookback/lookahead): 
        // If a cube is a target at trial i, then that cube must not appear 
        // in the n trials before OR after i.
        // for (int i = 0; i < trials.Count; i++)
        // {
        //     if (trials[i].isTarget)
        //     {
        //         int cIdx = trials[i].cubeIdx;

        //         // Look back n trials
        //         for (int j = Math.Max(0, i - tm.n); j < i; j++)
        //         {
        //             if (trials[j].cubeIdx == cIdx)
        //                 Debug.LogError(
        //                     $"Validator: Cube {cIdx} is a target at {i} but was highlighted at {j} (within previous {tm.n} trials)"
        //                 );
        //         }

        //         // Look ahead n trials
        //         for (int j = i + 1; j <= Math.Min(trials.Count - 1, i + tm.n); j++)
        //         {
        //             if (trials[j].cubeIdx == cIdx)
        //                 Debug.LogError(
        //                     $"Validator: Cube {cIdx} is a target at {i} but was highlighted again at {j} (within next {tm.n} trials)"
        //                 );
        //         }
        //     }
        // }

        // Rule 1: Ghost cube safety (lookback for both current and previous ghost cubes)
        int lastGhostCube = -1;
        for (int i = 0; i < trials.Count; i++)
        {
            if (trials[i].isTarget)
            {
                int currentGhost = trials[i].cubeIdx;

                // --- Check current ghost cube ---
                for (int j = Math.Max(0, i - tm.n); j < i; j++)
                {
                    if (trials[j].cubeIdx == currentGhost)
                        Debug.LogError(
                            $"Validator: Current ghost cube {currentGhost} is a target at {i} but was highlighted at {j} (within last {tm.n} trials)"
                        );
                }

                // --- Check previous ghost cube (if any) ---
                if (lastGhostCube != -1)
                {
                    for (int j = Math.Max(0, i - tm.n); j < i; j++)
                    {
                        if (trials[j].cubeIdx == lastGhostCube)
                            Debug.LogError(
                                $"Validator: Previous ghost cube {lastGhostCube} (from earlier target) is invalid at {i}, highlighted at {j} within last {tm.n} trials"
                            );
                    }
                }

                // update previous ghost cube
                lastGhostCube = currentGhost;
            }
        }


        // 5. Rule 4 (no consecutive targets)
        // int run = 0;
        // for (int i = 0; i < trials.Count; i++)
        // {
        //     if (trials[i].isTarget) run++; else run = 0;
        //     if (run > 2)
        //         Debug.LogError($"Validator: Too many consecutive targets at trial {i} (run={run})");
        // }

        // Rule X: no same-cube target more than 2 times consecutively (even with other trials in between)
        int? lastTargetCube = null;
        int sameCubeTargetRun = 0;

        for (int i = 0; i < trials.Count; i++)
        {
            if (trials[i].isTarget)
            {
                int cIdx = trials[i].cubeIdx;
                if (lastTargetCube.HasValue && lastTargetCube.Value == cIdx)
                {
                    sameCubeTargetRun++;
                }
                else
                {
                    sameCubeTargetRun = 1;
                    lastTargetCube = cIdx;
                }

                if (sameCubeTargetRun > 2)
                {
                    Debug.LogError($"Validator: Cube {cIdx} used as a target {sameCubeTargetRun} times in a row (violates rule) at trial {i}");
                }
            }
            else
            {
                // Non-target does not reset the "same cube target run" unless another cube becomes target later
                // You can decide whether to reset on any non-target:
                // sameCubeTargetRun = 0; lastTargetCube = null;
            }
        }


        // 6. Count targets/non-targets per cube
        int[] targetCounts = new int[numCubes];
        int[] nonTargetCounts = new int[numCubes];
        for (int i = 0; i < trials.Count; i++)
        {
            var trial = trials[i];
            if (trial.isTarget) targetCounts[trial.cubeIdx]++;
            else nonTargetCounts[trial.cubeIdx]++;
        }

        // Rule 2 + 3 (must be balanced across cubes)
        int expectedTargetsPerCube = expectedTargets / numCubes;
        int expectedNonTargetsPerCube = (tm.totalTrials - expectedTargets) / numCubes;

        for (int c = 0; c < numCubes; c++)
        {
            if (Math.Abs(targetCounts[c] - expectedTargetsPerCube) > 1)
                Debug.LogError($"Validator (Rule 3): Cube {c} has {targetCounts[c]} targets, expected ~{expectedTargetsPerCube} (±1)");

            if (Math.Abs(nonTargetCounts[c] - expectedNonTargetsPerCube) > 1)
                Debug.LogError($"Validator (Rule 2): Cube {c} has {nonTargetCounts[c]} non-targets, expected ~{expectedNonTargetsPerCube} (±1)");
        }


        // 7. Rule 5 (no targets in first m trials)
        for (int i = 0; i < Math.Min(tm.noGhostStartTrials, trials.Count); i++)
        {
            if (trials[i].isTarget)
                Debug.LogError($"Validator: Target found at trial {i} but first {tm.noGhostStartTrials} must be non-targets");
        }

        // 8. Rule 6 (target spacing between p and q)
        int prevTarget = -9999;
        for (int i = 0; i < trials.Count; i++)
        {
            if (trials[i].isTarget)
            {
                if (prevTarget != -9999)
                {
                    int dist = i - prevTarget;
                    if (dist < tm.minTargetGap || dist > tm.maxTargetGap)
                        Debug.LogError($"Validator: Target spacing {dist} between trials {prevTarget} and {i} not in range [{tm.minTargetGap},{tm.maxTargetGap}]");
                }
                prevTarget = i;
            }
        }

        // 9. noGhostStartTrials check
        for (int i = 0; i < Mathf.Min(tm.noGhostStartTrials, trials.Count); i++)
        {
            if (trials[i].isTarget)
                Debug.LogError($"Validator: Trial {i} is a target but noGhostStartTrials={tm.noGhostStartTrials}");
        }

        Debug.LogError("Validator: Validation complete");
    }


}
