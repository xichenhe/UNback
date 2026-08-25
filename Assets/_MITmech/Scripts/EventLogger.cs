using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Minimal event logger singleton. The original MITmech EventLogger was not
/// included in the _MITmech export; callers use EventLogger.Instance?.Log(...),
/// so logging silently no-ops when no instance exists in the scene.
/// </summary>
public class EventLogger : MonoBehaviour
{
    public static EventLogger Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Log(string eventType, string message = null, Dictionary<string, object> data = null)
    {
        var sb = new StringBuilder();
        sb.Append("[EventLogger] ").Append(eventType);
        if (!string.IsNullOrEmpty(message))
        {
            sb.Append(" | ").Append(message);
        }
        if (data != null)
        {
            foreach (KeyValuePair<string, object> entry in data)
            {
                sb.Append(" | ").Append(entry.Key).Append('=').Append(entry.Value);
            }
        }
        Debug.Log(sb.ToString());
    }
}
