using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Two-point table calibration using Meta AIO (or any XR) controller poses + Input System.
/// - Left trigger captures the REAL top-right corner (with inset compensation).
/// - Right trigger captures the REAL top-left corner (with inset compensation).
/// - When both are captured, call CalibrateNow_TwoPoint() (auto or manual).
///
/// Results:
///   * Applies YAW (around +Y) + XZ translation ONLY. Height (Y) is unchanged.
///   * Moves tableRoot; everything under it (props, canvases) follows.
/// </summary>
public class Calibration : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Parent of the entire table (mesh, props, canvases, etc.). Everything must be a child of this.")]
    public Transform tableRoot;

    [Tooltip("Virtual table's Top-Right corner marker (child of tableRoot).")]
    public Transform topRightAnchor;

    [Tooltip("Virtual table's Top-Left corner marker (child of tableRoot).")]
    public Transform topLeftAnchor;

    [Tooltip("Left controller Transform (world pose) from Meta AIO / OpenXR.")]
    public Transform leftController;

    [Tooltip("Right controller Transform (world pose) from Meta AIO / OpenXR.")]
    public Transform rightController;

    [Header("Input (New Input System)")]
    [Tooltip("Assign action bound to <XRController>{LeftHand}/trigger")]
    public InputActionReference leftTrigger;

    [Tooltip("Assign action bound to <XRController>{RightHand}/trigger")]
    public InputActionReference rightTrigger;

    [Header("Capture / Calibration Options")]
    [Tooltip("Meters to inset from each touched corner toward the table's interior (compensate controller tip size).")]
    [Min(0f)] public float edgeInsetMeters = 0.03f;

    [Tooltip("If true, will automatically run calibration as soon as both corners are captured.")]
    public bool autoCalibrateWhenBothCaptured = true;

    [Tooltip("Optional: Log extra details.")]
    public bool verboseLogs = true;

    // ---- internal capture state ----
    private bool topRightCaptured = false;
    private bool topLeftCaptured  = false;
    private Vector3 realTopRightWorld; // measured with inset applied
    private Vector3 realTopLeftWorld;  // measured with inset applied

    private void OnEnable()
    {
        // Enable actions & hook callbacks
        if (leftTrigger != null && leftTrigger.action != null)
        {
            leftTrigger.action.Enable();
            leftTrigger.action.performed += OnLeftTriggerPressed;
        }
        if (rightTrigger != null && rightTrigger.action != null)
        {
            rightTrigger.action.Enable();
            rightTrigger.action.performed += OnRightTriggerPressed;
        }
    }

    private void OnDisable()
    {
        // Unhook callbacks & disable actions
        if (leftTrigger != null && leftTrigger.action != null)
        {
            leftTrigger.action.performed -= OnLeftTriggerPressed;
            leftTrigger.action.Disable();
        }
        if (rightTrigger != null && rightTrigger.action != null)
        {
            rightTrigger.action.performed -= OnRightTriggerPressed;
            rightTrigger.action.Disable();
        }
    }

    // --------- INPUT CALLBACKS ---------
    private void OnLeftTriggerPressed(InputAction.CallbackContext ctx)
    {
        // Left trigger → capture REAL Top-Right corner (as requested earlier).
        // (If you want the opposite mapping, swap the two bodies of OnLeftTriggerPressed & OnRightTriggerPressed.)
        CaptureCornerTopRight();
        MaybeAutoCalibrate();
    }

    private void OnRightTriggerPressed(InputAction.CallbackContext ctx)
    {
        // Right trigger → capture REAL Top-Left corner.
        CaptureCornerTopLeft();
        MaybeAutoCalibrate();
    }

    private void MaybeAutoCalibrate()
    {
        if (autoCalibrateWhenBothCaptured && topRightCaptured && topLeftCaptured)
        {
            CalibrateNow_TwoPoint();
        }
    }

    // --------- CAPTURE METHODS ---------
    public void CaptureCornerTopRight()
    {
        if (!CheckRefs(leftController, "LeftController")) return;
        if (!CheckRefs(tableRoot, "TableRoot")) return;

        // Inward direction from Top-Right is toward ( -tableRight + -tableForward )
        Vector3 insetDir = (-tableRoot.right + -tableRoot.forward).normalized;
        realTopRightWorld = leftController.position + insetDir * edgeInsetMeters;
        topRightCaptured = true;

        if (verboseLogs)
            Debug.Log($"[Calib] Captured REAL TopRight at {leftController.position:F3} → inset → {realTopRightWorld:F3} (inset {edgeInsetMeters}m dir {insetDir})");
    }

    public void CaptureCornerTopLeft()
    {
        if (!CheckRefs(rightController, "RightController")) return;
        if (!CheckRefs(tableRoot, "TableRoot")) return;

        // Inward direction from Top-Left is toward ( +tableRight + -tableForward )
        Vector3 insetDir = (tableRoot.right + -tableRoot.forward).normalized;
        realTopLeftWorld = rightController.position + insetDir * edgeInsetMeters;
        topLeftCaptured = true;

        if (verboseLogs)
            Debug.Log($"[Calib] Captured REAL TopLeft at {rightController.position:F3} → inset → {realTopLeftWorld:F3} (inset {edgeInsetMeters}m dir {insetDir})");
    }

    // --------- CALIBRATION (2-POINT, XZ ONLY) ---------
    /// <summary>
    /// Aligns the virtual table using two points: TopRight & TopLeft.
    /// Applies yaw (around +Y) and XZ translation ONLY (Y height preserved).
    /// </summary>
    public void CalibrateNow_TwoPoint()
    {
        if (!CheckRefs(tableRoot, "TableRoot") ||
            !CheckRefs(topRightAnchor, "TopRightAnchor") ||
            !CheckRefs(topLeftAnchor, "TopLeftAnchor"))
        {
            return;
        }
        if (!topRightCaptured || !topLeftCaptured)
        {
            Debug.LogWarning("[Calib] Need both TopRight and TopLeft captures before two-point calibration.");
            return;
        }

        // Virtual anchors in world
        Vector3 vTR = topRightAnchor.position;
        Vector3 vTL = topLeftAnchor.position;

        // Real (sampled) anchors in world (already inset)
        Vector3 rTR = realTopRightWorld;
        Vector3 rTL = realTopLeftWorld;

        // 2D vectors on XZ plane
        Vector2 vVec = ToXZ(vTL - vTR);
        Vector2 rVec = ToXZ(rTL - rTR);

        if (vVec.sqrMagnitude < 1e-8f || rVec.sqrMagnitude < 1e-8f)
        {
            Debug.LogError("[Calib] Degenerate anchor vectors; are your anchors too close or identical?");
            return;
        }

        // Compute yaw: rotate vVec onto rVec (signed angle in XZ)
        float yawDeg = SignedAngleDeg(vVec, rVec);
        Quaternion deltaYaw = Quaternion.AngleAxis(yawDeg, Vector3.up);

        // Apply rotation first (around tableRoot pivot)
        Quaternion newRootRot = deltaYaw * tableRoot.rotation;

        // Where will the virtual TopRight land after rotation (no translation yet)?
        Vector3 trAfterRot = RotateAroundPivot(vTR, tableRoot.position, deltaYaw);

        // Compute XZ-only translation to put TopRight exactly at the real TopRight
        Vector2 diffXZ = ToXZ(rTR - trAfterRot);
        Vector3 deltaPos = new Vector3(diffXZ.x, 0f, diffXZ.y); // lock Y to 0 shift

        // Commit transform — height preserved
        tableRoot.rotation = newRootRot;
        tableRoot.position = new Vector3(
            tableRoot.position.x + deltaPos.x,
            tableRoot.position.y, // unchanged
            tableRoot.position.z + deltaPos.z
        );

        if (verboseLogs)
            Debug.Log($"[Calib] Applied Two-Point XZ calibration. Yaw={yawDeg:F2}°, ΔposXZ=({deltaPos.x:F3},{deltaPos.z:F3}). Height preserved.");

        // Ready for next round if desired
        // ResetCaptures(); // uncomment if you want to force new captures every time
    }

    public void ResetCaptures()
    {
        topRightCaptured = false;
        topLeftCaptured = false;
    }

    // --------- HELPERS ---------
    private static Vector2 ToXZ(in Vector3 v) => new Vector2(v.x, v.z);

    private static float SignedAngleDeg(Vector2 from, Vector2 to)
    {
        from.Normalize(); to.Normalize();
        float unsigned = Vector2.Angle(from, to);
        float sign = Mathf.Sign(from.x * to.y - from.y * to.x); // z-sign of 2D cross
        return unsigned * sign;
    }

    private static Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion rot)
    {
        return pivot + rot * (point - pivot);
    }

    private static bool CheckRefs(Object obj, string label)
    {
        if (obj == null)
        {
            Debug.LogError($"[Calib] Missing reference: {label}");
            return false;
        }
        return true;
    }
}
