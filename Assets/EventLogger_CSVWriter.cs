using UnityEngine;
using System.IO;
using System;

public class EventLogger_CSVWriter : MonoBehaviour
{
    private static string filePath;
    private static bool isInitialized = false;

    // void Start()
    // {
    //     Init();
    // }

    public static void Init()
    {
        if (isInitialized) return;

        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSV");
        Directory.CreateDirectory(folderPath);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(folderPath, timestamp + "_EventLog.csv");

        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("Time,Event");
        }

        isInitialized = true;
        Debug.LogWarning("✅ Event Logger initialized at: " + filePath);
    }

    public static void Log(string eventDescription)
    {
        if (!isInitialized)
        {
            Debug.LogError("❌ Event logger not initialized. Call Init() first.");
            return;
        }

        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");


        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{time},{eventDescription}");
        }

        Debug.Log("📝 Logged event: " + eventDescription);
    }
}
