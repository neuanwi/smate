using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

[RequireComponent(typeof(Camera))]
public class TransparentWindow : MonoBehaviour
{
    public static TransparentWindow Main = null;
    public static Camera Camera = null; //Used instead of Camera.main

    [Tooltip("What GameObject layers should trigger window focus when the mouse passes over objects?")] //
    [SerializeField] LayerMask clickLayerMask = ~0;

    [Tooltip("Allows Input to be detected even when focus is lost")] //
    [SerializeField] bool useSystemInput = false;

    [Tooltip("Should the window be fullscreen?")] //
    [SerializeField] bool fullscreen = true;

    [Tooltip("Force the window to match ScreenResolution")] //
    [SerializeField] bool customResolution = true;

    [Tooltip("Resolution the overlay should run at")] //
    [SerializeField] Vector2Int screenResolution = new Vector2Int(1280, 720);

    [Tooltip("The framerate the overlay should try to run at")] //
    [SerializeField] int targetFrameRate = 30;


    /////////////////////
    //Windows DLL stuff//
    /////////////////////

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
    static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

    [DllImport("user32.dll", EntryPoint = "GetWindowRect")]
    static extern bool GetWindowRect(IntPtr hwnd, out Rectangle rect);

    [DllImport("user32.dll")]
    static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

    [DllImportAttribute("user32.dll")]
    static extern bool ReleaseCapture();

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);

    [DllImport("Dwmapi.dll")]
    static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref Rectangle margins);

    const int GWL_STYLE = -16;
    const uint WS_POPUP = 0x80000000;
    const uint WS_VISIBLE = 0x10000000;
    const int HWND_TOPMOST = -1;

    const int WM_SYSCOMMAND = 0x112;
    const int WM_MOUSE_MOVE = 0xF012;

    int fWidth;
    int fHeight;
    IntPtr hwnd = IntPtr.Zero;
    Rectangle margins;
    Rectangle windowRect;

    //BUG: Sometimes fails to SetResolution if not focused on startup - if using Start(), WindowBoundsCollider2D sometimes fails to set the correct size
    void Awake()
    {
        Main = this;

        Camera = GetComponent<Camera>();
        Camera.backgroundColor = new Color();
        Camera.clearFlags = CameraClearFlags.SolidColor;

        // --- 수정된 부분 ---
        // 'customResolution' 설정을 무시하고,
        // 항상 모니터의 실제 네이티브 해상도를 사용하도록 강제합니다.

        // 1. 모니터의 실제 해상도를 가져옵니다.
        int nativeWidth = Display.main.systemWidth;
        int nativeHeight = Display.main.systemHeight;

        // 2. 스크립트 내부 변수도 업데이트합니다.
        screenResolution = new Vector2Int(nativeWidth, nativeHeight);

        // 3. '모니터의 실제 해상도'로 게임을 설정합니다.
        Screen.SetResolution(screenResolution.x, screenResolution.y, FullScreenMode.FullScreenWindow);
        // --- 수정 끝 ---

        Application.targetFrameRate = targetFrameRate;
        Application.runInBackground = true;

#if !UNITY_EDITOR
        fWidth = screenResolution.x;
		fHeight = screenResolution.y;
		margins = new Rectangle() {Left = -1};
		hwnd = GetActiveWindow();

		if (GetWindowRect(hwnd, out windowRect))
		{
			Debug.LogError("Couldn't get Window Rect");
		}

		SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
		SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, fWidth, fHeight, 32 | 64);
		DwmExtendFrameIntoClientArea(hwnd, ref margins);
#endif
    }

    void Update()
    {
        if (useSystemInput)
        {
            SystemInput.Process();
        }

        SetClickThrough();
    }

    //Returns true if the cursor is over a UI element or 2D physics object
    bool FocusForInput()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem && eventSystem.IsPointerOverGameObject())
        {
            return true;
        }

        Vector2 pos = Camera.ScreenToWorldPoint(Input.mousePosition);
        return Physics2D.OverlapPoint(pos, clickLayerMask);
    }

    void SetClickThrough()
    {
        var focusWindow = FocusForInput();

        //Get window position
        GetWindowRect(hwnd, out windowRect);

#if !UNITY_EDITOR
		if (focusWindow)
		{
			SetWindowLong (hwnd, -20, ~(((uint)524288) | ((uint)32)));
			SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, fWidth, fHeight, 32 | 64);
		}
		else
		{
			SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
			SetWindowLong (hwnd, -20, (uint)524288 | (uint)32);
			SetLayeredWindowAttributes (hwnd, 0, 255, 2);
			SetWindowPos(hwnd, HWND_TOPMOST, windowRect.Left, windowRect.Top, fWidth, fHeight, 32 | 64);
		}
#endif
    }

    public static void DragWindow()
    {
#if !UNITY_EDITOR
		if (Screen.fullScreenMode != FullScreenMode.Windowed)
		{
			return;
		}
		ReleaseCapture ();
		SendMessage(Main.hwnd, WM_SYSCOMMAND, WM_MOUSE_MOVE, 0);
		Input.ResetInputAxes();
#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}