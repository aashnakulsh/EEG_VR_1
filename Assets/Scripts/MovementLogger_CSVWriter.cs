using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

public class MovementLogger_CSVWriter : MonoBehaviour
{
    // NOTE: is not static (like the other two logger files) because this works with a specific tracked GameObject
    [SerializeField] private GameObject trackedObject;
    [SerializeField] private float interval = 0.2f;
    private float timer = 0.0f;

    private string filePath;

    void Start()
    {
        if (trackedObject == null)
        {
            Debug.LogError("Tracked object wasnt assigned");
            enabled = false;
            return;
        }

        // string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSV");
        // This filepath is specific to my laptop; if using another computer then change filepath appropriately
        //string folderPath = @"C:\Users\aashn\Documents\CSV";
        string folderPath = @"C:\Users\swapn\OneDrive\Documents\CSV";       //Sid's Laptop

        Directory.CreateDirectory(folderPath);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(folderPath, timestamp + "_MovementLog.csv");

        // Writes header
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
