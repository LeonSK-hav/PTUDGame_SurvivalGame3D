using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MyIndieGames.FantasticUI
{
    [ExecuteAlways]
    [AddComponentMenu("MyIndie.Games/Fantastic UI/Fantastic Bar")]
    public class FantasticBar : MonoBehaviour
    {
        [Serializable]
        public class ValueChangedEvent : UnityEvent<float> { }

        [SerializeField] Image fill;
        [SerializeField] Image trail;
        [SerializeField] TMP_Text label;
        [SerializeField] float value = 75f;
        [SerializeField] float maxValue = 100f;
        [SerializeField] string labelFormat = "{0} / {1}";
        [SerializeField, Min(0f)] float fillTime = 0.18f;
        [SerializeField, Min(0f)] float trailDelay = 0.35f;
        [SerializeField, Min(0.01f)] float trailSpeed = 0.8f;
        [SerializeField] bool useGradient;
        [SerializeField] Gradient gradient = DefaultGradient();
        [SerializeField] ValueChangedEvent onValueChanged = new ValueChangedEvent();

        float shown;
        float shownVelocity;
        float trailShown;
        float trailWait;

        public Image Fill
        {
            get => fill;
            set { fill = value; Apply(Normalized, Normalized); }
        }

        public Image Trail
        {
            get => trail;
            set { trail = value; Apply(shown, trailShown); }
        }

        public TMP_Text Label
        {
            get => label;
            set { label = value; UpdateLabel(); }
        }

        public float Value
        {
            get => value;
            set => SetValue(value);
        }

        public float MaxValue
        {
            get => maxValue;
            set
            {
                maxValue = Mathf.Max(0.0001f, value);
                SetValue(this.value);
            }
        }

        public float Normalized => maxValue <= 0f ? 0f : Mathf.Clamp01(value / maxValue);

        public float DisplayedNormalized => shown;

        public string LabelFormat
        {
            get => labelFormat;
            set { labelFormat = value; UpdateLabel(); }
        }

        public ValueChangedEvent OnValueChanged => onValueChanged;

        void OnEnable()
        {
            Snap();
        }

        void OnValidate()
        {
            maxValue = Mathf.Max(0.0001f, maxValue);
            value = Mathf.Clamp(value, 0f, maxValue);
            Snap();
        }

        public void SetValue(float newValue, bool instant = false)
        {
            var clamped = Mathf.Clamp(newValue, 0f, maxValue);
            var changed = !Mathf.Approximately(clamped, value);
            var target = maxValue <= 0f ? 0f : clamped / maxValue;
            if (target > trailShown || instant)
            {
                trailShown = target;
            }
            else if (changed)
            {
                trailWait = trailDelay;
            }

            value = clamped;
            if (instant || !Application.isPlaying)
            {
                Snap();
            }

            UpdateLabel();
            if (changed)
            {
                onValueChanged.Invoke(value);
            }
        }

        public void Add(float amount)
        {
            SetValue(value + amount);
        }

        public void SetValues(float newValue, float newMax, bool instant = false)
        {
            maxValue = Mathf.Max(0.0001f, newMax);
            SetValue(newValue, instant);
        }

        public void Snap()
        {
            shown = Normalized;
            shownVelocity = 0f;
            trailShown = shown;
            trailWait = 0f;
            Apply(shown, trailShown);
            UpdateLabel();
        }

        void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var target = Normalized;
            var delta = Time.unscaledDeltaTime;
            shown = fillTime <= 0f ? target : Mathf.SmoothDamp(shown, target, ref shownVelocity, fillTime, Mathf.Infinity, delta);
            if (Mathf.Abs(shown - target) < 0.0005f)
            {
                shown = target;
            }

            if (trailWait > 0f)
            {
                trailWait -= delta;
            }
            else if (trailShown > shown)
            {
                trailShown = Mathf.Max(shown, trailShown - trailSpeed * delta);
            }
            else
            {
                trailShown = shown;
            }

            Apply(shown, trailShown);
        }

        void Apply(float fillAmount, float trailAmount)
        {
            ApplyTo(fill, fillAmount);
            ApplyTo(trail, trailAmount);
            if (fill != null && useGradient)
            {
                fill.color = gradient.Evaluate(fillAmount);
            }
        }

        static void ApplyTo(Image image, float amount)
        {
            if (image == null)
            {
                return;
            }

            if (image.type == Image.Type.Filled)
            {
                image.fillAmount = amount;
                return;
            }

            var rect = image.rectTransform;
            var max = rect.anchorMax;
            max.x = Mathf.Lerp(rect.anchorMin.x, 1f, amount);
            rect.anchorMax = max;
            image.enabled = amount > 0.0001f;
        }

        void UpdateLabel()
        {
            if (label == null || string.IsNullOrEmpty(labelFormat))
            {
                return;
            }

            label.text = string.Format(labelFormat, Mathf.RoundToInt(value), Mathf.RoundToInt(maxValue), Mathf.RoundToInt(Normalized * 100f));
        }

        static Gradient DefaultGradient()
        {
            var result = new Gradient();
            result.SetKeys(
                new[] { new GradientColorKey(new Color(0.85f, 0.2f, 0.15f), 0f), new GradientColorKey(new Color(0.95f, 0.75f, 0.2f), 0.5f), new GradientColorKey(new Color(0.45f, 0.85f, 0.25f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return result;
        }
    }
}
