using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlaneTrigger : MonoBehaviour
{
    public List<CubeTrigger> cubeTriggers;
    public TrialManager trialManager; 
    public int cubeFlag;
    private int planeFlag;
    // public float timeHitPlane;
    // public float trialStartTime;
    private float jitterEndTime;   

    [SerializeField] private EEGMarkerPatterns eeg;


    void Start()
    {
        planeFlag = -1;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Debug.LogError($"[PlaneTrigger] enter t={Time.time:F3} frame={Time.frameCount} onBreak={trialManager.onBreak} planeFlag={planeFlag}");
        float currTime = Time.time;

        // Find the most recent cube hit
        float latestCubeHitTime = 0f;
        foreach (var cube in cubeTriggers)
        {
            if (cube.timeHitCube > latestCubeHitTime)
                latestCubeHitTime = cube.timeHitCube;
        }

        // ensure plane can be triggered
        if  (planeFlag == 0)
        {
            // sets planeFlag to -1 so that the Plane can't be triggered multiple times
            planeFlag = -1;
            Debug.LogWarning("Plane triggered: advancing to next trial");
            EventLogger_CSVWriter.Log("Plane Triggered");

            // timeHitPlane = Time.time;

            // Allows cubes to be hit
            //reset cube trigger flag
            ResetCubeFlag();

            // Begins next trial
            // StartCoroutine(trialManager.StartNextTrial());
            // If jitter already ended, start immediately.
            // If not, wait until jitterEndTime, then start.
            if (currTime >= jitterEndTime)
            {
                StartCoroutine(trialManager.StartNextTrial());
            }
            else
            {
                StartCoroutine(WaitForJitterAndStart());
            }
        }
    }
    private IEnumerator WaitForJitterAndStart()
    {
        float waitTime = Mathf.Max(0f, jitterEndTime - Time.time);
        if (waitTime > 0f)
        {
            yield return new WaitForSeconds(waitTime);
        }
        // trialStartTime = Time.time;
        StartCoroutine(trialManager.StartNextTrial());
    }

    
    public void ResetCubeFlag()
    {
        cubeFlag = 0;
    }
    // called in CubeTrigger.cs to reset planeFlag (similar to how we reset cubeTrigger)
    public void ResetPlaneFlag()
    {
        planeFlag = 0;
        // trial is over so:
        eeg?.MarkTrialEnd();
        jitterEndTime = Time.time + Random.Range(1f, 2f);
    }

    public void ResetPlaneFlagFirst()
    {
        planeFlag = 0;
        eeg?.MarkBlockStart();

    }

}
