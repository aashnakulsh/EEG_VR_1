using UnityEngine;

//TODO: Fix calibration
public class CubeLocationManager : MonoBehaviour
{
    public GameObject[] cubes;
    public GameObject plane;
    public GameObject table; 
    public GameObject elbowPoint;


    private Vector3 elbowPosition;
    public float tipToElbowLength;
    public float planeOffsetBelowTipToElbow;

    void Start()
    {
        if (elbowPoint != null)
        {
            elbowPosition = elbowPoint.transform.position;
        }
        else
        {
            Debug.LogError("elbowPoint GameObject not assigned!");
        }

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

        // Get table top Y only once
        float tableTopY = table.GetComponent<Renderer>().bounds.max.y;

        for (int i = 0; i < numCubes; i++)
        {
            float angleDeg = i * (90f / (numCubes - 1));
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Horizontal arc only (flat on XZ plane)
            Vector3 direction = new Vector3(-Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            Vector3 arcCenter = new Vector3(elbowPosition.x, tableTopY, elbowPosition.z); // elbow Y ignored
            Vector3 cubePos = arcCenter + direction * arcRadius;

            // Raise cube Y so it sits on top of table
            float cubeHeight = cubes[i].GetComponent<Renderer>().bounds.size.y;
            cubePos.y = tableTopY + (cubeHeight / 2f);

            cubes[i].transform.position = cubePos;

            // Make it look at elbow (horizontally only)
            Vector3 lookTarget = new Vector3(elbowPosition.x, cubePos.y, elbowPosition.z);
            cubes[i].transform.LookAt(lookTarget);
        }
    }


    //void PlaceCubesInArc()
    //{
    //    if (cubes.Length != 5)
    //    {
    //        Debug.LogError("Must assign 5 cubes");
    //        return;
    //    }

    //    float arcRadius = tipToElbowLength;
    //    int numCubes = cubes.Length;

    //    for (int i = 0; i < numCubes; i++)
    //    {
    //        // Go from 0 degrees to 90 degrees
    //        float angleDeg = i * (90f / (numCubes - 1)); // 0, 22.5, 45, 67.5, 90
    //        float angleRad = angleDeg * Mathf.Deg2Rad;

    //        // Invert direction to fan to the left (negative X)
    //        Vector3 direction = new Vector3(-Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
    //        Vector3 cubePos = elbowPosition + direction * arcRadius;



    //        //cubes[i].transform.position = cubePos;

    //        // Get the top Y of the table using its Renderer
    //        float tableTopY = table.GetComponent<Renderer>().bounds.max.y;

    //        // Get the height of the cube
    //        float cubeHeight = cubes[i].GetComponent<Renderer>().bounds.size.y;

    //        // Set Y so bottom of cube rests on table
    //        cubePos.y = tableTopY + (cubeHeight / 2f);

    //        cubes[i].transform.position = cubePos;

    //        //cubes[i].transform.LookAt(elbowPosition);
    //        Vector3 lookTarget = new Vector3(elbowPosition.x, cubes[i].transform.position.y, elbowPosition.z);
    //        cubes[i].transform.LookAt(lookTarget);

    //    }
    //}

    void PlacePlaneAboveElbow()
    {
        if (plane == null)
        {
            Debug.LogError("plane not assigned!");
            return;
        }

        //float finalOffset = tipToElbowLength - planeOffsetBelowTipToElbow;
        //Debug.LogError("Placing plane at elbow Y + offset: " + elbowPosition.y + " + " + finalOffset);


        Vector3 planePos = elbowPosition + Vector3.up * (tipToElbowLength - planeOffsetBelowTipToElbow);
        plane.transform.position = planePos;
    }
}
