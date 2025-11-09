using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterGridManager : MonoBehaviour
{
    // 1. 씬에 배치한 캐릭터 버튼 4개를 여기에 연결
    [SerializeField] private List<CharacterSelectButton> characterButtons;
    private CharacterSelectButton currentSelectedButton;

    void Start()
    {
        // 1. 모든 버튼에게 "내가 너의 매니저다"라고 알려줌
        foreach (var button in characterButtons)
        {
            button.Setup(this);
        }

        // 2. 시작 시 첫 번째 버튼을 기본 선택 상태로 설정
        if (characterButtons.Count > 0)
        {
            currentSelectedButton = characterButtons[0];
            currentSelectedButton.SetSelected(true);
        }
    }

    // 3. CharacterSelectButton이 이 함수를 호출함
    public void OnCharacterSelected(CharacterSelectButton selectedButton)
    {
        // 이미 선택된 버튼을 또 누르면 아무것도 안 함
        if (currentSelectedButton == selectedButton)
        {
            return;
        }

        // 4. 기존에 선택됐던 버튼은 비선택 상태로 돌림
        if (currentSelectedButton != null)
        {
            currentSelectedButton.SetSelected(false);
        }

        // 5. 새로 클릭된 버튼을 선택 상태로 만듦
        selectedButton.SetSelected(true);
        currentSelectedButton = selectedButton;
    }
}