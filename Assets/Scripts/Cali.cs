using UnityEngine;
using Unity.Mathematics;

public class Calibrate_UseFriendsLogic : MonoBehaviour
{
    [Header("Calibration")]
    public Transform controllerTip;           // tip you poke with
    public Transform virtualSpace;            // EMPTY root we move/rotate (friend's virtualSpace)
    public Transform anchor;                  // child of virtualSpace at local (0,0,+1)
//      [SerializeField] private MeshRenderer tabletopRenderer;  
// [   SerializeField] private float tableHeight = 0.74f;  
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
        if (!calibrating && OVRInput.GetDown(beginButton, whichController))
            Begin();

        if (calibrating && OVRInput.GetDown(recordButton, whichController))
            Capture();
    }

    void Begin()
    {
        if (!controllerTip || !virtualSpace || !anchor || vPoints[0]==null || vPoints[1]==null || vPoints[2]==null)
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
            Debug.Log("--------- Calibration ---------");
            Debug.Log("Click order with trigger: 0=Front-Left, 1=Front-Right, 2=Back-Left");
            Debug.Log("--------------------------------------");
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

        if (verbose) Debug.Log($"[Calib] Captured real point {idx-1} @ {p}");

        if (idx == 3)
        {
            Calibrate();            
            calibrating = false;
            if (verbose) Debug.Log("[Calib] ✅ Done.");
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


            // gpt: 
            // --- pack the 3 correspondence points as COLUMNS [x; z; 1] ---
            // REAL (rMatrix): columns = r0, r1, r2
            // rMatrix.c0 = new float3(rPoints[0].position.x, rPoints[0].position.z, 1f);
            // rMatrix.c1 = new float3(rPoints[1].position.x, rPoints[1].position.z, 1f);
            // rMatrix.c2 = new float3(rPoints[2].position.x, rPoints[2].position.z, 1f);

            // // VIRTUAL (vMatrix): columns = v0, v1, v2
            // vMatrix.c0 = new float3(vPoints[0].position.x, vPoints[0].position.z, 1f);
            // vMatrix.c1 = new float3(vPoints[1].position.x, vPoints[1].position.z, 1f);
            // vMatrix.c2 = new float3(vPoints[2].position.x, vPoints[2].position.z, 1f);
        }

        // Solve affine in XZ: rotationMatrix = r * inverse(v)
        float3x3 rotationMatrix = math.mul(rMatrix, math.inverse(vMatrix));   // :contentReference[oaicite:5]{index=5}

        // sMatrix holds TWO rows we care about: current virtualSpace pos and anchor pos (friend did this)
        // Row0 = virtualSpace (x,z,1), Row1 = anchor (x,z,1)
        float3x3 sMatrix = new float3x3();
        sMatrix[0][0] = virtualSpace.position.x;
        sMatrix[0][1] = virtualSpace.position.z;
        sMatrix[0][2] = 1f;

        sMatrix[1][0] = anchor.position.x;
        sMatrix[1][1] = anchor.position.z;
        sMatrix[1][2] = 1f;                                         // :contentReference[oaicite:6]{index=6}


        // sMatrix columns = [virtualSpacePos, anchorPos, (unused)]
        // gpt:
        // sMatrix.c0 = new float3(virtualSpace.position.x, virtualSpace.position.z, 1f);
        // sMatrix.c1 = new float3(anchor.position.x,       anchor.position.z,       1f);
        // sMatrix.c2 = new float3(0f, 0f, 1f); // unused but keeps matrix well-formed



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

        if (verbose)
        {
            Debug.Log($"[Calib] rotationMatrix:\n{rotationMatrix}");
            Debug.Log($"[Calib] virtualSpace -> {virtualSpace.position}, yaw -> {virtualSpace.rotation.eulerAngles.y:F1}°");
        }
    }
}
