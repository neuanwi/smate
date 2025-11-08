using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System; // 👈 Action (Event)을 위해 필요

/// <summary>
/// 5초마다 백엔드에 추천 내역이 있는지 폴링(Polling)으로 확인합니다.
/// </summary>
public class RecommendationPoller : MonoBehaviour
{
    [Header("폴링 설정")]
    [SerializeField]
    private float pollingInterval = 5.0f; // 5초

    [Header("백엔드 설정")]
    [SerializeField]
    private string backendUrl = "http://localhost:8080/api/recommendation";

    [Tooltip("Python 로거 및 ChatInputManager의 'currentSessionId'와 동일한 값이어야 합니다.")]
    [SerializeField]
    private string computerId = "my-pc-123"; // 👈 [중요] 사용자의 고유 ID로 설정

    // --- ⬇️ 다른 스크립트(UI 등)가 구독할 이벤트들 ⬇️ ---

    /// <summary>
    /// [10회 미만] 재치 있는 멘트가 도착했을 때 발생합니다.
    /// (string: 멘트 내용)
    /// </summary>
    public static event Action<string> OnWittyCommentReceived;

    /// <summary>
    /// [10회 이상] 실제 앱 추천이 도착했을 때 발생합니다.
    /// (string: 멘트, string: 앱 실행 경로)
    /// </summary>
    public static event Action<string, string> OnAppRecommendationReceived;


    // --- ⬇️ JSON 파싱을 위한 내부 클래스 ⬇️ ---

    [Serializable]
    private class RecommendationResponse
    {
        // 백엔드에서 보낸 JSON 필드와 이름/타입이 정확히 일치해야 합니다.
        public long id;
        public string computerId;
        public string recommendedApp;
        public string reasonApp;
        public string timestamp;
        public string recommendedAppPath;
        public string message;
    }

    // --- ⬇️ 폴링 로직 ⬇️ ---

    void Start()
    {
        // 게임 시작 시 폴링 루프를 시작합니다.
        StartCoroutine(PollingLoop());
    }

    /// <summary>
    /// pollingInterval마다 FetchRecommendation 코루틴을 반복 실행합니다.
    /// </summary>
    private IEnumerator PollingLoop()
    {
        // true 동안 무한 반복
        while (true)
        {
            // 1. 다음 루프 전까지 5초 대기
            yield return new WaitForSeconds(pollingInterval);

            // 2. 실제 웹 요청 실행
            StartCoroutine(FetchRecommendation());
        }
    }

    /// <summary>
    /// 백엔드에 실제 GET 요청을 보냅니다.
    /// </summary>
    private IEnumerator FetchRecommendation()
    {
        // 1. URL에 쿼리 파라미터 추가
        string urlWithQuery = $"{backendUrl}?computerId={computerId}";

        using (UnityWebRequest www = UnityWebRequest.Get(urlWithQuery))
        {
            // 2. 요청 보내고 응답 대기
            yield return www.SendWebRequest();

            // 3. 응답 결과 처리
            if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
            {
                // (Case 1) 서버 연결 실패 (서버가 꺼져있음 등)
                Debug.LogWarning($"[Poller] 서버 연결 오류: {www.error}");
            }
            else if (www.responseCode == 404)
            {
                // (Case 2) 404 Not Found 
                // ⭐️ 이건 오류가 아닙니다!
                // 백엔드는 추천을 한번 보내면 DB에서 삭제하므로, "새로운 추천 없음"을 의미합니다.
                Debug.Log("[Poller] 새로운 추천 없음 (Normal 404).");
            }
            else if (www.responseCode == 200)
            {
                // (Case 3) 200 OK - ⭐️ 추천 도착!
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"[Poller] ⭐️ 추천 수신: {jsonResponse}");

                try
                {
                    // 4. JSON 파싱
                    RecommendationResponse rec = JsonUtility.FromJson<RecommendationResponse>(jsonResponse);

                    // 5. 파싱된 데이터로 이벤트 발생시키기
                    HandleRecommendation(rec);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Poller] JSON 파싱 오류: {e.Message} \nRaw JSON: {jsonResponse}");
                }
            }
            else
            {
                // (Case 4) 기타 오류 (500 Internal Server Error 등)
                Debug.LogWarning($"[Poller] 기타 오류 (Code {www.responseCode}): {www.downloadHandler.text}");
            }
        }
    }

    /// <summary>
    /// 수신한 추천 데이터를 분석하여 적절한 이벤트를 발생시킵니다.
    /// </summary>
    private void HandleRecommendation(RecommendationResponse rec)
    {
        if (rec == null || string.IsNullOrEmpty(rec.message))
        {
            return; // 데이터가 비어있으면 무시
        }

        if (rec.recommendedApp == "Chat")
        {
            // [10회 미만] 재치 있는 멘트 (recommendedAppPath가 null임)
            // OnWittyCommentReceived 이벤트를 구독한 모든 곳에 멘트(rec.message)를 전달
            OnWittyCommentReceived?.Invoke(rec.message);
        }
        else
        {
            // [10회 이상] 실제 앱 추천
            // OnAppRecommendationReceived 이벤트를 구독한 모든 곳에 멘트와 경로를 전달
            OnAppRecommendationReceived?.Invoke(rec.message, rec.recommendedAppPath);
        }
    }
}