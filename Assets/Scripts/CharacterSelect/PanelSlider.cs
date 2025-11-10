using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class PanelSlider : MonoBehaviour
{
    private RectTransform rectTransform;
    private Coroutine animationCoroutine;

    // 1. 인스펙터에서 설정할 값들
    [Header("애니메이션 설정")]
    [SerializeField] private Vector2 onScreenPosition; // 화면에 보일 때의 Pos (X, Y)
    [SerializeField] private Vector2 offScreenPosition; // 화면 밖 (오른쪽)에 있을 때의 Pos (X, Y)
    [SerializeField] private float animationDuration = 0.4f; // 애니메이션 속도 (초)

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // 2. 패널을 '보이게' 하는 함수 (버튼에서 호출)
    public void ShowPanel()
    {
        // 1. 혹시 실행 중인 애니메이션이 있다면 중지
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        // 2. 패널을 활성화
        gameObject.SetActive(true);

        // 3. '보이는' 위치로 이동하는 애니메이션 시작
        animationCoroutine = StartCoroutine(AnimatePanel(offScreenPosition, onScreenPosition));
    }

    // 3. 패널을 '숨기는' 함수 (버튼에서 호출)
    public void HidePanel()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        // '숨겨진' 위치로 이동하는 애니메이션 시작
        animationCoroutine = StartCoroutine(AnimatePanel(onScreenPosition, offScreenPosition));
    }

    // 4. 실제 애니메이션을 처리하는 코루틴
    private IEnumerator AnimatePanel(Vector2 startPos, Vector2 endPos)
    {
        float time = 0;

        while (time < animationDuration)
        {
            // 부드러운 움직임을 위해 Lerp(선형 보간) 사용
            float t = time / animationDuration;
            // Ease-Out 효과 (점점 느려지게)
            t = 1 - Mathf.Pow(1 - t, 3);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, t);

            time += Time.deltaTime;
            yield return null; // 다음 프레임까지 대기
        }

        // 5. 애니메이션 종료 후
        rectTransform.anchoredPosition = endPos; // 정확한 위치에 고정

        // 만약 '숨겨진' 위치로 이동한 거라면, 애니메이션이 끝난 뒤 비활성화
        if (endPos == offScreenPosition)
        {
            gameObject.SetActive(false);
        }

        animationCoroutine = null;
    }
}