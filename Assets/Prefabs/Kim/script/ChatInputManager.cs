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
    public TMP_InputField questionInputField; // 질문 인풋
    public PopupSpawner popupSpawner;         // 팝업 생성기
    public UIAnimator inputFieldAnimator;     // 인풋 애니메이터

    [Header("배경 버튼 (닫기용)")]
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

    // 현재 떠 있는 팝업
    private PopupController _activePopup;

    // 마지막 대화 텍스트
    private const string DEFAULT_GREETING = "안녕하세요! 무엇이든 물어보세요.";
    private string _lastConversationText = DEFAULT_GREETING;

    // 마지막으로 대화한 페르소나
    private string _lastActivePersonaDomain = "";

    void Start()
    {
        if (inputFieldAnimator != null)
            inputFieldAnimator.HideUI();

        if (backgroundClickCatcher != null)
            backgroundClickCatcher.SetActive(false);
    }

    // 메인 채팅 버튼에서 호출
    public void OpenChatFlow()
    {
        if (inputFieldAnimator != null)
            inputFieldAnimator.ShowUI();

        if (backgroundClickCatcher != null)
            backgroundClickCatcher.SetActive(true);

        // 어떤 캐릭터가 켜져있는지 보고 도메인 결정
        string activePersona = ResolveActivePersona();

        // 캐릭터가 바뀌었으면 대화 리셋
        if (!string.IsNullOrEmpty(_lastActivePersonaDomain) &&
            _lastActivePersonaDomain != activePersona)
        {
            _lastConversationText = DEFAULT_GREETING;
        }

        // 기본 인사말 캐릭터별로 세팅
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

    // 인풋필드에서 엔터
    public void OnSubmitQuestion()
    {
        if (questionInputField == null || _activePopup == null)
            return;

        string question = questionInputField.text;
        if (string.IsNullOrWhiteSpace(question))
            return;

        questionInputField.text = "";
        questionInputField.ActivateInputField();

        StartCoroutine(HandleBackendResponse(question));
    }

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

            // 1) 백엔드가 우리가 말한 형태로 내려준 경우
            //    { "text": "...", "task": { "time": "...", "text": "..." } }
            BackendResponse parsed = null;
            try
            {
                parsed = JsonUtility.FromJson<BackendResponse>(raw);
            }
            catch
            {
                parsed = null;
            }

            if (parsed != null && !string.IsNullOrWhiteSpace(parsed.reply))
            {
                finalText = parsed.reply;

                // ✅ task가 있으면 AlarmManager에 저장
                if (parsed.task != null &&
                    !string.IsNullOrWhiteSpace(parsed.task.time) &&
                    !string.IsNullOrWhiteSpace(parsed.task.text))
                {
                    Debug.Log($"[ALARM from backend] time={parsed.task.time}, task={parsed.task.text}");

                    // AlarmManager로 전달하여 저장 (파일 + 메모리)
                    var alarmManager = FindObjectOfType<AlarmManager>();
                    if (alarmManager != null)
                        alarmManager.SaveAlarm(parsed.task.time, parsed.task.text);
                    else
                        Debug.LogWarning("[ChatInputManager] AlarmManager를 찾지 못함! 저장 실패");
                }
            }
            else
            {
                // 2) 기존 Gemini 원본 그대로 온 경우 → 텍스트만 뽑기
                string onlyText = ExtractGeminiText(raw);
                finalText = string.IsNullOrWhiteSpace(onlyText) ? raw : onlyText;
            }

        }

        if (_activePopup != null)
            _activePopup.SetText(finalText);

        _lastConversationText = finalText;
        _lastActivePersonaDomain = activePersona;
    }

    // 배경 클릭해서 닫기
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

    // 외부에서 캐릭터 바뀌었다고 알려줄 때
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
