using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class CalibrationManager : MonoBehaviour
{
    public Transform controllerTransform; // Assign this in Inspector (e.g., right-hand controller)
    public Transform calibrationRoot; // The root object to move/rotate (e.g., table + cubes parent)

    private InputAction calibrateAction;
    private List<Vector3> calibrationPoints = new List<Vector3>();

    private void Awake()
    {
        calibrateAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/g");
        calibrateAction.performed += ctx => CaptureCalibrationPoint();
        calibrateAction.Enable();
    }

    private void CaptureCalibrationPoint()
    {
        if (controllerTransform == null || calibrationRoot == null) return;

        Vector3 point = controllerTransform.position;
        calibrationPoints.Add(point);
        Debug.Log($"📌 Calibration Point {calibrationPoints.Count}: {point}");

        if (calibrationPoints.Count == 3)
        {
            ApplyCalibration();
            calibrationPoints.Clear();
        }
    }

    private void ApplyCalibration()
    {
        // We'll use the three points to define a plane and orientation
        Vector3 p0 = calibrationPoints[0];
        Vector3 p1 = calibrationPoints[1];
        Vector3 p2 = calibrationPoints[2];

        Vector3 origin = p0;
        Vector3 forward = (p1 - p0).normalized;
        Vector3 right = Vector3.Cross((p2 - p0).normalized, forward).normalized;
        Vector3 up = Vector3.Cross(forward, right).normalized;

        Quaternion rotation = Quaternion.LookRotation(forward, up);
        calibrationRoot.position = origin;
        calibrationRoot.rotation = rotation;

        Debug.Log("✅ Calibration applied to root object");
    }

    private void OnDestroy()
    {
        calibrateAction.Disable();
    }
}