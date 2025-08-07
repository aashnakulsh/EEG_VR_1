using UnityEngine;
using System.IO;
using System;

public class TrialLogger : MonoBehaviour
{
    [SerializeField] public string participantID;
    private string filePath;

    public void Init()
    {
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CSV");
        Directory.CreateDirectory(folderPath);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        filePath = Path.Combine(folderPath, timestamp + "_TrialLog.csv");

        using (StreamWriter writer = new StreamWriter(filePath, false))
        {
            writer.WriteLine("Timestamp,PID,TrialNumber, TargetTrial (T/F), GhostCube, HitCube,TargetCube, Mismatch (T/F), ReactionTime");
        }
    }

    public void LogTrial(int trialNumber, bool targetTrial, int ghostCube, int hitCube, int targetCube, bool mismatch, float reactionTime)
    {
        string time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            writer.WriteLine($"{time},{participantID},{trialNumber},{targetTrial}, {ghostCube}, {hitCube},{targetCube},{mismatch}, {reactionTime:F3}");
        }
    }
}
