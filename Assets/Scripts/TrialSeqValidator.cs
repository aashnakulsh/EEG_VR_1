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

 

        // Rule X: no same-cube target more than 2 times consecutively  
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
            // else
            // {
            //     // Non-target does not reset the "same cube target run" unless another cube becomes target later
            //     // You can decide whether to reset on any non-target:
            //     // sameCubeTargetRun = 0; lastTargetCube = null;
            // }
        }

        // --- Rule Y: no more than 2 consecutive non-targets at the same cube position ---
        for (int c = 0; c < numCubes; c++)
        {
            int run = 0; // how many consecutive non-targets at this cube
            for (int i = 0; i < trials.Count; i++)
            {
                if (!trials[i].isTarget && trials[i].cubeIdx == c)
                {
                    run++;
                    if (run > 2)
                    {
                        Debug.LogError($"Validator: Cube {c} used as a non-target {run} times in a row (violates rule) at trial {i}");
                    }
                }
                else
                {
                    // reset counter when we move to another cube or hit a target trial
                    run = 0;
                }
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
