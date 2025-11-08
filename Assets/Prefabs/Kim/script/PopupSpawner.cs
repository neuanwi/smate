using UnityEngine;

using UnityEngine.UI; // 👈 LayoutRebuilder를 사용하기 위해 필수입니다!



public class PopupSpawner : MonoBehaviour

{

    [Header("UI 프리팹 (4방향)")] // ⭐️ 1. 4방향 프리팹으로 교체

    public GameObject leftLowBubblePrefab;   // 좌측 하단 (x=0, y=0)

    public GameObject leftHighBubblePrefab;  // 좌측 상단 (x=0, y=1)

    public GameObject rightLowBubblePrefab;  // 우측 하단 (x=1, y=0)

    public GameObject rightHighBubblePrefab; // 우측 상단 (x=1, y=1)



    [Header("UI 프리팹 및 캔버스")]
    public Canvas parentCanvas;

    // 👇 [수정] --------------------------------
    [Header("타겟 및 카메라")]
    // 3. 위치의 기준이 될 3D 캐릭터 Transform
    // public Transform characterTarget; // 👈 [삭제] 이 줄을 삭제합니다.

    // 👇 [추가] 3. 위치의 기준이 될 동적 캐릭터 타겟들
    public GameObject kirbyCharacter;
    public GameObject shihoCharacter;

    // 4. 메인 카메라 (비워두면 Start에서 'MainCamera' 태그로 자동 검색)
    public Camera mainCamera;
    // 👆 [수정] --------------------------------

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
        // 👇 [추가] --------------------------------
        // --- 0. 활성화된 캐릭터 타겟 찾기 ---
        // ⭐️ kirbyCharacter와 shihoCharacter의 활성화 상태를 확인합니다.
        Transform activeCharacterTarget = null;
        if (kirbyCharacter != null && kirbyCharacter.activeInHierarchy)
        {
            activeCharacterTarget = kirbyCharacter.transform;
        }
        else if (shihoCharacter != null && shihoCharacter.activeInHierarchy)
        {
            activeCharacterTarget = shihoCharacter.transform;
        }
        // 👆 [추가] --------------------------------


        // --- 1. 필수 컴포넌트 검사 (4개 프리팹 모두 검사) ---
        // 👇 [수정] --------------------------------
        // characterTarget 검사를 삭제하고, activeCharacterTarget 검사를 아래로 이동
        if (leftLowBubblePrefab == null || leftHighBubblePrefab == null ||
             rightLowBubblePrefab == null || rightHighBubblePrefab == null ||
             parentCanvas == null || mainCamera == null)
        {
            Debug.LogError("[PopupSpawner] 필수 참조(프리팹 4종, Canvas, Camera)가 설정되지 않았습니다.");
            return null; // 👈 실패 시 null 반환
        }

        // 👇 [추가] --------------------------------
        // ⭐️ 활성화된 타겟이 있는지도 확인합니다.
        if (activeCharacterTarget == null)
        {
            Debug.LogError("[PopupSpawner] 활성화된 캐릭터 타겟(Kirby 또는 Shiho)을 찾을 수 없습니다!");
            return null;
        }
        // 👆 [추가] --------------------------------


        // --- 2. ⭐️ 위치 및 피벗 *먼저* 계산 (프리팹 선택을 위해) ---
        // 3-1. 3D 월드 좌표 -> 2D 스크린 좌표
        // 👇 [수정] --------------------------------
        // ⭐️ 'activeCharacterTarget'의 위치를 사용합니다.
        Vector2 screenPos = mainCamera.WorldToScreenPoint(activeCharacterTarget.position);
        // 👆 [수정] --------------------------------


        // 3-2. 동적 피벗 계산 (화면 4분면 기준)
        Vector2 newPivot = new Vector2(
             (screenPos.x < Screen.width / 2) ? 0f : 1f,
             (screenPos.y < Screen.height / 2) ? 0f : 1f
        );

        // --- 3. ⭐️ 올바른 프리팹 선택 ---
        GameObject prefabToSpawn = null;
        if (newPivot.x == 0) // 좌측
        {
            prefabToSpawn = (newPivot.y == 0) ? leftLowBubblePrefab : leftHighBubblePrefab;
        }
        else // 우측 (newPivot.x == 1)
        {
            prefabToSpawn = (newPivot.y == 0) ? rightLowBubblePrefab : rightHighBubblePrefab;
        }

        // --- 4. ⭐️ 팝업 인스턴스 확보 (및 '교체' 로직) ---
        if (_currentPopupInstance != null)
        {
            RectTransform existingRect = _currentPopupInstance.transform as RectTransform;
            if (existingRect != null && existingRect.pivot != newPivot)
            {
                Destroy(_currentPopupInstance.gameObject); // 기존 팝업 파괴
                _currentPopupInstance = null; // 리모컨 비우기 (새로 생성되도록)
            }
        }

        // 팝업이 없다면 (방금 파괴했거나 원래 없었으면) 새로 생성
        if (_currentPopupInstance == null)
        {
            GameObject newPopup = Instantiate(prefabToSpawn, parentCanvas.transform);
            _currentPopupInstance = newPopup.GetComponent<PopupController>();

            if (_currentPopupInstance == null)
            {
                Debug.LogError($"'{prefabToSpawn.name}' 프리팹에 PopupController.cs 스크립트가 없습니다!");
                Destroy(newPopup);
                return null; // 👈 실패 시 null 반환
            }
        }

        // 팝업이 비활성화 상태라면 활성화
        _currentPopupInstance.gameObject.SetActive(true);
        RectTransform popupRect = _currentPopupInstance.transform as RectTransform;

        // --- 5. ⭐️ 위치 계산 (OpenChatPanel 고급 로직) ---
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

        // --- 6. 팝업에 적용 및 갱신 예약 ---
        popupRect.pivot = newPivot;
        popupRect.anchoredPosition = localPoint + new Vector2(offsetX, offsetY);
        _needsPositionUpdate = true;

        // 7. ⭐️ ChatInputManager에게 "리모컨"을 반환합니다!
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

                    // ⭐️ 피벗 변경 후 위치가 어긋나는 것을 방지하기 위해

                    // ContentSizeFitter 등이 즉시 반영되도록 강제 갱신합니다.

                    LayoutRebuilder.ForceRebuildLayoutImmediate(popupRect);

                }

            }

        }

    }

}