using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectButton : MonoBehaviour
{
    // 1. 인스펙터에서 연결할 UI 요소들
    // 'backgroundImage' 대신 'selectionHighlight' 이미지를 연결합니다.
    [SerializeField] private Image selectionHighlight;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject checkmark;

    // 2. 선택/비선택 상태의 색상
    [SerializeField] private Color selectedNameColor = Color.white;
    [SerializeField] private Color defaultNameColor = Color.black;

    // (시작 시 알파값을 0으로 설정하기 위한 색상 변수)
    private Color highlightColorVisible;
    private Color highlightColorHidden;

    private CharacterGridManager gridManager;
    private Button myButton;

    void Start()
    {
        // 3. 색상 초기화 (검은색 + 알파값 1 또는 0)
        highlightColorVisible = new Color(0, 0, 0, 1); // A=255
        highlightColorHidden = new Color(0, 0, 0, 0);  // A=0

        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnClickButton);
        }
    }

    public void Setup(CharacterGridManager manager)
    {
        this.gridManager = manager;
    }

    private void OnClickButton()
    {
        if (gridManager != null)
        {
            gridManager.OnCharacterSelected(this);
        }
    }

    // 4. 상태 변경 함수 (★이것만 바뀜★)
    public void SetSelected(bool isSelected)
    {
        if (isSelected)
        {
            // --- 선택된 상태 ---
            selectionHighlight.color = highlightColorVisible; // A=255 (보이게)
            nameText.color = selectedNameColor;               // 글자 하얗게
            checkmark.SetActive(true);
        }
        else
        {
            // --- 비선택 상태 (초기화) ---
            selectionHighlight.color = highlightColorHidden;  // A=0 (투명하게)
            nameText.color = defaultNameColor;                // 글자 검은색으로
            checkmark.SetActive(false);
        }
    }
}