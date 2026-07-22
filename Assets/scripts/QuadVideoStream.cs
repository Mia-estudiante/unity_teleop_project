using System;
using System.Collections.Concurrent;
using NativeWebSocket;
using UnityEngine;

public class QuadVideoStream : MonoBehaviour
{
    [Header("Connection")]
    [SerializeField] private string serverUrl = "ws://147.47.124.81:8765/viewer/head";

    [Header("Display")]
    [SerializeField] private Renderer quadRenderer;
    [SerializeField] private string mainTextureProperty = "_MainTex"; // 셰이더의 텍스처 프로퍼티
    [SerializeField] private int initialWidth = 640;
    [SerializeField] private int initialHeight = 480;

    private WebSocket _ws;
    private Texture2D _streamTexture;
    private readonly ConcurrentQueue<byte[]> _incoming = new ConcurrentQueue<byte[]>();
    private byte[] _latestFrameBytes;
    private bool _streamingActive = true;

    public bool IsConnected => _ws != null && _ws.State == WebSocketState.Open;
    public byte[] LatestFrameBytes => _latestFrameBytes;

    private async void Start()
    {
        _streamTexture = new Texture2D(initialWidth, initialHeight, TextureFormat.RGB24, false);
        if (quadRenderer != null && quadRenderer.material != null)
        {
            if (quadRenderer.material.HasProperty(mainTextureProperty))
                quadRenderer.material.SetTexture(mainTextureProperty, _streamTexture);
            else
                quadRenderer.material.mainTexture = _streamTexture;
        }

        _ws = new WebSocket(serverUrl);
        _ws.OnOpen    += ()  => Debug.Log($"[Stream] connected: {serverUrl}");
        _ws.OnError   += (e) => Debug.LogError($"[Stream] error: {e}");
        _ws.OnClose   += (e) => Debug.Log($"[Stream] closed: {e}");
        _ws.OnMessage += OnMessageBytes;

        try { await _ws.Connect(); }
        catch (Exception e) { Debug.LogError($"[Stream] connect failed: {e.Message}"); }
    }

    private void OnMessageBytes(byte[] bytes)
    {
        // sender는 JPEG 바이너리만 보냄
        _incoming.Enqueue(bytes);
    }

    private void Update()
    {
        #if !UNITY_WEBGL || UNITY_EDITOR
        _ws?.DispatchMessageQueue();
        #endif

        byte[] latest = null;
        while (_incoming.TryDequeue(out var f)) latest = f;

        if (latest != null)
        {
            _latestFrameBytes = latest;       // click_init 전송용으로는 항상 저장

            if (_streamingActive)             // 스트리밍 활성 시에만 텍스처 갱신
            {
                try { _streamTexture.LoadImage(latest); }
                catch (Exception e) { Debug.LogError($"[Stream] decode: {e.Message}"); }
            }
        }
    }

    /// <summary> 핀치 좌표 + 최신 프레임으로 click_init 전송 </summary>
    public async void SendClickInit(Vector2 pixel)
    {
        if (!IsConnected)          { Debug.LogWarning("[Stream] not connected"); return; }
        if (_latestFrameBytes == null) { Debug.LogWarning("[Stream] no frame yet"); return; }

        string base64 = Convert.ToBase64String(_latestFrameBytes);
        string json = $"{{\"type\":\"click_init\"," +
                      $"\"frame\":\"{base64}\"," +
                      $"\"points\":[[{pixel.x:F0},{pixel.y:F0}]]," +
                      $"\"labels\":[1]}}";

        try
        {
            await _ws.SendText(json);
            Debug.Log($"[Stream] click_init sent pixel=({pixel.x:F0},{pixel.y:F0}) " +
                      $"frame={_latestFrameBytes.Length}B");
        }
        catch (Exception e) { Debug.LogError($"[Stream] send: {e.Message}"); }
    }

    public async void SendReset()
    {
        if (!IsConnected) return;
        try { await _ws.SendText("{\"type\":\"reset\"}"); }
        catch (Exception e) { Debug.LogError($"[Stream] reset: {e.Message}"); }
    }

    private async void OnDestroy()
    {
        if (_ws != null)
        {
            _ws.OnMessage -= OnMessageBytes;
            try { await _ws.Close(); } catch { }
        }
    }
    public async void SendTextInit(string text)
    {
        if (!IsConnected)              { Debug.LogWarning("[Stream] not connected"); return; }
        if (_latestFrameBytes == null) { Debug.LogWarning("[Stream] no frame yet"); return; }
        if (string.IsNullOrWhiteSpace(text)) { Debug.LogWarning("[Stream] empty text"); return; }

        string base64 = Convert.ToBase64String(_latestFrameBytes);
        string escaped = JsonEscape(text);
        string json = $"{{\"type\":\"find\"," +
                    $"\"label\":\"{escaped}\"," +
                    $"\"frame\":\"{base64}\"}}";

        try
        {
            await _ws.SendText(json);
            Debug.Log($"[Stream] find sent label=\"{text}\" frame={_latestFrameBytes.Length}B");
        }
        catch (Exception e) { Debug.LogError($"[Stream] send: {e.Message}"); }
    }

    private static string JsonEscape(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t");
    }

    public void PauseReceiving()
    {
        _streamingActive = false;
        Debug.Log("[Stream] paused");
    }

    public void ResumeReceiving()
    {
        _streamingActive = true;
        Debug.Log("[Stream] resumed");
    }
}