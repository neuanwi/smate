using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using TMPro;
using System;
using System.Text.RegularExpressions;

public class ChatManager : MonoBehaviour
{
    [Header("Backend Settings")]
    public string backendBaseUrl = "http://localhost:8080/gemini/simple";
    public string currentSessionId = "unityUser001";
    public string currentPersonaDomain = "tsundere";

    [Header("UI References")]
    public GameObject chatPanel;
    public TMP_InputField messageInputField;
    public TextMeshProUGUI requestText;
    public TextMeshProUGUI chatLogText;
    public ScrollRect requestScrollRect;
    public ScrollRect chatLogScrollRect;
    public Button closeButton;
    public GameObject backgroundClickCatcher;

    [Header("Typewriter Settings")]
    public float charDelay = 0.02f;
    private Coroutine typingCoroutine;
    private int _lastSubmitFrame = -1;

    void Start()
    {
        if (chatPanel != null) chatPanel.SetActive(false);
        if (backgroundClickCatcher != null) backgroundClickCatcher.SetActive(false);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseChatPanel);

        if (messageInputField != null)
        {
            messageInputField.lineType = TMP_InputField.LineType.SingleLine;
            messageInputField.onSubmit.AddListener(_ => OnSend());
            messageInputField.onEndEdit.AddListener(text =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    OnSend();
            });
        }
    }

    void Update()
    {
        if (messageInputField != null && messageInputField.isFocused &&
            (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
        {
            OnSend();
        }

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

    // ================= 전송 =================
    private void OnSend()
    {
        if (_lastSubmitFrame == Time.frameCount) return;
        _lastSubmitFrame = Time.frameCount;

        if (messageInputField == null) return;

        var message = messageInputField.text;
        if (string.IsNullOrWhiteSpace(message)) return;

        SetTextAndScroll(requestText, requestScrollRect, message);

        messageInputField.text = "";
        messageInputField.ActivateInputField();
        messageInputField.caretPosition = 0;

        StartCoroutine(SendChatMessage(message));
    }

    IEnumerator SendChatMessage(string message)
    {
        string url = $"{backendBaseUrl}?sessionId={currentSessionId}&domain={currentPersonaDomain}";
        Debug.Log($"[ChatManager] 요청 보내는 중: {url}");

        SetTextAndScroll(chatLogText, chatLogScrollRect, "… 응답 생성 중 …");

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(message);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError)
        {
            SetTextAndScroll(chatLogText, chatLogScrollRect, $"서버 오류: {www.error}");
            yield break;
        }

        string response = www.downloadHandler.text;
        Debug.Log($"[ChatManager] 서버 원본 응답(JSON): {response}");

        ChatResponseDto dto = null;
        try
        {
            dto = JsonUtility.FromJson<ChatResponseDto>(response);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ChatManager] JSON 파싱 실패 → 원문 표시: {e.Message}");
            SetTextAndScroll(chatLogText, chatLogScrollRect, response);
            yield break;
        }

        // ✅ reply는 UI에 출력
        if (dto != null && !string.IsNullOrEmpty(dto.reply))
        {
            TypewriterTo(chatLogText, chatLogScrollRect, dto.reply, charDelay);
        }
        else
        {
            SetTextAndScroll(chatLogText, chatLogScrollRect, "(응답 없음)");
        }

        if (dto != null && dto.task != null && !string.IsNullOrEmpty(dto.task.time))
        {
            Debug.Log($"[SERVER ALARM] 시간: {dto.task.time} / 내용: {dto.task.text}");
            AlarmManager.Instance?.SaveAlarm(dto.task.time, dto.task.text);
        }

    }

    // ================= 헬퍼 함수 =================
    private void SetTextAndScroll(TextMeshProUGUI target, ScrollRect rect, string text)
    {
        if (target == null) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        target.text = text;
        StartCoroutine(ScrollToBottom(rect));
    }

    private void TypewriterTo(TextMeshProUGUI target, ScrollRect rect, string fullText, float delayPerChar)
    {
        if (target == null) return;

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
            if (rect != null) rect.verticalNormalizedPosition = 0f;
            yield return new WaitForSeconds(delayPerChar);
        }

        yield return ScrollToBottom(rect);
        typingCoroutine = null;
    }

    private IEnumerator ScrollToBottom(ScrollRect rect)
    {
        if (rect == null) yield break;
        yield return new WaitForEndOfFrame();
        rect.verticalNormalizedPosition = 0f;
    }

    // ====== 백엔드 JSON 응답 DTO ======
    [Serializable]
    private class ChatResponseDto
    {
        public string reply;
        public TaskDto task;
    }

    [Serializable]
    private class TaskDto
    {
        public string time;
        public string text;
    }
}
