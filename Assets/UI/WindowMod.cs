using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class WindowMod : MonoBehaviour
{
    public Rect screenPosition = new Rect(Screen.width / 2 - 300, Screen.height / 2 - 200, 600, 400);
    public bool makewinchanges = false;
    public string winname = "WSEdit";

    #region dlls
    const int MAXTITLE = 255;
    private static List<String> lstTitles;
    private delegate bool EnumDelegate(IntPtr hWnd, int lParam);
    [DllImport("user32.dll", EntryPoint = "EnumDesktopWindows", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumDelegate lpEnumCallbackFunction, IntPtr lParam);
    [DllImport("user32.dll", EntryPoint = "GetWindowText", ExactSpelling = false, CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int _GetWindowText(IntPtr hWnd, StringBuilder lpWindowText, int nMaxCount);
    [DllImport("user32.dll")]
    static extern IntPtr SetWindowLong(IntPtr hwnd, int _nIndex, int dwNewLong);
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
    [DllImport("user32.dll")]
    public static extern int EnumDesktopWindows(int hDesktop, int lpfn, int lParam);
    [DllImport("user32.dll")]
    public static extern IntPtr FindWindow(String sClassName, String sAppName);
    [return: MarshalAs(UnmanagedType.Bool)]
    [DllImport("user32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
    public static extern bool SetForegroundWindow(IntPtr hwnd);
    #endregion

    #region sysvars
    const uint SWP_SHOWWINDOW = 0x0040;
    const int GWL_EXSTYLE = -20;
    const int GWL_STYLE = -16;
    const int WS_BORDER = 1;
    #endregion

    void Awake()
    {
        if (makewinchanges == true)
        {
            GetDesktopWindowsTitles();
        }
    }

    private void GetDesktopWindowsTitles()
    {
        lstTitles = new List<String>();
        EnumDelegate delEnumfunc = new EnumDelegate(EnumWindowsProc);
        bool bSuccessful = EnumDesktopWindows(IntPtr.Zero, delEnumfunc, IntPtr.Zero); //for current desktop
    }

    private bool EnumWindowsProc(IntPtr hWnd, int lParam)
    {
        string strTitle = GetWindowText(hWnd);
        if (strTitle.Contains(winname))
        {
            lstTitles.Add(strTitle);
            bool result = SetForegroundWindow(hWnd);
            SetWindowLong(hWnd, GWL_STYLE, WS_BORDER);
            //bool result2 = SetWindowPos(hWnd, 0, (int)(Screen.currentResolution.width / 2 - (int)(1280/2)), (int)(Screen.currentResolution.height/2) - (int)(720/2), 1280, 720, SWP_SHOWWINDOW);
            return false;
        }
        return true;
    }

    public static string GetWindowText(IntPtr hWnd)
    {
        StringBuilder strbTitle = new StringBuilder(MAXTITLE);
        int nLength = _GetWindowText(hWnd, strbTitle, strbTitle.Capacity + 1);
        strbTitle.Length = nLength;
        return strbTitle.ToString();
    }
}