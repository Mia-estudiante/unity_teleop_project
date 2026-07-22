import Foundation
import Speech
import AVFoundation

// 이 파일은 Assets/Plugins/visionOS/ 에 두어 UnityFramework 타깃으로 컴파일된다.
// (SwiftAppSupport 폴더에 두면 안 됨 — DllImport("__Internal") 링크가 깨진다.)
//
// UnityFramework 안에 있으므로 import UnityFramework 는 불가능(자기 자신).
// 대신 UnityFramework 안에 존재하는 C 함수 UnitySendMessage 를 직접 호출한다.
@_silgen_name("UnitySendMessage")
private func UnitySendMessage(_ obj: UnsafePointer<CChar>?,
                              _ method: UnsafePointer<CChar>?,
                              _ msg: UnsafePointer<CChar>?)

@objc public class SpeechBridge: NSObject {
    static let shared = SpeechBridge()

    private let recognizer = SFSpeechRecognizer(locale: Locale(identifier: "en-US"))
    private var request: SFSpeechAudioBufferRecognitionRequest?
    private var task: SFSpeechRecognitionTask?
    private let audioEngine = AVAudioEngine()

    // Unity 쪽 GameObject 이름 (UnitySendMessage 타깃) — 씬의 GameObject 이름과 정확히 일치해야 함
    private let unityListenerObject = "SpeechManager"
    private let unityPartialMethod  = "OnPartialResult"
    private let unityFinalMethod    = "OnFinalResult"
    private let unityErrorMethod    = "OnError"

    // MARK: - Unity로 메시지 전송 (항상 메인 스레드에서)
    private func sendToUnity(_ method: String, _ message: String) {
        DispatchQueue.main.async {
            self.unityListenerObject.withCString { obj in
                method.withCString { mth in
                    message.withCString { msg in
                        UnitySendMessage(obj, mth, msg)
                    }
                }
            }
        }
    }

    // MARK: - 권한 요청 (마이크 + 음성 인식)
    private func requestAuth(_ completion: @escaping (Bool) -> Void) {
        SFSpeechRecognizer.requestAuthorization { status in
            AVAudioApplication.requestRecordPermission { micGranted in
                completion(status == .authorized && micGranted)
            }
        }
    }

    // MARK: - 시작
    func start() {
        print("[SpeechBridge] start() called")
        requestAuth { [weak self] ok in
            print("[SpeechBridge] auth result: \(ok)")
            DispatchQueue.main.async {
                guard let self = self else { return }
                guard ok else {
                    self.sendToUnity(self.unityErrorMethod, "permission_denied")
                    return
                }
                do {
                    try self.startRecognition()
                } catch {
                    print("[SpeechBridge] startRecognition threw: \(error)")
                    self.sendToUnity(self.unityErrorMethod,
                                    "start_failed: \(error.localizedDescription)")
                }
            }
        }
    }

    // MARK: - 정지 (메인 스레드 보장)
    func stop() {
        if !Thread.isMainThread {
            DispatchQueue.main.async { [weak self] in self?.stop() }
            return
        }
        audioEngine.stop()
        audioEngine.inputNode.removeTap(onBus: 0)
        request?.endAudio()
        task?.finish()
        request = nil
        task = nil
    }

    // MARK: - 인식 시작
    private func startRecognition() throws {
        print("[SpeechBridge] startRecognition() entered")
        task?.cancel()
        task = nil

        let session = AVAudioSession.sharedInstance()
        // ★ visionOS 친화적 설정으로 변경
        try session.setCategory(.playAndRecord, mode: .default, options: [.duckOthers, .defaultToSpeaker])
        try session.setActive(true, options: .notifyOthersOnDeactivation)
        print("[SpeechBridge] audio session ready")

        let req = SFSpeechAudioBufferRecognitionRequest()
        req.shouldReportPartialResults = true
        if #available(visionOS 1.0, *) {
            req.requiresOnDeviceRecognition = false
        }
        self.request = req

        let inputNode = audioEngine.inputNode
        let format = inputNode.outputFormat(forBus: 0)
        print("[SpeechBridge] input format: \(format)")
        inputNode.installTap(onBus: 0, bufferSize: 1024, format: format) { buffer, _ in
            req.append(buffer)
        }

        audioEngine.prepare()
        try audioEngine.start()
        print("[SpeechBridge] audioEngine started")

        task = recognizer?.recognitionTask(with: req) { [weak self] result, error in
            guard let self = self else { return }

            if let result = result {
                let text = result.bestTranscription.formattedString
                print("[SpeechBridge] result: '\(text)' final=\(result.isFinal)")
                let method = result.isFinal ? self.unityFinalMethod : self.unityPartialMethod
                self.sendToUnity(method, text)
            }
            if let error = error {
                print("[SpeechBridge] recognition error: \(error)")
            }
            if error != nil || (result?.isFinal ?? false) {
                self.stop()
            }
        }
        print("[SpeechBridge] recognitionTask created")
    }
}

// MARK: - C 인터페이스 (Unity의 DllImport("__Internal")에서 호출)
@_cdecl("Speech_Start")
public func Speech_Start() {
    SpeechBridge.shared.start()
}

@_cdecl("Speech_Stop")
public func Speech_Stop() {
    SpeechBridge.shared.stop()
}