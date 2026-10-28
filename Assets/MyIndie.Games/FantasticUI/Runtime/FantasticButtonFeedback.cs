using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Button Feedback")]
    public class FantasticButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform target;
        [SerializeField] float hoverScale = 1.04f;
        [SerializeField] float pressedScale = 0.96f;
        [SerializeField, Min(0.01f)] float speed = 18f;
        [SerializeField] Graphic glow;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip hoverSound;
        [SerializeField] AudioClip clickSound;

        Selectable selectable;
        bool hovered;
        bool pressed;
        float scale = 1f;
        float glowAlpha;

        RectTransform Target => target != null ? target : target = (RectTransform)transform;

        void Awake()
        {
            selectable = GetComponent<Selectable>();
            if (glow != null)
            {
                glow.canvasRenderer.SetAlpha(0f);
            }
        }

        void OnDisable()
        {
            hovered = false;
            pressed = false;
            scale = 1f;
            glowAlpha = 0f;
            Target.localScale = Vector3.one;
            if (glow != null)
            {
                glow.canvasRenderer.SetAlpha(0f);
            }
        }

        bool Interactable => selectable == null || selectable.IsInteractable();

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (Interactable)
            {
                Play(hoverSound);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            if (Interactable)
            {
                Play(clickSound);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        void Update()
        {
            var active = Interactable;
            var targetScale = !active ? 1f : pressed ? pressedScale : hovered ? hoverScale : 1f;
            var blend = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);
            scale = Mathf.Lerp(scale, targetScale, blend);
            Target.localScale = new Vector3(scale, scale, 1f);
            if (glow != null)
            {
                glowAlpha = Mathf.Lerp(glowAlpha, active && hovered ? 1f : 0f, blend);
                glow.canvasRenderer.SetAlpha(glowAlpha);
            }
        }

        void Play(AudioClip clip)
        {
            if (clip != null && audioSource != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}
