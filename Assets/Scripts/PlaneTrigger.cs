using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlaneTrigger : MonoBehaviour
{
    public List<CubeTrigger> cubeTriggers;
    public TrialManager trialManager;
    public int cubeFlag;
    private int planeFlag;

    public float timeHitPlane;

    void Start()
    {
        planeFlag = 0;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (planeFlag == 0)
        {
            // sets planeFlag to -1 so that the Plane can't be triggered multiple times
            planeFlag = -1;
            Debug.LogWarning("Plane triggered: advancing to next trial");
            EventLogger_CSVWriter.Log("Plane Triggered");

            timeHitPlane = Time.time;

            // Reset all cube triggers counters (cubes have a similar system to the planeFlag variable)
            // foreach (var cubeTrigger in cubeTriggers)
            // {
            //     cubeTrigger.ResetCubeFlag();
            // }

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
