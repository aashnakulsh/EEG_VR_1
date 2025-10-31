using UnityEngine;
using System.IO;
using System;

public class TrialLogger_CSVWriter : MonoBehaviour
{
    [SerializeField] public static string participantID;
    private static string filePath;

    public static void Init()
    {
        // string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSV");
        // This filepath is specific to my laptop; if using another computer then change filepath appropriately
        string folderPath = @"C:\Users\aashn\Documents\CSV";
        // string folderPath = @"C:\Users\swapn\OneDrive\Documents\CSV";       //Sid's Laptop
        Directory.CreateDirectory(folderPath);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(folderPath, timestamp + "_TrialLog.csv");

        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("Timestamp,PID,TrialNumber, TargetTrial (T/F), GhostCube, HitCube,TargetCube, Mismatch (T/F), ReactionTime");
        }
    }

    public static void LogTrial(int trialNumber, bool targetTrial, int ghostCube, int hitCube, int targetCube, bool mismatch, float reactionTime)
    {
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{time},{participantID},{trialNumber},{targetTrial}, {ghostCube}, {hitCube},{targetCube},{mismatch}, {reactionTime:F3}");
        }
    }
}
