using System.Collections;
using UnityEngine;
using UnityEngine.UI; // 👈 [추가 1] UI 기능을 사용하기 위해 추가

public class SonicAI : MonoBehaviour
{
    // 설정값 (Inspector 창에서 수정 가능)
    public float moveSpeed = 1.5f;     // 걷는 속도
    public float minIdleTime = 1.0f;   // 최소한 가만히 있는 시간
    public float maxIdleTime = 3.0f;   // 최대한 가만히 있는 시간
    public float minMoveTime = 2.0f;   // 최소한 걷는 시간
    public float maxMoveTime = 4.0f;   // 최대한 걷는 시간
    public float changeYDirectionChance = 0.5f;

    [Range(0, 1)]
    public float event1Chance = 0.3f;
    public float event1Duration = 2.0f; // 한숨 애니메이션 실제 길이

    private Animator anim;
    private SpriteRenderer spriteRenderer;

    // --- 화면 경계를 위한 변수들 (이전과 동일) ---
    private Camera mainCamera;
    private float minX, maxX, minY, maxY;
    private float spriteHalfWidth, spriteHalfHeight;

    /// <summary>
    /// Setting UI 관련
    /// </summary>

    //마우스가 끌고 있는 상태인지 기록
    private bool bMouseDrag = false;

    // UI 패널을 연결할 변수 추가
    public GameObject contextMenuPanel;
    public GameObject characterGridPanel;       // 캐릭터 선택 그리드 UI
    public GameObject gridBackgroundCatcher;    // 그리드 배경 클릭 캐처
    public GameObject clickCatcher; // '허공' 클릭을 감지하는 메인 캐처

    // UI 오프셋 변수 추가
    public Vector3 uiOffset = new Vector3(0f, 50f, 0f); // (이 값은 이제 사용되지 않습니다)
    private bool isPausedByMenu = false; // 메뉴 때문에 AI가 멈췄는지 기억

    // 👈 --- [추가 2] KirbyAI의 애니메이션 변수들 추가 ---
    [Header("Context Menu Animation")]
    public GameObject[] animatedMenuItems; // 애니메이션을 적용할 메뉴 아이템(버튼)들
    public float menuAnimDuration = 0.15f; // 각 아이템이 커지는 데 걸리는 시간
    public float menuAnimStagger = 0.05f;  // 아이템이 순차적으로 나타나는 시간 간격
    // --- ⬆️⬆️⬆️ ---

    void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        spriteHalfWidth = spriteRenderer.bounds.size.x / 2f;
        spriteHalfHeight = spriteRenderer.bounds.size.y / 2f;
        Vector3 minScreenPos = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 maxScreenPos = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        minX = minScreenPos.x + spriteHalfWidth;
        maxX = maxScreenPos.x - spriteHalfWidth;
        minY = minScreenPos.y + spriteHalfHeight;
        maxY = maxScreenPos.y - spriteHalfHeight;
    }

    void OnEnable()
    {
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(false);
        }
        if (characterGridPanel != null)
        {
            characterGridPanel.SetActive(false);
        }
        if (gridBackgroundCatcher != null)
        {
            gridBackgroundCatcher.SetActive(false);
        }
        isPausedByMenu = false;
        StopAllCoroutines();
        StartCoroutine(ThinkAndAct());
    }

    void Start()
    {
        // 비워둠
    }

    void Update()
    {
        if (isPausedByMenu && contextMenuPanel != null && !contextMenuPanel.activeSelf && !bMouseDrag)
        {
            if (characterGridPanel != null && characterGridPanel.activeSelf)
            {
                return;
            }
            isPausedByMenu = false;
            StopAllCoroutines();
            StartCoroutine(ThinkAndAct());
        }
    }

    void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        currentPosition.x = Mathf.Clamp(currentPosition.x, minX, maxX);
        currentPosition.y = Mathf.Clamp(currentPosition.y, minY, maxY);
        transform.position = currentPosition;
    }


    IEnumerator ThinkAndAct()
    {
        while (true)
        {
            anim.SetBool("isWalking", false);

            if (Random.value < event1Chance)
            {
                anim.SetTrigger("Event1");
                yield return new WaitForSeconds(event1Duration);
            }
            else
            {
                float idleTime = Random.Range(minIdleTime, maxIdleTime);
                yield return new WaitForSeconds(idleTime);
            }

            anim.SetBool("isWalking", true);
            float xDirection = (Random.Range(0, 2) == 0) ? -1f : 1f;
            float yDirection = 0f;
            if (Random.value < changeYDirectionChance)
            {
                yDirection = (Random.Range(0, 2) == 0) ? -1f : 1f;
            }

            spriteRenderer.flipX = (xDirection == 1f); // Sonic 고유 로직

            float moveTime = Random.Range(minMoveTime, maxMoveTime);
            float timer = 0;
            while (timer < moveTime)
            {
                Vector3 moveVector = new Vector3(xDirection, yDirection, 0);
                transform.Translate(moveVector.normalized * moveSpeed * Time.deltaTime);
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    // --- 마우스 드래그 로직 (이전과 동일) ---
    void OnMouseDown()
    {
        anim.SetBool("isDragging", true);
        StopAllCoroutines();
        bMouseDrag = true;
        if (characterGridPanel != null)
        {
            characterGridPanel.SetActive(false);
        }
        if (gridBackgroundCatcher != null)
        {
            gridBackgroundCatcher.SetActive(false);
        }
    }

    void OnMouseDrag()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);
    }

    void OnMouseUp()
    {
        anim.SetBool("isDragging", false);
        isPausedByMenu = false;
        StopAllCoroutines();
        StartCoroutine(ThinkAndAct());
        bMouseDrag = false;
    }

    // 👈 --- [수정 3] KirbyAI의 OnMouseOver 로직 적용 (단, uiOffset 제거) ---
    void OnMouseOver()
    {
        bool isGridPanelActive = (characterGridPanel != null && characterGridPanel.activeSelf);

        if (Input.GetMouseButtonDown(1) && !bMouseDrag && !isGridPanelActive)
        {
            StopAllCoroutines();
            isPausedByMenu = true;

            if (contextMenuPanel != null)
            {
                // 🚨🚨🚨 여기가 수정된 핵심입니다! 🚨🚨🚨
                // 'uiOffset'을 더하지 않고, 캐릭터의 월드 위치를 그대로 사용합니다.
                // (Kirby가 0,0,0으로 잘 작동하는 것과 동일하게 맞춥니다)
                contextMenuPanel.transform.position = gameObject.transform.position;

                // 패널(배경)을 먼저 활성화
                contextMenuPanel.SetActive(true);

                // 애니메이션 코루틴 시작!
                StartCoroutine(ShowAnimatedMenuItems());
            }

            if (clickCatcher != null)
            {
                clickCatcher.SetActive(true);
            }
        }
        else if (Input.GetMouseButtonDown(0))
        {
            if (contextMenuPanel != null)
            {
                contextMenuPanel.SetActive(false);
            }
        }
    }

    // 👈 --- [추가 4] KirbyAI의 애니메이션 코루틴 2개 추가 ---

    /// <summary>
    /// 메뉴 아이템들을 순차적으로 보여주는 코루틴 (깜빡임 수정됨)
    /// </summary>
    IEnumerator ShowAnimatedMenuItems()
    {
        // "깜빡임" 현상을 제거하기 위해
        // 애니메이션 시작 전, 모든 버튼을 미리 끄고 크기를 리셋합니다.
        foreach (var item in animatedMenuItems)
        {
            if (item != null)
            {
                item.SetActive(false);
                item.transform.localScale = Vector3.zero;
            }
        }

        // 이제 (깨끗한 상태에서) 하나씩 순차적으로 켭니다.
        foreach (var item in animatedMenuItems)
        {
            if (item == null) continue;

            item.SetActive(true);
            StartCoroutine(AnimateItemPopIn(item.transform));
            yield return new WaitForSeconds(menuAnimStagger);
        }
    }

    /// <summary>
    /// 개별 아이템의 크기를 0에서 1로 키우는 'Pop-in' 애니메이션
    /// </summary>
    IEnumerator AnimateItemPopIn(Transform itemTransform)
    {
        float timer = 0f;
        while (timer < menuAnimDuration)
        {
            if (itemTransform == null || !itemTransform.gameObject.activeInHierarchy)
                yield break;

            float progress = Mathf.Clamp01(timer / menuAnimDuration);
            float scale = 1f - (1f - progress) * (1f - progress); // Quadratic Ease-Out

            itemTransform.localScale = new Vector3(scale, scale, scale);
            timer += Time.deltaTime;
            yield return null;
        }

        if (itemTransform != null)
            itemTransform.localScale = Vector3.one;
    }
}