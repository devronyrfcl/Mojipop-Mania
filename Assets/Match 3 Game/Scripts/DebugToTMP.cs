using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class DebugToTMP : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI logText;

    [Header("Settings")]
    public int maxLines = 2; // Only keep 2 lines

    private Queue<string> logQueue = new Queue<string>();

    private bool isActive = true; // Default active

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (!isActive) return; // Do nothing if disabled

        string logEntry = "";

        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                logEntry = $"<color=red>[ERROR]</color> {logString}";
                break;
            case LogType.Warning:
                logEntry = $"<color=yellow>[WARNING]</color> {logString}";
                break;
            default:
                logEntry = logString;
                break;
        }

        // Add new log to queue
        logQueue.Enqueue(logEntry);

        // Keep only maxLines
        while (logQueue.Count > maxLines)
        {
            logQueue.Dequeue();
        }

        // Update UI
        if (logText != null)
        {
            logText.text = string.Join("\n", logQueue.ToArray());
        }
    }

    // Toggle function
    public void ToggleLogActive()
    {
        isActive = !isActive;

        if (!isActive && logText != null)
        {
            logText.text = ""; // Clear text when disabling
        }
    }
}
