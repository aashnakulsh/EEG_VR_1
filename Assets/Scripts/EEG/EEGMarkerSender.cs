// // EEGMarkerSender.cs
// // Unity 2021+
// // Opens a serial port to Arduino and sends lines "code,pulseMs\n"
// // to trigger an opto-isolated pulse on the EEG BNC.
// //
// // Public API:
// //   void SendMarker(int code, int pulseMs)
// //     - code:    int32 scalar, [0..255] typical
// //     - pulseMs: int32 scalar, recommended 5..20 ms
// //
// // Expected inputs (types):
// //   portName: string scalar (e.g., "COM5", "/dev/tty.usbmodem14101")
// //   baudRate: int32 scalar (e.g., 115200)
// //
// // Outputs:
// //   Writes ASCII lines to serial; no Unity return values.
// //   Arduino echoes "OK,<code>,<pulse>" for logging.
// //
// // Notes:
// //   - Don�t block the main thread. We use a ConcurrentQueue + background writer.
// //   - Closes port cleanly on quit / disable.

// using System;
// using System.IO.Ports;
// using System.Collections.Concurrent;
// using System.Threading;
// using UnityEngine;

// public class EEGMarkerSender : MonoBehaviour
// {
//     [Header("Serial Settings")]
//     [Tooltip("Windows: COM3/COM5/etc.  macOS: /dev/tty.usbmodem*")]
//     public string portName = "COM3";
//     public int baudRate = 115200;

//     [Header("Defaults")]
//     [Tooltip("Default pulse width in ms if not specified")]
//     public int defaultPulseMs = 5;

//     SerialPort _port;
//     Thread _writerThread;
//     readonly ConcurrentQueue<string> _outbox = new();
//     volatile bool _running;

//     void Awake()
//     {
//         TryOpen();
//     }

//     void TryOpen()
//     {
//         try
//         {
//             _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
//             _port.NewLine = "\n";
//             _port.DtrEnable = false; // avoid spurious resets; flip to true if your board needs it
//             _port.RtsEnable = false;
//             _port.ReadTimeout = 1;
//             _port.WriteTimeout = 1;
//             _port.Open();
//             _running = true;
//             _writerThread = new Thread(WriterLoop) { IsBackground = true };
//             _writerThread.Start();

//             // Optional: detect Arduino ready banner
//             Enqueue("PING"); // Arduino replies "PONG"
//         }
//         catch (Exception e)
//         {
//             foreach (var p in System.IO.Ports.SerialPort.GetPortNames())
//                 Debug.Log($"[EEGMarkerSender] Detected port: {p}");
//             Debug.LogError($"[EEGMarkerSender] Failed to open {portName} @ {baudRate}: {e.Message}");
//         }
//     }

//     void WriterLoop()
//     {
//         while (_running)
//         {
//             try
//             {
//                 if (_port != null && _port.IsOpen)
//                 {
//                     if (_outbox.TryDequeue(out string msg))
//                     {
//                         _port.WriteLine(msg);
//                     }

//                     // Optional: read brief echoes without blocking
//                     try
//                     {
//                         string line = _port.ReadLine(); // will timeout quickly
//                         if (!string.IsNullOrEmpty(line))
//                             Debug.Log($"[EEGMarkerSender] {line.Trim()}");
//                     }
//                     catch (TimeoutException) { /* ignore */ }
//                 }
//             }
//             catch (Exception e)
//             {
//                 Debug.LogWarning($"[EEGMarkerSender] WriterLoop error: {e.Message}");
//                 Thread.Sleep(1);
//             }
//         }
//     }

//     public void SendMarker(int code, int pulseMs)
//     {
//         // Input expectations:
//         //   code: int32 scalar [0..255]
//         //   pulseMs: int32 scalar [>=1]
//         if (pulseMs < 1) pulseMs = 1;
//         if (pulseMs > 1000) pulseMs = 1000;
//         code = Mathf.Clamp(code, 0, 255);
//         Enqueue($"{code},{pulseMs}");
//     }

//     public void SendMarker(int code)
//     {
//         SendMarker(code, defaultPulseMs);
//     }

//     void Enqueue(string msg)
//     {
//         if (_port == null || !_port.IsOpen)
//         {
//             Debug.LogWarning("[EEGMarkerSender] Port not open; dropping message.");
//             return;
//         }
//         _outbox.Enqueue(msg);
//     }

//     void OnApplicationQuit() { Cleanup(); }
//     void OnDisable() { Cleanup(); }

//     void Cleanup()
//     {
//         _running = false;
//         try { _writerThread?.Join(50); } catch { }
//         if (_port != null)
//         {
//             try { if (_port.IsOpen) _port.Close(); } catch { }
//             _port.Dispose();
//             _port = null;
//         }
//     }

//     // Example test: press Space to send marker 101 at defaultPulseMs
//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.Space))
//             SendMarker(101);
//     }
// }
