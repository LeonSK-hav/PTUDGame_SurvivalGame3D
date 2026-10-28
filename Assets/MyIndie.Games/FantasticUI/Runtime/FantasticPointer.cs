using UnityEngine;
#if MYINDIEGAMES_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MyIndieGames.FantasticUI
{
    public static class FantasticPointer
    {
        public static Vector2 Position
        {
            get
            {
#if MYINDIEGAMES_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                if (Mouse.current != null)
                {
                    return Mouse.current.position.ReadValue();
                }

                if (Touchscreen.current != null)
                {
                    return Touchscreen.current.primaryTouch.position.ReadValue();
                }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }
    }
}
