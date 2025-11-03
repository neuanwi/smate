using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using UnityEngine.Networking;
using System.Text.RegularExpressions;

public class ChatClient : MonoBehaviour
{
    public InputField inputField;   // 사용자 입력 필드
    public Text chatOutput;         // 출력 텍스트 UI

    // 서버 URL (프록시 서버 또는 직접 API 엔드포인트)
    public string serverUrl = "http://localhost:8080/gemini/simple";

    // 서버가 단순 문자열(plain text)을 기대하는지, 아니면 JSON 객체를 기대하는지 선택
    // 참고: 현재 Java 컨트롤러(GeminiSimpleController)는 @RequestBody String input을 사용하므로
    // plain text 바디를 보내는 것이 맞습니다.
    public bool serverExpectsJsonObject = false;
    public string jsonKey = "prompt"; // serverExpectsJsonObject가 true일 때 사용되는 키

    // 선택적 인증 헤더(예: "Bearer <token>" 전체를 직접 넣을 수 있음)
    // 빈 문자열이면 Authorization 헤더를 추가하지 않음
    public string authorizationHeader = "";

    // 요청 타임아웃(초)
    public int requestTimeoutSeconds = 30;

    void Start()
    {
        if (inputField != null)
        {
            inputField.onEndEdit.AddListener(OnInputEndEdit);
        }
        else
        {
            Debug.LogError("[ChatClient] inputField가 할당되지 않았습니다.");
        }

        if (chatOutput == null)
        {
            Debug.LogError("[ChatClient] chatOutput(Text)가 할당되지 않았습니다.");
        }
    }

    void OnDestroy()
    {
        if (inputField != null)
            inputField.onEndEdit.RemoveListener(OnInputEndEdit);
    }

    void Update()
    {
        // Reserved
    }

    // onEndEdit 이벤트로 엔터(또는 입력 종료) 시 호출됨
    private void OnInputEndEdit(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        OnSendButtonClicked();
    }

    public void OnSendButtonClicked()
    {
        string userMessage = inputField.text.Trim();
        if (string.IsNullOrEmpty(userMessage)) return;

        // UI에는 사용자 입력을 표시하지 않음 (요청 시 콘솔에만 로그)
        Debug.Log($"[Unity] 입력 메시지: {userMessage}");

        // Clear input field
        inputField.text = "";
        inputField.ActivateInputField();

        // Clear previous chat output so next response replaces it
        if (chatOutput != null)
        {
            chatOutput.text = "";
        }

        // 코루틴 호출
        Debug.Log("[Unity] ✅ 코루틴 실행 직전");
        StartCoroutine(SendMessageToServer(userMessage));
        Debug.Log("[Unity] ✅ 코루틴 실행 호출 완료");
    }

    IEnumerator SendMessageToServer(string message)
    {
        Debug.Log("[Unity] 🚀 SendMessageToServer 진입");

        string payload;
        string contentType;

        if (serverExpectsJsonObject)
        {
            // JSON 객체로 전송하는 경우
            string escaped = EscapeForJson(message);
            payload = $"{{\"{jsonKey}\":\"{escaped}\"}}";
            contentType = "application/json";
        }
        else
        {
            // Java 컨트롤러가 @RequestBody String input을 사용하므로 plain text로 전송
            payload = message;
            contentType = "text/plain; charset=utf-8";
        }

        Debug.Log("[Unity] 🛰 요청 시작: " + serverUrl + " | Payload: " + payload);

        using (UnityWebRequest req = new UnityWebRequest(serverUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", contentType);

            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                req.SetRequestHeader("Authorization", authorizationHeader);
                Debug.Log("[Unity] Authorization 헤더 설정됨");
            }

            req.timeout = requestTimeoutSeconds;

            // 요청 전송
            yield return req.SendWebRequest();

            Debug.Log("[Unity] 🌐 요청 완료, 결과: " + req.result);

            if (req.result == UnityWebRequest.Result.Success)
            {
                string responseText = req.downloadHandler.text;
                Debug.Log("[Unity] ✅ 응답 수신 (raw): " + responseText);

                // Try to extract meaningful text fields from JSON response for clearer console output
                string extracted = ExtractTextFromJson(responseText);
                // Clean tokens in both extracted and raw before display
                if (!string.IsNullOrEmpty(extracted))
                {
                    extracted = CleanModelToken(extracted);
                    Debug.Log("[Unity] 🔎 Extracted Gemini text (clean): " + extracted);
                    if (chatOutput != null) chatOutput.text = $"Gemini: {extracted}\n";
                }
                else
                {
                    Debug.Log("[Unity] ⚠️ 텍스트 필드 추출 실패, raw 응답 출력 (clean) ");
                    string cleaned = CleanModelToken(responseText);
                    if (chatOutput != null) chatOutput.text = $"Gemini: {cleaned}\n";
                }
            }
            else
            {
                string errorMsg = req.error;
#if UNITY_2020_1_OR_NEWER
                int responseCode = (int)req.responseCode;
                Debug.LogError($"[Unity] ❌ 요청 실패: {errorMsg} (HTTP {responseCode})");
#else
                Debug.LogError("[Unity] ❌ 요청 실패: " + errorMsg);
#endif
                // UI에는 실패 메시지 대신 아무것도 추가하지 않음
            }
        }
    }

    // 아주 간단한 JSON 문자열 이스케이프 (큰따옴표, 역슬래시, 줄바꿈 등 처리)
    private string EscapeForJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (char.IsControl(c))
                        sb.AppendFormat("\\u{0:X4}", (int)c);
                    else
                        sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    // 간단한 JSON에서 "text" 필드들을 정규식으로 추출하여 합칩니다.
    private string ExtractTextFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return "";

        // 모든 "text": "..." 패턴을 찾아 결합
        var matches = Regex.Matches(json, "\\\"text\\\"\\s*:\\s*\\\"(.*?)\\\"", RegexOptions.Singleline);
        if (matches.Count > 0)
        {
            var sb = new StringBuilder();
            foreach (Match m in matches)
            {
                if (m.Groups.Count > 1)
                {
                    string part = m.Groups[1].Value;
                    // 간단한 이스케이프 시퀀스 처리
                    part = part.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
                    sb.Append(part);
                }
            }
            return sb.ToString().Trim();
        }

        // 다른 키 이름(예: "outputText")가 있을 수 있으니 그 패턴도 시도
        var m2 = Regex.Match(json, "\\\"outputText\\\"\\s*:\\s*\\\"(.*?)\\\"", RegexOptions.Singleline);
        if (m2.Success && m2.Groups.Count > 1)
        {
            string outp = m2.Groups[1].Value;
            outp = outp.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\\"", "\"").Replace("\\\\", "\\");
            return outp.Trim();
        }

        return "";
    }

    // Remove trailing ".model" or standalone "model" tokens and clean punctuation/extra spaces
    private string CleanModelToken(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        string cleaned = s;
        // remove common ASCII ".model" and standalone model
        cleaned = Regex.Replace(cleaned, "\\.model", "", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, "\\bmodel\\b", "", RegexOptions.IgnoreCase);
        // remove variants like fullwidth dot + model, and any trailing punctuation/spaces
        cleaned = Regex.Replace(cleaned, @"[\.。．·･・\u2024\uFE52\uFF0E]?model[\,\.\:\;\s]*", "", RegexOptions.IgnoreCase);
        // collapse multiple spaces
        cleaned = Regex.Replace(cleaned, "\\s{2,}", " ");
        cleaned = cleaned.Trim();
        // trim trailing punctuation
        cleaned = cleaned.TrimEnd(',', '.', ';', ':');
        return cleaned;
    }
}