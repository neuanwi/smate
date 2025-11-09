using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectButton : MonoBehaviour
{
    // 1. 인스펙터에서 연결할 UI 요소들
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject checkmark;
    // [SerializeField] private Image iconContainerImage; // <-- 아이콘 컨테이너 관련 변수 제거

    // 2. 선택/비선택 상태의 색상
    [SerializeField] private Color selectedBgColor = Color.black;
    [SerializeField] private Color selectedNameColor = Color.white;
    // [SerializeField] private Color selectedIconBgColor; // <-- 제거

    [SerializeField] private Color defaultBgColor = Color.white;
    [SerializeField] private Color defaultNameColor = Color.black;
    // [SerializeField] private Color defaultIconBgColor; // <-- 제거

    // 3. 버튼 관리자 (CharacterGridManager)
    private CharacterGridManager gridManager;
    private Button myButton; // <-- Button 컴포넌트 캐싱

    void Start()
    {
        // 1. Button 컴포넌트를 찾아서 리스너 연결
        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnClickButton);
        }
        else
        {
            Debug.LogError("Button 컴포넌트를 찾을 수 없습니다!", this.gameObject);
        }
    }

    // 이 버튼을 초기화할 때 매니저를 설정
    public void Setup(CharacterGridManager manager)
    {
        this.gridManager = manager;
    }

    // 버튼이 클릭되었을 때 (시각 효과 처리용)
    private void OnClickButton()
    {
        // 매니저에게 "내가 클릭되었다"고 알림
        if (gridManager != null)
        {
            gridManager.OnCharacterSelected(this);
        }
        else
        {
            Debug.LogError("Grid Manager가 설정되지 않았습니다!", this.gameObject);
        }
    }

    // 4. 상태 변경 함수 (React의 조건부 스타일링과 동일)
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            // --- 선택된 상태 ---
            backgroundImage.color = selectedBgColor;
            nameText.color = selectedNameColor;
            // iconContainerImage.color = selectedIconBgColor; // <-- 제거
            checkmark.SetActive(true);
        }
        else
        {
            // --- 비선택 상태 (초기화) ---
            backgroundImage.color = defaultBgColor;
            nameText.color = defaultNameColor;
            // iconContainerImage.color = defaultIconBgColor; // <-- 제거
            checkmark.SetActive(false); // <-- ★★★ 이 부분이 체크마크 문제를 해결합니다!
        }
    }
}