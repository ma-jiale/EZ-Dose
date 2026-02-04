using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    public class AnimatedFoldout : VisualElement
    {
        private readonly VisualElement header;
        private readonly VisualElement body;
        private readonly float duration;
        private readonly AnimationCurve curve;
        private IVisualElementScheduledItem anim;
        private Color _bodyColor;
        private bool startedMeasure;

        private static readonly Dictionary<string, bool> _stateDict = new Dictionary<string, bool>();
        private readonly string _stateKey;

        public bool Expanded { get; private set; }
        public event Action<bool> Toggled;

        public AnimatedFoldout
        (
            string id,
            VisualElement header,
            VisualElement body,
            bool startExpanded,
            AnimationCurve curve,
            float duration,
            Color bodyColor
        )
        {
            _bodyColor = bodyColor;
            _stateKey = id;

            this.header = header;
            this.body = body;
            this.duration = Mathf.Max(0.01f, duration);
            this.curve = curve == null ? AnimationCurve.EaseInOut(0, 0, 1, 1) : curve;

            this.header.style.width = Length.Percent(100);
            this.body.style.width = Length.Percent(100);

#if UNITY_2020_1_OR_NEWER
            this.header.RegisterCallback<ClickEvent>(_ => Toggle());
#else
            this.header.RegisterCallback<MouseUpEvent>(evt =>
            {
                if (evt.button == 0)
                    Toggle();
            });
#endif

            this.body.style.overflow = Overflow.Hidden;

            Add(this.header);
            Add(this.body);

            if (_stateDict.TryGetValue(_stateKey, out bool saved))
            {
                startExpanded = saved;
            }

            if (startExpanded)
            {
                Expanded = true;
                this.body.style.display = DisplayStyle.Flex;
                this.body.style.height = StyleKeyword.Null;
                this.body.style.backgroundColor = new StyleColor(_bodyColor);
            }
            else
            {
                Expanded = false;
                this.body.style.display = DisplayStyle.None;
                this.body.style.height = 0;
                this.body.style.backgroundColor = new StyleColor(_bodyColor);
            }

            PersistState();
        }

        public void Toggle()
        {
            SetExpanded(!Expanded, true);
        }

        public void SetExpanded(bool expand, bool animate)
        {
            if (Expanded == expand && anim == null)
            {
                return;
            }

            Expanded = expand;
            PersistState();
            Toggled?.Invoke(Expanded);

            anim?.Pause();
            anim = null;

            if (!animate)
            {
                body.style.display = expand ? DisplayStyle.Flex : DisplayStyle.None;
                body.style.height = StyleKeyword.Null;
                body.style.backgroundColor = new StyleColor(_bodyColor);
                return;
            }

            if (expand)
            {
                startedMeasure = false;
                body.style.display = DisplayStyle.Flex;
                body.style.height = StyleKeyword.Null;
                body.style.backgroundColor = new StyleColor(_bodyColor);

                EventCallback<GeometryChangedEvent> onGeom = null;
                onGeom = (GeometryChangedEvent e) =>
                {
                    if (startedMeasure)
                    {
                        return;
                    }

                    var h = Mathf.Max(1f, body.contentRect.height);

                    if (h <= 1f)
                    {
                        return;
                    }

                    startedMeasure = true;
                    body.UnregisterCallback(onGeom);
                    body.style.height = 0;

                    RunAnimation(body, 0f, h, () =>
                    {
                        body.style.height = StyleKeyword.Null;
                        body.style.backgroundColor = new StyleColor(_bodyColor);
                    });
                };

                body.RegisterCallback(onGeom);

                body.schedule.Execute(() =>
                {
                    if (!startedMeasure)
                    {
                        var h = Mathf.Max(1f, body.contentRect.height);

                        if (h > 1f)
                        {
                            startedMeasure = true;
                            body.UnregisterCallback(onGeom);
                            body.style.height = 0;

                            RunAnimation(body, 0f, h, () =>
                            {
                                body.style.height = StyleKeyword.Null;
                                body.style.backgroundColor = new StyleColor(_bodyColor);
                            });
                        }
                    }
                }).StartingIn(10);
            }
            else
            {
                float current = body.resolvedStyle.height > 0 ? body.resolvedStyle.height : Mathf.Max(1f, body.contentRect.height);

                RunAnimation(body, current, 0f, () =>
                {
                    body.style.height = StyleKeyword.Null;
                    body.style.display = DisplayStyle.None;
                    body.style.backgroundColor = new StyleColor(_bodyColor);
                });
            }
        }

        private void RunAnimation(VisualElement elem, float from, float to, Action onComplete)
        {
            float t0 = (float)EditorApplication.timeSinceStartup;

            anim = elem.schedule.Execute(() =>
            {
                float t = Mathf.Clamp01(((float)EditorApplication.timeSinceStartup - t0) / duration);
                float v = curve.Evaluate(t);
                elem.style.height = Mathf.Lerp(from, to, v);

                if (t >= 1f)
                {
                    anim.Pause();
                    anim = null;
                    onComplete?.Invoke();
                }
            }).Every(10);
        }

        private void PersistState()
        {
            _stateDict[_stateKey] = Expanded;
        }
    }
}