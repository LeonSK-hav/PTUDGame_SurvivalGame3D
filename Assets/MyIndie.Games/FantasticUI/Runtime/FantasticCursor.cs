using UnityEngine;
using UnityEngine.EventSystems;

namespace MyIndieGames.FantasticUI
{
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Cursor")]
    public class FantasticCursor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Texture2D cursor;
        [SerializeField] Vector2 hotspot;
        [SerializeField] bool applyOnEnable;

        bool applied;

        public Texture2D Cursor { get => cursor; set => cursor = value; }

        public Vector2 Hotspot { get => hotspot; set => hotspot = value; }

        void OnEnable()
        {
            if (applyOnEnable)
            {
                Apply(cursor, hotspot);
            }
        }

        void OnDisable()
        {
            if (applied || applyOnEnable)
            {
                ResetCursor();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!applyOnEnable && cursor != null)
            {
                Apply(cursor, hotspot);
                applied = true;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (applied)
            {
                ResetCursor();
                applied = false;
            }
        }

        public static void Apply(Texture2D texture, Vector2 hotspot)
        {
            UnityEngine.Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }

        public static void ResetCursor()
        {
            UnityEngine.Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
