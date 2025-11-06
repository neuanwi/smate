using UnityEngine;
using UnityEngine.UI; // 👈 LayoutRebuilder를 사용하기 위해 필수입니다!

public class PopupSpawner : MonoBehaviour
{
    [Header("UI 프리팹 및 캔버스")]
    // 1. 생성할 팝업 프리팹 (rightPopup)
    public GameObject rightPopupPrefab;

    // 2. 팝업이 속할 최상위 캔버스 (⭐️ Transform이 아닌 Canvas 컴포넌트)
    public Canvas parentCanvas;

    [Header("타겟 및 카메라")]
    // 3. 위치의 기준이 될 3D 캐릭터 Transform
    public Transform characterTarget;

    // 4. 메인 카메라 (비워두면 Start에서 'MainCamera' 태그로 자동 검색)
    public Camera mainCamera;

    // 5. 캐릭터 위치로부터의 추가 오프셋
    public Vector2 positionOffset;

    // --- 내부 변수 ---
    // 6. 현재 생성되어 떠 있는 팝업의 리모컨
    private PopupController _currentPopupInstance;

    // 7. LateUpdate에서 위치를 갱신해야 하는지 여부 (피벗 변경 시 필수)
    private bool _needsPositionUpdate = false;

    void Start()
    {
        // 카메라가 할당되지 않았다면 'MainCamera' 태그로 자동 검색
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// (ChatInputManager가 호출) 3D 타겟 근처에 팝업을 표시하고,
    /// 해당 팝업의 'PopupController' 리모컨을 반환합니다.
    /// </summary>
    public PopupController ShowPopupNearTarget()
    {
        // --- 1. 필수 컴포넌트 검사 ---
        if (rightPopupPrefab == null || parentCanvas == null || characterTarget == null || mainCamera == null)
        {
            Debug.LogError("[PopupSpawner] 필수 참조(Prefab, Canvas, Target, Camera)가 설정되지 않았습니다.");
            return null; // 👈 실패 시 null 반환
        }

        // --- 2. 팝업 인스턴스 확보 ---
        if (_currentPopupInstance == null)
        {
            // 팝업이 없다면 새로 생성
            GameObject newPopup = Instantiate(rightPopupPrefab, parentCanvas.transform);

            // ⭐️ 핵심: 생성된 팝업에서 '리모컨'을 가져옵니다.
            _currentPopupInstance = newPopup.GetComponent<PopupController>();

            if (_currentPopupInstance == null)
            {
                // 프리팹에 PopupController.cs 스크립트가 없는 경우 에러 발생
                Debug.LogError($"'{rightPopupPrefab.name}' 프리팹에 PopupController.cs 스크립트가 없습니다!");
                Destroy(newPopup);
                return null; // 👈 실패 시 null 반환
            }
        }

        // 팝업이 비활성화 상태라면 활성화
        _currentPopupInstance.gameObject.SetActive(true);

        // 위치 조정을 위해 팝업의 RectTransform을 가져옵니다.
        RectTransform popupRect = _currentPopupInstance.transform as RectTransform;

        // --- 3. 위치 계산 (OpenChatPanel 고급 로직) ---

        // 3-1. 3D 월드 좌표 -> 2D 스크린 좌표
        Vector2 screenPos = mainCamera.WorldToScreenPoint(characterTarget.position);

        // 3-2. 동적 피벗 계산 (화면 4분면 기준)
        Vector2 newPivot = new Vector2(
            (screenPos.x < Screen.width / 2) ? 0f : 1f,
            (screenPos.y < Screen.height / 2) ? 0f : 1f
        );

        // 3-3. 스크린 좌표 -> 캔버스 로컬 좌표
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            screenPos,
            parentCanvas.worldCamera,
            out localPoint
        );

        // 3-4. 피벗에 따른 오프셋 방향 조정
        float offsetX = (newPivot.x == 0) ? positionOffset.x : -positionOffset.x;
        float offsetY = (newPivot.y == 0) ? positionOffset.y : -positionOffset.y;

        // --- 4. 팝업에 적용 및 갱신 예약 ---
        popupRect.pivot = newPivot;
        popupRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);

        _needsPositionUpdate = true;

        // 5. ⭐️ ChatInputManager에게 "리모컨"을 반환합니다!
        return _currentPopupInstance;
    }

    /// <summary>
    /// LateUpdate에서 레이아웃을 강제 갱신합니다.
    /// </summary>
    void LateUpdate()
    {
        if (_needsPositionUpdate)
        {
            _needsPositionUpdate = false;

            if (_currentPopupInstance != null)
            {
                RectTransform popupRect = _currentPopupInstance.transform as RectTransform;
                if (popupRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
                }
            }
        }
    }
}
