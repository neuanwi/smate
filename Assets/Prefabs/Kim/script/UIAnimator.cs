using System.Collections;
using UnityEngine;

public class UIAnimator : MonoBehaviour
{
    [Header("�ʼ� ����")]
    // 1. ������ ��� (��ǲ �ʵ�)�� RectTransform
    public RectTransform targetRect;

    [Header("��ġ ����")]
    // 2. '����' ��ġ (������ ��ġ)
    public Vector2 hiddenPosition = new Vector2(0, -100f);

    // 3. '������' ��ġ (������ ��ġ)
    public Vector2 shownPosition = new Vector2(0, 100f);

    [Header("�ִϸ��̼� ����")]
    // 4. �ִϸ��̼� �ӵ� (��)
    public float animationDuration = 0.5f;

    // ���� ���� ���� �ִϸ��̼� �ڷ�ƾ�� ����
    private Coroutine _animationCoroutine;

    // --- ���� ���� ---
    private bool _isShown = false; // ���� UI�� �������� �ִ��� ����


    /// <summary>
    /// ��ũ��Ʈ ���� ��, UI�� ��� ������ ��ġ�� �����մϴ�.
    /// </summary>
    void Start()
    {
        if (targetRect == null)
        {
            Debug.LogError("Target Rect�� �������� �ʾҽ��ϴ�!");
            return;
        }
        // ������ �� ������ ��ġ�� �ֵ��� ����
        targetRect.anchoredPosition = hiddenPosition;
        _isShown = false;
    }

    /// <summary>
    /// ��ư OnClick()�� ������ �Լ��Դϴ�.
    /// UI�� '������' ��ġ(shownPosition)�� �ε巴�� �̵���ŵ�ϴ�.
    /// </summary>
    public void ShowUI()
    {
        // �̹� �������� �ְų� �ִϸ��̼� ���̸� �������� ����
        if (_isShown) return;

        // ������ ���� ���� �ִϸ��̼��� �ִٸ� ����
        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        // '�����ֱ�' �ִϸ��̼� ����
        _animationCoroutine = StartCoroutine(AnimatePosition(shownPosition));
        _isShown = true;
    }

    /// <summary>
    /// (���� ����) UI�� �ٽ� '����' ��ġ(hiddenPosition)�� ����ϴ�.
    /// </summary>
    public void HideUI()
    {
        // �̹� ������ �ְų� �ִϸ��̼� ���̸� �������� ����
        if (!_isShown) return;

        if (_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        // '�����' �ִϸ��̼� ����
        _animationCoroutine = StartCoroutine(AnimatePosition(hiddenPosition));
        _isShown = false;
    }

    /// <summary>
    /// (��õ) Show/Hide�� ������ ���� �����ϴ� ��� �Լ�
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
    /// ������ ��ǥ ��ġ(targetPos)���� �ε巴�� �̵��ϴ� �ڷ�ƾ
    /// </summary>
    private IEnumerator AnimatePosition(Vector2 targetPos)
    {
        float timer = 0f;
        Vector2 startPos = targetRect.anchoredPosition; // ���� ��ġ���� ����

        while (timer < animationDuration)
        {
            // ��� �ð��� 0~1 ������ ��(�����)���� ��ȯ
            float progress = timer / animationDuration;

            // (����) �ε巯�� ���۰� ���� ���� Ease-Out ȿ��
            progress = 1 - Mathf.Pow(1 - progress, 3); // Ease-Out Cublic

            // Vector2.Lerp�� ����� ���� ��ġ�� ��ǥ ��ġ ���̸� ����
            targetRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, progress);

            // �ð� ���� �� ���� �����ӱ��� ���
            timer += Time.deltaTime;
            yield return null; // 1������ ���
        }

        // �ִϸ��̼��� ������ ��Ȯ�� ��ǥ ��ġ�� ����
        targetRect.anchoredPosition = targetPos;
        _animationCoroutine = null; // �ڷ�ƾ �Ϸ�
    }
}
