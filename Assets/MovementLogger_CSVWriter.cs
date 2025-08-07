// using UnityEngine;
// using System.Collections.Generic;
// using System.IO;
// using System;

// public class CSVWriter : MonoBehaviour
// {
//     [SerializeField] private GameObject trackedObject; // Assign in Inspector
//     [SerializeField] private float interval = 0.1f;     // Seconds between data points
//     [SerializeField] private string filePrefix = "TrackingData_";

//     private float timer = 0.0f;
//     private string filePath;

//     private void Start()
//     {
//         if (trackedObject == null)
//         {
//             Debug.LogError("❌ Tracked object not assigned.");
//             enabled = false;
//             return;
//         }

//         // Create folder and unique timestamped filename
//         string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSV");
//         // string folderPath = Path.Combine(Application.persistentDataPath, "CSV");
//         Directory.CreateDirectory(folderPath);

//         string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH:mm:ss");
//         Debug.LogWarning(timestamp);
//         filePath = Path.Combine(folderPath, filePrefix + timestamp + ".csv");

//         Debug.LogWarning("OKKKK");
//         // Write header row
//         using (StreamWriter writer = new StreamWriter(filePath, false)) // overwrite if exists (very unlikely)
//         {
//             writer.WriteLine("Time,Position X,Position Y,Position Z,Rotation X,Rotation Y,Rotation Z");
//         }
//         Debug.LogWarning("AHOGAHOG");
//         Debug.LogWarning("THIS IS THE FILEPATH: " + filePath);

//         Debug.LogWarning("📄 CSV file created at: " + filePath);
//     }

//     private void Update()
//     {
//         timer += Time.deltaTime;
//         if (timer >= interval)
//         {
//             WriteDataRow();
//             timer = 0.0f;
//         }
//     }

//     private void WriteDataRow()
//     {
//         Transform t = trackedObject.transform;
//         Vector3 pos = t.position;
//         Vector3 rot = t.eulerAngles;

//         string[] row = new string[]
//         {
//             Time.time.ToString("F3"),
//             pos.x.ToString("F4"),
//             pos.y.ToString("F4"),
//             pos.z.ToString("F4"),
//             rot.x.ToString("F4"),
//             rot.y.ToString("F4"),
//             rot.z.ToString("F4")
//         };

//         using (StreamWriter writer = new StreamWriter(filePath, true)) // append mode
//         {
//             writer.WriteLine(string.Join(",", row));
//         }
//     }
// }


using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class MovementLogger_CSVWriter : MonoBehaviour
{
    [SerializeField] private GameObject trackedObject;
    [SerializeField] private float interval = 0.1f;
    private float timer = 0.0f;

    private string filePath;

    void Start()
    {
        if (trackedObject == null)
        {
            Debug.LogError("❌ Tracked object not assigned.");
            enabled = false;
            return;
        }

        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSV");
        Directory.CreateDirectory(folderPath);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(folderPath, timestamp + "_MovementLog.csv");

        // Write header
        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("Time,Position X,Position Y,Position Z,Rotation X,Rotation Y,Rotation Z");
        }


        Debug.LogWarning("CSV initialized at: " + filePath);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            AppendData(trackedObject);
            timer = 0.0f;
        }
    }

    void AppendData(GameObject obj)
    {
        string[] row = new string[7];
        row[0] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        // row[0] = Time.time.ToString("F3");
        row[1] = obj.transform.position.x.ToString("F4");
        row[2] = obj.transform.position.y.ToString("F4");
        row[3] = obj.transform.position.z.ToString("F4");
        row[4] = obj.transform.rotation.eulerAngles.x.ToString("F4");
        row[5] = obj.transform.rotation.eulerAngles.y.ToString("F4");
        row[6] = obj.transform.rotation.eulerAngles.z.ToString("F4");

        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine(string.Join(",", row));
        }
    }
}
