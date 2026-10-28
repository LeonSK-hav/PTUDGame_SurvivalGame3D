using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Window")]
    public class FantasticWindow : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] bool startOpen = true;
        [SerializeField, Min(0f)] float duration = 0.16f;
        [SerializeField, Range(0.5f, 1f)] float closedScale = 0.92f;
        [SerializeField] bool bringToFront = true;
        [SerializeField] Button closeButton;
        [SerializeField] UnityEvent onOpened = new UnityEvent();
        [SerializeField] UnityEvent onClosed = new UnityEvent();

        CanvasGroup group;
        Coroutine routine;
        bool open;
        bool initialized;

        public bool IsOpen => open;

        public Button CloseButton
        {
            get => closeButton;
            set
            {
                if (closeButton != null)
                {
                    closeButton.onClick.RemoveListener(Close);
                }

                closeButton = value;
                if (closeButton != null && isActiveAndEnabled)
                {
                    closeButton.onClick.AddListener(Close);
                }
            }
        }

        public UnityEvent OnOpened => onOpened;

        public UnityEvent OnClosed => onClosed;

        public event Action<FantasticWindow, bool> VisibilityChanged;

        CanvasGroup Group => group != null ? group : group = GetComponent<CanvasGroup>();

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            open = startOpen;
            ApplyInstant(open);
        }

        void OnEnable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
        }

        void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        public void Toggle()
        {
            SetOpen(!open);
        }

        public void SetOpen(bool value, bool instant = false)
        {
            Initialize();
            if (value == open && !instant)
            {
                return;
            }

            open = value;
            if (open && bringToFront)
            {
                transform.SetAsLastSibling();
            }

            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            if (instant || duration <= 0f || !isActiveAndEnabled || !Application.isPlaying)
            {
                ApplyInstant(open);
            }
            else
            {
                routine = StartCoroutine(Animate(open));
            }

            VisibilityChanged?.Invoke(this, open);
            if (open)
            {
                onOpened.Invoke();
            }
            else
            {
                onClosed.Invoke();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (bringToFront)
            {
                transform.SetAsLastSibling();
            }
        }

        IEnumerator Animate(bool opening)
        {
            Group.blocksRaycasts = opening;
            Group.interactable = opening;
            var startAlpha = Group.alpha;
            var targetAlpha = opening ? 1f : 0f;
            var startScale = transform.localScale.x;
            var targetScale = opening ? 1f : closedScale;
            var time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(time / duration);
                var eased = opening ? 1f - Mathf.Pow(1f - t, 3f) : t * t;
                Group.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
                transform.localScale = Vector3.one * Mathf.Lerp(startScale, targetScale, eased);
                yield return null;
            }

            ApplyInstant(opening);
            routine = null;
        }

        void ApplyInstant(bool visible)
        {
            Group.alpha = visible ? 1f : 0f;
            Group.blocksRaycasts = visible;
            Group.interactable = visible;
            transform.localScale = Vector3.one * (visible ? 1f : closedScale);
        }
    }
}
