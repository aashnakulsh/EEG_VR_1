using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlaneTrigger : MonoBehaviour
{
    public List<CubeTrigger> cubeTriggers;
    public TrialManager trialManager;
    // public CubeTrigger cubeTrigger;
    public int cubeFlag;
    private int planeFlag;
    public float timeHitPlane;

    void Start()
    {
        planeFlag = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        float currTime = Time.time;
        float jitterTime = Random.Range(1f, 2f);

        // Find the most recent cube hit
        float latestCubeHitTime = 0f;
        foreach (var cube in cubeTriggers)
        {
            if (cube.timeHitCube > latestCubeHitTime)
                latestCubeHitTime = cube.timeHitCube;
        }

            // allow randomized jitter time to pass + ensure plane can be triggered
        if ((currTime - latestCubeHitTime >= jitterTime) && (planeFlag == 0))
        {
            // sets planeFlag to -1 so that the Plane can't be triggered multiple times
            planeFlag = -1;
            Debug.LogWarning("Plane triggered: advancing to next trial");
            EventLogger_CSVWriter.Log("Plane Triggered");

            timeHitPlane = Time.time;

            // Allows cubes to be hit
            //reset cube trigger flag
            ResetCubeFlag();

            // Begins next trial
            StartCoroutine(trialManager.StartNextTrial());
        }
    }
    public void ResetCubeFlag()
    {
        cubeFlag = 0;
    }
    // called in CubeTrigger.cs to reset planeFlag (similar to how we reset cubeTrigger on line 27)
    public void ResetPlaneFlag()
    {
        planeFlag = 0;
    }

}
