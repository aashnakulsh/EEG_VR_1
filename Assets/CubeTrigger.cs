using UnityEngine;
using System.Collections;

public class CubeTrigger : MonoBehaviour
{
    private Renderer objectRenderer;
    // public Color newColor = Color.yellow;            // used for debugging

    public PlaneTrigger planeTrigger;
    public TrialManager trialManager;
    public BreakUIController buic;
    public int cubeIndex;

    // used to determine whether or not to trigger cube-related scripts (cube can only be hit after "resetting", 
    // so cubeFlag = 0 if participant has successful reset or -1 if participant has not reset)
    // currently, resetting is done by the participant triggering the plane
    // also prevents multilpe triggers of cube-related scripts (due to there being multiple colliderers on hand) 
    private int cubeFlag;            
    private float timeHitCube;
    private AudioSource aud;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        cubeFlag = 0;
        aud = GetComponent<AudioSource>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (cubeFlag == 0 & !trialManager.onBreak & !trialManager.isExperimentComplete)
        {
            cubeFlag = -1;
            // Debugging code:
            // Debug.Log("OBJECT ENTERED");
            // sets cube to newColor when hand is in it -- used for debeugging
            // objectRenderer.material.color = newColor;

            aud.Play(); // plays ping audio

            // getting statistics for logging
            timeHitCube = Time.time;
            EventLogger_CSVWriter.Log($"Cube Touched: {cubeIndex}");  

            planeTrigger.ResetPlaneFlag();               // Allow plane to be touched again by reseting plane trigger counter
            trialManager.ClearCubeHighlight();                    // Clears highlight

            // getting statistics for logging
            float reactionTime = timeHitCube - planeTrigger.timeHitPlane;
            if (trialManager.tookBreak) {reactionTime = reactionTime-buic.totalBreakDuration;}
            int touchedIndex = trialManager.cubes.IndexOf(this.gameObject);

            // Logs trial
            TrialLogger_CSVWriter.LogTrial(
                trialManager.currentTrial,        // trial #
                trialManager.currentTargetCubeIndex == trialManager.ghostCubeIndex, //true if target trial (ie. target/highlighted cube = ghost cube)
                trialManager.ghostCubeIndex,            // ghost cube index
                touchedIndex,                           // index of cube that was hit
                trialManager.currentTargetCubeIndex,    // index of target cube/cube that should have been hit
                touchedIndex != trialManager.currentTargetCubeIndex,    // T if hit cube != target => there was a mismatch
                reactionTime                            // time between user touching plane (activiating trial) and hitting cube (finishing trial)
            );

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

    private void OnTriggerExit(Collider other)
    {
        // used for debgugging:
        // Debug.Log("OBJECT EXITED");
        // TODO: is this line needed? i think not
        objectRenderer.material.color = trialManager.defaultColor;
    }

    public void ResetCubeFlag()
    {
        cubeFlag = 0;
    }

}
