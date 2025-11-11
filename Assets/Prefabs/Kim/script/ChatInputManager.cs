using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// ⭐️ [추가됨] 스크린샷(P/Invoke)에 필요한 네임스페이스
using System.Runtime.InteropServices;
using System.Drawing; // 👈 [중요] System.Drawing.dll을 Assets 폴더에 추가해야 합니다!
using System.Drawing.Imaging;
using System.IO;


public class ChatInputManager : MonoBehaviour
{

    [Header("배경 스크린샷 설정")]
    [Tooltip("배경 스크린샷을 찍어 백엔드로 전송할 주기 (초)")]
    public float screenshotIntervalSeconds = 180f; // 3분 (3 * 60)


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

    // ⭐️ [추가] 이 값을 실제 사용하는 ID로 변경하세요 (예: "roy17-desktop")
    public string computerId = "roy17-desktop";

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

    // ⭐️ [신규] 시스템 스크린샷 응답(재밌는 말)을 저장할 변수
    private string _lastSystemRemark = "";




    void Start()
    {

        StartCoroutine(PeriodicScreenshotCoroutine());

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
        string url = $"{backendBaseUrl}?sessionId={currentSessionId}&domain={activePersona}&computerId={computerId}";
        Debug.Log($"[ChatInputManager] 요청: {url}");

        // 1. WWWForm 생성
        WWWForm form = new WWWForm();

        // 2. "question" 필드에 텍스트 추가
        form.AddField("question", question);

        byte[] screenshotBytes = null;

        // 3. "여기서" 키워드가 포함되면 스크린샷 캡처
        if (question.Contains("여기서"))
        {
            Debug.Log("[ChatInputManager] '여기서' 감지됨. 데스크탑 캡처 시도...");
            try
            {
                // ⭐️ (주의) 이 작업은 동기식이므로 캡처 동안 잠시 멈출 수 있습니다.
                screenshotBytes = DesktopCapture.CaptureDesktopAsPNG();

                if (screenshotBytes != null)
                {
                    // 4. "screenshot" 필드에 이미지 바이트 추가
                    form.AddBinaryData("screenshot", screenshotBytes, "desktop_screenshot.png", "image/png");
                    Debug.Log($"[ChatInputManager] 데스크탑 스크린샷 폼에 추가 완료 (크기: {screenshotBytes.Length} bytes)");
                }
                else
                {
                    Debug.LogWarning("[ChatInputManager] 스크린샷 캡처 실패 (Bytes == null)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatInputManager] 스크린샷 캡처 중 예외 발생: {e.Message}");
            }
        }

        // 5. WWWForm을 사용하여 POST 요청 생성 (Content-Type이 multipart/form-data로 자동 설정됨)
        UnityWebRequest www = UnityWebRequest.Post(url, form);

        // ⭐️ [변경됨] 기존 text/plain 관련 핸들러 및 헤더 설정 코드 삭제
        // byte[] bodyRaw = Encoding.UTF8.GetBytes(question); // (삭제)
        // www.uploadHandler = new UploadHandlerRaw(bodyRaw); // (삭제)
        // www.SetRequestHeader("Content-Type", "text/plain; charset=utf-8"); // (삭제)

        www.downloadHandler = new DownloadHandlerBuffer();

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

            // ... (이하 JSON 파싱 및 감정 처리 로직은 기존과 동일) ...

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

            (string cleanedText, string detectedEmotion) = CleanAndDetectEmotion(finalText);
            finalText = cleanedText; // 실제 팝업에 표시될 텍스트 (모두 제거된)

            if (_activePopup != null)
            {
                _activePopup.SetText(finalText);
                if (!string.IsNullOrEmpty(detectedEmotion))
                    Debug.Log($"[감정 감지됨] {detectedEmotion}");
                else
                    Debug.Log("[감정 없음]");
            }

            // 3. 감정이 감지되었으면 캐릭터 Animator에 Trigger 전송
            if (!string.IsNullOrEmpty(detectedEmotion))
            {
                // 1. 활성화된 캐릭터의 Animator 찾기
                Animator activeAnimator = null;
                if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
                {
                    activeAnimator = kirbyCharacter.GetComponent<Animator>();
                }
                else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
                {
                    activeAnimator = shihoCharacter.GetComponent<Animator>();
                }

                if (activeAnimator != null)
                {
                    // 2. 감지된 문자열(string)을 Trigger 이름(string)으로 변환
                    string triggerName = "";
                    switch (detectedEmotion)
                    {
                        case "기쁨":
                            triggerName = "isHappy"; // (Animator의 Trigger 이름과 일치해야 함)
                            break;
                        case "슬픔":
                            triggerName = "isSad";
                            break;
                        case "화남":
                            triggerName = "isAngry";
                            break;
                            // "보통"은 아무것도 안 함
                    }

                    // 3. 유효한 Trigger가 있으면 실행(SetTrigger)!
                    if (!string.IsNullOrEmpty(triggerName))
                    {
                        Debug.Log($"[Animator] {triggerName} 트리거 실행!");
                        activeAnimator.SetTrigger(triggerName);
                    }
                }
            }
        }

        // 4. 마지막 대화로 저장
        _lastConversationText = finalText;
        _lastActivePersonaDomain = activePersona;
    }

    /// <summary>
    /// ⭐️ [신규 추가]
    /// 정해진 주기(screenshotIntervalSeconds)마다 스크린샷을 캡처하고
    /// 백엔드로 전송하는 코루틴을 시작합니다.
    /// </summary>
    private IEnumerator PeriodicScreenshotCoroutine()
    {
        // 첫 실행은 게임 시작 후 5초 뒤에 시작 (선택적)
        yield return new WaitForSeconds(5.0f);

        while (true)
        {
            Debug.Log($"[ChatInputManager] {screenshotIntervalSeconds}초 경과. 배경 스크린샷 캡처 시도...");

            byte[] screenshotBytes = null;
            try
            {
                // ⭐️ (주의) 이 작업은 동기식이므로 캡처 동안 잠시 멈출 수 있습니다.
                screenshotBytes = DesktopCapture.CaptureDesktopAsPNG();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChatInputManager] 배경 스크린샷 캡처 중 예외: {e.Message}");
                screenshotBytes = null;
            }

            if (screenshotBytes != null && screenshotBytes.Length > 0)
            {
                Debug.Log($"[ChatInputManager] 배경 스크린샷 캡처 성공 (크기: {screenshotBytes.Length} bytes). 백엔드 전송 시작...");
                // 캡처한 스크린샷을 백엔드로 전송하는 새 코루틴을 시작합니다.
                // 이 코루틴이 끝나는 것을 기다리지 않고(StartCoroutine), 
                // 바로 다음 타이머를 위해 yield합니다.
                StartCoroutine(SendScreenshotToBackend(screenshotBytes));
            }
            else
            {
                Debug.LogWarning("[ChatInputManager] 배경 스크린샷 캡처 실패 (Bytes == null or 0)");
            }

            // 다음 주기까지 대기
            yield return new WaitForSeconds(screenshotIntervalSeconds);
        }
    }

    /// <summary>
    /// ⭐️ [수정됨]
    /// 캡처된 스크린샷 바이트를 백엔드로 전송합니다.
    /// 응답을 파싱하여 'SYSTEM_REMARK' 타입이면 별도 변수에 저장합니다.
    /// </summary>
    /// <param name="screenshotBytes">전송할 PNG 이미지 바이트</param>
    private IEnumerator SendScreenshotToBackend(byte[] screenshotBytes)
    {
        if (screenshotBytes == null || screenshotBytes.Length == 0)
        {
            Debug.LogError("[ChatInputManager] 전송할 스크린샷 데이터가 없습니다.");
            yield break; // 코루틴 종료
        }

        // 1. WWWForm 생성
        WWWForm form = new WWWForm();
        form.AddField("question", "[SYSTEM_SCREENSHOT]");
        form.AddBinaryData("screenshot", screenshotBytes, "desktop_screenshot.png", "image/png");

        // 4. URL 설정 (기존과 동일)
        string activePersona = ResolveActivePersona();
        string url = $"{backendBaseUrl}?sessionId={currentSessionId}&domain={activePersona}&computerId={computerId}";

        Debug.Log($"[ChatInputManager] 배경 스크린샷 전송 요청: {url}");

        // 5. WWWForm을 사용하여 POST 요청 생성 및 전송
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            // 6. ⭐️ [수정] 결과 로깅 및 '파싱'
            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[ChatInputManager] 배경 스크린샷 전송 실패: {www.error}");
            }
            else
            {
                Debug.Log($"[ChatInputManager] 배경 스크린샷 전송 완료. 응답 코드: {www.responseCode}");

                string rawJson = www.downloadHandler.text;
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    Debug.Log("[ChatInputManager] 배경 응답이 비어있습니다.");
                    yield break;
                }

                // ⭐️ [신규] 백엔드 응답 파싱
                try
                {
                    BackendResponse parsed = JsonUtility.FromJson<BackendResponse>(rawJson);

                    // ⭐️ 'SYSTEM_REMARK' 타입이고, 'reply'에 내용이 있는지 확인
                    if (parsed != null && parsed.type == "SYSTEM_REMARK" && !string.IsNullOrWhiteSpace(parsed.reply))
                    {
                        // ⭐️ 말풍선 대신, 별도 변수에 저장!
                        _lastSystemRemark = parsed.reply;
                        Debug.Log($"[ChatInputManager] 💬 새 시스템 한마디 저장: {_lastSystemRemark}");

                        // ⭐️ (선택적) 여기서 이 _lastSystemRemark를 
                        //    다른 컴포넌트(예: 별도 UI)로 보낼 수 있습니다.
                        //    FindObjectOfType<MyRemarkUI>()?.ShowRemark(_lastSystemRemark);

                        // ⭐️ 2. [신규] PopupSpawner를 통해 5초간 팝업으로 띄우기
                        if (popupSpawner != null)
                        {
                            popupSpawner.ShowTemporaryRemark(_lastSystemRemark, 5.0f);
                        }
                    }
                    else
                    {
                        Debug.Log($"[ChatInputManager] 배경 응답 수신 (저장할 내용 없음): {rawJson}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[ChatInputManager] 배경 응답 파싱 실패: {e.Message}. Raw: {rawJson}");
                }
            }
        }
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
        public string type; // ⭐️ [신규] "CHAT" 또는 "SYSTEM_REMARK"
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


// ======================================================================
// ⭐️ [신규 추가] P/Invoke를 사용한 Windows 데스크탑 캡처 헬퍼 클래스
// (System.Drawing.dll 참조가 필요합니다!)
// ======================================================================
public class DesktopCapture
{
    // C#에서 사용할 GDI 함수들 임포트
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(
        IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc,
        TernaryRasterOperations dwRop
    );

    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hGdiObj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hDC);

    // GetSystemMetrics 상수
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    // BitBlt 연산
    private enum TernaryRasterOperations : uint
    {
        SRCCOPY = 0x00CC0020
    }

    /// <summary>
    /// (Windows 전용) 현재 바탕화면 전체를 캡처하여 PNG 바이트 배열로 반환합니다.
    /// </summary>
    /// <returns>PNG 이미지의 byte[] 또는 실패 시 null</returns>
    public static byte[] CaptureDesktopAsPNG()
    {
        IntPtr hDesktop = GetDesktopWindow();
        if (hDesktop == IntPtr.Zero) return null;

        IntPtr hdcSrc = GetWindowDC(hDesktop);
        if (hdcSrc == IntPtr.Zero) return null;

        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);

        IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
        IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
        IntPtr hOld = SelectObject(hdcDest, hBitmap);

        try
        {
            // 화면 DC의 내용을 비트맵 DC로 복사
            BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, TernaryRasterOperations.SRCCOPY);

            // GDI 비트맵 핸들(hBitmap)을 System.Drawing.Bitmap 객체로 변환
            using (Bitmap bitmap = Bitmap.FromHbitmap(hBitmap))
            {
                // Bitmap을 메모리 스트림에 PNG 형식으로 저장
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DesktopCapture] 캡처 실패: {ex.Message}");
            return null;
        }
        finally
        {
            // 사용한 GDI 객체들 해제
            SelectObject(hdcDest, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            ReleaseDC(hDesktop, hdcSrc);
        }
    }
}