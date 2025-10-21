using UnityEngine;
using Unity.Mathematics; // float3x3, math.mul, math.inverse
using UnityEngine.InputSystem;
using TMPro;

/// Matrix-based 3‑point calibration.
///
/// Workflow:
/// 1) Press trigger on three corresponding real corners (in the SAME order as vPoints[0..2]).
/// 2) On the 3rd press, we build 3×3 matrices for virtual (v) and real (r) points in XZ, solve
///    rotationMatrix = r * inverse(v), then apply to the virtualSpace (position + yaw via LookAt anchor).
/// 3) Optionally show prompts via displayText.
///
/// Notes:
/// - Assumes virtualSpace is authored around the origin and contains your virtual table.
/// - Only XZ is calibrated (Y is kept as-is). We write Y = yKeep for both virtualSpace and anchor.
/// - If your virtualSpace has a rotated/scaled parent, keep it uniform scale; LookAt handles yaw.
public class Calibration : MonoBehaviour
{
    [Header("Scene Objects (match FullDemo1)")]
    [Tooltip("Virtual anchor points on the table (order must match the order you will touch in real world).")]
    public GameObject[] vPoints = new GameObject[3];

    [Tooltip("Real-world capture placeholders (we'll fill their positions when you press trigger).")]
    public GameObject[] rPoints = new GameObject[3];

    [Tooltip("Object that holds all virtual table content (equiv. to virtualSpace). Move/rotate this.")]
    public GameObject virtualSpace;

    [Tooltip("An object initially placed at (0,0,+z) from virtualSpace to define its forward axis.")]
    public GameObject anchor;

    [Header("Input")]
    [Tooltip("Left/Right controller transform used to capture the sample when you press trigger.")]
    public Transform controller;

    [Tooltip("Bind to <XRController>/trigger if you want auto-capture; otherwise call StateTrigger().")]
    public InputActionReference triggerAction;

    [Header("UI (optional)")]
    public TMP_Text displayText;

    [Header("Debug")]
    public bool verboseLogs = true;

    private int state = 0;      // 0..3 then done
    private int totalStates = 0; // 1 + vPoints.Length (parity w/ FullDemo1)

    private void OnEnable()
    {
        totalStates = 1 + (vPoints != null ? vPoints.Length : 0);
        if (displayText) displayText.text = $"Calibrating (1/{(vPoints?.Length ?? 3)})";
        if (triggerAction?.action != null)
        {
            triggerAction.action.Enable();
            triggerAction.action.performed += OnTrigger;
        }
    }

    private void OnDisable()
    {
        if (triggerAction?.action != null)
        {
            triggerAction.action.performed -= OnTrigger;
            triggerAction.action.Disable();
        }
    }

    private void OnTrigger(InputAction.CallbackContext _)
    {
        StateTrigger();
    }

    /// Call this from UI or input. Mirrors FullDemo1.StateTrigger.
    public void StateTrigger()
    {
        if (!CheckBasics()) return;

        if (state < totalStates - 2)
        {
            // Capture a real point into rPoints[state]
            rPoints[state].transform.position = controller.position;
            state++;
            if (displayText) displayText.text = $"Calibrating ({state + 1}/{rPoints.Length})";
        }
        else if (state == totalStates - 2)
        {
            // Final capture, then calibrate
            rPoints[state].transform.position = controller.position;
            Calibrate();
            state++;
            if (displayText) displayText.text = "Finished! Press Trigger to Begin!";
        }
        else
        {
            // After calibration, you can toggle a start animation, etc. (optional)
        }
    }

    /// Core: identical math style to FullDemo1.Calibrate()
    public void Calibrate()
    {
        // Build rMatrix and vMatrix as 3×3 homogeneous (x,z,1) columns (matching your colleague).
        // Important: We treat each column as one point (c0, c1, c2) consistent with Unity.Mathematics float3x3 layout.
        float3x3 rMatrix = new float3x3();
        float3x3 vMatrix = new float3x3();

        // sMatrix stores two columns: virtualSpace position and anchor position (both projected to XZ, with 1 in w).
        float3x3 sMatrix = new float3x3();

        // Fill rMatrix & vMatrix
        for (int i = 0; i < 3; i++)
        {
            Vector3 rp = rPoints[i].transform.position;
            Vector3 vp = vPoints[i].transform.position;

            // Column i (x, z, 1)
            if (i == 0)
            {
                rMatrix.c0 = new float3(rp.x, rp.z, 1f);
                vMatrix.c0 = new float3(vp.x, vp.z, 1f);
            }
            else if (i == 1)
            {
                rMatrix.c1 = new float3(rp.x, rp.z, 1f);
                vMatrix.c1 = new float3(vp.x, vp.z, 1f);
            }
            else // i == 2
            {
                rMatrix.c2 = new float3(rp.x, rp.z, 1f);
                vMatrix.c2 = new float3(vp.x, vp.z, 1f);
            }
        }

        // Solve affine (really similarity-with-yaw) in XZ: rotationMatrix = r * inverse(v)
        float3x3 rotationMatrix = math.mul(rMatrix, math.inverse(vMatrix));
        if (verboseLogs) Debug.Log($"[Calib] rotationMatrix =\n{rotationMatrix}");

        // Transform the virtual points (for optional debug visualization)
        float3x3 transformedPoints = math.mul(rotationMatrix, vMatrix);
        if (verboseLogs)
        {
            Debug.Log($"[Calib] v0'={transformedPoints.c0.x:0.###},{transformedPoints.c0.y:0.###}  v1'={transformedPoints.c1.x:0.###},{transformedPoints.c1.y:0.###}  v2'={transformedPoints.c2.x:0.###},{transformedPoints.c2.y:0.###}");
        }

        // Optionally write them back (comment out in production)
        // vPoints[0].transform.position = new Vector3(transformedPoints.c0.x, vPoints[0].transform.position.y, transformedPoints.c0.y);
        // vPoints[1].transform.position = new Vector3(transformedPoints.c1.x, vPoints[1].transform.position.y, transformedPoints.c1.y);
        // vPoints[2].transform.position = new Vector3(transformedPoints.c2.x, vPoints[2].transform.position.y, transformedPoints.c2.y);

        // sMatrix: column 0 is virtualSpace position, column 1 is anchor position (both in XZ plane, 1 in w)
        Vector3 vsPos = virtualSpace.transform.position;
        Vector3 anPos = anchor.transform.position;

        sMatrix.c0 = new float3(vsPos.x, vsPos.z, 1f);
        sMatrix.c1 = new float3(anPos.x, anPos.z, 1f);
        sMatrix.c2 = new float3(0f, 0f, 1f); // unused but keep homogeneous structure

        float3x3 virtualSpaceTransformed = math.mul(rotationMatrix, sMatrix);
        if (verboseLogs) Debug.Log($"[Calib] virtualSpaceTransformed =\n{virtualSpaceTransformed}");

        // Apply: set virtualSpace to transformed col0 (XZ), set anchor to transformed col1 (XZ), then LookAt.
        // Preserve existing Y heights (you can change yKeep to the averaged real Y if preferred).
        float yKeepVS = virtualSpace.transform.position.y;
        float yKeepAN = anchor.transform.position.y;

        Vector3 newVS = new Vector3(virtualSpaceTransformed.c0.x, yKeepVS, virtualSpaceTransformed.c0.y);
        Vector3 newAN = new Vector3(virtualSpaceTransformed.c1.x, yKeepAN, virtualSpaceTransformed.c1.y);

        virtualSpace.transform.position = newVS;
        anchor.transform.position = newAN;
        virtualSpace.transform.LookAt(anchor.transform.position);

        if (verboseLogs)
        {
            Debug.Log($"[Calib] Applied. VS pos -> {newVS}  Anchor -> {newAN}  VS yaw -> {virtualSpace.transform.eulerAngles.y:0.##}°");
        }
    }

    private bool CheckBasics()
    {
        if (vPoints == null || vPoints.Length < 3 || vPoints[0] == null || vPoints[1] == null || vPoints[2] == null)
        { Debug.LogError("[Calib] Assign vPoints[0..2]."); return false; }
        if (rPoints == null || rPoints.Length < 3 || rPoints[0] == null || rPoints[1] == null || rPoints[2] == null)
        { Debug.LogError("[Calib] Assign rPoints[0..2]."); return false; }
        if (virtualSpace == null || anchor == null) { Debug.LogError("[Calib] Assign virtualSpace and anchor."); return false; }
        if (controller == null) { Debug.LogError("[Calib] Assign controller Transform."); return false; }
        return true;
    }
}
