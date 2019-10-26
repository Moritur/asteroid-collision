using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class TMP_FPS : MonoBehaviour
{
    float deltaTime = 0.0f;
    int usedHeight = (Screen.height * 2) / 100;
    GUIStyle style;
    Rect rect;
    float msec;
    float fps;
    string text;

    void Awake()
    {
        style = new GUIStyle();
        rect = new Rect(0, 0, Screen.width, usedHeight);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = usedHeight;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
    }

    const int framesToIgnore = 100;
    int ignoredFrames = 0;
    void Update()
    {
        ignoredFrames++;
        if (ignoredFrames < framesToIgnore) return;

        ignoredFrames = 0;
        deltaTime = Time.unscaledDeltaTime;
    }

    void OnGUI()
    {
        msec = deltaTime * 1000.0f;
        fps = 1.0f / deltaTime;
        text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        GUI.Label(rect, text, style);
    }
}
