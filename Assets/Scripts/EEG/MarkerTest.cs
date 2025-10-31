//using UnityEngine;
//using System.IO;
//using System.Diagnostics;
//using UnityEngine.InputSystem;

//public class MarkerTest : MonoBehaviour
//{
//    // === CSV logging ===
//    private string csvPath;        // full path to CSV
//    private StreamWriter csv;      // writer handle
//    private Stopwatch sw;          // high-res Unity-side clock
//    private readonly object csvLock = new object();


//    void OnEnable()
//    {
//        csvPath = Path.Combine(Application.persistentDataPath, "marker_log.csv");
//        bool exists = File.Exists(csvPath);
//        csv = new StreamWriter(csvPath, append: true);
//        if (!exists)
//            csv.WriteLine("iso8601,unity_ms,src,event,code,t1,t2,t3,raw"); // structured header
//        sw = Stopwatch.StartNew();

//        // Application.logMessageReceived += HandleLog;
//        Application.logMessageReceivedThreaded += HandleLogThreaded; // NEW
//        UnityEngine.Debug.LogError($"CSV logging to: {csvPath}");
//    }

//    void OnDisable()
//    {
//        // Application.logMessageReceived -= HandleLog;
//        Application.logMessageReceivedThreaded -= HandleLogThreaded; // NEW
//        csv?.Flush();
//        csv?.Dispose();
//        csv = null;
//    }

//    private void HandleLogThreaded(string condition, string stackTrace, LogType type)
//    {
//        HandleLog(condition, stackTrace, type);
//    }

//    // Console -> CSV tap (parses Arduino lines into structured columns)
//    // Console -> CSV tap (parses Arduino lines into structured columns)
//    private void HandleLog(string condition, string stackTrace, LogType type)
//    {
//        if (csv == null) return;

//        string iso = System.DateTime.UtcNow.ToString("o");
//        double unityMs = sw != null ? sw.Elapsed.TotalMilliseconds : (double)System.Environment.TickCount;

//        // Normalize payload and remove a leading "[...]" tag if present
//        string payload = condition.Replace("\r", " ").Replace("\n", " ").Trim();
//        if (payload.Length > 0 && payload[0] == '[')
//        {
//            int close = payload.IndexOf(']');
//            if (close > 0 && close + 1 < payload.Length)
//                payload = payload.Substring(close + 1).TrimStart();
//        }

//        // Treat lines that start with Arduino tokens as Arduino; everything else = Unity
//        string src = (payload.StartsWith("PARSED") ||
//                      payload.StartsWith("RISE") ||
//                      payload.StartsWith("FALL") ||
//                      payload.StartsWith("DONE")) ? "Arduino" : "Unity";

//        string ev = "";
//        string code = "";
//        string t1 = "";
//        string t2 = "";
//        string t3 = "";

//        if (src == "Arduino")
//        {
//            // Arduino emits: PARSED,code,t_us | RISE,code,t1,t2 | FALL,code,t_us | DONE,code,width_ms,duration_us
//            var parts = payload.Split(',');
//            if (parts.Length >= 1) { ev = parts[0].Trim(); }
//            if (parts.Length >= 2) { code = parts[1].Trim(); }
//            if (parts.Length >= 3) { t1 = parts[2].Trim(); }
//            if (parts.Length >= 4) { t2 = parts[3].Trim(); }
//            if (parts.Length >= 5) { t3 = parts[4].Trim(); }
//            if (string.IsNullOrEmpty(ev)) ev = "RAW";   // tolerate partial lines
//        }
//        else
//        {
//            ev = type.ToString(); // Unity log type
//        }

//        string raw = payload.Replace(",", ";");

//        // Thread-safe write (handles main-thread + threaded callbacks)
//        lock (csvLock)
//        {
//            csv.WriteLine($"{iso},{unityMs:F3},{src},{ev},{code},{t1},{t2},{t3},{raw}");
//            csv.Flush();
//        }
//    }


//    void Update()
//    {
//        // Space bar: quick single marker (code 7, width 10 ms)
//        //if (Input.GetKeyDown(KeyCode.Space))
//        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
//        {
//            FindObjectOfType<EEGMarkerSender>()?.SendMarker(7, 10);
//            UnityEngine.Debug.Log("Marker 7 sent!");
//        }

//        // 'T' key: run full test sequence
//        //if (Input.GetKeyDown(KeyCode.T))
//        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
//            StartCoroutine(TestSequence());
//    }

//    // Helper: send, log SEND, wait for pulse to end, log DONE, then add an extra gap
//    private System.Collections.IEnumerator SendAndLog(EEGMarkerSender sender, int code, int widthMs, float extraGapSec = 1f)
//    {
//        sender.SendMarker(code, widthMs);
//        UnityEngine.Debug.Log($"TEST send: code={code} width_ms={widthMs}");
//        yield return new WaitForSeconds(widthMs / 1000f);  // wait until pulse duration elapses
//        UnityEngine.Debug.Log($"TEST done: code={code} width_ms={widthMs}");
//        if (extraGapSec > 0f)
//            yield return new WaitForSeconds(extraGapSec);  // spacing before next event
//    }

//    // Test plan: 5s, 10s, 15s pulses, then five 1s pulses
//    private System.Collections.IEnumerator TestSequence()
//    {
//        UnityEngine.Debug.LogError("TEST sequence STARTED");
//        var sender = FindObjectOfType<EEGMarkerSender>();
//        if (sender == null) { UnityEngine.Debug.LogError("EEGMarkerSender not found"); yield break; }

//        // small settle time
//        yield return new WaitForSeconds(0.5f);
//        UnityEngine.Debug.LogError("TEST sequence SETTEL TIME DONE");
//        // Long pulses (1 s gap after each)
//        yield return SendAndLog(sender, 5, 5000, 1f);   // 5 s + 1 s gap
//        yield return SendAndLog(sender, 10, 10000, 1f); // 10 s + 1 s gap
//        yield return SendAndLog(sender, 15, 15000, 1f); // 15 s + 1 s gap
 
//        // Five � 1 s pulses (0.5 s gap so they�re distinct but compact)
//        for (int i = 0; i < 5; i++)
//            yield return SendAndLog(sender, 1, 1000, 0.5f);

//        UnityEngine.Debug.LogError("TEST sequence complete");
//    }
//}

using UnityEngine;
using System.IO;
using System.Diagnostics;
using UnityEngine.InputSystem;


public class MarkerTest : MonoBehaviour
{
    // === CSV logging ===
    private string csvPath;        // full path to CSV
    private StreamWriter csv;      // writer handle
    private Stopwatch sw;          // high-res Unity-side clock
    private readonly object csvLock = new object();


    void OnEnable()
    {
        csvPath = Path.Combine(Application.persistentDataPath, "marker_log.csv");
        bool exists = File.Exists(csvPath);
        csv = new StreamWriter(csvPath, append: true);
        if (!exists)
            csv.WriteLine("iso8601,unity_ms,src,event,code,t1,t2,t3,raw"); // structured header
        sw = Stopwatch.StartNew();

        // Application.logMessageReceived += HandleLog;
        Application.logMessageReceivedThreaded += HandleLogThreaded; // NEW
        UnityEngine.Debug.Log($"CSV logging to: {csvPath}");
    }

    void OnDisable()
    {
        // Application.logMessageReceived -= HandleLog;
        Application.logMessageReceivedThreaded -= HandleLogThreaded; // NEW
        csv?.Flush();
        csv?.Dispose();
        csv = null;
    }

    private void HandleLogThreaded(string condition, string stackTrace, LogType type)
    {
        HandleLog(condition, stackTrace, type);
    }

    // Console -> CSV tap (parses Arduino lines into structured columns)
    // Console -> CSV tap (parses Arduino lines into structured columns)
    private void HandleLog(string condition, string stackTrace, LogType type)
    {
        if (csv == null) return;

        string iso = System.DateTime.UtcNow.ToString("o");
        double unityMs = sw != null ? sw.Elapsed.TotalMilliseconds : (double)System.Environment.TickCount;

        // Normalize payload and remove a leading "[...]" tag if present
        string payload = condition.Replace("\r", " ").Replace("\n", " ").Trim();
        if (payload.Length > 0 && payload[0] == '[')
        {
            int close = payload.IndexOf(']');
            if (close > 0 && close + 1 < payload.Length)
                payload = payload.Substring(close + 1).TrimStart();
        }

        // Treat lines that start with Arduino tokens as Arduino; everything else = Unity
        string src = (payload.StartsWith("PARSED") ||
                      payload.StartsWith("RISE") ||
                      payload.StartsWith("FALL") ||
                      payload.StartsWith("DONE")) ? "Arduino" : "Unity";

        string ev = "";
        string code = "";
        string t1 = "";
        string t2 = "";
        string t3 = "";

        if (src == "Arduino")
        {
            // Arduino emits: PARSED,code,t_us | RISE,code,t1,t2 | FALL,code,t_us | DONE,code,width_ms,duration_us
            var parts = payload.Split(',');
            if (parts.Length >= 1) { ev = parts[0].Trim(); }
            if (parts.Length >= 2) { code = parts[1].Trim(); }
            if (parts.Length >= 3) { t1 = parts[2].Trim(); }
            if (parts.Length >= 4) { t2 = parts[3].Trim(); }
            if (parts.Length >= 5) { t3 = parts[4].Trim(); }
            if (string.IsNullOrEmpty(ev)) ev = "RAW";   // tolerate partial lines
        }
        else
        {
            ev = type.ToString(); // Unity log type
        }

        string raw = payload.Replace(",", ";");

        // Thread-safe write (handles main-thread + threaded callbacks)
        lock (csvLock)
        {
            csv.WriteLine($"{iso},{unityMs:F3},{src},{ev},{code},{t1},{t2},{t3},{raw}");
            csv.Flush();
        }
    }


    void Update()
    {
        // Space bar: quick single marker (code 7, width 10 ms)
        //if (Input.GetKeyDown(KeyCode.Space))
        if (Keyboard.current != null && Keyboard.current.aKey.wasPressedThisFrame)
        {
            FindObjectOfType<EEGMarkerSender>()?.SendMarker(7, 10);
            UnityEngine.Debug.Log("Marker 7 sent!");
        }

        // 'T' key: run full test sequence
        //if (Input.GetKeyDown(KeyCode.T))
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
        StartCoroutine(TestSequence());
        }  

    // Helper: send, log SEND, wait for pulse to end, log DONE, then add an extra gap
    private System.Collections.IEnumerator SendAndLog(EEGMarkerSender sender, int code, int widthMs, float extraGapSec = 1f)
    {
        sender.SendMarker(code, widthMs);
        UnityEngine.Debug.Log($"TEST send: code={code} width_ms={widthMs}");
        yield return new WaitForSeconds(widthMs / 1000f);  // wait until pulse duration elapses
        UnityEngine.Debug.Log($"TEST done: code={code} width_ms={widthMs}");
        if (extraGapSec > 0f)
            yield return new WaitForSeconds(extraGapSec);  // spacing before next event
    }

    // Test plan: 5s, 10s, 15s pulses, then five 1s pulses
    private System.Collections.IEnumerator TestSequence()
    {
        UnityEngine.Debug.LogError("TEST sequence started");
        var sender = FindObjectOfType<EEGMarkerSender>();
        if (sender == null) { UnityEngine.Debug.LogError("EEGMarkerSender not found"); yield break; }

        // small settle time
        yield return new WaitForSeconds(0.5f);
        UnityEngine.Debug.LogError("TEST settle time is over");
        // Long pulses (1 s gap after each)
        yield return SendAndLog(sender, 5, 5000, 1f);   // 5 s + 1 s gap
        yield return SendAndLog(sender, 10, 10000, 1f); // 10 s + 1 s gap
        yield return SendAndLog(sender, 15, 15000, 1f); // 15 s + 1 s gap

        // Five × 1 s pulses (0.5 s gap so they’re distinct but compact)
        for (int i = 0; i < 5; i++)
            yield return SendAndLog(sender, 1, 1000, 0.5f);

        UnityEngine.Debug.LogError("TEST sequence complete");
    }
}

