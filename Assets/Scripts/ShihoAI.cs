using System.Collections;
using UnityEngine;

public class ShihoAI : MonoBehaviour
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

    // --- 화면 경계를 위한 변수들 (기존과 동일) ---
    private Camera mainCamera;
    private float minX, maxX, minY, maxY;
    private float spriteHalfWidth, spriteHalfHeight;

    // --- ⬇️⬇️⬇️ UI 기능을 여기에 추가 ⬇️⬇️⬇️ ---

    /// <summary>
    /// Setting UI 관련
    /// </summary>

    //마우스가 끌고 있는 상태인지 기록
    private bool bMouseDrag = false;

    // UI 패널을 연결할 변수 추가
    public GameObject contextMenuPanel;

    // --- ⬇️⬇️⬇️ 여기에 두 줄을 추가하세요! ⬇️⬇️⬇️ ---
    public GameObject characterGridPanel;       // 캐릭터 선택 그리드 UI
    public GameObject gridBackgroundCatcher;    // 그리드 배경 클릭 캐처
    // --- ⬆️⬆️⬆️ 추가 끝 ⬆️⬆️⬆️ ---
    public GameObject clickCatcher; // '허공' 클릭을 감지하는 메인 캐처
    // AI가 메뉴 때문에 멈췄는지 기억
    private bool isPausedByMenu = false;

    // --- ⬆️⬆️⬆️ ---

    // 👈 [1] Start()를 Awake()로 변경합니다. (변수 초기화)
    void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // --- 화면 경계 계산 (기존과 동일) ---
        mainCamera = Camera.main;
        spriteHalfWidth = spriteRenderer.bounds.size.x / 2f;
        spriteHalfHeight = spriteRenderer.bounds.size.y / 2f;
        Vector3 minScreenPos = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, 0));
        Vector3 maxScreenPos = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, 0));
        minX = minScreenPos.x + spriteHalfWidth;
        maxX = maxScreenPos.x - spriteHalfWidth;
        minY = minScreenPos.y + spriteHalfHeight;
        maxY = maxScreenPos.y - spriteHalfHeight;
        // ------------------------------------------
    }

    // 👈 [2] OnEnable() 메서드를 추가합니다. (UI 숨김 및 AI 시작)
    // 이 메서드는 캐릭터가 활성화될 때마다 (캐릭터 변경 시) 호출됩니다.
    void OnEnable()
    {
        //시작할 때 UI 패널을 숨김
        if (contextMenuPanel != null)
        {
            contextMenuPanel.SetActive(false);
        }

        // (추가) 시작할 때 그리드 패널들도 숨김
        if (characterGridPanel != null)
        {
            characterGridPanel.SetActive(false);
        }
        if (gridBackgroundCatcher != null)
        {
            gridBackgroundCatcher.SetActive(false);
        }
        // --- ⬆️⬆️⬆️ ---

        isPausedByMenu = false;

        // AI가 중복 실행되지 않도록 확실하게 초기화
        StopAllCoroutines();
        StartCoroutine(ThinkAndAct());
    }

    // 👈 [3] Start()는 비워둡니다 (혹은 삭제)
    void Start()
    {
        // 모든 초기화 로직은 Awake()와 OnEnable()로 이동했습니다.
    }

    // --- ⬇️⬇️⬇️ UI 기능을 여기에 추가 ⬇️⬇️⬇️ ---
    void Update()
    {
        // AI가 메뉴 때문에 멈췄는데, 메뉴가 (허공 클릭 등으로) 꺼졌다면
        // (contextMenuPanel이 null이 아닌지 확인하는 방어 코드 추가)
        if (isPausedByMenu && contextMenuPanel != null && !contextMenuPanel.activeSelf && !bMouseDrag)
        {
            // (추가) 그리드 패널도 혹시 모르니 껐는지 확인
            if (characterGridPanel != null && characterGridPanel.activeSelf)
            {
                // 그리드 패널이 켜져있다면 AI는 계속 멈춰있어야 함
                return;
            }

            isPausedByMenu = false; // AI를 다시 시작시킬 거니까, 상태를 리셋

            // 👈 [4] AI 중복 실행을 막기 위해 StopAllCoroutines() 추가
            StopAllCoroutines();
            StartCoroutine(ThinkAndAct()); // AI(생각) 다시 시작!
        }
    }
    // --- ⬆️⬆️⬆️ ---

    // --- 화면 경계 제한 (기존과 동일) ---
    void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        currentPosition.x = Mathf.Clamp(currentPosition.x, minX, maxX);
        currentPosition.y = Mathf.Clamp(currentPosition.y, minY, maxY);
        transform.position = currentPosition;
    }


    // --- AI 행동 로직 (기존과 동일) ---
    IEnumerator ThinkAndAct()
    {
        while (true)
        {
            // --- 1. IDLE 또는 SIGH 상태 ---
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

            // --- 2. 걷기 상태 (기존과 동일) ---
            anim.SetBool("isWalking", true);

            float xDirection = (Random.Range(0, 2) == 0) ? -1f : 1f;
            float yDirection = 0f;
            if (Random.value < changeYDirectionChance)
            {
                yDirection = (Random.Range(0, 2) == 0) ? -1f : 1f;
            }

            spriteRenderer.flipX = (xDirection == -1f);

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

    // --- ⬇️⬇️⬇️ KirbyAI의 드래그 및 UI 로직으로 수정 ⬇️⬇️⬇️ ---

    // 1. Collider에 마우스 클릭이 '시작'될 때 1번 호출됨
    void OnMouseDown()
    {
        anim.SetBool("isDragging", true);
        StopAllCoroutines();

        bMouseDrag = true; // (KirbyAI 기능)

        // --- ⬇️⬇️⬇️ 여기가 수정된 부분입니다! ⬇️⬇️⬇️ ---
        // 3. 드래그가 시작되면 캐릭터 선택 관련 UI들을 강제로 숨깁니다.
        if (characterGridPanel != null)
        {
            characterGridPanel.SetActive(false);
        }
        if (gridBackgroundCatcher != null)
        {
            gridBackgroundCatcher.SetActive(false);
        }
        // --- ⬆️⬆️⬆️ 수정 끝 ⬆️⬆️⬆️ ---
    }

    // 2. 마우스를 '클릭한 채로 움직이는' 동안 매 프레임 호출됨
    void OnMouseDrag()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);
    }

    // 3. 마우스 버튼을 '뗄' 때 1번 호출됨
    void OnMouseUp()
    {
        anim.SetBool("isDragging", false);

        // 👈 [5] 드래그가 끝나면 AI의 메뉴 멈춤 상태도 해제합니다.
        isPausedByMenu = false;

        // AI 다시 시작
        StopAllCoroutines(); // 중복 방지
        StartCoroutine(ThinkAndAct());

        bMouseDrag = false; // (KirbyAI 기능)
    }

    // KirbyAI.cs 와 ShihoAI.cs 둘 다 수정
    void OnMouseOver()
    {
        // --- ⬇️⬇️⬇️ 바로 이 줄을 추가해야 합니다! ⬇️⬇️⬇️ ---
        // (수정) 그리드 패널이 켜져있는지 확인하는 변수
        bool isGridPanelActive = (characterGridPanel != null && characterGridPanel.activeSelf);
        // --- ⬆️⬆️⬆️ ---

        if (Input.GetMouseButtonDown(1) && !bMouseDrag && !isGridPanelActive) // 👈 이제 이 변수를 알 수 있음
        {
            StopAllCoroutines();

            isPausedByMenu = true; // AI를 메뉴 때문에 멈췄다고 기록

            if (contextMenuPanel != null)
            {
                contextMenuPanel.SetActive(true);
                contextMenuPanel.transform.position = gameObject.transform.position;
            }

            // (추가한 버그 수정 코드)
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
}