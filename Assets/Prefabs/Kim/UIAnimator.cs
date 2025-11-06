using System.Collections;
using UnityEngine;

public class UIAnimator : MonoBehaviour
{
    [Header("필수 설정")]
    // 1. 움직일 대상 (인풋 필드)의 RectTransform
    public RectTransform targetRect;

    [Header("위치 설정")]
    // 2. '비포' 위치 (숨겨진 위치)
    public Vector2 hiddenPosition = new Vector2(0, -100f);

    // 3. '애프터' 위치 (보여질 위치)
    public Vector2 shownPosition = new Vector2(0, 100f);

    [Header("애니메이션 설정")]
    // 4. 애니메이션 속도 (초)
    public float animationDuration = 0.5f;

    // 현재 실행 중인 애니메이션 코루틴을 저장
    private Coroutine _animationCoroutine;

    // --- 상태 변수 ---
    private bool _isShown = false; // 현재 UI가 보여지고 있는지 여부


    /// <summary>
    /// 스크립트 시작 시, UI를 즉시 숨겨진 위치로 설정합니다.
    /// </summary>
    void Start()
    {
        if (targetRect == null)
        {
            Debug.LogError("Target Rect가 설정되지 않았습니다!");
            return;
        }
        // 시작할 때 숨겨진 위치에 있도록 설정
        targetRect.anchoredPosition = hiddenPosition;
        _isShown = false;
    }

    /// <summary>
    /// 버튼 OnClick()에 연결할 함수입니다.
    /// UI를 '애프터' 위치(shownPosition)로 부드럽게 이동시킵니다.
    /// </summary>
    public void ShowUI()
    {
        // 이미 보여지고 있거나 애니메이션 중이면 실행하지 않음
        if (_isShown) return;

        // 이전에 실행 중인 애니메이션이 있다면 중지
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        // '보여주기' 애니메이션 시작
        _animationCoroutine = StartCoroutine(AnimatePosition(shownPosition));
        _isShown = true;
    }

    /// <summary>
    /// (선택 사항) UI를 다시 '비포' 위치(hiddenPosition)로 숨깁니다.
    /// </summary>
    public void HideUI()
    {
        // 이미 숨겨져 있거나 애니메이션 중이면 실행하지 않음
        if (!_isShown) return;

        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        // '숨기기' 애니메이션 시작
        _animationCoroutine = StartCoroutine(AnimatePosition(hiddenPosition));
        _isShown = false;
    }

    /// <summary>
    /// (추천) Show/Hide를 번갈아 가며 실행하는 토글 함수
    /// </summary>
    public void ToggleUI()
    {
        if (_isShown)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }


    /// <summary>
    /// 지정된 목표 위치(targetPos)까지 부드럽게 이동하는 코루틴
    /// </summary>
    private IEnumerator AnimatePosition(Vector2 targetPos)
    {
        float timer = 0f;
        Vector2 startPos = targetRect.anchoredPosition; // 현재 위치에서 시작

        while (timer < animationDuration)
        {
            // 경과 시간을 0~1 사이의 값(진행률)으로 변환
            float progress = timer / animationDuration;

            // (선택) 부드러운 시작과 끝을 위한 Ease-Out 효과
            progress = 1 - Mathf.Pow(1 - progress, 3); // Ease-Out Cublic

            // Vector2.Lerp를 사용해 시작 위치와 목표 위치 사이를 보간
            targetRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);

            // 시간 증가 및 다음 프레임까지 대기
            timer += Time.deltaTime;
            yield return null; // 1프레임 대기
        }

        // 애니메이션이 끝나면 정확한 목표 위치로 설정
        targetRect.anchoredPosition = targetPos;
        _animationCoroutine = null; // 코루틴 완료
    }
}
