using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Lightweight runtime logger for board debugging.
/// Writes JSONL (one JSON event per line) to Application.persistentDataPath.
/// </summary>
public sealed class BoardDebugLogger
{
    [Serializable]
    public class BoardDebugEvent
    {
        public int stepIndex;
        public string eventType;
        public string timestamp;
        public string boardState;
        public Dictionary<string, string> extra;
    }

    private static BoardDebugLogger _instance;
    public static BoardDebugLogger Instance => _instance ??= new BoardDebugLogger();

    private int _stepIndex;
    private readonly string _filePath;
    private readonly StringBuilder _buffer = new StringBuilder(32 * 1024);

    private BoardDebugLogger()
    {
        var sessionId = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        _filePath = Path.Combine(Application.persistentDataPath, $"boardlog_{sessionId}.jsonl");
        Debug.Log($"[BoardDebugLogger] Logging to: {_filePath}");
    }

    /// <summary>
    /// Log an event with a pre-built boardState string.
    /// </summary>
    public void Log(string eventType, string boardState, Dictionary<string, string> extra = null)
    {
        if (!BoardDebugConfig.IsEnabled)
            return;

        var e = new BoardDebugEvent
        {
            stepIndex = _stepIndex++,
            eventType = eventType,
            timestamp = DateTime.UtcNow.ToString("O"),
            boardState = boardState,
            extra = extra ?? new Dictionary<string, string>()
        };

        var json = JsonUtility.ToJson(e);
        _buffer.AppendLine(json);

        if (_buffer.Length > 16 * 1024)
        {
            Flush();
        }
    }

    /// <summary>
    /// Convenience overload that serializes the current grid first.
    /// </summary>
    public void LogBoard(string eventType, TileData[,] data, int width, int height, Dictionary<string, string> extra = null)
    {
        var boardState = BoardSnapshot.Serialize(data, width, height);
        Log(eventType, boardState, extra);
    }

    /// <summary>
    /// Flush any buffered events to disk.
    /// </summary>
    public void Flush()
    {
        if (_buffer.Length == 0)
            return;

        try
        {
            File.AppendAllText(_filePath, _buffer.ToString());
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BoardDebugLogger] Failed to write log file: {ex}");
        }
        finally
        {
            _buffer.Length = 0;
        }
    }

    public string GetFilePath()
    {
        return _filePath;
    }
}

