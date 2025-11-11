using UnityEngine;
using TMPro;
using System.Collections;

public class PopupController : MonoBehaviour
{
    [Header("텍스트 타이핑")]
    [SerializeField] private TextMeshProUGUI answerText;
    [SerializeField][Tooltip("글자당 딜레이(초)")] private float typingSpeed = 0.02f;
    private Coroutine _typingCoroutine;

    [Header("슬라이드 인 설정")]
    [SerializeField]
    [Tooltip("팝업이 켜질 때 슬라이드 인을 자동 실행할지 여부")]
    private bool playSlideOnEnable = true;

    [SerializeField]
    [Tooltip("슬라이드 인 시간(초)")]
    private float slideDuration = 0.25f;

    [SerializeField]
    [Tooltip("기본 위치에서 얼마나 떨어진 곳에서 시작할지 (x,y)")]
    private Vector2 slideInOffset = new Vector2(-80f, 0f);

    private RectTransform _rect;
    private CanvasGroup _canvasGroup;
    private Coroutine _slideCoroutine;


    private void Awake()
    {
        _rect = transform as RectTransform;
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        if (playSlideOnEnable)
        {
            PlaySlideIn();
        }
    }

    /// <summary>
    /// 외부에서 강제로 슬라이드 인을 실행하고 싶을 때 호출
    /// </summary>
    public void PlaySlideIn()
    {
        if (_rect == null) return;

        // 이전 슬라이드 코루틴이 돌고 있으면 중단
        if (_slideCoroutine != null)
        {
            StopCoroutine(_slideCoroutine);
            _slideCoroutine = null;
        }

        // 최종 위치 기억
        Vector2 targetPos = _rect.anchoredPosition;

        // 시작 위치를 오프셋만큼 밀어둠
        _rect.anchoredPosition = targetPos + slideInOffset;

        // 투명하게 시작
        _canvasGroup.alpha = 0f;

        _slideCoroutine = StartCoroutine(SlideInRoutine(targetPos));
    }

    private IEnumerator SlideInRoutine(Vector2 targetPos)
    {
        float t = 0f;
        Vector2 startPos = _rect.anchoredPosition;

        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            // 부드러운 감속(Ease-out)
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            _rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
            _canvasGroup.alpha = eased;

            yield return null;
        }

        _rect.anchoredPosition = targetPos;
        _canvasGroup.alpha = 1f;
        _slideCoroutine = null;
    }

    /// <summary>
    /// 관제 역할 스크립트에서 호출하는 텍스트 세팅
    /// </summary>
    public void SetText(string message)
    {
        // 타이핑 중이면 중단
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }

        if (answerText == null)
        {
            Debug.LogError("[PopupController] TextMeshProUGUI가 연결되지 않았습니다.");
            return;
        }

        _typingCoroutine = StartCoroutine(AnimateTypingText(message));
    }

    private IEnumerator AnimateTypingText(string fullMessage)
    {
        answerText.text = "";

        foreach (char letter in fullMessage)
        {
            answerText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        _typingCoroutine = null;
    }

    public void ClosePopup()
    {
        gameObject.SetActive(false);
    }
}