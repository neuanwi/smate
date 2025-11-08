using System;                       // 👈 JsonUtility용

using System.Collections;

using System.Collections;

using System.Text;

using System.Text.RegularExpressions;// 👈 BackgroundClickCatcher (Button)를 위해 추가

using TMPro;

using TMPro;

using UnityEngine;

using UnityEngine;

using UnityEngine.Networking; // 👈 실제 웹 통신을 위해 추가!

using UnityEngine.Networking;

using UnityEngine.UI;

using UnityEngine.UI;



public class ChatInputManager : MonoBehaviour

{

    [Header("필수 연결 요소")]

    public TMP_InputField questionInputField; // 1. 질문 인풋필드

    public PopupSpawner popupSpawner;         // 2. 팝업 생성기

    public UIAnimator inputFieldAnimator;     // 3. 인풋필드 애니메이터



    [Header("배경 버튼 (닫기용)")]

    // 4. 배경을 덮는 투명 버튼 (또는 Panel)

    public GameObject backgroundClickCatcher;



    [Header("백엔드 설정")]

    public string backendBaseUrl = "http://localhost:8080/gemini/simple";

    public string currentSessionId = "unityUser001";

    public string currentPersonaDomain = "tsundere";

    // 👇 [추가 2] - 캐릭터별 페르소나 설정을 위한 새 섹션
    [Header("캐릭터 페르소나 설정")]
    public GameObject kirbyCharacter;
    public string kirbyPersonaName = "kirby"; // 👈 커비에 해당하는 페르소나 도메인

    public GameObject shihoCharacter;
    public string shihoPersonaName = "tsundere";



    [Header("알람 매니저 연걸")]

    public AlarmManager alarmManager;



    // --- 내부 상태 변수 ---



    // 5. 현재 활성화된 팝업의 리모컨

    private PopupController _activePopup;

    // 👇 [수정] ---------------------------------
    // 6. ⭐️ 흐름 유지를 위한 마지막 답변 저장
    private const string DEFAULT_GREETING = "안녕하세요! 무엇이든 물어보세요.";
    private string _lastConversationText = DEFAULT_GREETING; // (기본 멘트)

    // 👇 [추가] ---------------------------------
    // 7. 마지막으로 "대화했던" 캐릭터의 페르소나 이름 저장용
    private string _lastActivePersonaDomain = "";
    // 👆 [추가] ---------------------------------



    void Start()

    {

        // 시작할 때 인풋필드와 배경 버튼을 확실히 숨깁니다.

        if (inputFieldAnimator != null) inputFieldAnimator.HideUI();

        if (backgroundClickCatcher != null) backgroundClickCatcher.SetActive(false);

    }



  

    // --- 1. [채팅 버튼]이 호출할 함수 ---
    /// <summary>
    /// 전체 채팅 UI 플로우를 엽니다. (메인 채팅 버튼 OnClick()에 연결)
    /// </summary>
    public void OpenChatFlow()
    {
        // 인풋필드 올리기
        if (inputFieldAnimator != null) inputFieldAnimator.ShowUI();

        // 배경 버튼 활성화 (다른 곳 클릭 시 닫기용)
        if (backgroundClickCatcher != null) backgroundClickCatcher.SetActive(true);

        // 👇 [수정] ---------------------------------------------------

        // 1. 현재 활성화된 캐릭터의 페르소나 파악
        string currentActivePersona = currentPersonaDomain; // 기본값
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
        {
            currentActivePersona = kirbyPersonaName;
        }
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
        {
            currentActivePersona = shihoPersonaName;
        }

        // 2. ⭐️ 핵심: 마지막으로 대화한 페르소나와 현재 페르소나가 다른지 확인
        //    (첫 실행 시 _lastActivePersonaDomain은 ""이므로 이 조건은 통과됨)
        if (!string.IsNullOrEmpty(_lastActivePersonaDomain) && _lastActivePersonaDomain != currentActivePersona)
        {
            Debug.Log($"[ChatInputManager] 캐릭터 변경 감지! (이전: {_lastActivePersonaDomain}, 현재: {currentActivePersona}) 대화 초기화.");
            _lastConversationText = DEFAULT_GREETING; // 👈 대화 기록 강제 리셋
        }

        // 3. (기존 로직) 대화 기록이 '기본 인사말'인지 확인하여 캐릭터별 인사 설정
        if (_lastConversationText == DEFAULT_GREETING)
        {
            if (currentActivePersona == kirbyPersonaName)
            {
                _lastConversationText = "하이! 무엇을 물어볼거야?";
            }
            else if (currentActivePersona == shihoPersonaName)
            {
                _lastConversationText = "흐음... 뭘 물어볼건데?";
            }

            // 4. ⭐️ 중요: (인사말을 방금 설정했으므로) 현재 캐릭터를 '마지막 대화 캐릭터'로 *미리* 기록.
            //    (대화 없이 껐다 켜도, 이 캐릭터의 인사말이 유지되어야 하므로)
            _lastActivePersonaDomain = currentActivePersona;
        }

        // 👆 [수정 끝] ------------------------------------------------

        // 팝업 생성
        if (popupSpawner != null)
        {
            // 팝업 스포너에게 팝업을 만들고 "리모컨"을 달라고 요청
            _activePopup = popupSpawner.ShowPopupNearTarget();

            if (_activePopup != null)
            {
                // ⭐️ 핵심: (위에서 상황에 맞게 설정된) 마지막 텍스트로 즉시 설정
                _activePopup.SetText(_lastConversationText);
            }
        }

        // (선택) 인풋필드에 바로 포커스
        if (questionInputField != null) questionInputField.ActivateInputField();
    }



    // --- 2. [인풋필드]가 호출할 함수 ---

    /// <summary>

    /// 인풋필드에서 엔터(Submit) 시 호출됩니다. (InputField의 OnSubmit 이벤트에 연결)

    /// </summary>

    public void OnSubmitQuestion()

    {

        if (questionInputField == null || _activePopup == null)

        {

            Debug.LogError("인풋필드 또는 활성화된 팝업이 없습니다.");

            return;

        }



        string question = questionInputField.text;

        if (string.IsNullOrEmpty(question)) return;



        // 인풋필드 비우기

        questionInputField.text = "";



        // (선택) 인풋필드 포커스 유지를 위해 다시 활성화

        questionInputField.ActivateInputField();



        // ⭐️ 백엔드 응답 처리 코루틴 시작

        StartCoroutine(HandleBackendResponse(question));

    }



    /// <summary>

    /// 질문을 보내고, 답변을 받아, 팝업 텍스트를 갱신합니다.

    /// </summary>

    private IEnumerator HandleBackendResponse(string question)

    {

        // 1. ⭐️ (요청사항) 텍스트 "초기화" (로딩 메시지)
        if (_activePopup != null)
        {
            _activePopup.SetText("생각 중...");
        }

        // --- 🚀 실제 백엔드 통신 코드 (UnityWebRequest) ---

        // 👇 [수정 1] - 활성화된 캐릭터 기준으로 페르소나 결정
        string activePersonaDomain = currentPersonaDomain; // 1. 기본값으로 시작

        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
        {
            activePersonaDomain = kirbyPersonaName; // 2. 커비가 활성화됨
        }
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
        {
            activePersonaDomain = shihoPersonaName; // 3. 시호가 활성화됨
        }
        // 👆 [수정 1] - 여기까지


        // 👇 [수정 2] - 결정된 'activePersonaDomain' 변수를 URL에 사용
        string url = $"{backendBaseUrl}?sessionId={currentSessionId}&domain={activePersonaDomain}";
        Debug.Log($"[ChatManager] 요청: {url}");





        string backendAnswer;



        UnityWebRequest www = new UnityWebRequest(url, "POST");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(question);

        www.uploadHandler = new UploadHandlerRaw(bodyRaw);

        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");



        yield return www.SendWebRequest();



        Debug.Log($"[ChatManager] 응답 상태: {www.result}, 코드: {www.responseCode}");

        Debug.Log($"[ChatManager] 본문: {www.downloadHandler.text}");



        if (www.result == UnityWebRequest.Result.ConnectionError ||

            www.result == UnityWebRequest.Result.ProtocolError)

        {

            backendAnswer = "서버 오류: " + www.error;

        }



        else

        {

            string response = www.downloadHandler.text;

            if (string.IsNullOrWhiteSpace(response))

            {

                backendAnswer = "서버에서 빈 응답 받음";

            }

            else

            {

                // ✅ JSON에서 텍스트만 추출

                string onlyText = ExtractGeminiText(response);



                // 추출 실패 시 원문을 보여주고 싶지 않다면 아래 한 줄을 에러 문구로 교체 가능

                string toShow = string.IsNullOrWhiteSpace(onlyText) ? response : onlyText;

                backendAnswer = toShow;







                // ✅ 알람 기능 추가 위치

                if (question.Contains("알람") || question.Contains("깨워") || question.Contains("설정"))

                {

                    alarmManager.TryCreateAlarmFromMessage(question);

                }

            }

        }



        // 3. (요청사항) 팝업에 새 답변으로 "업데이트"

        if (_activePopup != null)

        {

            _activePopup.SetText(backendAnswer);

        }



        // 4. ⭐️ (요청사항) 다음 흐름 유지를 위해 답변을 "저장", 캐릭터도 마찬가지

        _lastConversationText = backendAnswer;
        _lastActivePersonaDomain = activePersonaDomain;
        Debug.Log($"[ChatInputManager] 마지막 대화 페르소나 기록: {activePersonaDomain}");
    }





    private static string ExtractGeminiText(string json)

    {

        if (string.IsNullOrEmpty(json)) return null;



        try

        {

            var root = JsonUtility.FromJson<GeminiRoot>(json); // 'GeminiRoot' 형식 또는 네임스페이스 이름을 찾을 수 없습니다. using 지시문 또는 어셈블리 참조가 있는지 확인하세요.

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





    // --- 3. [배경 버튼]이 호출할 함수 ---

    /// <summary>

    /// 전체 채팅 UI 플로우를 닫습니다. (배경 버튼 OnClick()에 연결)

    /// </summary>

    public void CloseChatFlow()

    {

        // 인풋필드 내리기

        if (inputFieldAnimator != null) inputFieldAnimator.HideUI();



        // 배경 버튼 비활성화

        if (backgroundClickCatcher != null) backgroundClickCatcher.SetActive(false);



        // 팝업 "삭제"

        if (_activePopup != null)

        {

            Destroy(_activePopup.gameObject);

            _activePopup = null; // 👈 참조를 깨끗하게 비웁니다.

        }

    }


    // 👇 [추가] ---------------------------------
    // --- 4. [캐릭터 변경 시] 호출될 함수 ---
    /// <summary>
    /// 캐릭터가 변경되었을 때 외부(예: CharacterManager)에서 호출하여
    /// 마지막 대화 내용을 초기화합니다.
    /// </summary>
    public void OnCharacterSwitched()
    {
        Debug.Log("[ChatInputManager] 캐릭터 변경 감지! 대화 내용을 초기화합니다.");

        // 1. 대화 기록을 기본 인사말로 리셋
        _lastConversationText = DEFAULT_GREETING;

        // 2. (선택사항) 혹시 채팅창이 열려있는 상태로 캐릭터를 바꿨다면,
        //    채팅창도 같이 닫아주는 것이 자연스럽습니다.
        if (_activePopup != null)
        {
            CloseChatFlow();
        }
    }
    // 👆 [추가] ---------------------------------









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













}