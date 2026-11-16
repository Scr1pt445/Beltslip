using System;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Скриншоты для портфолио: F12 — снимок в двойном разрешении без интерфейса Unity.
// Файлы сохраняются в папку Screenshots рядом с папкой Assets.
// Повесь на любой объект в сцене (или на тот же объект, где главный скрипт игры).
public class ScreenshotKey : MonoBehaviour
{
    [SerializeField] int superSize = 2;   // 2 = вдвое больше размера окна Game

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool pressed = Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame;
#else
        bool pressed = Input.GetKeyDown(KeyCode.F12);
#endif
        if (!pressed) return;

        string folder = Path.Combine(Application.dataPath, "..", "Screenshots");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, $"{Application.productName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshot(file, superSize);
        Debug.Log($"Скриншот сохранён: {Path.GetFullPath(file)}");
    }
}