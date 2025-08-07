using UnityEngine;
using System.Collections;

public class CubeTrigger : MonoBehaviour
{
    private Renderer objectRenderer;
    // public Color newColor = Color.yellow;
    public Color originalColor;

    public PlaneTrigger planeTrigger;
    public TrialManager trialManager;
    public BreakUIController buic;
    public int cubeIndex;
    private int counterCube;
    private float timeHitCube;
    private AudioSource aud;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        // originalColor = objectRenderer.material.color;
        counterCube = 0;
        aud = GetComponent<AudioSource>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (counterCube == 0 & !trialManager.onBreak & !trialManager.isExperimentComplete)
        {
            counterCube = -1;
            // Debug.Log("OBJECT ENTERED");
            // sets cube to green when hand is in it -- used for debeugging
            // objectRenderer.material.color = newColor;

             aud.Play(); // Low-latency audio ping play


            timeHitCube = Time.time;

            //Debug.LogWarning($"Cube {cubeIndex} touched.");
            EventLogger_CSVWriter.Log($"Cube Touched: {cubeIndex}");



            // Allow plane to be touched again by reseting plane trigger counter
            planeTrigger?.ResetCounterPlane();

            // Clear highlight
            EventLogger_CSVWriter.Log($"Cubes Reset");
            trialManager?.ResetCubes();

            //log trial
            float reactionTime = timeHitCube - planeTrigger.timeHitPlane;
            if (trialManager.tookBreak) {reactionTime = reactionTime-buic.totalBreakDuration;}

            int touchedIndex = trialManager.cubes.IndexOf(this.gameObject);

            trialManager.trialLogger.LogTrial(
                trialManager.CurrentTrialNumber,        // trial #
                trialManager.CurrentTargetCubeIndex == trialManager.ghostCubeIndex, //true if target trial (ie. target/highlighted cube = ghost cube)
                trialManager.ghostCubeIndex,            // ghost cube index
                touchedIndex,                           // index of cube that was hit
                trialManager.CurrentTargetCubeIndex,    // index of target cube/cube that should have been hit
                touchedIndex != trialManager.CurrentTargetCubeIndex,    // T if hit cube != target => there was a mismatch
                reactionTime                            // time between user touching plane (activiating trial) and hitting cube (finishing trial)
            );

            // score calculations
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

    // private void OnTriggerStay(Collider other) {
    //     // Debug.Log("OBJECT WITHIN");
    // }

    private void OnTriggerExit(Collider other)
    {
        // Debug.Log("OBJECT EXITED");
        // TODO: is this line needed? i think not
        objectRenderer.material.color = originalColor;
    }

    public void ResetCounterCube()
    {
        counterCube = 0;
    }

}
