using UnityEngine;

//TODO: Fix calibration
public class CubeLocationManager : MonoBehaviour
{
    public GameObject[] cubes;
    public GameObject plane;

    public Vector3 elbowPosition;
    public float tipToElbowLength;
    public float planeOffsetBelowTipToElbow;

    void Start()
    {
        EventLogger_CSVWriter.Log("Cubes Positioned");
        PlaceCubesInArc();
        EventLogger_CSVWriter.Log("Plane Positioned");
        PlacePlaneAboveElbow();
    }

    void PlaceCubesInArc()
    {
        if (cubes.Length != 5)
        {
            Debug.LogError("Must assign 5 cubes");
            return;
        }

        float arcRadius = tipToElbowLength;
        int numCubes = cubes.Length;

        for (int i = 0; i < numCubes; i++)
        {
            // Go from 0 degrees to 90 degrees
            float angleDeg = i * (90f / (numCubes - 1)); // 0, 22.5, 45, 67.5, 90
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Invert direction to fan to the left (negative X)
            Vector3 direction = new Vector3(-Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            Vector3 cubePos = elbowPosition + direction * arcRadius;

            cubes[i].transform.position = cubePos;
            cubes[i].transform.LookAt(elbowPosition);
        }
    }

    void PlacePlaneAboveElbow()
    {
        if (plane == null)
        {
            Debug.LogError("plane not assigned!");
            return;
        }

        Vector3 planePos = elbowPosition + Vector3.up * (tipToElbowLength - planeOffsetBelowTipToElbow);
        plane.transform.position = planePos;
    }
}
