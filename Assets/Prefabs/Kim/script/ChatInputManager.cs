using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ChatInputManager : MonoBehaviour
{
    [Header("필수 연결 요소")]
    public TMP_InputField questionInputField;
    public PopupSpawner popupSpawner;
    public UIAnimator inputFieldAnimator;
    public GameObject backgroundClickCatcher;

    [Header("백엔드 설정")]
    public string backendBaseUrl = "http://localhost:8080/gemini/simple";
    public string currentSessionId = "unityUser001";
    public string currentPersonaDomain = "tsundere";

    [Header("캐릭터 페르소나 설정")]
    public GameObject kirbyCharacter;
    public string kirbyPersonaName = "kirby";
    public GameObject shihoCharacter;
    public string shihoPersonaName = "tsundere";

    private PopupController _activePopup;
    private const string DEFAULT_GREETING = "안녕하세요! 무엇이든 물어보세요.";
    private string _lastConversationText = DEFAULT_GREETING;
    private string _lastActivePersonaDomain = "";

    void Start()
    {
        if (inputFieldAnimator != null)
            inputFieldAnimator.HideUI();

        if (backgroundClickCatcher != null)
            backgroundClickCatcher.SetActive(false);
    }

    public void OpenChatFlow()
    {
        if (inputFieldAnimator != null)
            inputFieldAnimator.ShowUI();

        if (backgroundClickCatcher != null)
            backgroundClickCatcher.SetActive(true);

        string activePersona = ResolveActivePersona();

        if (!string.IsNullOrEmpty(_lastActivePersonaDomain) &&
            _lastActivePersonaDomain != activePersona)
        {
            _lastConversationText = DEFAULT_GREETING;
        }

        if (_lastConversationText == DEFAULT_GREETING)
        {
            if (activePersona == kirbyPersonaName)
                _lastConversationText = "하이! 뭐 물어볼 거야?";
            else if (activePersona == shihoPersonaName)
                _lastConversationText = "흐음... 뭘 물어볼 건데?";
        }

        _lastActivePersonaDomain = activePersona;

        if (popupSpawner != null)
        {
            _activePopup = popupSpawner.ShowPopupNearTarget();
            if (_activePopup != null)
                _activePopup.SetText(_lastConversationText);
        }

        if (questionInputField != null)
            questionInputField.ActivateInputField();
    }

    public void OnSubmitQuestion()
    {
        if (questionInputField == null || _activePopup == null)
            return;

        string question = questionInputField.text;
        if (string.IsNullOrWhiteSpace(question))
            return;

        questionInputField.text = "";
        questionInputField.ActivateInputField();

        // 🔎 여기서 스크린샷 키워드 먼저 확인
        if (IsScreenshotCommand(question))
        {
            StartCoroutine(CaptureAndSendWithExplain(question));
        }
        else
        {
            StartCoroutine(HandleBackendResponse(question));
        }
    }

    // ─────────────────────────────────────────
    // 스크린샷 키워드 감지
    private bool IsScreenshotCommand(string msg)
    {
        msg = msg.ToLower();
        return msg.Contains("스크린샷") ||
               msg.Contains("screenshot") ||
               msg.Contains("캡쳐") ||
               msg.Contains("화면 찍");
    }

    // 스크린샷 찍어서 with-image로 보내기
    private IEnumerator CaptureAndSendWithExplain(string userMessage)
    {
        if (_activePopup != null)
            _activePopup.SetText("스크린샷 찍는 중...");

        yield return new WaitForEndOfFrame();

        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        if (tex == null)
        {
            if (_activePopup != null)
                _activePopup.SetText("스크린샷 캡처 실패");
            yield break;
        }

        Debug.Log($"[ChatInputManager] captured tex: {tex.width}x{tex.height}");

        byte[] pngData = tex.EncodeToPNG();
        Debug.Log($"[ChatInputManager] png size: {pngData.Length} bytes");

        UnityEngine.Object.Destroy(tex);

        yield return StartCoroutine(SendScreenshotRequest(userMessage, pngData));
    }

    private IEnumerator SendScreenshotRequest(string userMessage, byte[] pngData)
    {
        string activePersona = ResolveActivePersona();

        string url = $"{backendBaseUrl}/with-image?sessionId={currentSessionId}&domain={activePersona}";
        Debug.Log("[ChatInputManager] screenshot POST url = " + url);

        WWWForm form = new WWWForm();
        form.AddField("message", userMessage);
        form.AddBinaryData("screenshot", pngData, "capture.png", "image/png");

        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();

            Debug.Log($"[ChatInputManager] screenshot resp code={www.responseCode}, err={www.error}");
            Debug.Log($"[ChatInputManager] screenshot body={www.downloadHandler.text}");

            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                if (_activePopup != null)
                    _activePopup.SetText("서버 오류(이미지): " + www.error);
                yield break;
            }

            string raw = www.downloadHandler.text;
            BackendResponse parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<BackendResponse>(raw);
            }
            catch { }

            string finalText;
            if (parsed != null && !string.IsNullOrWhiteSpace(parsed.reply))
            {
                finalText = parsed.reply;

                if (parsed.task != null &&
                    !string.IsNullOrWhiteSpace(parsed.task.time) &&
                    !string.IsNullOrWhiteSpace(parsed.task.text))
                {
                    var alarmManager = FindObjectOfType<AlarmManager>();
                    if (alarmManager != null)
                        alarmManager.SaveAlarm(parsed.task.time, parsed.task.text);
                }
            }
            else
            {
                finalText = raw;
            }

            (string cleanedText, string detectedEmotion) = CleanAndDetectEmotion(finalText);
            finalText = cleanedText;

            if (_activePopup != null)
                _activePopup.SetText(finalText);

            _lastConversationText = finalText;
            _lastActivePersonaDomain = activePersona;
        }
    }
    // ─────────────────────────────────────────

    private IEnumerator HandleBackendResponse(string question)
    {
        if (_activePopup != null)
            _activePopup.SetText("생각 중...");

        string activePersona = ResolveActivePersona();
        string url = $"{backendBaseUrl}?sessionId={currentSessionId}&domain={activePersona}";
        Debug.Log($"[ChatInputManager] 요청: {url}");

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(question);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");

        yield return www.SendWebRequest();

        Debug.Log($"[ChatInputManager] 응답 상태: {www.result}, 코드: {www.responseCode}");
        Debug.Log($"[ChatInputManager] 응답 본문: {www.downloadHandler.text}");

        string finalText;

        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError)
        {
            finalText = "서버 오류: " + www.error;
        }
        else
        {
            string raw = www.downloadHandler.text;
            BackendResponse parsed = null;
            try { parsed = JsonUtility.FromJson<BackendResponse>(raw); } catch { }

            if (parsed != null && !string.IsNullOrWhiteSpace(parsed.reply))
            {
                finalText = parsed.reply;

                if (parsed.task != null &&
                    !string.IsNullOrWhiteSpace(parsed.task.time) &&
                    !string.IsNullOrWhiteSpace(parsed.task.text))
                {
                    Debug.Log($"[ALARM from backend] time={parsed.task.time}, task={parsed.task.text}");
                    var alarmManager = FindObjectOfType<AlarmManager>();
                    if (alarmManager != null)
                        alarmManager.SaveAlarm(parsed.task.time, parsed.task.text);
                }
            }
            else
            {
                string onlyText = ExtractGeminiText(raw);
                finalText = string.IsNullOrWhiteSpace(onlyText) ? raw : onlyText;
            }

            (string cleanedText, string detectedEmotion) = CleanAndDetectEmotion(finalText);
            finalText = cleanedText;

            if (_activePopup != null)
            {
                _activePopup.SetText(finalText);
                if (!string.IsNullOrEmpty(detectedEmotion))
                    Debug.Log($"[감정 감지됨] {detectedEmotion}");
            }
        }

        _lastConversationText = finalText;
        _lastActivePersonaDomain = activePersona;
    }

    public void CloseChatFlow()
    {
        if (inputFieldAnimator != null)
            inputFieldAnimator.HideUI();

        if (backgroundClickCatcher != null)
            backgroundClickCatcher.SetActive(false);

        if (_activePopup != null)
        {
            Destroy(_activePopup.gameObject);
            _activePopup = null;
        }
    }

    public void OnCharacterSwitched()
    {
        _lastConversationText = DEFAULT_GREETING;
        if (_activePopup != null)
            CloseChatFlow();
    }

    // 어떤 캐릭터가 켜져있는지 판단
    private string ResolveActivePersona()
    {
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
            return kirbyPersonaName;

        if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
            return shihoPersonaName;

        return currentPersonaDomain; // 기본
    }

    // --- Gemini JSON 텍스트 뽑기 (기존 방식) ---
    private static string ExtractGeminiText(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            var root = JsonUtility.FromJson<GeminiRoot>(json);
            var text = root?.candidates != null && root.candidates.Length > 0
                ? root.candidates[0]?.content?.parts != null && root.candidates[0].content.parts.Length > 0
                    ? root.candidates[0].content.parts[0]?.text
                    : null
                : null;

            if (string.IsNullOrWhiteSpace(text)) return null;

            // "(117자)" 꼬리표 제거
            text = Regex.Replace(text, @"\s*\(\d+자\)\s*$", "");
            return text;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 텍스트를 정리하고 감정 태그를 감지합니다.
    /// (숫자자) 꼬리표를 제거하고, (감정) 태그도 감지 후 제거합니다.
    /// </summary>
    /// <param name="src">원본 텍스트</param>
    /// <returns>(정리된 텍스트, 감지된 감정(없으면 null))</returns>
    private (string cleaned, string emotion) CleanAndDetectEmotion(string src)
    {
        if (string.IsNullOrWhiteSpace(src)) return (src, null);

        // ① 문장 끝의 "(숫자자)" 꼬리표 제거 (예: "(200자)", "(117자)")
        string temp = Regex.Replace(src, @"\s*\(\d+자\)\s*$", "");

        // ② 문장 끝의 감정 태그 감지: (기쁨|슬픔|보통|화남)
        var m = Regex.Match(temp, @"\((기쁨|슬픔|보통|화남)\)\s*$");
        string emotion = null;
        string cleaned = temp; // 기본값은 (숫자자)만 제거된 텍스트

        if (m.Success)
        {
            // ⭐️ 감정이 감지되면
            emotion = m.Groups[1].Value;
            // ⭐️ 텍스트에서도 (감정) 태그 부분을 제거합니다.
            cleaned = temp.Substring(0, m.Index).TrimEnd();
        }

        return (cleaned, emotion);
    }

    // =====================
    // 백엔드 응답용 구조체
    // =====================
    [Serializable]
    private class BackendResponse
    {
        public string reply; // 대화 텍스트
        public BackendTask task; // 알람이 있으면 채워짐
    }

    [Serializable]
    private class BackendTask
    {
        public string time;
        public string text;
    }

    // =====================
    // Gemini 원형 파싱용 구조체
    // =====================
    [Serializable]
    private class GeminiRoot { public Candidate[] candidates; }

    [Serializable]
    private class Candidate
    {
        public Content content;
        public string finishReason;
        public int index;
    }

    [Serializable]
    private class Content
    {
        public Part[] parts;
        public string role;
    }

    [Serializable]
    private class Part
    {
        public string text;
    }
}
