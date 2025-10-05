using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

#region Text Builder Implementation
public class TextBuilder : UIElementBuilder<Text, TextBuilder>
{
    private bool useTMP = false;
    private TMP_Text tmpTextComponent;
    private string textContent = "";
    private TextAnchor textAlignment = TextAnchor.MiddleCenter;
    private TextAlignmentOptions tmpAlignment = TextAlignmentOptions.Center;
    private Font font;
    private TMP_FontAsset tmpFont;
    private int fontSize = 14;
    private FontStyle fontStyle = FontStyle.Normal;
    private bool autoSize = false;
    private int minFontSize = 10;
    private int maxFontSize = 32;
    private bool raycastTarget = true;
    private Color textColor = Color.black;
    private float lineSpacing = 1.0f;
    private bool wordWrapping = true;
    private bool richText = true;
    private float characterSpacing = 0;
    private float paragraphSpacing = 0;
    private TextOverflowModes overflowMode = TextOverflowModes.Overflow;

    public TextBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform));
            rectTransform = elementObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 50);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize TextBuilder: {ex.Message}");
        }
    }

    public TextBuilder WithText(string text, bool useTMP = false)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            textContent = text;
            this.useTMP = useTMP;
        }, $"Failed to set text for {elementName}");
    }

    public TextBuilder WithAlignment(TextAnchor alignment)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            textAlignment = alignment;

            // Map TextAnchor to TMP alignment
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    tmpAlignment = TextAlignmentOptions.TopLeft;
                    break;
                case TextAnchor.UpperCenter:
                    tmpAlignment = TextAlignmentOptions.Top;
                    break;
                case TextAnchor.UpperRight:
                    tmpAlignment = TextAlignmentOptions.TopRight;
                    break;
                case TextAnchor.MiddleLeft:
                    tmpAlignment = TextAlignmentOptions.Left;
                    break;
                case TextAnchor.MiddleCenter:
                    tmpAlignment = TextAlignmentOptions.Center;
                    break;
                case TextAnchor.MiddleRight:
                    tmpAlignment = TextAlignmentOptions.Right;
                    break;
                case TextAnchor.LowerLeft:
                    tmpAlignment = TextAlignmentOptions.BottomLeft;
                    break;
                case TextAnchor.LowerCenter:
                    tmpAlignment = TextAlignmentOptions.Bottom;
                    break;
                case TextAnchor.LowerRight:
                    tmpAlignment = TextAlignmentOptions.BottomRight;
                    break;
            }
        }, $"Failed to set alignment for {elementName}");
    }

    public TextBuilder WithAlignment(TextAlignmentOptions alignment)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            tmpAlignment = alignment;

            // Attempt to map TMP alignment to TextAnchor
            if (alignment == TextAlignmentOptions.TopLeft) textAlignment = TextAnchor.UpperLeft;
            else if (alignment == TextAlignmentOptions.Top) textAlignment = TextAnchor.UpperCenter;
            else if (alignment == TextAlignmentOptions.TopRight) textAlignment = TextAnchor.UpperRight;
            else if (alignment == TextAlignmentOptions.Left) textAlignment = TextAnchor.MiddleLeft;
            else if (alignment == TextAlignmentOptions.Center) textAlignment = TextAnchor.MiddleCenter;
            else if (alignment == TextAlignmentOptions.Right) textAlignment = TextAnchor.MiddleRight;
            else if (alignment == TextAlignmentOptions.BottomLeft) textAlignment = TextAnchor.LowerLeft;
            else if (alignment == TextAlignmentOptions.Bottom) textAlignment = TextAnchor.LowerCenter;
            else if (alignment == TextAlignmentOptions.BottomRight) textAlignment = TextAnchor.LowerRight;
        }, $"Failed to set alignment for {elementName}");
    }

    public TextBuilder WithFont(Font font)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.font = font;
        }, $"Failed to set font for {elementName}");
    }

    public TextBuilder WithTMPFont(TMP_FontAsset font)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.tmpFont = font;
        }, $"Failed to set TMP font for {elementName}");
    }

    public TextBuilder WithFontSize(int size)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fontSize = size;
        }, $"Failed to set font size for {elementName}");
    }

    public TextBuilder WithFontStyle(FontStyle style)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fontStyle = style;
        }, $"Failed to set font style for {elementName}");
    }

    public TextBuilder WithAutoSize(bool autoSize, int minSize = 10, int maxSize = 32)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.autoSize = autoSize;
            this.minFontSize = minSize;
            this.maxFontSize = maxSize;
        }, $"Failed to set auto size for {elementName}");
    }

    public TextBuilder WithTextColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            textColor = color;
        }, $"Failed to set text color for {elementName}");
    }

    public TextBuilder WithLineSpacing(float spacing)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            lineSpacing = spacing;
        }, $"Failed to set line spacing for {elementName}");
    }

    public TextBuilder WithCharacterSpacing(float spacing)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            characterSpacing = spacing;
        }, $"Failed to set character spacing for {elementName}");
    }

    public TextBuilder WithParagraphSpacing(float spacing)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            paragraphSpacing = spacing;
        }, $"Failed to set paragraph spacing for {elementName}");
    }

    public TextBuilder WithWordWrapping(bool enabled)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            wordWrapping = enabled;
        }, $"Failed to set word wrapping for {elementName}");
    }

    public TextBuilder WithRichText(bool enabled)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            richText = enabled;
        }, $"Failed to set rich text for {elementName}");
    }

    public TextBuilder WithOverflowMode(TextOverflowModes mode)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            overflowMode = mode;
        }, $"Failed to set overflow mode for {elementName}");
    }

    public TextBuilder WithRaycastTarget(bool raycastTarget)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.raycastTarget = raycastTarget;
        }, $"Failed to set raycast target for {elementName}");
    }

    public override Text AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            if (useTMP)
            {
                if (elementObject.GetComponent<TMP_Text>() == null)
                {
                    tmpTextComponent = elementObject.AddComponent<TMP_Text>();
                }
                else
                {
                    tmpTextComponent = elementObject.GetComponent<TMP_Text>();
                }

                tmpTextComponent.text = textContent;
                tmpTextComponent.alignment = tmpAlignment;
                tmpTextComponent.fontSize = fontSize;
                tmpTextComponent.fontStyle = (TMPro.FontStyles)fontStyle;
                tmpTextComponent.enableAutoSizing = autoSize;
                tmpTextComponent.color = textColor;
                tmpTextComponent.lineSpacing = lineSpacing;
                tmpTextComponent.characterSpacing = characterSpacing;
                tmpTextComponent.paragraphSpacing = paragraphSpacing;
                tmpTextComponent.enableWordWrapping = wordWrapping;
                tmpTextComponent.richText = richText;
                tmpTextComponent.overflowMode = overflowMode;

                if (autoSize)
                {
                    tmpTextComponent.fontSizeMin = minFontSize;
                    tmpTextComponent.fontSizeMax = maxFontSize;
                }
                tmpTextComponent.raycastTarget = raycastTarget;

                // Set font - use provided font or default TMP font
                if (tmpFont != null)
                {
                    tmpTextComponent.font = tmpFont;
                }
                else if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    tmpTextComponent.font = TMPro.TMP_Settings.defaultFontAsset;
                }

                component = null; // Text component won't exist since we're using TMP
            }
            else
            {
                if (elementObject.GetComponent<Text>() == null)
                {
                    component = elementObject.AddComponent<Text>();
                }
                else
                {
                    component = elementObject.GetComponent<Text>();
                }

                component.text = textContent;
                component.alignment = textAlignment;
                component.fontSize = fontSize;
                component.fontStyle = fontStyle;
                component.resizeTextForBestFit = autoSize;
                component.color = textColor;
                component.lineSpacing = lineSpacing;
                component.supportRichText = richText;
                component.horizontalOverflow = wordWrapping ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
                component.verticalOverflow = (overflowMode == TextOverflowModes.Overflow || overflowMode == TextOverflowModes.Ellipsis) ?
                    VerticalWrapMode.Overflow : VerticalWrapMode.Truncate;

                if (autoSize)
                {
                    component.resizeTextMinSize = minFontSize;
                    component.resizeTextMaxSize = maxFontSize;
                }
                component.raycastTarget = raycastTarget;

                // Set font - use provided font or default Unity font
                if (font != null)
                {
                    component.font = font;
                }
                else
                {
                    component.font = GetDefaultFont();
                }
            }

            // Apply custom customizations
            foreach (var customization in customizationActions)
            {
                customization(this);
            }

            // Register with UIManager
            RegisterWithUIManager();
        }
        catch (Exception ex)
        {
            HandleError($"Failed to add text to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion