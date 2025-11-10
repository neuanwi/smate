using UnityEngine;
using UnityEngine.UI; // 👈 LayoutRebuilder

public class PopupSpawner : MonoBehaviour
{
    [Header("UI 프리팹 (4방향)")] // ⭐️ (v1) 4방향 프리팹 사용
    public GameObject leftLowBubblePrefab;   // 좌측 하단 (x=0, y=0)
    public GameObject leftHighBubblePrefab;  // 좌측 상단 (x=0, y=1)
    public GameObject rightLowBubblePrefab;  // 우측 하단 (x=1, y=0)
    public GameObject rightHighBubblePrefab; // 우측 상단 (x=1, y=1)

    [Header("UI 프리팹 및 캔버스")]
    public Canvas parentCanvas;

    [Header("타겟 및 카메라")]
    public GameObject kirbyCharacter;
    public GameObject shihoCharacter;
    public Camera mainCamera;

    [Header("위치 오프셋")]
    [Tooltip("캐릭터 위치로부터의 UI 오프셋")]
    public Vector2 positionOffset; // ⭐️ (v1) 오프셋

    // --- 내부 변수 ---
    private PopupController _currentPopupInstance;
    private Transform _targetToFollow; // ⭐️ (v2) 따라다닐 대상

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    /// <summary>
    /// (v1) 4방향 로직으로 팝업을 '스폰'하고,
    /// (v2) '따라다니기'를 시작합니다.
    /// </summary>
    public PopupController ShowPopupNearTarget()
    {
        // --- 0. 활성화된 캐릭터 타겟 찾기 ---
        Transform activeCharacterTarget = null;
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
        {
            activeCharacterTarget = kirbyCharacter.transform;
        }
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
        {
            activeCharacterTarget = shihoCharacter.transform;
        }

        // --- 1. 필수 컴포넌트 검사 (4개 프리팹 모두 검사) ---
        if (leftLowBubblePrefab == null || leftHighBubblePrefab == null ||
             rightLowBubblePrefab == null || rightHighBubblePrefab == null ||
             parentCanvas == null || mainCamera == null)
        {
            Debug.LogError("[PopupSpawner] 필수 참조(프리팹 4종, Canvas, Camera)가 설정되지 않았습니다.");
            return null;
        }

        if (activeCharacterTarget == null)
        {
            Debug.LogError("[PopupSpawner] 활성화된 캐릭터 타겟(Kirby 또는 Shiho)을 찾을 수 없습니다!");
            return null;
        }

        // --- 2. ⭐️ (v1) 위치 및 피벗 *먼저* 계산 (프리팹 선택을 위해) ---
        Vector2 screenPos = mainCamera.WorldToScreenPoint(activeCharacterTarget.position);
        Vector2 newPivot = new Vector2(
             (screenPos.x < Screen.width / 2) ? 0f : 1f,
             (screenPos.y < Screen.height / 2) ? 0f : 1f
        );

        // --- 3. ⭐️ (v1) 올바른 프리팹 선택 ---
        GameObject prefabToSpawn = null;
        if (newPivot.x == 0) // 좌측
        {
            prefabToSpawn = (newPivot.y == 0) ? leftLowBubblePrefab : leftHighBubblePrefab;
        }
        else // 우측
        {
            prefabToSpawn = (newPivot.y == 0) ? rightLowBubblePrefab : rightHighBubblePrefab;
        }

        // --- 4. ⭐️ (v1) 팝업 인스턴스 확보 (및 '교체' 로직) ---
        // (만약 피벗이 달라졌다면, 기존 팝업을 파괴하고 새로 만듭니다)
        if (_currentPopupInstance != null)
        {
            RectTransform existingRect = _currentPopupInstance.transform as RectTransform;
            if (existingRect != null && existingRect.pivot != newPivot)
            {
                Destroy(_currentPopupInstance.gameObject);
                _currentPopupInstance = null;
            }
        }

        if (_currentPopupInstance == null)
        {
            GameObject newPopup = Instantiate(prefabToSpawn, parentCanvas.transform);
            _currentPopupInstance = newPopup.GetComponent<PopupController>();

            if (_currentPopupInstance == null)
            {
                Debug.LogError($"'{prefabToSpawn.name}' 프리팹에 PopupController.cs 스크립트가 없습니다!");
                Destroy(newPopup);
                return null;
            }
        }

        _currentPopupInstance.gameObject.SetActive(true);
        RectTransform popupRect = _currentPopupInstance.transform as RectTransform;

        // --- 5. ⭐️ (v1) 위치/피벗/오프셋 설정 ---
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
             parentCanvas.transform as RectTransform,
             screenPos,
             parentCanvas.worldCamera,
             out localPoint
        );

        float offsetX = (newPivot.x == 0) ? positionOffset.x : -positionOffset.x;
        float offsetY = (newPivot.y == 0) ? positionOffset.y : -positionOffset.y;

        popupRect.pivot = newPivot; // ⭐️ (v1) 피벗 설정
        popupRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY); // ⭐️ (v1) 위치 설정

        // --- 6. ⭐️ (v2) 따라다닐 대상으로 저장 ---
        _targetToFollow = activeCharacterTarget;

        // 7. 레이아웃 강제 갱신 및 리모컨 반환
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);
        return _currentPopupInstance;
    }

    /// <summary>
    /// (v2) 팝업을 숨기고 '따라다니기'를 중지합니다.
    /// </summary>
    public void HidePopup()
    {
        if (_currentPopupInstance != null)
        {
            _currentPopupInstance.gameObject.SetActive(false);
        }
        _targetToFollow = null; // ⭐️ 따라다니기 중지
    }


    /// <summary>
    /// (v2) LateUpdate에서 팝업이 캐릭터를 따라다니도록 위치를 갱신합니다.
    /// </summary>
    void LateUpdate()
    {
        // ⭐️ 따라다닐 대상(_targetToFollow)과 팝업(_currentPopupInstance)이 모두 유효할 때만 실행
        if (_targetToFollow != null && _currentPopupInstance != null)
        {
            RectTransform popupRect = _currentPopupInstance.transform as RectTransform;

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
            // (스폰될 때 설정된 '현재 피벗'을 기준으로 오프셋을 다시 계산합니다)
            Vector2 currentPivot = popupRect.pivot;
            float offsetX = (currentPivot.x == 0) ? positionOffset.x : -positionOffset.x;
            float offsetY = (currentPivot.y == 0) ? positionOffset.y : -positionOffset.y;

            // 3. ⭐️ 최종 위치 적용
            popupRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);

            // (선택적) 텍스트가 동적으로 변하는 경우 레이아웃 갱신
            // LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect); 
        }
    }
}