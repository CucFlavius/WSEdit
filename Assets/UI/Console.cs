using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Console : MonoBehaviour
{

    public static void Log(string text, LogType type = LogType.Text)
    {
        Debug.Log("\n" + text);
    }

    public static void Log(int value, LogType type = LogType.Text)
    {
        Debug.Log("\n" + value);
    }

    public enum LogType
    {
        Text,
        Error,
        Warning
    }

}
