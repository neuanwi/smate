using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;
using System;                       // 👈 JsonUtility용
using System.Text.RegularExpressions; // 👈 "(117자)" 꼬리표 제거용

public class ChatManager : MonoBehaviour
{

    public AlarmManager alarmManager;

    [Header("Backend Settings")]
    public string backendBaseUrl = "http://localhost:8080/gemini/simple"; // 백엔드 주소
    public string currentSessionId = "unityUser001";                      // 세션 ID
    public string currentPersonaDomain = "tsundere";                       // 도메인(페르소나)

    [Header("UI References")]
    public GameObject chatPanel;
    public TMP_InputField messageInputField;
    public TextMeshProUGUI requestText;      // ✅ 내 질문이 표시될 텍스트 (Request ScrollView의 Content 안)
    public TextMeshProUGUI chatLogText;      // ✅ AI 답이 표시될 텍스트 (ChatLog ScrollView의 Content 안)
    public ScrollRect requestScrollRect;     // ✅ Request용 ScrollRect
    public ScrollRect chatLogScrollRect;     // ✅ ChatLog용 ScrollRect
    public Button closeButton;
    public GameObject backgroundClickCatcher; // (선택) 배경 클릭 가로채기

    private int _lastSubmitFrame = -1;

    // 타자 효과 제어
    [Header("Typewriter Settings")]
    [Tooltip("AI 답변 글자당 지연(초). 예: 0.02f")]
    public float charDelay = 0.02f;
    private Coroutine typingCoroutine;

    void Start()
    {
        if (chatPanel != null) chatPanel.SetActive(false);
        if (backgroundClickCatcher != null) backgroundClickCatcher.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseChatPanel);

        // 한 줄 입력 & 엔터로 제출
        if (messageInputField != null)
        {
            messageInputField.lineType = TMP_InputField.LineType.SingleLine;

            // 엔터 눌렀을 때 콜백 (TMP는 onSubmit 사용 가능 / onEndEdit도 백업으로 연결)
            messageInputField.onSubmit.AddListener(_ => OnSend());
            messageInputField.onEndEdit.AddListener(text =>
            {
                // IME/포커스 상황에 따라 onSubmit이 안 올 수도 있어서 백업 처리
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnSend();
            });
        }

        // ✅ AlarmManager 자동 연결 (Inspector에서 비어 있으면 씬에서 자동으로 찾기)
        if (alarmManager == null)
        {
            alarmManager = FindFirstObjectByType<AlarmManager>();

            if (alarmManager == null)
            {
                Debug.LogError("[ChatManager] AlarmManager를 씬에서 찾을 수 없습니다. " +
                               "Hierarchy에 AlarmManager 오브젝트를 추가하거나, Inspector에 직접 할당하세요.");
            }
        }

    }

    void Update()
    {
        // 포커스 된 상태에서 Enter/KeypadEnter 로 전송
        if (messageInputField != null && messageInputField.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            OnSend();
        }

        // 포커스 없을 때 Enter 누르면 포커스 주기
        if (messageInputField != null && !messageInputField.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            messageInputField.ActivateInputField();
        }
    }

    public void OpenChatPanel()
    {
        if (chatPanel != null) chatPanel.SetActive(true);
        if (backgroundClickCatcher != null) backgroundClickCatcher.SetActive(true);
        if (messageInputField != null) messageInputField.ActivateInputField();
    }

    public void CloseChatPanel()
    {
        if (chatPanel != null) chatPanel.SetActive(false);
        if (backgroundClickCatcher != null) backgroundClickCatcher.SetActive(false);
    }

    public void ChangePersona(string newPersonaDomain)
    {
        currentPersonaDomain = newPersonaDomain;
        Debug.Log($"[ChatManager] Persona changed: {currentPersonaDomain}");
    }

    // === 전송 메인 로직 ===
    private void OnSend()
    {
        // ✅ 동일 프레임 중복 전송 방지
        if (_lastSubmitFrame == Time.frameCount) return;
        _lastSubmitFrame = Time.frameCount;

        if (messageInputField == null) return;
        var message = messageInputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        SetTextAndScroll(requestText, requestScrollRect, message);

        messageInputField.text = "";

        // ✅ 전송 후 바로 포커스 유지 (엔터 연타 UX)
        messageInputField.ActivateInputField();
        messageInputField.caretPosition = 0;
        messageInputField.selectionAnchorPosition = 0;
        messageInputField.selectionFocusPosition = 0;

        StartCoroutine(SendChatMessage(message));
    }

    IEnumerator SendChatMessage(string message)
    {
        string url = $"{backendBaseUrl}?sessionId={currentSessionId}&domain={currentPersonaDomain}";
        Debug.Log($"[ChatManager] 요청: {url}");

        // AI 영역은 로딩 표시로 먼저 덮어쓰기
        SetTextAndScroll(chatLogText, chatLogScrollRect, "… 응답 생성 중 …");

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(message);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");

        yield return www.SendWebRequest();

        Debug.Log($"[ChatManager] 응답 상태: {www.result}, 코드: {www.responseCode}");
        Debug.Log($"[ChatManager] 본문: {www.downloadHandler.text}");

        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError)
        {
            SetTextAndScroll(chatLogText, chatLogScrollRect, $"서버 오류: {www.error}");
        }
        else
        {
            string response = www.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(response))
            {
                SetTextAndScroll(chatLogText, chatLogScrollRect, "서버에서 빈 응답을 받았습니다.");
            }
            else
            {
                // ✅ JSON에서 텍스트만 추출
                string onlyText = ExtractGeminiText(response);

                // 추출 실패 시 원문을 보여주고 싶지 않다면 아래 한 줄을 에러 문구로 교체 가능
                string toShow = string.IsNullOrWhiteSpace(onlyText) ? response : onlyText;

                // ✅ 누적 대신 덮어쓰기 + 타자 효과로 출력
                TypewriterTo(chatLogText, chatLogScrollRect, toShow, charDelay);

                // ✅ 알람 기능 추가 위치
                if (message.Contains("알람") || message.Contains("깨워") || message.Contains("설정"))
                {
                    alarmManager.TryCreateAlarmFromMessage(message);
                }
            }
        }
    }

    // === 헬퍼들 ===

    // 텍스트를 즉시 덮어쓰기 + 스크롤 하단 고정
    private void SetTextAndScroll(TextMeshProUGUI target, ScrollRect rect, string text)
    {
        if (target == null) return;

        // 기존 타이핑 중이면 중단
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        target.text = text;
        StartCoroutine(ScrollToBottom(rect));
    }

    // 타자치는 효과로 텍스트 덮어쓰기
    private void TypewriterTo(TextMeshProUGUI target, ScrollRect rect, string fullText, float delayPerChar)
    {
        if (target == null) return;

        // 이전 타이핑 중이면 취소
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        typingCoroutine = StartCoroutine(TypeRoutine(target, rect, fullText, delayPerChar));
    }

    private IEnumerator TypeRoutine(TextMeshProUGUI target, ScrollRect rect, string fullText, float delayPerChar)
    {
        target.text = "";
        yield return null;

        for (int i = 0; i < fullText.Length; i++)
        {
            target.text += fullText[i];
            // 줄 바꿈/길이 증가 시 계속 아래로 유지
            if (rect != null) rect.verticalNormalizedPosition = 0f;

            yield return new WaitForSeconds(delayPerChar);
        }

        // 마지막에 한 번 더 바닥 고정
        yield return ScrollToBottom(rect);
        typingCoroutine = null;
    }

    private IEnumerator ScrollToBottom(ScrollRect rect)
    {
        if (rect == null) yield break;
        // 레이아웃 갱신 후 스크롤
        yield return new WaitForEndOfFrame();
        rect.verticalNormalizedPosition = 0f;
    }

    // ====== ▼▼▼ JSON 파서 & 텍스트 추출 ▼▼▼ ======

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
    private class Part { public string text; }

    /// <summary>
    /// 응답 JSON에서 candidates[0].content.parts[0].text만 추출.
    /// 끝의 "(117자)" 같은 꼬리표는 제거.
    /// </summary>
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

            // "(117자)" 같은 꼬리표 제거
            text = Regex.Replace(text, @"\s*\(\d+자\)\s*$", "");

            return text;
        }
        catch
        {
            // 파싱 실패 시 null 반환 -> 호출부에서 원문 fallback
            return null;
        }
    }
}
