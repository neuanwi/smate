using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    public GameObject mainUIPanelToClose;
    public List<GameObject> allCharacters;
    private int currentCharacterIndex = 0;
    public static CharacterManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        foreach (GameObject character in allCharacters)
        {
            character.SetActive(false);
        }

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

        // --- ▼▼▼ 여기가 수정된 부분입니다 ▼▼▼ ---

        // 1. 현재 켜져있는 (old) 캐릭터의 Transform을 가져옵니다.
        Transform oldCharacterTransform = allCharacters[currentCharacterIndex].transform;

        // 2. (NEW) 현재 캐릭터의 위치와 회전값을 변수에 저장합니다.
        Vector3 oldPosition = oldCharacterTransform.position;
        Quaternion oldRotation = oldCharacterTransform.rotation;

        // 3. (Original) 현재 캐릭터를 끕니다.
        allCharacters[currentCharacterIndex].SetActive(false);

        // 4. (Original) 인덱스를 새 캐릭터로 업데이트합니다.
        currentCharacterIndex = index;

        // 5. (NEW) 이제 '새' 캐릭터의 Transform을 가져옵니다.
        Transform newCharacterTransform = allCharacters[currentCharacterIndex].transform;

        // 6. (NEW) 새 캐릭터의 위치/회전값을 2번에서 저장한 값으로 덮어씁니다.
        newCharacterTransform.position = oldPosition;
        newCharacterTransform.rotation = oldRotation;

        // 7. (Original) 새 캐릭터를 켭니다. (이제 oldPosition 위치에서 켜집니다)
        allCharacters[currentCharacterIndex].SetActive(true);

        // --- ▲▲▲ 수정 끝 ▲▲▲ ---

        if (mainUIPanelToClose != null)
        {
            mainUIPanelToClose.SetActive(false);
        }
    }
}