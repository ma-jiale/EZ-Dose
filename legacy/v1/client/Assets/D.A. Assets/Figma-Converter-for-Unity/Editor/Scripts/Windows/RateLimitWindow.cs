using DA_Assets.DAI;
using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class RateLimitWindow : LinkedEditorWindow<RateLimitWindow, FcuEditor, FigmaConverterUnity>
    {
        private RateLimitWindowData _data;
        private Action<RateLimitWindowResult> _callback;
        private Label _timerLabel;
        private VisualElement _helpPanel;
        private CustomButton _retryButton;
        private CustomButton _stopButton;
        private double _lastRemainingSeconds = double.MaxValue;
        private bool _resultSent;
        private bool _hasData;
        private const string RateLimitDocsUrl = "https://developers.figma.com/docs/rest-api/rate-limits/";

        private readonly FcuLocKey[] _helpSteps =
        {
            FcuLocKey.label_you_can,
            FcuLocKey.rate_limit_window_action_wait,
            FcuLocKey.rate_limit_window_action_retry,
            FcuLocKey.rate_limit_window_action_stop
        };

        public void CreateGUI()
        {
            if (_hasData == false)
            {
                return;
            }

            BuildContent();
            UpdateCountdownLabel(true);
        }

        protected override void OnEnable()
        {
            _hasData = false;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (_resultSent || _callback == null)
            {
                return;
            }

            monoBeh.AssetTools.StopAsset(StopAssetReason.RateLimit);
        }

        private void Update()
        {
            if (_hasData == false && (_resultSent || _callback == null))
            {
                Close();
                return;
            }
            else if (_resultSent || _callback == null) 
            {
                return;
            }

            double remaining = (_data.WaitUntilUtc - DateTime.UtcNow).TotalSeconds;

            if (remaining <= 0d)
            {
                SendResult(RateLimitWindowAction.WaitCompleted);
                return;
            }

            UpdateCountdownLabel();
        }

        public void SetData(RateLimitWindowData data, Action<RateLimitWindowResult> callback)
        {
            _data = data;
            _callback = callback;
            _resultSent = false;
            _hasData = true;
        }

        private void BuildContent()
        {
            if (_hasData == false)
            {
                return;
            }

            var root = rootVisualElement;
            root.Clear();

            UIHelpers.SetDefaultPadding(root);

            root.style.backgroundColor = new StyleColor(uitk.ColorScheme.BG);
            root.style.flexDirection = FlexDirection.Column;

            var topArea = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    minHeight = 0,
                    alignItems = Align.FlexStart
                }
            };
            root.Add(topArea);

            _helpPanel = uitk.BuildHelpPanel(FcuConfig.Instance.Localizator.Language, LocalizeHelpSteps());
            topArea.Add(_helpPanel);
            MakeHelpPanelLinksClickable(_helpPanel);

            var contentCol = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            topArea.Add(contentCol);

            var summaryHeader = CreateCardHeader(FcuLocKey.rate_limit_window_description.Localize());
            summaryHeader.style.fontSize = DAI_UitkConstants.FontSizeNormal;
            contentCol.Add(summaryHeader);

            var summaryCard = uitk.CreateSectionPanel(withBorder: true);
            contentCol.Add(summaryCard);

            _timerLabel = new Label
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            summaryCard.Add(_timerLabel);

            contentCol.Add(uitk.Space10());

            if (string.IsNullOrWhiteSpace(_data.RateLimitDetails) == false)
            {
                contentCol.Add(uitk.Space5());
                var detailsHeader = CreateCardHeader(FcuLocKey.rate_limit_window_details_header.Localize());
                contentCol.Add(detailsHeader);

                var detailsBox = uitk.HelpBox(new HelpBoxData
                {
                    Message = _data.RateLimitDetails,
                    MessageType = MessageType.Info
                });
                detailsBox.style.whiteSpace = WhiteSpace.Normal;
                contentCol.Add(detailsBox);
            }

            var footer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    flexShrink = 0
                }
            };
            root.Add(footer);

            var helpButton = uitk.HelpButton(() => uitk.ToggleHelpPanel(_helpPanel));
            footer.Add(helpButton);
            footer.Add(uitk.Space5());

            footer.Add(new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            });

            _stopButton = uitk.Button(
                FcuLocKey.rate_limit_window_button_stop.Localize(),
                () => SendResult(RateLimitWindowAction.StopImport, true));

            _stopButton.color = uitk.ColorScheme.RED;
            _stopButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            footer.Add(_stopButton);

            footer.Add(uitk.Space5());

            _retryButton = uitk.Button(
                FcuLocKey.rate_limit_window_button_retry.Localize(),
                () => SendResult(RateLimitWindowAction.RetryNow));

            _retryButton.color = EditorGUIUtility.isProSkin ? uitk.ColorScheme.ACCENT_SECOND : uitk.ColorScheme.BUTTON;
            _retryButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            _retryButton.style.minWidth = DAI_UitkConstants.ApplyButtonMinWidth;
            footer.Add(_retryButton);
        }

        private static Label CreateWrappedLabel(string text, bool bold = false)
        {
            var label = new Label(text ?? string.Empty)
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    flexGrow = 0,
                    unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal
                }
            };

            return label;
        }

        private Label CreateCardHeader(string text)
        {
            var header = CreateWrappedLabel(text, bold: true);
            header.style.marginBottom = 4;
            header.style.marginLeft = 4;
            return header;
        }

        private string[] LocalizeHelpSteps()
        {
            if (_helpSteps == null || _helpSteps.Length == 0)
            {
                return Array.Empty<string>();
            }

            var steps = new string[_helpSteps.Length];
            for (int i = 0; i < _helpSteps.Length; i++)
            {
                if (_helpSteps[i] == FcuLocKey.rate_limit_window_action_retry)
                {
                    steps[i] = _helpSteps[i].Localize($"<u>{RateLimitDocsUrl}</u>");
                }
                else
                {
                    steps[i] = _helpSteps[i].Localize();
                }
            }

            return steps;
        }

        private void MakeHelpPanelLinksClickable(VisualElement panel)
        {
            if (panel == null)
            {
                return;
            }

            const string url = RateLimitDocsUrl;

            foreach (var lbl in panel.Query<Label>().Build())
            {
                if (lbl == null || string.IsNullOrEmpty(lbl.text) || lbl.text.Contains(url) == false)
                {
                    continue;
                }

                lbl.enableRichText = true;
                lbl.tooltip = url;

                lbl.RegisterCallback<PointerUpEvent>(_ =>
                {
                    Application.OpenURL(url);
                });
            }
        }

        private void UpdateCountdownLabel(bool force = false)
        {
            TimeSpan remaining = _data.WaitUntilUtc - DateTime.UtcNow;

            if (_timerLabel != null)
            {
                if (remaining <= TimeSpan.Zero)
                {
                    _timerLabel.text = FcuLocKey.rate_limit_window_waiting_done.Localize();
                }
                else if (force || Math.Abs(remaining.TotalSeconds - _lastRemainingSeconds) >= 1d)
                {
                    _timerLabel.text = FormatDurationWithSeconds(remaining);
                    _lastRemainingSeconds = remaining.TotalSeconds;
                }
            }
        }


        private void SendResult(RateLimitWindowAction action, bool stopImport = false)
        {
            if (_resultSent)
            {
                return;
            }

            _resultSent = true;

            if (stopImport)
            {
                monoBeh.AssetTools.StopAsset(StopAssetReason.RateLimit);
            }

            _callback?.Invoke(new RateLimitWindowResult { Action = action });
            Close();
        }

        private static string FormatDurationWithSeconds(TimeSpan span)
        {
            string d = FcuLocKey.time_unit_d.Localize();
            string h = FcuLocKey.time_unit_h.Localize();
            string m = FcuLocKey.time_unit_m.Localize();
            string s = FcuLocKey.time_unit_s.Localize();
            string secFull = FcuLocKey.time_unit_seconds_full.Localize();

            int totalSecondsInt = Math.Max(0, (int)Math.Ceiling(span.TotalSeconds));
            string totalSecondsStr = totalSecondsInt.ToString(CultureInfo.InvariantCulture);
            string totalPart = $"({totalSecondsStr} {secFull})";

            if (span.TotalSeconds <= 0d)
            {
                return $"0{s} {totalPart}";
            }

            string humanReadable;

            if (span.TotalDays >= 1d)
            {
                int days = (int)Math.Floor(span.TotalDays);
                humanReadable = $"{days}{d} {span.Hours:D2}{h} {span.Minutes:D2}{m}";
            }
            else if (span.TotalHours >= 1d)
            {
                int hours = (int)Math.Floor(span.TotalHours);
                humanReadable = $"{hours}{h} {span.Minutes:D2}{m} {span.Seconds:D2}{s}";
            }
            else if (span.TotalMinutes >= 1d)
            {
                int minutes = (int)Math.Floor(span.TotalMinutes);
                humanReadable = $"{minutes}{m} {span.Seconds:D2}{s}";
            }
            else
            {
                int seconds = Math.Max(1, (int)Math.Ceiling(span.TotalSeconds));
                humanReadable = $"{seconds}{s}";
            }

            return $"{humanReadable} {totalPart}";
        }
    }
}
