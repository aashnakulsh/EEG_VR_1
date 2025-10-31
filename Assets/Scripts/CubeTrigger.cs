using UnityEngine;
using System.Collections;

public class CubeTrigger : MonoBehaviour
{
    // private Renderer objectRenderer;
    // public Color newColor = Color.yellow;            // used for debugging

    public PlaneTrigger planeTrigger;
    public TrialManager trialManager;
    public BreakUIController buic;
    public int cubeIdx;

    // used to determine whether or not to trigger cube-related scripts (cube can only be hit after "resetting", 
    // so cubeFlag = 0 if participant has successful reset or -1 if participant has not reset)
    // currently, resetting is done by the participant triggering the plane
    // also prevents multilpe triggers of cube-related scripts (due to there being multiple colliderers on hand) 
    // private int cubeFlag;            (MOVED TO PLANETRIGGER TO AVOID HAVING MULTIPLE INSTANCES OF THE FLAG)
    public float timeHitCube;

 
    private AudioSource aud;

    void Start()
    {
        // objectRenderer = GetComponent<Renderer>();
        planeTrigger.cubeFlag = -1;
        aud = GetComponent<AudioSource>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (planeTrigger.cubeFlag == 0 & !trialManager.onBreak & !trialManager.isExperimentComplete)
        {
            planeTrigger.cubeFlag = -1;
            // Debugging code:
            // Debug.Log("OBJECT ENTERED");
            // sets cube to newColor when hand is in it -- used for debeugging
            // objectRenderer.material.color = newColor;

            aud.Play(); // plays ping audio

            // getting statistics for logging
            timeHitCube = Time.time;
            // eeg?.MarkTrialEnd();
            EventLogger_CSVWriter.Log($"Cube Touched: {cubeIdx}");  

            planeTrigger.ResetPlaneFlag();               // Allow plane to be touched again by reseting plane trigger counter
            trialManager.ClearCubeHighlight();                    // Clears highlight

            // getting statistics for logging
            float reactionTime = timeHitCube - planeTrigger.timeHitPlane;
            if (trialManager.tookBreak) {reactionTime = reactionTime-buic.totalBreakDuration;}
            int touchedIdx = trialManager.cubes.IndexOf(this.gameObject);

            if (trialManager.currentTrial != 0)
            {
                // Logs trial
                TrialLogger_CSVWriter.LogTrial(
                    trialManager.currentTrial,        // trial #
                    trialManager.currTargetCubeIdx == trialManager.ghostCubeIdx, //true if target trial (ie. target/highlighted cube = ghost cube)
                    trialManager.ghostCubeIdx,            // ghost cube Idx
                    touchedIdx,                           // Idx of cube that was hit
                    trialManager.currTargetCubeIdx,    // Idx of target cube/cube that should have been hit
                    touchedIdx != trialManager.currTargetCubeIdx,    // T if hit cube != target => there was a mismatch
                    reactionTime                            // time between user touching plane (activiating trial) and hitting cube (finishing trial)
                );
            }

            // Score Calculations
                if (reactionTime <= 1)
                {
                    trialManager.score += 5;
                }
                else if (reactionTime <= 2)
                {
                    trialManager.score += 3;
                }
                else
                {
                    trialManager.score += 1;
                }

            trialManager.totalPossibleScore += 5;

            planeTrigger.timeHitPlane = -1f;

        }
    }

    // used for debugging:
    // private void OnTriggerStay(Collider other) {
    //     // Debug.Log("OBJECT WITHIN");
    // }

    // private void OnTriggerExit(Collider other)
    // {
        // used for debgugging:
        // Debug.Log("OBJECT EXITED");
        // objectRenderer.material.color = trialManager.defaultColor;
    // }

}
