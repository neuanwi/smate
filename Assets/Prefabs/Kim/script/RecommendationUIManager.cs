using UnityEngine;
using UnityEngine.UI;           // RectTransformUtility, LayoutRebuilder
using System.Collections;       // IEnumerator
using UnityEngine.Networking;   // UnityWebRequest
using System.Text;              // Encoding

/// <summary>
/// 추천(위티 코멘트/앱 추천)이 오면 말풍선과 체크 버튼을 4방향으로 띄우고,
/// 캐릭터를 따라다니게 만든다.
/// 체크 버튼은 슬라이드 인으로 등장한다.
/// </summary>
public class RecommendationUIManager : MonoBehaviour
{
    [Header("핵심 연결")]
    [SerializeField] private PopupSpawner bubbleSpawner;

    [Header("체크 버튼 프리팹 (4방향)")]
    [SerializeField] private GameObject leftLowCheckButtonPrefab;
    [SerializeField] private GameObject leftHighCheckButtonPrefab;
    [SerializeField] private GameObject rightLowCheckButtonPrefab;
    [SerializeField] private GameObject rightHighCheckButtonPrefab;

    [Header("위치 계산 참조")]
    [SerializeField] private Canvas parentCanvas;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject kirbyCharacter;
    [SerializeField] private GameObject shihoCharacter;

    [Header("위치 오프셋")]
    [Tooltip("캐릭터로부터 체크 버튼까지의 기본 오프셋")]
    [SerializeField] private Vector2 checkButtonOffset = new Vector2(50f, 50f);

    [Header("체크 버튼 슬라이드 인")]
    [Tooltip("슬라이드 인 시간")]
    [SerializeField] private float checkSlideDuration = 0.25f;
    [Tooltip("기본 슬라이드 시작 오프셋 (왼쪽에서 들어오게)")]
    [SerializeField] private Vector2 checkSlideOffset = new Vector2(-70f, 0f);

    // --- 내부 상태 ---
    private PopupController _currentBubble;
    private RecommendationButtonPopup _currentCheckButton;
    private Transform _targetToFollow;          // 따라다닐 캐릭터
    private bool _isCheckSliding = false;       // 슬라이드 중이면 LateUpdate에서 위치 덮어쓰지 않음

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

    // 위트 멘트만 오는 경우: 말풍선만 띄움
    private void HandleWittyComment(string message)
    {
        CloseAllPopups();

        _currentBubble = bubbleSpawner.ShowPopupNearTarget();
        if (_currentBubble != null)
            _currentBubble.SetText(message);
    }

    /// <summary>
    /// 앱 추천이 온 경우: 말풍선 + 체크버튼
    /// </summary>
    private void HandleAppRecommendation(string message, string appPath)
    {
        CloseAllPopups();

        // 1. 말풍선 띄우기
        _currentBubble = bubbleSpawner.ShowPopupNearTarget();
        if (_currentBubble == null)
        {
            Debug.LogError("[RecUIManager] 말풍선 스폰 실패");
            return;
        }
        _currentBubble.SetText(message);

        // 2. 체크 버튼을 말풍선의 반대쪽에 띄우기
        RectTransform bubbleRect = _currentBubble.transform as RectTransform;
        Vector2 bubblePivot = bubbleRect.pivot;                  // (0 or 1, 0 or 1)
        Vector2 checkButtonPivot = new Vector2(1f - bubblePivot.x, bubblePivot.y);

        // 2-1. 활성 캐릭터 찾기
        Transform activeCharacterTarget = null;
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
            activeCharacterTarget = kirbyCharacter.transform;
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
            activeCharacterTarget = shihoCharacter.transform;

        // 2-2. 필수 체크
        if (activeCharacterTarget == null || parentCanvas == null || mainCamera == null ||
            leftLowCheckButtonPrefab == null || leftHighCheckButtonPrefab == null ||
            rightLowCheckButtonPrefab == null || rightHighCheckButtonPrefab == null)
        {
            Debug.LogError("[RecUIManager] 체크버튼을 만들기 위한 참조가 부족합니다.");
            return;
        }

        // 2-3. 프리팹 선택
        GameObject prefabToSpawn;
        if (checkButtonPivot.x == 0f)   // 좌
            prefabToSpawn = (checkButtonPivot.y == 0f) ? leftLowCheckButtonPrefab : leftHighCheckButtonPrefab;
        else                            // 우
            prefabToSpawn = (checkButtonPivot.y == 0f) ? rightLowCheckButtonPrefab : rightHighCheckButtonPrefab;

        // 2-4. 화면좌표 → 캔버스 로컬좌표
        Vector2 screenPos = mainCamera.WorldToScreenPoint(activeCharacterTarget.position);
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos,
            parentCanvas.worldCamera,
            out localPoint
        );

        // 2-5. 오프셋
        float offsetX = (checkButtonPivot.x == 0f) ? checkButtonOffset.x : -checkButtonOffset.x;
        float offsetY = (checkButtonPivot.y == 0f) ? checkButtonOffset.y : -checkButtonOffset.y;
        Vector2 finalPos = localPoint + new Vector2(offsetX, offsetY);

        // 2-6. 기존 체크버튼 피벗 다르면 파괴
        if (_currentCheckButton != null)
        {
            RectTransform existingRect = _currentCheckButton.transform as RectTransform;
            if (existingRect != null && existingRect.pivot != checkButtonPivot)
            {
                Destroy(_currentCheckButton.gameObject);
                _currentCheckButton = null;
            }
        }

        // 2-7. 새로 생성
        if (_currentCheckButton == null)
        {
            GameObject cbInstance = Instantiate(prefabToSpawn, parentCanvas.transform);
            _currentCheckButton = cbInstance.GetComponent<RecommendationButtonPopup>();
            if (_currentCheckButton == null)
            {
                Debug.LogError($"[RecUIManager] {prefabToSpawn.name} 에 RecommendationButtonPopup이 없습니다.");
                Destroy(cbInstance);
                return;
            }
        }

        _currentCheckButton.gameObject.SetActive(true);
        RectTransform cbRect = _currentCheckButton.transform as RectTransform;
        cbRect.pivot = checkButtonPivot;

        // 2-8. 슬라이드 시작 위치 세팅
        // pivot이 오른쪽이라면 오른쪽에서 들어오게, 왼쪽이라면 왼쪽에서 들어오게 방향 보정
        Vector2 dynamicSlideOffset = checkSlideOffset;
        if (checkButtonPivot.x == 1f)       // 우측 버튼이면
            dynamicSlideOffset.x = -checkSlideOffset.x;

        cbRect.anchoredPosition = finalPos + dynamicSlideOffset;

        // CanvasGroup 확보 후 알파 0
        CanvasGroup cbCg = _currentCheckButton.GetComponent<CanvasGroup>();
        if (cbCg == null) cbCg = _currentCheckButton.gameObject.AddComponent<CanvasGroup>();
        cbCg.alpha = 0f;

        // 2-9. 슬라이드 인 시작
        StartCoroutine(SlideInCheckButton(cbRect, cbCg, finalPos));

        // 2-10. 따라다닐 대상 저장
        _targetToFollow = activeCharacterTarget;

        // 2-11. 버튼 콜백 설정
        _currentCheckButton.Setup(
            onAccept: () => OnAcceptRecommendation(appPath),
            onDecline: OnDeclineRecommendation
        );
    }

    // 체크 버튼 슬라이드 인 코루틴
    private IEnumerator SlideInCheckButton(RectTransform rect, CanvasGroup cg, Vector2 targetPos)
    {
        _isCheckSliding = true;

        float t = 0f;
        Vector2 startPos = rect.anchoredPosition;

        while (t < 1f)
        {
            t += Time.deltaTime / checkSlideDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f);   // ease-out

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            cg.alpha = eased;

            yield return null;
        }

        rect.anchoredPosition = targetPos;
        cg.alpha = 1f;
        _isCheckSliding = false;
    }

    private void LateUpdate()
    {
        // 체크버튼이 존재하고, 따라다닐 대상이 있고, 그리고 슬라이드 중이 아닐 때만 위치 갱신
        if (_targetToFollow != null && _currentCheckButton != null && !_isCheckSliding)
        {
            RectTransform cbRect = _currentCheckButton.transform as RectTransform;

            Vector2 screenPos = mainCamera.WorldToScreenPoint(_targetToFollow.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPos,
                parentCanvas.worldCamera,
                out localPoint
            );

            Vector2 currentPivot = cbRect.pivot;
            float offsetX = (currentPivot.x == 0f) ? checkButtonOffset.x : -checkButtonOffset.x;
            float offsetY = (currentPivot.y == 0f) ? checkButtonOffset.y : -checkButtonOffset.y;

            cbRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);
        }
    }

    // --- 수락 / 거절 / 닫기 ---

    private void OnAcceptRecommendation(string path)
    {
        Debug.Log($"[RecUIManager] 앱 실행 요청: {path}");
        StartCoroutine(SendExecuteCommand(path));
        CloseAllPopups();
    }

    private IEnumerator SendExecuteCommand(string appPath)
    {
        string url = "http://localhost:5001/execute";
        string escapedAppPath = appPath.Replace("\\", "\\\\").Replace("\"", "\\\"");
        string jsonBody = $"{{\"command\": \"{escapedAppPath}\"}}";

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[RecUIManager] 서버 호출 실패: {www.error}");
                Debug.LogError($"[RecUIManager] 응답: {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[RecUIManager] 응답: {www.downloadHandler.text}");
            }
        }
    }

    private void OnDeclineRecommendation()
    {
        Debug.Log("[RecUIManager] 추천 거절");
        CloseAllPopups();
    }

    public void CloseAllPopups()
    {
        // 말풍선 숨기기
        if (bubbleSpawner != null && _currentBubble != null)
            bubbleSpawner.HidePopup();     // 사용 중인 PopupSpawner에 이 메서드가 있다고 가정

        _currentBubble = null;

        // 체크 버튼 제거
        if (_currentCheckButton != null)
        {
            Destroy(_currentCheckButton.gameObject);
            _currentCheckButton = null;
        }

        _targetToFollow = null;
        _isCheckSliding = false;
    }
}
