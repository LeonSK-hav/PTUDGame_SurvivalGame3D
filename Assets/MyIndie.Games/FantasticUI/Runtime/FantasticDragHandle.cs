using UnityEngine;
using UnityEngine.EventSystems;

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Drag Handle")]
    public class FantasticDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] RectTransform target;
        [SerializeField] bool keepInsideCanvas = true;

        Canvas canvas;

        public RectTransform Target
        {
            get => target != null ? target : target = transform.parent as RectTransform;
            set => target = value;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (Target != null)
            {
                Target.SetAsLastSibling();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (Target == null)
            {
                return;
            }

            var scale = canvas != null ? canvas.scaleFactor : 1f;
            Target.anchoredPosition += eventData.delta / Mathf.Max(0.0001f, scale);
            if (keepInsideCanvas && canvas != null)
            {
                Clamp();
            }
        }

        void Clamp()
        {
            var canvasRect = (RectTransform)canvas.transform;
            var corners = new Vector3[4];
            Target.GetWorldCorners(corners);
            var bounds = new Vector3[4];
            canvasRect.GetWorldCorners(bounds);
            var offset = Vector3.zero;
            if (corners[0].x < bounds[0].x)
            {
                offset.x = bounds[0].x - corners[0].x;
            }
            else if (corners[2].x > bounds[2].x)
            {
                offset.x = bounds[2].x - corners[2].x;
            }

            if (corners[0].y < bounds[0].y)
            {
                offset.y = bounds[0].y - corners[0].y;
            }
            else if (corners[2].y > bounds[2].y)
            {
                offset.y = bounds[2].y - corners[2].y;
            }

            Target.position += offset;
        }
    }
}
