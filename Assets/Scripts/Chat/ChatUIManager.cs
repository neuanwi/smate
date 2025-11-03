using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking; // 웹 통신에 필요
using System.Collections;       // 코루틴에 필요
using System.Text;// UTF-8 인코딩에 필요
using TMPro;
using UnityEngine.UI;


public class ChatManager : MonoBehaviour
{
    public ScrollRect chatScrollRect;


    [Header("Backend Settings")]
    public string backendBaseUrl = "http://localhost:8080/gemini/simple"; // 백엔드 주소
    public string currentSessionId = "unityUser001"; // 고유 세션 ID (나중에 유저별로 다르게 설정)

    [Header("Current Persona")]
    public string currentPersonaDomain = "yandere"; // 얀데레, 츤데레 등 API의 'domain' 값

    [Header("UI References")]
    public GameObject chatPanel;
    public InputField messageInputField;
    public Button sendButton;
    public TextMeshProUGUI chatLogText;
    public Button closeButton;

    // (이 외에 채팅창을 여는 버튼은 1단계처럼 밖에서 별도로 연결)

    void Start()
    {
        // 시작할 때 채팅창 숨김
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }

        // 전송 버튼 눌렀을 때 실행할 함수 연결
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClick);
        }

        // 닫기 버튼 눌렀을 때 실행할 함수 연결
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseChatPanel);
        }
    }

    /// <summary>
    /// 채팅창 패널을 엽니다. (외부 버튼에서 호출)
    /// </summary>
    public void OpenChatPanel()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(true);
            AddMessageToLog("시스템", $"'{currentPersonaDomain}' 인격과 대화를 시작합니다.");
        }
    }

    /// <summary>
    /// 채팅창 패널을 닫습니다.
    /// </summary>
    public void CloseChatPanel()
    {
        if (chatPanel != null)
        {
            chatPanel.SetActive(false);
        }
    }

    /// <summary>
    /// (중요) 페르소나(domain)를 동적으로 변경하는 함수
    /// </summary>
    public void ChangePersona(string newPersonaDomain)
    {
        currentPersonaDomain = newPersonaDomain;
        Debug.Log($"페르소나 변경: {currentPersonaDomain}");
        // (선택) 페르소나 변경 시 채팅 로그 초기화
        // if(chatLogText != null) chatLogText.text = ""; 
    }

    /// <summary>
    /// 전송 버튼 클릭 시 호출되는 함수
    /// </summary>
    private void OnSendButtonClick()
    {
        string message = messageInputField.text;
        if (string.IsNullOrEmpty(message))
        {
            return; // 입력된 내용이 없으면 무시
        }

        // 사용자가 입력한 메시지를 로그에 먼저 추가
        AddMessageToLog("나", message);

        // 백엔드로 메시지 전송 요청
        StartCoroutine(SendChatMessage(message));

        // 입력창 비우기
        messageInputField.text = "";
    }

    /// <summary>
    /// 실제 백엔드로 통신하는 코루틴(Coroutine)
    /// </summary>
    IEnumerator SendChatMessage(string message)
    {
        string url = $"{backendBaseUrl}?sessionId={currentSessionId}&domain={currentPersonaDomain}";

        Debug.Log($"[ChatManager] 요청 시작: {url}");
        AddMessageToLog("시스템", "AI 응답 대기 중...");

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(message);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");

        yield return www.SendWebRequest();

        // ✅ 응답 결과 로깅
        Debug.Log($"[ChatManager] 응답 상태: {www.result}");
        Debug.Log($"[ChatManager] 응답 코드: {www.responseCode}");
        Debug.Log($"[ChatManager] 응답 본문: {www.downloadHandler.text}");

        if (www.result == UnityWebRequest.Result.ConnectionError ||
            www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError($"[ChatManager] 서버 오류: {www.error}");
            AddMessageToLog("시스템", $"서버 오류 발생: {www.error}");
        }
        else
        {
            string response = www.downloadHandler.text;
            if (string.IsNullOrWhiteSpace(response))
            {
                Debug.LogWarning("[ChatManager] 서버 응답이 비어 있음.");
                AddMessageToLog("시스템", "서버에서 응답이 없습니다.");
            }
            else
            {
                AddMessageToLog(currentPersonaDomain, response);
                AddMessageToLog("시스템", $"(응답 코드: {www.responseCode})");
            }
        }
    }


    /// <summary>
    /// 채팅 로그 UI에 메시지를 추가하는 헬퍼 함수
    /// </summary>
    private void AddMessageToLog(string user, string message)
    {
        if (chatLogText == null) return;

        chatLogText.text += $"<b>{user}</b>: {message}\n\n";

        StartCoroutine(ScrollToBottom());

        // (팁) 채팅 로그가 길어지면 스크롤이 자동으로 맨 아래로 가도록 처리하면 좋습니다.
        // ScrollRect가 있다면 : scrollRect.verticalNormalizedPosition = 0f;
    }
    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame(); // 다음 프레임까지 대기
        if (chatScrollRect != null)
            chatScrollRect.verticalNormalizedPosition = 0f;
    }
}