using UnityEngine;
using UnityEngine.UI;   // LayoutRebuilder, CanvasGroup
using System.Text.RegularExpressions;

public class PopupSpawner : MonoBehaviour
{
    [Header("UI 프리팹 (4방향)")]
    public GameObject leftLowBubblePrefab;    // 좌하 (x=0, y=0)
    public GameObject leftHighBubblePrefab;   // 좌상 (x=0, y=1)
    public GameObject rightLowBubblePrefab;   // 우하 (x=1, y=0)
    public GameObject rightHighBubblePrefab;  // 우상 (x=1, y=1)

    [Header("UI 프리팹 및 캔버스")]
    public Canvas parentCanvas;

    [Header("타겟 및 카메라")]
    public GameObject kirbyCharacter;
    public GameObject shihoCharacter;
    public Camera mainCamera;

    [Header("말풍선 위치 오프셋")]
    public Vector2 positionOffset = new Vector2(60f, 40f);

    [Header("슬라이드 인 설정")]
    public float slideDuration = 0.25f;
    public Vector2 slideInOffset = new Vector2(-80f, 0f);   // 기본: 왼쪽에서 들어옴

    [Header("감정 스티커")]
    public GameObject emotionStickerPrefab;
    public Vector2 stickerOffset = new Vector2(80f, 80f);
    private RectTransform _lastSticker;

    // 내부 상태
    private PopupController _currentPopupInstance;
    private bool _needsPositionUpdate = false;
    private Vector2 _lastLocalPoint;
    private Vector2 _lastPivot;

    // ★ 커밋에서 했던 것: 따라다닐 대상
    private Transform _targetToFollow;

    private bool _isSliding = false;

    private Coroutine _hideTimerCoroutine;
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    /// <summary>
    /// 활성 캐릭터 근처에 말풍선을 띄우고, 이후 LateUpdate에서 따라다니게 한다.
    /// </summary>
    public PopupController ShowPopupNearTarget()
    {
        // 0. 어떤 캐릭터가 살아 있는지 확인
        Transform activeCharacterTarget = null;
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
            activeCharacterTarget = kirbyCharacter.transform;
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
            activeCharacterTarget = shihoCharacter.transform;

        // 필수 참조 체크
        if (activeCharacterTarget == null ||
            leftLowBubblePrefab == null || leftHighBubblePrefab == null ||
            rightLowBubblePrefab == null || rightHighBubblePrefab == null ||
            parentCanvas == null || mainCamera == null)
        {
            Debug.LogError("[PopupSpawner] 말풍선을 만들 수 있는 조건이 안 갖춰졌습니다.");
            return null;
        }

        // 1. 현재 캐릭터 위치를 스크린 좌표로 변환
        Vector2 screenPos = mainCamera.WorldToScreenPoint(activeCharacterTarget.position);

        // 2. 화면 위치 기준으로 말풍선이 어느 쪽에 나와야 할지 피벗 계산
        Vector2 newPivot = new Vector2(
            (screenPos.x < Screen.width / 2f) ? 0f : 1f,
            (screenPos.y < Screen.height / 2f) ? 0f : 1f
        );

        // 3. 피벗에 맞는 프리팹 선택
        GameObject prefabToSpawn = null;
        if (newPivot.x == 0f)      // 좌측
            prefabToSpawn = (newPivot.y == 0f) ? leftLowBubblePrefab : leftHighBubblePrefab;
        else                       // 우측
            prefabToSpawn = (newPivot.y == 0f) ? rightLowBubblePrefab : rightHighBubblePrefab;

        // 4. 기존 팝업이 있고, 피벗이 달라졌으면 교체
        if (_currentPopupInstance != null)
        {
            RectTransform existingRect = _currentPopupInstance.transform as RectTransform;
            if (existingRect != null && existingRect.pivot != newPivot)
            {
                Destroy(_currentPopupInstance.gameObject);
                _currentPopupInstance = null;
            }
        }

        // 5. 팝업이 없다면 새로 생성
        if (_currentPopupInstance == null)
        {
            GameObject newPopup = Instantiate(prefabToSpawn, parentCanvas.transform);
            _currentPopupInstance = newPopup.GetComponent<PopupController>();
            if (_currentPopupInstance == null)
            {
                Debug.LogError($"[PopupSpawner] {prefabToSpawn.name} 에 PopupController가 없습니다.");
                Destroy(newPopup);
                return null;
            }
        }

        _currentPopupInstance.gameObject.SetActive(true);
        RectTransform popupRect = _currentPopupInstance.transform as RectTransform;

        // 6. 스크린 좌표를 캔버스 로컬 좌표로 변환
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos,
            parentCanvas.worldCamera,
            out localPoint
        );

        // 7. 피벗에 맞는 오프셋 적용
        float offsetX = (newPivot.x == 0f) ? positionOffset.x : -positionOffset.x;
        float offsetY = (newPivot.y == 0f) ? positionOffset.y : -positionOffset.y;
        Vector2 finalPos = localPoint + new Vector2(offsetX, offsetY);

        // 8. 슬라이드 인 방향 조정
        Vector2 dynamicSlideOffset = slideInOffset;
        if (newPivot.x == 1f) // 오른쪽 말풍선이면 반대쪽에서 들어오게
            dynamicSlideOffset.x = -slideInOffset.x;

        popupRect.pivot = newPivot;
        popupRect.anchoredPosition = finalPos + dynamicSlideOffset;

        // CanvasGroup으로 페이드/슬라이드
        CanvasGroup cg = _currentPopupInstance.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = _currentPopupInstance.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        // 슬라이드 시작 전에 슬라이딩 중이라고 표시
        _isSliding = true;

        StartCoroutine(SlideInRoutine(popupRect, cg, finalPos));

        // 스티커용으로 현재 위치/피벗 저장
        _lastLocalPoint = localPoint;
        _lastPivot = newPivot;
        _needsPositionUpdate = true;

        // ★ 여기서 “이 캐릭터를 따라가라”고 기억시킴
        _targetToFollow = activeCharacterTarget;

        if (_hideTimerCoroutine != null)
        {
            StopCoroutine(_hideTimerCoroutine);
            _hideTimerCoroutine = null;
        }

        return _currentPopupInstance;
    }

    void LateUpdate()
    {
        // 슬라이드 들어온 직후 레이아웃 한 번만 강제
        if (_needsPositionUpdate)
        {
            _needsPositionUpdate = false;
            if (_currentPopupInstance != null)
            {
                RectTransform popupRect = _currentPopupInstance.transform as RectTransform;
                if (popupRect != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
            }
        }

        if (_isSliding)
            return;

        // ★ 커밋에서 했던 실제 “따라다니기” 부분
        if (_targetToFollow != null && _currentPopupInstance != null)
        {
            RectTransform popupRect = _currentPopupInstance.transform as RectTransform;
            if (popupRect == null) return;

            // 1. 현재 타겟 위치를 다시 스크린 좌표로
            Vector2 screenPos = mainCamera.WorldToScreenPoint(_targetToFollow.position);

            // 2. 다시 캔버스 로컬로
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.transform as RectTransform,
                screenPos,
                parentCanvas.worldCamera,
                out localPoint
            );

            // 3. 스폰 당시 피벗 기준 오프셋 재적용
            Vector2 currentPivot = popupRect.pivot;
            float offsetX = (currentPivot.x == 0f) ? positionOffset.x : -positionOffset.x;
            float offsetY = (currentPivot.y == 0f) ? positionOffset.y : -positionOffset.y;

            popupRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);

            // 스티커도 이 위치를 써야 하니까 같이 갱신
            _lastLocalPoint = localPoint;
        }
    }

    private System.Collections.IEnumerator SlideInRoutine(RectTransform rect, CanvasGroup cg, Vector2 targetPos)
    {
        float t = 0f;
        Vector2 startPos = rect.anchoredPosition;

        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            float eased = 1f - Mathf.Pow(1f - t, 3f); // ease-out

            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            cg.alpha = eased;

            yield return null;
        }

        rect.anchoredPosition = targetPos;
        cg.alpha = 1f;

        // ★ 슬라이드 끝났다고 표시
        _isSliding = false;
    }

    public void ShowEmotionSticker(RectTransform popupRect, string emotion)
    {
        if (emotionStickerPrefab == null)
        {
            Debug.LogWarning("[PopupSpawner] 스티커 프리팹이 없습니다.");
            return;
        }

        if (_lastSticker == null)
        {
            GameObject stickerObj = Instantiate(emotionStickerPrefab, parentCanvas.transform);
            _lastSticker = stickerObj.GetComponent<RectTransform>();
            if (_lastSticker == null)
            {
                Debug.LogError("[PopupSpawner] 스티커 프리팹에 RectTransform이 없습니다.");
                Destroy(stickerObj);
                return;
            }
        }

        _lastSticker.gameObject.SetActive(true);

        float stickerPivotX = 1f - _lastPivot.x; // 말풍선 반대쪽
        float stickerPivotY = _lastPivot.y;

        float stickerOffsetX = (stickerPivotX == 0f) ? stickerOffset.x : -stickerOffset.x;
        float stickerOffsetY = (stickerPivotY == 0f) ? stickerOffset.y : -stickerOffset.y;

        _lastSticker.anchoredPosition = _lastLocalPoint + new Vector2(stickerOffsetX, stickerOffsetY);
        _lastSticker.SetAsLastSibling();
    }

    public void HideEmotionSticker()
    {
        if (_lastSticker != null)
            _lastSticker.gameObject.SetActive(false);
    }

    public void HidePopup()
    {
        if (_currentPopupInstance != null)
            _currentPopupInstance.gameObject.SetActive(false);

        // 스티커도 같이 숨김
        HideEmotionSticker();

        // ★ 따라다니기 중지
        _targetToFollow = null;

        if (_hideTimerCoroutine != null)
        {
            StopCoroutine(_hideTimerCoroutine);
            _hideTimerCoroutine = null;
        }
    }

    /// <summary>
    /// ⭐️ [신규]
    /// 텍스트를 담은 팝업을 띄우고, 일정 시간 뒤에 자동으로 숨깁니다.
    /// (ChatInputManager의 시스템 혼잣말에 사용)
    /// </summary>
    public void ShowTemporaryRemark(string text, float duration)
    {

        // ⭐️⭐️⭐️ [신규 추가된 로직] ⭐️⭐️⭐️
        // 팝업 인스턴스가 '존재하고' '활성화(보이는 상태)'인지 확인
        // (Unity의 'fake null'을 처리하기 위해 _currentPopupInstance만으로도 null 체크 가능)
        if (_currentPopupInstance && _currentPopupInstance.gameObject.activeInHierarchy)
        {
            // ⭐️ 이미 팝업이 떠 있다면 (채팅 중이거나 다른 혼잣말 중)
            // ⭐️ 아무것도 하지 않고, 새 혼잣말을 무시합니다.
            Debug.Log("[PopupSpawner] 팝업이 이미 활성화되어 있어 새 혼잣말을 무시합니다.");
            return; // ⭐️ 함수 종료
        }
        // ⭐️⭐️⭐️ [여기까지 추가] ⭐️⭐️⭐️

        // 1. 기존 팝업 띄우는 로직을 그대로 재사용하여 팝업을 가져옴
        //    (이 과정에서 _currentPopupInstance가 생성/설정됨)
        PopupController popup = ShowPopupNearTarget();

        if (popup == null)
        {
            Debug.LogError("[PopupSpawner] ShowTemporaryRemark가 팝업을 만들지 못했습니다.");
            return;
        }

        // 2. 텍스트 설정
        string cleanedText = CleanEmotionTags(text);
        popup.SetText(cleanedText); // ⭐️ 'text' 대신 'cleanedText'를 전달

        // 3. 기존에 실행 중인 자동 숨김 타이머가 있다면 중지 (새 타이머로 갱신)
        if (_hideTimerCoroutine != null)
        {
            StopCoroutine(_hideTimerCoroutine);
        }

        // 4. 새로운 자동 숨김 타이머 시작
        _hideTimerCoroutine = StartCoroutine(HidePopupAfterDelay(duration));
    }

    /// <summary>
    /// ⭐️ [신규]
    /// N초 뒤에 팝업을 숨기는 코루틴
    /// </summary>
    private System.Collections.IEnumerator HidePopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // ⭐️ 5초가 지났으므로 팝업 숨김
        Debug.Log("[PopupSpawner] 임시 팝업 시간 만료. 숨깁니다.");
        HidePopup();
        _hideTimerCoroutine = null; // 타이머 코루틴 참조 클리어
    }
    // ⭐️⭐️⭐️ [아래 메서드를 새로 추가하세요] ⭐️⭐️⭐️

    /// <summary>
    /// ⭐️ [신규] 텍스트에서 (감정) 태그를 정리합니다.
    /// (ChatInputManager의 CleanAndDetectEmotion와 유사한 로직)
    /// </summary>
    /// <param name="src">원본 텍스트 (예: "흥... (화남)")</param>
    /// <returns>정리된 텍스트 (예: "흥...")</returns>
    private string CleanEmotionTags(string src)
    {
        if (string.IsNullOrWhiteSpace(src)) return src;

        // ① (숫자자) 꼬리표 제거 (혹시 포함될 경우 대비)
        string temp = Regex.Replace(src, @"\s*\(\d+자\)\s*$", "");

        // ② (감정) 태그 감지: (기쁨|슬픔|보통|화남)
        var m = Regex.Match(temp, @"\((기쁨|슬픔|보통|화남)\)\s*$");

        if (m.Success)
        {
            // ⭐️ 감정이 감지되면, (감정) 태그 앞부분까지만 잘라서 반환
            return temp.Substring(0, m.Index).TrimEnd();
        }

        // ⭐️ 감지되지 않으면, (숫자자)만 제거된 텍스트 반환
        return temp;
    }
}
