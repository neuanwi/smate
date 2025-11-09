using UnityEngine;
using UnityEngine.UI; // LayoutRebuilder
using System; // Action

/// <summary>
/// RecommendationPoller의 이벤트를 구독하여,
/// 1. PopupSpawner로 '말풍선'을 띄우고
/// 2. 필요시 'CheckButton' 팝업을 반대쪽에 띄우는 관리자 스크립트입니다.
/// </summary>
public class RecommendationUIManager : MonoBehaviour
{
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

    // --- 이벤트 구독 ---

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

    // --- 핸들러 1: 재치 있는 멘트 (말풍선만) ---

    private void HandleWittyComment(string message)
    {
        CloseAllPopups();
        _currentBubble = bubbleSpawner.ShowPopupNearTarget();

        if (_currentBubble != null)
        {
            // 👇 [수정됨] SetupMessage(message) 대신 SetText(message) 호출
            _currentBubble.SetText(message);
        }
    }

    // --- 핸들러 2: 앱 추천 (말풍선 + 체크 버튼) ---

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

        // 👇 [수정됨] SetupMessage(message) 대신 SetText(message) 호출
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
    // (이하 로직은 동일합니다)

    private void OnAcceptRecommendation(string path)
    {
        Debug.Log($"[RecUIManager] ⭐ 앱 실행! 경로: {path}");
        // (선택) 여기에 실제 앱을 실행하는 로직 추가
        CloseAllPopups();
    }

    private void OnDeclineRecommendation()
    {
        Debug.Log("[RecUIManager] 추천 거절됨.");
        CloseAllPopups();
    }

    public void CloseAllPopups()
    {
        if (_currentBubble != null)
        {
            // SetActive(false) 대신 스크립트에 있는 ClosePopup() 호출
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