using UnityEngine;
using UnityEngine.UI; // LayoutRebuilder
using System; // Action
using System.Collections; // IEnumerator
using UnityEngine.Networking; // UnityWebRequest
using System.Text; // Encoding

/// <summary>
/// (하이브리드) 4방향 로직으로 스폰하고, 캐릭터를 따라다니도록 관리합니다.
/// </summary>
public class RecommendationUIManager : MonoBehaviour
{
    [Header("핵심 연결")]
    [Tooltip("말풍선을 띄워줄 PopupSpawner")]
    [SerializeField]
    private PopupSpawner bubbleSpawner;

    [Header("체크 버튼 프리팹 (4방향)")] // ⭐️ (v1) 4방향 프리팹 사용
    [SerializeField]
    private GameObject leftLowCheckButtonPrefab;
    [SerializeField]
    private GameObject leftHighCheckButtonPrefab;
    [SerializeField]
    private GameObject rightLowCheckButtonPrefab;
    [SerializeField]
    private GameObject rightHighCheckButtonPrefab;

    [Header("위치 계산 참조")]
    [SerializeField]
    private Canvas parentCanvas;
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private GameObject kirbyCharacter;
    [SerializeField]
    private GameObject shihoCharacter;

    [Header("위치 오프셋")]
    [Tooltip("캐릭터로부터의 UI 오프셋 (체크 버튼용)")]
    [SerializeField]
    private Vector2 checkButtonOffset = new Vector2(50f, 50f); // ⭐️ (v1) 오프셋

    // --- 내부 변수 ---
    private PopupController _currentBubble;
    private RecommendationButtonPopup _currentCheckButton;
    private Transform _targetToFollow; // ⭐️ (v2) 따라다닐 대상

    // (AI 멈춤, 자동 닫기 관련 변수/코루틴 모두 삭제)


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

    // 👇 [수정됨] --------------------------------
    private void HandleWittyComment(string message)
    {
        CloseAllPopups(); // 👈 기존 팝업 닫기 (따라다니기 중지 포함)

        // ⭐️ 1. 말풍선 띄우기 (PopupSpawner가 (v1)스폰 + (v2)따라다니기 시작)
        _currentBubble = bubbleSpawner.ShowPopupNearTarget();

        if (_currentBubble != null)
        {
            _currentBubble.SetText(message);
        }
        // (AI 멈춤, 자동 닫기 코루틴 모두 삭제)
    }
    // 👆 [수정됨] --------------------------------

    // 👇 [수정됨] --------------------------------
    /// <summary>
    /// (핵심 로직) 4방향 체크 버튼을 '스폰'하고, '따라다니기'를 시작합니다.
    /// </summary>
    private void HandleAppRecommendation(string message, string appPath)
    {
        CloseAllPopups();

        // --- 1. 말풍선 띄우기 (PopupSpawner가 알아서 함) ---
        _currentBubble = bubbleSpawner.ShowPopupNearTarget();
        if (_currentBubble == null)
        {
            Debug.LogError("[RecUIManager] 말풍선 스폰에 실패했습니다!");
            return;
        }
        _currentBubble.SetText(message);

        // --- 2. 체크 버튼 띄우기 (직접 스폰 및 따라다니기) ---

        // 2-1. 말풍선 피벗을 기준으로 체크 버튼 피벗 결정 (v1)
        RectTransform bubbleRect = _currentBubble.transform as RectTransform;
        Vector2 bubblePivot = bubbleRect.pivot;
        Vector2 checkButtonPivot = new Vector2(1f - bubblePivot.x, bubblePivot.y);

        // 2-2. 활성화된 캐릭터 타겟 찾기
        Transform activeCharacterTarget = null;
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
        {
            activeCharacterTarget = kirbyCharacter.transform;
        }
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
        {
            activeCharacterTarget = shihoCharacter.transform;
        }

        // 2-3. ⭐️ (v1) 필수 참조 항목 확인
        if (activeCharacterTarget == null || mainCamera == null || parentCanvas == null ||
            leftLowCheckButtonPrefab == null || leftHighCheckButtonPrefab == null ||
            rightLowCheckButtonPrefab == null || rightHighCheckButtonPrefab == null)
        {
            Debug.LogError("[RecUIManager] 체크 버튼 스폰에 필요한 참조가 부족합니다!");
            return;
        }

        // 2-4. ⭐️ (v1) 피벗에 맞는 프리팹 선택
        GameObject prefabToSpawn = null;
        Vector2 newPivot = checkButtonPivot;
        if (newPivot.x == 0) // 좌측
            prefabToSpawn = (newPivot.y == 0) ? leftLowCheckButtonPrefab : leftHighCheckButtonPrefab;
        else // 우측
            prefabToSpawn = (newPivot.y == 0) ? rightLowCheckButtonPrefab : rightHighCheckButtonPrefab;

        // 2-5. ⭐️ (v1) 위치 계산
        Vector2 screenPos = mainCamera.WorldToScreenPoint(activeCharacterTarget.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos,
            parentCanvas.worldCamera,
            out localPoint
        );

        // 2-6. ⭐️ (v1) 오프셋 적용
        float offsetX = (checkButtonPivot.x == 0) ? checkButtonOffset.x : -checkButtonOffset.x;
        float offsetY = (checkButtonPivot.y == 0) ? checkButtonOffset.y : -checkButtonOffset.y;

        // 2-7. ⭐️ (v1) 인스턴스 생성 및 '교체'
        // (피벗이 달라지면 기존 인스턴스 파괴)
        if (_currentCheckButton != null)
        {
            RectTransform existingRect = _currentCheckButton.transform as RectTransform;
            if (existingRect != null && existingRect.pivot != newPivot)
            {
                Destroy(_currentCheckButton.gameObject);
                _currentCheckButton = null;
            }
        }

        if (_currentCheckButton == null)
        {
            GameObject cbInstance = Instantiate(prefabToSpawn, parentCanvas.transform);
            _currentCheckButton = cbInstance.GetComponent<RecommendationButtonPopup>();

            if (_currentCheckButton == null)
            {
                Debug.LogError($"'{prefabToSpawn.name}' 프리팹에 RecommendationButtonPopup.cs 스크립트가 없습니다!");
                Destroy(cbInstance);
                return;
            }
        }

        _currentCheckButton.gameObject.SetActive(true); // ⭐️ 활성화
        RectTransform cbRect = _currentCheckButton.transform as RectTransform;

        // 2-8. (v1) 피벗 및 위치 설정
        cbRect.pivot = checkButtonPivot;
        cbRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);
        LayoutRebuilder.ForceRebuildLayoutImmediate(cbRect);

        // 2-9. (v2) 따라다닐 대상으로 저장
        _targetToFollow = activeCharacterTarget;

        // 2-10. 버튼 콜백 설정
        _currentCheckButton.Setup(
            onAccept: () => { OnAcceptRecommendation(appPath); },
            onDecline: () => { OnDeclineRecommendation(); }
        );

        // (AI 멈춤, 자동 닫기 코루틴 모두 삭제)
    }
    // 👆 [수정됨] --------------------------------


    // --- 팝업 제어 로직 ---

    // (OnAcceptRecommendation, SendExecuteCommand, OnDeclineRecommendation은 그대로)
    private void OnAcceptRecommendation(string path)
    {
        Debug.Log($"[RecUIManager] ⭐ 앱 실행 요청! 경로: {path}");
        StartCoroutine(SendExecuteCommand(path));
        CloseAllPopups(); // 👈 따라다니기 중지 포함
    }

    private IEnumerator SendExecuteCommand(string appPath)
    {
        string url = "http://localhost:5001/execute";
        string escapedAppPath = appPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string jsonBody = $"{{\"command\": \"{escapedAppPath}\"}}";
        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            Debug.Log($"[RecUIManager] 파이썬 서버({url}) 호출 시도: {jsonBody}");
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[RecUIManager] 파이썬 서버 호출 실패: {www.error}");
                Debug.LogError($"[RecUIManager] 실패 본문: {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[RecUIManager] 파이썬 서버 응답: {www.downloadHandler.text}");
            }
        }
    }

    private void OnDeclineRecommendation()
    {
        Debug.Log("[RecUIManager] 추천 거절됨.");
        CloseAllPopups(); // 👈 따라다니기 중지 포함
    }

    // 👇 [수정됨] --------------------------------
    public void CloseAllPopups()
    {
        // ⭐️ 1. 말풍선을 숨기고, PopupSpawner의 따라다니기를 중지시킴
        if (bubbleSpawner != null && _currentBubble != null)
        {
            bubbleSpawner.HidePopup();
        }
        _currentBubble = null;

        // ⭐️ 2. 체크 버튼을 파괴 (재활용을 원하면 SetActive(false)로 변경)
        if (_currentCheckButton != null)
        {
            Destroy(_currentCheckButton.gameObject);
            _currentCheckButton = null;
        }

        // ⭐️ 3. 이 스크립트의 따라다니기를 중지시킴
        _targetToFollow = null;
    }
    // 👆 [수정됨] --------------------------------


    // 👇 [신규 추가] --------------------------------
    /// <summary>
    /// (v2) LateUpdate에서 체크 버튼이 캐릭터를 따라다니도록 위치를 갱신합니다.
    /// </summary>
    void LateUpdate()
    {
        // ⭐️ 따라다닐 대상(_targetToFollow)과 체크 버튼(_currentCheckButton)이 모두 유효할 때만 실행
        if (_targetToFollow != null && _currentCheckButton != null)
        {
            RectTransform cbRect = _currentCheckButton.transform as RectTransform;

            // 1. 새 위치 계산
            Vector2 screenPos = mainCamera.WorldToScreenPoint(_targetToFollow.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPos,
                parentCanvas.worldCamera,
                out localPoint
            );

            // 2. ⭐️ (v1)의 오프셋 로직을 매 프레임 다시 계산
            Vector2 currentPivot = cbRect.pivot;
            float offsetX = (currentPivot.x == 0) ? checkButtonOffset.x : -checkButtonOffset.x;
            float offsetY = (currentPivot.y == 0) ? checkButtonOffset.y : -checkButtonOffset.y;

            // 3. ⭐️ 최종 위치 적용
            cbRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);

            // (선택적) 
            // LayoutRebuilder.ForceRebuildLayoutImmediate(cbRect);
        }
    }
    // 👆 [신규 추가] --------------------------------
}