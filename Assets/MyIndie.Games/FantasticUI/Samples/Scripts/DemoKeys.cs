using UnityEngine;
#if MYINDIEGAMES_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MyIndieGames.FantasticUI.Samples
{
    public static class DemoKeys
    {
        public static bool Pressed(KeyCode key)
        {
#if MYINDIEGAMES_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                var control = Map(keyboard, key);
                if (control != null && control.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

#if MYINDIEGAMES_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
        static UnityEngine.InputSystem.Controls.KeyControl Map(Keyboard keyboard, KeyCode key)
        {
            switch (key)
            {
                case KeyCode.I:
                    return keyboard.iKey;
                case KeyCode.C:
                    return keyboard.cKey;
                case KeyCode.Escape:
                    return keyboard.escapeKey;
                case KeyCode.Alpha1:
                    return keyboard.digit1Key;
                case KeyCode.Alpha2:
                    return keyboard.digit2Key;
                case KeyCode.Alpha3:
                    return keyboard.digit3Key;
                case KeyCode.Alpha4:
                    return keyboard.digit4Key;
                case KeyCode.Alpha5:
                    return keyboard.digit5Key;
                case KeyCode.Alpha6:
                    return keyboard.digit6Key;
                default:
                    return null;
            }
        }
#endif
    }
}
