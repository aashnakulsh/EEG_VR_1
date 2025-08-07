using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlaneTrigger : MonoBehaviour
{
    public List<CubeTrigger> cubeTriggers;
    public TrialManager trialManager;
    private int counterPlane;

    public float timeHitPlane;

    void Start()
    {
        counterPlane = 0;
    }

    private void OnTriggerEnter(Collider other)
    {

        if (counterPlane == 0)
        {
            // sets counterPlane to -1 so that the Plane can't be triggered multiple times
            counterPlane = -1;
            // Debug.Log("PLANE TRIGGERED");
            Debug.LogWarning("Plane triggered: advancing to next trial");
            EventLogger_CSVWriter.Log("Plane Triggered");

            timeHitPlane = Time.time;

            // Reset all cube triggers counters (cubes have a similar system to the counterPlane variable)
            foreach (var cubeTrigger in cubeTriggers)
            {
                cubeTrigger?.ResetCounterCube();
            }

            // begins next trial
            StartCoroutine(trialManager?.StartNextTrial());
        }
    }

    // called in CubeTrigger.cs to reset counterPlane (similar to how we reset cubeTrigger on line 27)
    public void ResetCounterPlane()
    {
        counterPlane = 0;
    }

}
