using UnityEngine;
using UnityEngine.EventSystems;
#if MYINDIEGAMES_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventSystem))]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Event System")]
    public class FantasticEventSystem : MonoBehaviour
    {
        void Awake()
        {
            EnsureInputModule(gameObject);
        }

        public static void EnsureInputModule(GameObject target)
        {
            if (target.GetComponent<BaseInputModule>() != null)
            {
                return;
            }

#if MYINDIEGAMES_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            target.AddComponent<InputSystemUIInputModule>();
#else
            target.AddComponent<StandaloneInputModule>();
#endif
        }

        public static EventSystem Ensure()
        {
            var current = EventSystem.current != null ? EventSystem.current : FindAnyObjectByType<EventSystem>();
            if (current != null)
            {
                return current;
            }

            var created = new GameObject("EventSystem", typeof(EventSystem), typeof(FantasticEventSystem));
            return created.GetComponent<EventSystem>();
        }
    }
}
