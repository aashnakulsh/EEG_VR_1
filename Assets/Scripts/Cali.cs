using UnityEngine;
using Unity.Mathematics;

public class Calibrate_UseFriendsLogic : MonoBehaviour
{
    [Header("Calibration")]
    public Transform controllerTip;           // tip you poke with
    public Transform virtualSpace;            // EMPTY root we move/rotate (friend's virtualSpace)
    public Transform anchor;                  // child of virtualSpace at local (0,0,+1)

    
    [Header("Table")]
    public Transform table;                   // the actual table object to move in Y
//      [SerializeField] private MeshRenderer tabletopRenderer;  
// [   SerializeField] private float tableHeight = 0.74f;  
    [Header("Y Offset")]
    public float tableYOffset = -0.11176f; // positive = move table up, negative = down

    [Tooltip("3 virtual corner markers, IN ORDER: 0=front-left, 1=front-right, 2=back-left")]
    public Transform[] vPoints = new Transform[3];

    [Header("Input (OVR)")]
    public OVRInput.Controller whichController = OVRInput.Controller.RTouch;
    public OVRInput.Button beginButton  = OVRInput.Button.One;                   // A/X
    public OVRInput.Button recordButton = OVRInput.Button.PrimaryIndexTrigger;   // trigger

    [Header("Debug")]
    public bool verbose = true;
    public float markerSize = 0.02f;
    public float markerLife = 30f;
    public Color markerColor = Color.green;

    // internal
    private bool calibrating = false;
    private int idx = 0;
    private Transform[] rPoints = new Transform[3];   // transient holders for real clicks
    private float lockedY;



    // void Start()
    // {
    //     if (!tabletopRenderer)
    //     {
    //         Debug.LogError("[TableHeightAlign] No tabletopRenderer assigned!");
    //         return;
    //     }

    //     // Get current world-space top Y
    //     float currentTopY = tabletopRenderer.bounds.max.y;

    //     // Compute shift needed so top lands at desired height
    //     float deltaY = tableHeight - currentTopY;

    //     // Move the table root accordingly
    //     transform.position += new Vector3(0f, deltaY, 0f);

    //     Debug.Log($"[TableHeightAlign] Shifted by {deltaY:F3} m → top at {tableHeight:F3} m.");
    // }

    void Update()
    {
        // if (!calibrating && OVRInput.GetDown(beginButton, whichController))
        //     Begin();

        // if (calibrating && OVRInput.GetDown(recordButton, whichController))
        //     Capture();
        if (!calibrating && (OVRInput.GetDown(beginButton, OVRInput.Controller.RTouch) ||
            OVRInput.GetDown(beginButton, OVRInput.Controller.LTouch)))
            Begin();
            
        if (calibrating && (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) ||
            OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch)))
            Capture();

        // if (calibrating && OVRInput.GetDown(recordButton, whichController))
        //     Capture();
    }

    void Begin()
    {
        if (!controllerTip || !virtualSpace || !anchor || !table  ||vPoints[0]==null || vPoints[1]==null || vPoints[2]==null)
        {
            Debug.LogError("[Calib] Assign controllerTip, virtualSpace, anchor, and all 3 vPoints.");
            return;
        }

        // lock current Y height of the rig (friend keeps Y fixed; we do the same)
        lockedY = virtualSpace.position.y;
        if (anchor != null)
            {
                anchor.localPosition = new Vector3(0f, 0f, 1f);
                anchor.localRotation = Quaternion.identity;
            }

        // make/clear real-point holders
        for (int i = 0; i < 3; i++)
        {
            if (rPoints[i] == null)
            {
                GameObject rp = new GameObject("RealPoint" + i);
                rPoints[i] = rp.transform;
            }
        }

        idx = 0;
        calibrating = true;

        if (verbose)
        {
            Debug.LogError("--------- Calibration ---------");
            Debug.LogError("Click order with trigger: 0=Front-Left, 1=Front-Right, 2=Back-Left");
            Debug.LogError("--------------------------------------");
        }
        EventLogger_CSVWriter.Log("Manual 3-Point Calibration Begun");

    }

    void Capture()
    {
        if (!calibrating || idx >= 3) return;

        Vector3 p = controllerTip.position;
        rPoints[idx].position = p;
        idx++;

        // green sphere marker
        var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.transform.position = p;
        s.transform.localScale = Vector3.one * markerSize;
        var rend = s.GetComponent<Renderer>(); if (rend) rend.material.color = markerColor;
        Destroy(s, markerLife);

        if (verbose) Debug.LogError($"[Calib] Captured real point {idx-1} @ {p}");

        if (idx == 3)
        {
            Calibrate();            
            calibrating = false;
            if (verbose) Debug.LogError("[Calib] ✅ Done.");
            EventLogger_CSVWriter.Log("Manual 3-Point Calibration Over");
        }
    }

     // Build rMatrix & vMatrix (rows are [x,z,1]), solve A = r * inv(v),
    // transform a 2-row sMatrix: row0 is virtualSpace pos, row1 is anchor pos; then LookAt(anchor)
    void Calibrate()
    {
        // rMatrix, vMatrix: rows (x,z,1) for 3 points
        float3x3 rMatrix = new float3x3();
        float3x3 vMatrix = new float3x3();

        for (int i = 0; i < 3; i++)
        {
            Transform rp = rPoints[i];
            Transform vp = vPoints[i];

            rMatrix[i][0] = rp.position.x;
            rMatrix[i][1] = rp.position.z;
            rMatrix[i][2] = 1f;

            vMatrix[i][0] = vp.position.x;
            vMatrix[i][1] = vp.position.z;
            vMatrix[i][2] = 1f;

        }

        // Solve affine in XZ: rotationMatrix = r * inverse(v)
        float3x3 rotationMatrix = math.mul(rMatrix, math.inverse(vMatrix));   // :contentReference[oaicite:5]{index=5}

        // sMatrix holds TWO rows we care about: current virtualSpace pos and anchor pos (friend did this)
        float3x3 sMatrix = new float3x3();
        sMatrix[0][0] = virtualSpace.position.x;
        sMatrix[0][1] = virtualSpace.position.z;
        sMatrix[0][2] = 1f;

        sMatrix[1][0] = anchor.position.x;
        sMatrix[1][1] = anchor.position.z;
        sMatrix[1][2] = 1f;                                         // :contentReference[oaicite:6]{index=6}


        // Transform those two rows by the solved matrix; this gives target XZ for rig + anchor
        float3x3 virtualSpaceTransformed = math.mul(rotationMatrix, sMatrix); // :contentReference[oaicite:7]{index=7}

        // Apply: move rig to transformed row0, keep Y we locked
        Vector3 newRigPos = new Vector3(virtualSpaceTransformed.c0[0], lockedY, virtualSpaceTransformed.c0[1]);
        virtualSpace.position = newRigPos;                                   // :contentReference[oaicite:8]{index=8}

        // Move anchor to transformed row1 (keep same Y), then LookAt it to set yaw
        Vector3 newAnchorPos = new Vector3(virtualSpaceTransformed.c1[0], lockedY, virtualSpaceTransformed.c1[1]);
        anchor.position = newAnchorPos;                                       // :contentReference[oaicite:9]{index=9}

        // LookAt but constrain pitch/roll: aim at same-Y target so we only get yaw
        Vector3 lookTarget = new Vector3(newAnchorPos.x, virtualSpace.position.y, newAnchorPos.z);
        virtualSpace.transform.LookAt(lookTarget);                            // :contentReference[oaicite:10]{index=10}

        if (anchor != null)
        {
            anchor.localPosition = new Vector3(0f, 0f, 1f);
            anchor.localRotation = Quaternion.identity;
        }
        // NEW: hard-snap so VirtualCorner0 lands exactly on the first real click (XZ only)
        {
            Vector3 vc0 = vPoints[0].position;            // current world pos of VirtualCorner0
            Vector3 r0  = rPoints[0].position;            // first real click (FL)
            Vector3 delta = new Vector3(r0.x - vc0.x, 0f, r0.z - vc0.z);
            virtualSpace.position += delta;               // nudge rig so FL matches exactly
        }
        // // =====================
        // // Y CALIBRATION (table is child of virtualSpace)
        // // =====================
        // float avgWorldY =
        //     (rPoints[0].position.y +
        //     rPoints[1].position.y +
        //     rPoints[2].position.y) / 3f;

        // float localTableY = avgWorldY - virtualSpace.position.y;

        // table.localPosition = new Vector3(
        //     table.localPosition.x,
        //     localTableY,
        //     table.localPosition.z
        // );

        // =====================
        // Y CALIBRATION (move virtualSpace so the TABLE lands at avg tip height)
        // =====================
        float targetWorldY =
            (rPoints[0].position.y +
            rPoints[1].position.y +
            rPoints[2].position.y) / 3f;

        targetWorldY += tableYOffset;

        // tableLocalY is table's height within virtualSpace (doesn't change when moving virtualSpace)
        float tableLocalY = table.localPosition.y;

        // Move virtualSpace so that: table.worldY == targetWorldY
        float newVirtualSpaceY = targetWorldY - tableLocalY;

        virtualSpace.position = new Vector3(
            virtualSpace.position.x,
            newVirtualSpaceY,
            virtualSpace.position.z
        );



        if (verbose)
        {
            Debug.LogError($"[Calib] rotationMatrix:\n{rotationMatrix}");
            Debug.LogError($"[Calib] virtualSpace -> {virtualSpace.position}, yaw -> {virtualSpace.rotation.eulerAngles.y:F1}°");
        }
    }
}
