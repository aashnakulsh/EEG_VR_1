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
            // gets vector position of elbowPoint
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

    // Places 5 cubes in an arc according to the length of the participant's forearm 
    void PlaceCubesInArc()
    {
        // must have 5 cubes assigned (NOTE: can change later if we want more cubes)
        if (cubes.Length != 5)
        {
            Debug.LogError("Must assign 5 cubes");
            return;
        }

        // ensures distance b/t cubes and elbow point is the participant's forearm length (index fingertip to elbow)
        float arcRadius = tipToElbowLength;         

        // Get table top's Y position 
        float tableTopY = table.GetComponent<Renderer>().bounds.max.y;
        
        // sets the position of each cube
        for (int i = 0; i < cubes.Length; i++)
        {
            // NOTE: gets cube's angle degree in the arc... (subtracted 10f so that the entire arc is shifted to the right) 
            float angleDeg = (i * (90f / (cubes.Length - 1))) - 10f;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            // Horizontal arc 
            Vector3 direction = new Vector3(-Mathf.Sin(angleRad), 0f, Mathf.Cos(angleRad));
            Vector3 arcCenter = new Vector3(elbowPosition.x, tableTopY, elbowPosition.z); 
            // GPT CODE:
            // Vector3 elbow = elbowPoint.transform.position;
            // Vector3 arcCenter = new Vector3(elbow.x, tableTopY, elbow.z);
            Vector3 cubePos = arcCenter + direction * arcRadius;

            // Raise cube's Y pos so it sits on top of table
            float cubeHeight = cubes[i].GetComponent<Renderer>().bounds.size.y;
            cubePos.y = tableTopY + (cubeHeight / 2f);

            cubes[i].transform.position = cubePos;

            // Make cube look at elbow (horizontally only)
            Vector3 lookTarget = new Vector3(elbowPosition.x, cubePos.y, elbowPosition.z);
            //GPT CODE: Vector3 lookTarget = new Vector3(elbow.x, cubePos.y, elbow.z);
            cubes[i].transform.LookAt(lookTarget);
        }
    }

    // Places Plane's Y at the height of the participant's arm so that they are forced to the "reset" position
    void PlacePlaneAboveElbow()
    {
        if (plane == null)
        {
            Debug.LogError("plane not assigned!");
            return;
        }
        Vector3 planePos = elbowPosition + Vector3.up * (tipToElbowLength - planeOffsetBelowTipToElbow);
        // GPT CODE: Vector3 planePos = elbowPoint.transform.position + Vector3.up * (tipToElbowLength - planeOffsetBelowTipToElbow);
        plane.transform.position = planePos;
    }
}
