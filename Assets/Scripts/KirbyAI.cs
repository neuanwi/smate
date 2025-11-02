using System.Collections;
using UnityEngine;

public class KirbyAI : MonoBehaviour
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

    void Start()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // --- 화면 경계 계산 (이전과 동일) ---
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

        StartCoroutine(ThinkAndAct());
    }

    // --- 화면 경계 제한 (이전과 동일) ---
    void LateUpdate()
    {
        Vector3 currentPosition = transform.position;
        currentPosition.x = Mathf.Clamp(currentPosition.x, minX, maxX);
        currentPosition.y = Mathf.Clamp(currentPosition.y, minY, maxY);
        transform.position = currentPosition;
    }


    // --- AI 행동 로직 (이전과 동일) ---
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

            // --- 2. 걷기 상태 (이전과 동일) ---
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

    // --- ⬇️⬇️⬇️ 새로 추가된 드래그 기능 함수 3개 ⬇️⬇️⬇️ ---

    // 1. 커비의 Collider에 마우스 클릭이 '시작'될 때 1번 호출됨
    void OnMouseDown()
    {
        // 1. 'isDragging' 스위치를 켠다 (애니메이션 재생)
        anim.SetBool("isDragging", true);

        // 2. AI의 '생각'을 멈춘다! (가장 중요)
        // (ThinkAndAct 코루틴을 강제 종료해서, 드래그 중에 맘대로 걷지 못하게 함)
        StopAllCoroutines();
    }

    // 2. 마우스를 '클릭한 채로 움직이는' 동안 매 프레임 호출됨
    void OnMouseDrag()
    {
        // 1. 마우스의 현재 위치를 게임 세계 좌표로 변환
        // (mainCamera는 Start()에서 이미 찾아놨음)
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 2. 커비의 위치를 마우스 위치로 강제 이동
        // (Z축은 원래 값으로 유지해야 카메라에서 보임)
        transform.position = new Vector3(mousePos.x, mousePos.y, transform.position.z);

        // (참고: 이렇게 움직여도 LateUpdate()가 경계선 밖으로 못 나가게 잡아줍니다)
    }

    // 3. 마우스 버튼을 '뗄' 때 1번 호출됨
    void OnMouseUp()
    {
        // 1. 'isDragging' 스위치를 끈다 (Idle 애니메이션으로 돌아감)
        anim.SetBool("isDragging", false);

        // 2. 멈췄던 AI의 '생각'을 다시 시작시킨다!
        StartCoroutine(ThinkAndAct());
    }
}