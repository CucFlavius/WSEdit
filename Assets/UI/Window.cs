using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

public class Window : MonoBehaviour {

    [DllImport("user32.dll")] static extern int GetForegroundWindow();

    [DllImport("User32.dll")]
    public static extern int FindWindow(string lpClassName, string lpWindowName);


    [DllImport("user32", EntryPoint = "GetWindowLongA")]
    public static extern long GetWindowLong(long hwnd, long nIndex);

    [DllImport("user32", EntryPoint = "SetWindowLongA")]
    public static extern long SetWindowLong(long hwnd, long nIndex, long dwNewLong);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(int hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", EntryPoint = "MoveWindow")]
    static extern int MoveWindow(int hwnd, int x, int y, int nWidth, int nHeight, int bRepaint);

    [DllImport("user32.dll")]
    static extern bool ShowWindowAsync(int hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out MousePosition lpMousePosition);

    [StructLayout(LayoutKind.Sequential)]
    public struct MousePosition
    {
        public int x;
        public int y;
    }
    const uint SWP_SHOWWINDOW = 0x0040;
    const int GWL_EXSTYLE = -20;
    const int GWL_STYLE = -16;
    const int WS_BORDER = 1;

    private Vector2 currentWindowSize;
    private Vector3 mouseClickPosition;
    private int hwnd;



    void Awake()
    {
        //Screen.fullScreenMode = FullScreenMode.Windowed;
        // Screen.SetResolution(400, 600, FullScreenMode.Windowed);
        //Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        Screen.SetResolution(400, 600, FullScreenMode.Windowed);
        bool result2 = SetWindowPos(hwnd, 0, (int)(Screen.currentResolution.width / 2 - (int)(400 / 2)), (int)(Screen.currentResolution.height / 2) - (int)(600 / 2), 400, 600, SWP_SHOWWINDOW);
        //int hwnd = GetForegroundWindow();
        hwnd = FindWindow("ConsoleWindowClass", "WSEdit");
        long style = GetWindowLong(hwnd, -16L);
        //style |= 0xc00000L;
        style &= -12582913L;
        //SetWindowLong(hwnd, -16L, style);
        //SetWindowLong(hwnd, GWL_STYLE, WS_BORDER);


        int fWidth = Screen.width;
        int fHeight = Screen.height;
        //SetWindowLong(GetForegroundWindow(), GWL_STYLE, WS_BORDER);
    }

    private void Start()
    {
        bool result2 = SetWindowPos(hwnd, 0, (int)(Screen.currentResolution.width / 2 - (int)(1280 / 2)), (int)(Screen.currentResolution.height / 2) - (int)(720 / 2), 1280, 720, SWP_SHOWWINDOW);
        //SetWindowLong(hwnd, GWL_STYLE, WS_BORDER);
        Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mouseClickPosition = Input.mousePosition;

        }
            if (Input.GetMouseButton(0))
        {
            //Vector3 mousepos = Input.mousePosition;
            //int handle = GetForegroundWindow();
            MousePosition lpMousePosition;
            GetCursorPos(out lpMousePosition);
            Vector3 mousepos2 = Display.RelativeMouseAt(new Vector3(lpMousePosition.x, lpMousePosition.y));
            MoveWindow(hwnd, (int)mousepos2.x - (int)mouseClickPosition.x, (int)mousepos2.y - (600-(int)mouseClickPosition.y), 1280, 720, 1); // move the Unity Projet windows >>> 2000,0 Secondary monitor ;)
        }
    }
    
}
