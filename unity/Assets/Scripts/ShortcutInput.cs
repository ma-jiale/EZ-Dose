using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EZDose.UI
{
    /// <summary>
    /// Small helper for Windows-only keyboard shortcuts that trigger existing UI buttons.
    /// </summary>
    public static class ShortcutInput
    {
        public static bool IsTextInputFocused()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            {
                return false;
            }

            var selected = eventSystem.currentSelectedGameObject;
            if (!selected.activeInHierarchy)
            {
                return false;
            }

            return selected.GetComponent<InputField>() != null ||
                   selected.GetComponentInParent<InputField>() != null;
        }

        public static bool GetKeyDown(KeyCode key)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return !IsTextInputFocused() && Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        public static bool GetAnyKeyDown(params KeyCode[] keys)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (IsTextInputFocused())
            {
                return false;
            }

            foreach (var key in keys)
            {
                if (Input.GetKeyDown(key))
                {
                    return true;
                }
            }
#endif
            return false;
        }

        public static bool InvokeButtonIfKeyDown(Button button, params KeyCode[] keys)
        {
            if (!GetAnyKeyDown(keys))
            {
                return false;
            }

            return InvokeButton(button);
        }

        public static bool InvokeButton(Button button)
        {
            if (!CanInvokeButton(button))
            {
                return false;
            }

            button.onClick.Invoke();
            return true;
        }

        public static bool CanInvokeButton(Button button)
        {
            return button != null &&
                   button.isActiveAndEnabled &&
                   button.gameObject.activeInHierarchy &&
                   button.interactable;
        }
    }
}
