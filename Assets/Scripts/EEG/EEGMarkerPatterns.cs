// EEGMarkerPatterns.cs
// Unity 2021+
// Provides burst helpers (repeat markers with gaps) and one-liner event markers:
//   Experiment start/end (4 pulses), Block start/end (3), Trial start (2), Trial end (1).
//
// Usage:
//   - Put EEGMarkerSender on a GameObject (opens the serial port).
//   - Put EEGMarkerPatterns on the same or another GameObject.
//   - Assign 'sender' in the Inspector (or leave empty to auto-find).
//   - Call: patterns.MarkTrialStart(); patterns.MarkExperimentStart(1); etc.
//
// Notes:
//   - Non-blocking (uses coroutine + WaitForSeconds).
//   - No nullable types, no '??'.
//   - If you build for Quest/Android, exclude EEG serial scripts from that build.

using UnityEngine;

public class EEGMarkerPatterns : MonoBehaviour
{

    [Header("Burst Settings")]
    // public int defaultPulseMsOverride = 0;      //Marker width in ms (<=0 means use sender.defaultPulseMs)

    public int defaultGapMs = 20;           //Gap between consecutive markers in a burst (ms)
    private EEGMarkerSender sender;

    private bool _burstBusy = false;

    void Awake()
    {
        sender = GetComponent<EEGMarkerSender>();
        if (sender == null)
            sender = FindFirstObjectByType<EEGMarkerSender>();
        if (sender == null)
            Debug.LogWarning("[EEGMarkerPatterns] EEGMarkerSender not found. Calls will be no-ops.");
    }

    // === Public low-level burst API ===
    // Repeat 'count' pulses of 'code' with 'pulseMs' width and 'gapMs' between.
    public void SendBurst(int code, int count, int pulseMs, int gapMs)
    {
        if (sender == null) return;
        if (count <= 0) return;
        if (pulseMs < 1) pulseMs = 1;
        if (gapMs   < 0) gapMs   = 0;
        StartCoroutine(SendBurstCo(code, count, pulseMs, gapMs));
    }

    System.Collections.IEnumerator SendBurstCo(int code, int count, int pulseMs, int gapMs)
    {
        // Prevent overlapping bursts
        while (_burstBusy) yield return null;
        _burstBusy = true;

        for (int i = 0; i < count; i++)
        {
            // sender.SendMarker(code, pulseMs > 0 ? pulseMs : GetDefaultPulse());
            sender.SendMarker(code, pulseMs);
            if (i < count - 1)
                yield return new WaitForSeconds(gapMs / 1000f);
        }

        _burstBusy = false;
    }

    // int GetDefaultPulse()
    // {
    //     // if (defaultPulseMsOverride > 0) return defaultPulseMsOverride;
    //     // return sender != null ? sender.defaultPulseMs : 10; // safe fallback
    //     return sender.defaultPulseMs;
    // }

    // === Convenience helpers (use defaults) ===
    public void MarkExperimentStart() { SendBurst(1, 5, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("--------------------------------MarkExperimentStart ran!"); }     // in RestThenShowCalibration() of CaliPhase.cs
    public void MarkExperimentEnd()   { SendBurst(1, 5, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("-------------------------MarkExperimentEnd ran!"); }     // in StartNextTrial() of Trialmanager.cs
    public void MarkBlockStart()      { SendBurst(1, 4, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("-------------------MarkBlockStart ran!"); }     // in ShowBreakUI() of BreakUIController.cs AND ResetPlaneFlagFirst() of CaliPhase.cs
    public void MarkBlockEnd()        { SendBurst(1, 5, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("---------------MarkBlockEnd ran!"); }     // in StartNextTrial() of Trialmanager.cs twice (once before a break and another at end of experiment)
    // public void MarkTrialStart()      { SendBurst(1, 2, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("----------MarkTrialStart ran!"); }     // in StartNextTrial() of Trialmanager.cs
    public void MarkTargetTrialStart()      { SendBurst(1, 3, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("----------MarkTargetTrialStart ran!"); }     // in StartNextTrial() of Trialmanager.cs
    public void MarkNonTargetTrialStart()      { SendBurst(1, 2, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("----------MarkNonTargetTrialStart ran!"); }     // in StartNextTrial() of Trialmanager.cs
    public void MarkTrialEnd()        { SendBurst(1, 1, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("---MarkTrialEnd ran!"); }     // in OnTriggerEnter() of PlaneTrigger.cs
    // public void MarkTargetTrialEnd()        { SendBurst(1, 1, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("---MarkTrialEnd ran!"); }     // in OnTriggerEnter() of PlaneTrigger.cs
    // public void MarkNonTargetTrialEnd()        { SendBurst(1, 1, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("---MarkTrialEnd ran!"); }     // in OnTriggerEnter() of PlaneTrigger.cs

    public void BaselinePhaseStart() { SendBurst(1, 1, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("---BaselinePhaseStart ran!"); }     // in OnTriggerEnter() of PlaneTrigger.cs
    public void BaselinePhaseEnd() { SendBurst(1, 1, sender.defaultPulseMs, defaultGapMs); UnityEngine.Debug.LogWarning("---BaselinePhaseEnd ran!"); }     // in OnTriggerEnter() of PlaneTrigger.cs

    // Overloads that let you specify the code explicitly
    // public void MarkExperimentStart(int code) { SendBurst(code, 4, GetDefaultPulse(), defaultGapMs); }
    // public void MarkExperimentEnd  (int code) { SendBurst(code, 4, GetDefaultPulse(), defaultGapMs); }
    // public void MarkBlockStart     (int code) { SendBurst(code, 3, GetDefaultPulse(), defaultGapMs); }
    // public void MarkBlockEnd       (int code) { SendBurst(code, 3, GetDefaultPulse(), defaultGapMs); }
    // public void MarkTrialStart     (int code) { SendBurst(code, 2, GetDefaultPulse(), defaultGapMs); }
    // public void MarkTrialEnd       (int code) { SendBurst(code, 1, GetDefaultPulse(), defaultGapMs); }
}
