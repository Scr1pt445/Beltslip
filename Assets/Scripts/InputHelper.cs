using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Единая точка ввода: новая Input System (клавиатура, мышь и тач — чтобы игра нормально шла в браузере и на телефоне).
public static class InputHelper
{
    // Удержание: тяга вверх (игнорирует клик по UI-кнопке, например паузе)
    public static bool ThrustHeld()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed)) return true;

        var es = EventSystem.current;

        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed)
        {
            if (es == null || !es.IsPointerOverGameObject())
                return true;
        }

        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.isPressed)
        {
            int touchId = touch.primaryTouch.touchId.ReadValue();
            if (es == null || !es.IsPointerOverGameObject(touchId))
                return true;
        }

        return false;
    }

    // Нажатие в этом кадре: старт и рестарт
    public static bool ActionPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) return true;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;
        var touch = Touchscreen.current;
        if (touch != null && touch.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }

    // Нажатие паузы (Esc)
    public static bool PausePressed()
    {
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
    }
}
