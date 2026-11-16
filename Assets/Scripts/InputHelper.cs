using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Единая точка ввода: работает и с новой Input System, и со старым Input Manager.
// Клавиатура, мышь и тач — чтобы игра нормально шла в браузере и на телефоне.
public static class InputHelper
{
    // Удержание: тяга вверх
    public static bool ThrustHeld()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed)) return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed) return true;
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed) return true;
        return false;
#else
        return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)
            || Input.GetMouseButton(0) || Input.touchCount > 0;
#endif
    }

    // Нажатие в этом кадре: старт и рестарт
    public static bool ActionPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
#else
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0)) return true;
        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
#endif
    }
}