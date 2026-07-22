using System;
using UnityEngine;
using NativeWebSocket;

public class QuadTrigger : MonoBehaviour
{
    public Renderer rend;

    [Header("Server")]
    public string serverUrl =
        "ws://147.47.124.81:8765/viewer/head";

    [Header("Initial texture size (sender 해상도 맞추면 좋음)")]
    public int initialWidth  = 640;
    public int initialHeight = 480;

    private WebSocket webSocket;
    private Material  mat;

    //-------------------------------------------------
    // Stable texture: material에 binding된 텍스처
    // - 한 번만 생성, 재할당 X
    // - 매 프레임 SetPixels32 + Apply로 내용물만 갈아끼움
    // - 이게 PolySpatial이 안정적으로 추적하는 경로
    //-------------------------------------------------
    private Texture2D videoTex2D;

    //-------------------------------------------------
    // Scratch texture: JPEG decode 임시 버퍼
    //-------------------------------------------------
    private Texture2D decodeTex;

    private byte[] pendingFrame = null;

    // Diagnostic counters
    private int   recvCount      = 0;
    private int   decodeCount    = 0;
    private int   decodeFailures = 0;
    private float lastSummaryT   = 0f;

    async void Start()
    {
        mat = rend.material;

        //-------------------------------------------------
        // Stable texture 미리 할당
        //-------------------------------------------------
        videoTex2D = new Texture2D(
            initialWidth,
            initialHeight,
            TextureFormat.RGBA32,
            false
        );
        videoTex2D.wrapMode   = TextureWrapMode.Clamp;
        videoTex2D.filterMode = FilterMode.Bilinear;

        mat.SetTexture("_MainTex", videoTex2D);

        //-------------------------------------------------
        // Scratch texture
        //-------------------------------------------------
        decodeTex = new Texture2D(
            2, 2,
            TextureFormat.RGBA32,
            false
        );

        //-------------------------------------------------
        // WebSocket
        //-------------------------------------------------
        webSocket = new WebSocket(serverUrl);

        webSocket.OnOpen  += ()   => Debug.Log("[ws] CONNECTED");
        webSocket.OnError += err  => Debug.LogError($"[ws] ERROR: {err}");
        webSocket.OnClose += code => Debug.Log($"[ws] CLOSED: {code}");

        webSocket.OnMessage += bytes =>
        {
            recvCount++;
            pendingFrame = bytes;

            if (recvCount <= 3 || recvCount % 30 == 0)
            {
                string first4 = "??";
                if (bytes != null && bytes.Length >= 4)
                {
                    first4 =
                        $"{bytes[0]:X2}{bytes[1]:X2}" +
                        $"{bytes[2]:X2}{bytes[3]:X2}";
                }
                Debug.Log(
                    $"[ws-recv] #{recvCount} " +
                    $"len={bytes?.Length ?? 0} " +
                    $"first4={first4}"
                );
            }
        };

        Debug.Log($"[ws] connecting to {serverUrl}");
        await webSocket.Connect();
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket?.DispatchMessageQueue();
#endif

        //-------------------------------------------------
        // 1초마다 상태 요약
        //-------------------------------------------------
        if (Time.time - lastSummaryT > 1.0f)
        {
            lastSummaryT = Time.time;
            Debug.Log(
                $"[stat] recv={recvCount} " +
                $"decode={decodeCount} " +
                $"fail={decodeFailures} " +
                $"tex={videoTex2D.width}x{videoTex2D.height}"
            );
        }

        if (pendingFrame == null) return;

        byte[] frame = pendingFrame;
        pendingFrame = null;

        //-------------------------------------------------
        // 1) JPEG 디코드 → scratch texture
        //-------------------------------------------------
        if (!decodeTex.LoadImage(frame, false))
        {
            decodeFailures++;
            if (decodeFailures <= 5)
            {
                Debug.LogError(
                    $"[decode] FAILED #{decodeFailures} " +
                    $"bytes={frame.Length}"
                );
            }
            return;
        }

        int w = decodeTex.width;
        int h = decodeTex.height;

        //-------------------------------------------------
        // 2) Stable texture size 맞추기
        // (sender 해상도 안 바뀌면 처음 한 번만 실행됨)
        //-------------------------------------------------
        if (videoTex2D.width != w || videoTex2D.height != h)
        {
            videoTex2D.Reinitialize(w, h);
            Debug.Log(
                $"[decode] stable tex reinitialized → {w}x{h}"
            );
        }

        //-------------------------------------------------
        // 3) Scratch → Stable 픽셀 복사 + 명시적 Apply
        //    이 Apply가 PolySpatial이 확실히 추적하는 dirty 신호
        //-------------------------------------------------
        videoTex2D.SetPixels32(decodeTex.GetPixels32());
        videoTex2D.Apply(false, false);

        //-------------------------------------------------
        // 4) 머티리얼 바인딩 보험
        //-------------------------------------------------
        mat.SetTexture("_MainTex", videoTex2D);

        decodeCount++;
        if (decodeCount <= 3 || decodeCount % 30 == 0)
        {
            Debug.Log(
                $"[decode] OK #{decodeCount} → {w}x{h}"
            );
        }
    }

    /// <summary>
    /// 음성 인식 / UI 버튼에서 호출
    /// 예: SendFindCommand("desk")
    /// </summary>
    public async void SendFindCommand(string label)
    {
        if (webSocket == null ||
            webSocket.State != WebSocketState.Open)
        {
            Debug.LogWarning($"[cmd] WS not open: {label}");
            return;
        }

        string json =
            $"{{\"type\":\"find\",\"label\":\"{label}\"}}";
        await webSocket.SendText(json);
        Debug.Log($"[cmd] sent: {json}");
    }

    private async void OnApplicationQuit()
    {
        if (webSocket != null) await webSocket.Close();

        if (videoTex2D != null)
        {
            Destroy(videoTex2D);
            videoTex2D = null;
        }

        if (decodeTex != null)
        {
            Destroy(decodeTex);
            decodeTex = null;
        }
    }
}