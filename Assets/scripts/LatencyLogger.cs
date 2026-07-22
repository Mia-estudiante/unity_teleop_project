// LatencyLogger.cs
// Unity 측 latency 측정 utility.
// 
// 사용법:
//   1. 빈 GameObject 만들고 이 스크립트를 컴포넌트로 추가.
//   2. 다른 스크립트에서: LatencyLogger.Instance?.Log("stage_name", tSourceMs);
//
// Output: Application.persistentDataPath/unity_latency_YYYYMMDD_HHMMSS.csv
// - Windows: %USERPROFILE%\AppData\LocalLow\<company>\<product>\
// - Mac:     ~/Library/Application Support/<company>/<product>/
// - Linux:   ~/.config/unity3d/<company>/<product>/
//
// Columns: stage, t_source_ms, t_event_ms, latency_ms

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class LatencyLogger : MonoBehaviour
{
    public static LatencyLogger Instance { get; private set; }

    private List<(string stage, double tSourceMs, double tEventMs)> records
        = new List<(string, double, double)>();
    private string outPath;
    private readonly object lockObj = new object();
    private const int FLUSH_EVERY = 100;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        outPath = Path.Combine(Application.persistentDataPath,
                               $"unity_latency_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        File.WriteAllText(outPath, "stage,t_source_ms,t_event_ms,latency_ms\n");

        Debug.Log($"[LatencyLogger] CSV → {outPath}");
    }

    /// <summary>
    /// Stage 처리 시간을 측정할 때:
    ///   double t0 = LatencyLogger.NowMs();
    ///   // ... 처리 ...
    ///   LatencyLogger.Instance.Log("my_stage", t0);
    /// </summary>
    public void Log(string stage, double tSourceMs)
    {
        double tNow = NowMs();
        lock (lockObj)
        {
            records.Add((stage, tSourceMs, tNow));
            if (records.Count >= FLUSH_EVERY) FlushUnlocked();
        }
    }

    /// <summary>
    /// 현재 시각 (ms 단위, double precision).
    /// </summary>
    public static double NowMs()
    {
        return Time.realtimeSinceStartupAsDouble * 1000.0;
    }

    void Flush()
    {
        lock (lockObj) { FlushUnlocked(); }
    }

    void FlushUnlocked()
    {
        if (records.Count == 0) return;
        using (var w = new StreamWriter(outPath, append: true))
        {
            foreach (var r in records)
                w.WriteLine($"{r.stage},{r.tSourceMs},{r.tEventMs},{r.tEventMs - r.tSourceMs}");
        }
        records.Clear();
    }

    void OnApplicationQuit() => Flush();
    void OnDisable() => Flush();
}