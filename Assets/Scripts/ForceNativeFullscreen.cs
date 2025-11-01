using System.Collections;
using UnityEngine;

public class ForceNativeFullscreen : MonoBehaviour
{
    void Start()
    {
        // 0.1초 정도 기다려서, 유니티 엔진이 모니터 정보를
        // 완벽하게 불러올 시간을 줍니다. (실행 순서 꼬임 방지)
        StartCoroutine(SetNativeResolutionAfterDelay());
    }

    IEnumerator SetNativeResolutionAfterDelay()
    {
        // 0.1초 대기
        yield return new WaitForSeconds(0.1f);

        // --- 이게 핵심입니다 ---
        // '게임의 현재 해상도' (Screen.currentResolution)가 아니라,
        // '모니터의 실제 시스템 해상도' (Display.main.systemWidth)를 가져옵니다.

        int nativeWidth = Display.main.systemWidth;
        int nativeHeight = Display.main.systemHeight;

        // "모니터의 실제 21:9 해상도"로 "경계 없는 전체 창"을 강제로 설정합니다.
        Screen.SetResolution(nativeWidth, nativeHeight, FullScreenMode.FullScreenWindow);

        Debug.Log("Forcing NATIVE Fullscreen: " + nativeWidth + "x" + nativeHeight);
    }
}