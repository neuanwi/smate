using UnityEngine;
using UnityEngine.UI; // LayoutRebuilder
using System; // Action
using System.Collections; // 👈 [추가] IEnumerator를 위해 추가
using UnityEngine.Networking; // 👈 [추가] UnityWebRequest를 위해 추가
using System.Text; // 👈 [추가] JSON 인코딩을 위해 추가

/// <summary>
/// RecommendationPoller의 이벤트를 구독하여,
/// 1. PopupSpawner로 '말풍선'을 띄우고
/// 2. 필요시 'CheckButton' 팝업을 반대쪽에 띄우는 관리자 스크립트입니다.
/// </summary>
public class RecommendationUIManager : MonoBehaviour
{
    // ... (필드 변수들은 모두 그대로) ...
    [Header("핵심 연결")]
    [Tooltip("말풍선을 띄워줄 PopupSpawner")]
    [SerializeField]
    private PopupSpawner bubbleSpawner;

    [Tooltip("체크 버튼(CheckButton) 프리팹 원본")]
    [SerializeField]
    private GameObject checkButtonPrefab;

    [Header("위치 계산 참조 (PopupSpawner와 동일하게)")]
    [Tooltip("팝업이 생성될 부모 캔버스")]
    [SerializeField]
    private Canvas parentCanvas;

    [Tooltip("월드->스크린 좌표 변환용 카메라")]
    [SerializeField]
    private Camera mainCamera;

    [Tooltip("위치 기준이 될 캐릭터 1")]
    [SerializeField]
    private GameObject kirbyCharacter;

    [Tooltip("위치 기준이 될 캐릭터 2")]
    [SerializeField]
    private GameObject shihoCharacter;

    [Tooltip("캐릭터로부터의 UI 오프셋 (체크 버튼용)")]
    [SerializeField]
    private Vector2 checkButtonOffset = new Vector2(50f, 50f);


    // --- 내부 변수 ---
    private PopupController _currentBubble;
    private RecommendationButtonPopup _currentCheckButton;

    // ... (OnEnable, OnDisable은 그대로) ...
    private void OnEnable()
    {
        RecommendationPoller.OnWittyCommentReceived += HandleWittyComment;
        RecommendationPoller.OnAppRecommendationReceived += HandleAppRecommendation;
    }

    private void OnDisable()
    {
        RecommendationPoller.OnWittyCommentReceived -= HandleWittyComment;
        RecommendationPoller.OnAppRecommendationReceived -= HandleAppRecommendation;
    }

    // ... (HandleWittyComment는 그대로) ...
    private void HandleWittyComment(string message)
    {
        CloseAllPopups();
        _currentBubble = bubbleSpawner.ShowPopupNearTarget();

        if (_currentBubble != null)
        {
            _currentBubble.SetText(message);
        }
    }

    // ... (HandleAppRecommendation는 그대로) ...
    private void HandleAppRecommendation(string message, string appPath)
    {
        CloseAllPopups();

        // --- 1. 말풍선 띄우기 (PopupSpawner 사용) ---
        _currentBubble = bubbleSpawner.ShowPopupNearTarget();
        if (_currentBubble == null)
        {
            Debug.LogError("[RecUIManager] 말풍선 스폰에 실패했습니다!");
            return;
        }

        _currentBubble.SetText(message);

        // --- 2. 체크 버튼 띄우기 (직접 스폰) ---
        // (이하 로직은 동일합니다)

        RectTransform bubbleRect = _currentBubble.transform as RectTransform;
        Vector2 bubblePivot = bubbleRect.pivot;
        Vector2 checkButtonPivot = new Vector2(1f - bubblePivot.x, bubblePivot.y);

        Transform activeCharacterTarget = null;
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
        {
            activeCharacterTarget = kirbyCharacter.transform;
        }
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
        {
            activeCharacterTarget = shihoCharacter.transform;
        }

        if (activeCharacterTarget == null || mainCamera == null || parentCanvas == null || checkButtonPrefab == null)
        {
            Debug.LogError("[RecUIManager] 체크 버튼 스폰에 필요한 참조가 부족합니다!");
            return;
        }

        Vector2 screenPos = mainCamera.WorldToScreenPoint(activeCharacterTarget.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos,
            parentCanvas.worldCamera,
            out localPoint
        );

        float offsetX = (checkButtonPivot.x == 0) ? checkButtonOffset.x : -checkButtonOffset.x;
        float offsetY = (checkButtonPivot.y == 0) ? checkButtonOffset.y : -checkButtonOffset.y;

        GameObject cbInstance = Instantiate(checkButtonPrefab, parentCanvas.transform);
        cbInstance.SetActive(true);

        RectTransform cbRect = cbInstance.transform as RectTransform;
        cbRect.pivot = checkButtonPivot;
        cbRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);

        LayoutRebuilder.ForceRebuildLayoutImmediate(cbRect);

        _currentCheckButton = cbInstance.GetComponent<RecommendationButtonPopup>();
        if (_currentCheckButton != null)
        {
            _currentCheckButton.Setup(
                onAccept: () => { OnAcceptRecommendation(appPath); },
                onDecline: () => { OnDeclineRecommendation(); }
            );
        }
    }

    // --- 팝업 제어 로직 ---

    // 👇 [수정됨] --------------------------------
    private void OnAcceptRecommendation(string path)
    {
        Debug.Log($"[RecUIManager] ⭐ 앱 실행 요청! 경로: {path}");

        // (선택) 여기에 실제 앱을 실행하는 로직 추가
        // ⭐️ 파이썬 서버에 '실행' POST 요청을 보내는 코루틴을 시작합니다.
        StartCoroutine(SendExecuteCommand(path));

        CloseAllPopups();
    }
    // 👆 [수정됨] --------------------------------

    // 👇 [신규 추가] --------------------------------
    /// <summary>
    /// 파이썬 Flask 서버의 /execute 엔드포인트로 앱 실행 명령을 보냅니다.
    /// </summary>
    /// <param name="appPath">"Photoshop.exe" 등 실행할 앱 경로/이름</param>
    private IEnumerator SendExecuteCommand(string appPath)
    {
        // 1. 파이썬 서버 주소
        string url = "http://localhost:5001/execute";

        // 2. 파이썬이 받을 JSON 형식: { "command": "Photoshop.exe" }
        // (JSON 특수문자를 이스케이프 처리합니다)
        // 👇 [수정됨] --------------------------------
        // ⭐️ JSON 표준을 위해 백슬래시(\)도 \\로, 큰따옴표(")는 \"로 이스케이프합니다.
        string escapedAppPath = appPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string jsonBody = $"{{\"command\": \"{escapedAppPath}\"}}";
        // 👆 [수정됨] --------------------------------

        // 3. UnityWebRequest 생성
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            // 4. JSON 바디를 UTF-8 바이트로 변환하여 업로드 핸들러에 설정
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();

            // 5. ⭐️ Content-Type 헤더를 'application/json'으로 설정 (필수!)
            www.SetRequestHeader("Content-Type", "application/json");

            // 6. 요청 전송 및 대기
            Debug.Log($"[RecUIManager] 파이썬 서버({url}) 호출 시도: {jsonBody}");
            yield return www.SendWebRequest();

            // 7. 결과 로깅
            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[RecUIManager] 파이썬 서버 호출 실패: {www.error}");
                Debug.LogError($"[RecUIManager] 실패 본문: {www.downloadHandler.text}");
            }
            else
            {
                // 파이썬 서버가 보낸 "실행을 시작했습니다." 메시지
                Debug.Log($"[RecUIManager] 파이썬 서버 응답: {www.downloadHandler.text}");
            }
        }
    }
    // 👆 [신규 추가] --------------------------------


    private void OnDeclineRecommendation()
    {
        Debug.Log("[RecUIManager] 추천 거절됨.");
        CloseAllPopups();
    }

    public void CloseAllPopups()
    {
        if (_currentBubble != null)
        {
            _currentBubble.ClosePopup();
            _currentBubble = null;
        }

        if (_currentCheckButton != null)
        {
            Destroy(_currentCheckButton.gameObject);
            _currentCheckButton = null;
        }
    }
}