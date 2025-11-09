using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System; // 👈 Action을 위해 필요

/// <summary>
/// '추천 팝업 프리팹'에 붙어있어야 하는 스크립트입니다.
/// Accept(✓) 버튼, Decline(X) 버튼만 관리합니다. (멘트 텍스트 없음)
/// </summary>
public class RecommendationButtonPopup : MonoBehaviour
{
    [Header("프리팹 내부 UI 연결")]
    // [SerializeField] // 👈 [삭제됨]
    // private TMP_Text messageText; 

    [SerializeField]
    private Button acceptButton; // [Check ✓] 버튼

    [SerializeField]
    private Button declineButton; // [X] 버튼

    /// <summary>
    /// RecommendationUIManager가 호출하여 팝업의 내용을 설정합니다.
    /// (멘트 텍스트가 없는 버전)
    /// </summary>
    /// <param name="onAccept">[Check] 버튼 누르면 실행될 함수</param>
    /// <param name="onDecline">[X] 버튼 누르면 실행될 함수</param>
    public void Setup(Action onAccept, Action onDecline) // 👈 [수정됨] message 파라미터 삭제
    {
        // 1. 멘트 설정 (삭제됨)

        // 2. 버튼 리스너 설정
        if (acceptButton != null)
        {
            // (중복 방지) 기존 리스너 제거
            acceptButton.onClick.RemoveAllListeners();
            // 새 리스너 추가
            acceptButton.onClick.AddListener(() => onAccept());
        }

        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(() => onDecline());
        }
    }
}