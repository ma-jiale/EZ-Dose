using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    public struct HelpBoxData
    {
        public string Message;
        public MessageType MessageType;
        public Action OnClick;
        public int FontSize;
    }

    public class CustomHelpBox : VisualElement
    {
        public CustomHelpBox(DAInspectorUITK uitk, HelpBoxData data)
        {
            var colorScheme = uitk.ColorScheme;
            var originalColor = colorScheme.GROUP;

            var hoverColor = EditorGUIUtility.isProSkin
                ? new StyleColor(UIHelpers.Lighten(originalColor, 0.05f))
                : new StyleColor(UIHelpers.Darken(originalColor, 0.05f));

            var activeColor = EditorGUIUtility.isProSkin
                ? new StyleColor(UIHelpers.Lighten(originalColor, 0.1f))
                : new StyleColor(UIHelpers.Darken(originalColor, 0.1f));

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.backgroundColor = originalColor;

            UIHelpers.SetDefaultRadius(this);
            UIHelpers.SetDefaultPadding(this);
            UIHelpers.SetBorderWidth(this, 1f);
            UIHelpers.SetBorderColor(this, uitk.ColorScheme.OUTLINE);

            style.transitionProperty = new List<StylePropertyName> { "background-color" };
            style.transitionDuration = new List<TimeValue> { new TimeValue(0.15f, TimeUnit.Second) };
            style.transitionTimingFunction = new List<EasingFunction> { EasingMode.Ease };

            RegisterCallback<PointerEnterEvent>(evt => style.backgroundColor = hoverColor);
            RegisterCallback<PointerLeaveEvent>(evt => style.backgroundColor = originalColor);
            RegisterCallback<PointerDownEvent>(evt => style.backgroundColor = activeColor);
            RegisterCallback<PointerUpEvent>(evt => style.backgroundColor = hoverColor);

            var icon = new Image
            {
                image = GetIconForMessageType(data.MessageType),
                scaleMode = ScaleMode.ScaleToFit
            };

            icon.style.width = 36;
            icon.style.height = 36;
            icon.style.marginRight = 8;
            icon.style.flexShrink = 0;

            var label = new Label(data.Message);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.fontSize = data.FontSize > 0 ? data.FontSize : DAI_UitkConstants.FontSizeNormal;
            label.style.color = colorScheme.TEXT;
            label.style.flexShrink = 1;
            label.enableRichText = true;

            Add(icon);
            Add(label);

            if (data.OnClick != null)
            {
                this.AddManipulator(new Clickable(data.OnClick));
            }
        }

        private Texture2D GetIconForMessageType(MessageType type)
        {
            string iconName;
            switch (type)
            {
                case MessageType.Info:
                    iconName = "console.infoicon";
                    break;
                case MessageType.Warning:
                    iconName = "console.warnicon";
                    break;
                case MessageType.Error:
                    iconName = "console.erroricon";
                    break;
                default:
                    iconName = "console.infoicon";
                    break;
            }

            return EditorGUIUtility.IconContent(iconName).image as Texture2D;
        }
    }
}