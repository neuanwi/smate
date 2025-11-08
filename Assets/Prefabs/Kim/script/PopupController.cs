using UnityEngine;
using TMPro; // 👈 TextMeshPro를 사용하려면 필수!
using System.Collections; // 👈 Coroutine(코루틴)을 사용하려면 필수!

public class PopupController : MonoBehaviour
{
    // 1. 인스펙터에서 팝업 안의 'Text (TMP)' 오브젝트를 연결합니다.
    [SerializeField]
    private TextMeshProUGUI answerText;

    // 2. ⭐️ 유저가 요청한 바로 그 기능! 인스펙터에서 속도 조절
    [SerializeField]
    [Tooltip("AI 답변 글자당 지연(초). 예: 0.02f")]
    private float typingSpeed = 0.02f;

    // 3. ⭐️ 현재 실행 중인 타이핑 애니메이션을 제어하기 위한 변수
    private Coroutine _typingCoroutine;

    /// <summary>
    /// 관제탑(ChatManager)에서 호출할 함수입니다.
    /// 이제 텍스트를 '즉시' 설정하지 않고, '타이핑 애니메이션'을 시작시킵니다.
    /// </summary>
    public void SetText(string message)
    {
        // 1. ⭐️ 만약 이전 타이핑 애니메이션이 실행 중이었다면, 즉시 중지!
        // (예: "생각 중..."이 타이핑되다가, 답변이 와서 덮어쓸 때)
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        if (answerText == null)
        {
            Debug.LogError("PopupController에 'Answer Text'가 연결되지 않았습니다!");
            return;
        }

        // 2. ⭐️ 새로운 타이핑 코루틴을 시작하고, 제어할 수 있도록 저장합니다.
        _typingCoroutine = StartCoroutine(AnimateTypingText(message));
    }

    /// <summary>
    /// 텍스트를 한 글자씩 타이핑하는 애니메이션 코루틴
    /// </summary>
    private IEnumerator AnimateTypingText(string fullMessage)
    {
        // 1. 텍스트를 비워서 "초기화"
        answerText.text = "";

        // 2. 메시지를 한 글자씩 루프
        foreach (char letter in fullMessage)
        {
            answerText.text += letter; // 텍스트에 한 글자 추가
            yield return new WaitForSeconds(typingSpeed); // 딜레이
        }

        // 3. ⭐️ 타이핑이 끝났으므로 "리모컨" 변수를 비움
        _typingCoroutine = null;
    }


    /// <summary>
    /// (선택) 팝업에 '닫기' 버튼이 있다면 이 함수를 연결하세요.
    /// </summary>
    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}