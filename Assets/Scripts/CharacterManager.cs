using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    // 1. Inspector 창에서 우리가 가진 모든 캐릭터를 이 리스트에 등록합니다.
    public List<GameObject> allCharacters;

    // 2. 현재 활성화된 캐릭터가 리스트의 몇 번째인지 기억합니다.
    private int currentCharacterIndex = 0;

    // 3. (추가) 이 매니저를 '싱글톤(Singleton)'으로 만듭니다.
    //    (어디서든 "CharacterManager.instance"로 쉽게 접근 가능)
    public static CharacterManager instance;

    void Awake()
    {
        // 4. instance를 자기 자신으로 설정
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // 이미 존재하면 스스로 파괴 (중복 방지)
            Destroy(gameObject);
        }
    }


    void Start()
    {
        // 5. 게임이 시작되면, '모든' 캐릭터를 일단 다 끕니다.
        foreach (GameObject character in allCharacters)
        {
            character.SetActive(false);
        }

        // 6. 리스트의 첫 번째(Index 0) 캐릭터만 켭니다.
        if (allCharacters.Count > 0)
        {
            allCharacters[currentCharacterIndex].SetActive(true);
        }
    }

    // 7. '캐릭터 선택창'이 호출할 공용 함수입니다.
    public void SwitchToCharacter(int index)
    {
        // 0~10+ 사이의 유효한 숫자일 때만 실행
        if (index < 0 || index >= allCharacters.Count)
        {
            Debug.LogWarning("잘못된 캐릭터 인덱스입니다: " + index);
            return;
        }

        // 이미 선택된 캐릭터면 무시
        if (index == currentCharacterIndex)
        {
            return;
        }

        // 1. 현재 켜져있는 캐릭터를 끈다.
        allCharacters[currentCharacterIndex].SetActive(false);

        // 2. 선택된 인덱스로 캐릭터를 바꾼다.
        currentCharacterIndex = index;
        allCharacters[currentCharacterIndex].SetActive(true);
    }
}