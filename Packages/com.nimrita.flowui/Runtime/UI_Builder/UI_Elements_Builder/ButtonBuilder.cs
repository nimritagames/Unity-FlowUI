using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;

#region Button Builder Implementation
public class ButtonBuilder : UIElementBuilder<Button, ButtonBuilder>
{
    private Text textComponent;
    private TMP_Text tmpTextComponent;
    private ColorBlock colors;
    private bool useCustomColors = false;
    private Sprite backgroundSprite;
    private Image.Type imageType = Image.Type.Sliced;
    private List<UnityAction> clickActions = new List<UnityAction>();
    private Selectable.Transition transitionType = Selectable.Transition.ColorTint;
    private bool isInteractable = true;
    private SpriteState spriteState;
    private bool useSpriteState = false;
    private float fadeDuration = 0.1f;

    public ButtonBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            // Create the button GameObject
            elementObject = new GameObject(name, typeof(RectTransform), typeof(Button), typeof(Image));
            rectTransform = elementObject.GetComponent<RectTransform>();
            component = elementObject.GetComponent<Button>();

            // Set default properties
            rectTransform.sizeDelta = new Vector2(160, 40);

            // Get default colors
            colors = component.colors;
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize ButtonBuilder: {ex.Message}");
        }
    }

    public ButtonBuilder WithText(string text, bool useTMP = false)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (useTMP)
            {
                if (tmpTextComponent == null)
                {
                    GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMP_Text));
                    textObj.transform.SetParent(elementObject.transform, false);
                    tmpTextComponent = textObj.GetComponent<TMP_Text>();

                    RectTransform textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                }

                tmpTextComponent.text = text;
                tmpTextComponent.color = Color.white;
                tmpTextComponent.alignment = TextAlignmentOptions.Center;
                tmpTextComponent.fontSize = 14;

                // Set default font for TMP
                if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    tmpTextComponent.font = TMPro.TMP_Settings.defaultFontAsset;
                }
            }
            else
            {
                if (textComponent == null)
                {
                    GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                    textObj.transform.SetParent(elementObject.transform, false);
                    textComponent = textObj.GetComponent<Text>();

                    RectTransform textRect = textObj.GetComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                }

                textComponent.text = text;
                textComponent.color = Color.white;
                textComponent.alignment = TextAnchor.MiddleCenter;
                textComponent.fontSize = 14;
                textComponent.font = GetDefaultFont();
            }
        }, $"Failed to set text for {elementName}");
    }

    public ButtonBuilder WithTextColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (textComponent != null)
                textComponent.color = color;

            if (tmpTextComponent != null)
                tmpTextComponent.color = color;
        }, $"Failed to set text color for {elementName}");
    }

    public ButtonBuilder WithFontSize(int fontSize)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (textComponent != null)
                textComponent.fontSize = fontSize;

            if (tmpTextComponent != null)
                tmpTextComponent.fontSize = fontSize;
        }, $"Failed to set font size for {elementName}");
    }

    public ButtonBuilder WithFontStyle(FontStyle style)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (textComponent != null)
                textComponent.fontStyle = style;

            if (tmpTextComponent != null)
                tmpTextComponent.fontStyle = (TMPro.FontStyles)style;
        }, $"Failed to set font style for {elementName}");
    }

    public ButtonBuilder WithColors(Color normalColor, Color highlightedColor, Color pressedColor, Color disabledColor)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            useCustomColors = true;
            colors.normalColor = normalColor;
            colors.highlightedColor = highlightedColor;
            colors.pressedColor = pressedColor;
            colors.disabledColor = disabledColor;
        }, $"Failed to set colors for {elementName}");
    }

    public ButtonBuilder WithFadeDuration(float duration)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fadeDuration = duration;
            colors.fadeDuration = duration;
        }, $"Failed to set fade duration for {elementName}");
    }

    public ButtonBuilder WithSpriteState(Sprite highlightedSprite, Sprite pressedSprite, Sprite disabledSprite)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            useSpriteState = true;
            spriteState = new SpriteState
            {
                highlightedSprite = highlightedSprite,
                pressedSprite = pressedSprite,
                disabledSprite = disabledSprite
            };
        }, $"Failed to set sprite state for {elementName}");
    }

    public ButtonBuilder WithBackgroundSprite(Sprite sprite, Image.Type imageType = Image.Type.Sliced)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            backgroundSprite = sprite;
            this.imageType = imageType;
        }, $"Failed to set background sprite for {elementName}");
    }

    public ButtonBuilder WithBackgroundSprite(string resourcePath, Image.Type imageType = Image.Type.Sliced)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            Sprite sprite = LoadSpriteFromResources(resourcePath);
            if (sprite != null)
            {
                backgroundSprite = sprite;
                this.imageType = imageType;
            }
        }, $"Failed to load background sprite from resources for {elementName}");
    }

    public ButtonBuilder WithTransition(Selectable.Transition transition)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            transitionType = transition;
        }, $"Failed to set transition for {elementName}");
    }

    public ButtonBuilder WithInteractable(bool interactable)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            isInteractable = interactable;
        }, $"Failed to set interactable for {elementName}");
    }

    public ButtonBuilder OnClick(UnityAction action)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (action != null)
                clickActions.Add(action);
        }, $"Failed to add click action for {elementName}");
    }

    public ButtonBuilder WithIcon(Sprite icon, Vector2 size, bool leftSide = true, float spacing = 10f)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(elementObject.transform, false);

                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                iconRect.sizeDelta = size;

                // Position the icon
                if (leftSide)
                {
                    // Position on the left with spacing from text
                    iconRect.anchorMin = new Vector2(0, 0.5f);
                    iconRect.anchorMax = new Vector2(0, 0.5f);
                    iconRect.pivot = new Vector2(0, 0.5f);
                    iconRect.anchoredPosition = new Vector2(spacing, 0);

                    // Adjust text if present
                    if (textComponent != null)
                    {
                        RectTransform textRect = textComponent.GetComponent<RectTransform>();
                        textRect.offsetMin = new Vector2(size.x + spacing * 2, textRect.offsetMin.y);
                    }
                    else if (tmpTextComponent != null)
                    {
                        RectTransform textRect = tmpTextComponent.GetComponent<RectTransform>();
                        textRect.offsetMin = new Vector2(size.x + spacing * 2, textRect.offsetMin.y);
                    }
                }
                else
                {
                    // Position on the right with spacing from text
                    iconRect.anchorMin = new Vector2(1, 0.5f);
                    iconRect.anchorMax = new Vector2(1, 0.5f);
                    iconRect.pivot = new Vector2(1, 0.5f);
                    iconRect.anchoredPosition = new Vector2(-spacing, 0);

                    // Adjust text if present
                    if (textComponent != null)
                    {
                        RectTransform textRect = textComponent.GetComponent<RectTransform>();
                        textRect.offsetMax = new Vector2(-(size.x + spacing * 2), textRect.offsetMax.y);
                    }
                    else if (tmpTextComponent != null)
                    {
                        RectTransform textRect = tmpTextComponent.GetComponent<RectTransform>();
                        textRect.offsetMax = new Vector2(-(size.x + spacing * 2), textRect.offsetMax.y);
                    }
                }

                // Set the icon sprite
                Image iconImage = iconObj.GetComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
            }
        }, $"Failed to add icon to {elementName}");
    }

    public override Button AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            // Apply background sprite if set
            if (backgroundSprite != null)
            {
                Image image = elementObject.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = backgroundSprite;
                    image.type = imageType;
                }
            }

            // Apply custom colors if set
            if (useCustomColors)
            {
                colors.fadeDuration = fadeDuration;
                component.colors = colors;
            }

            // Apply sprite state if set
            if (useSpriteState)
            {
                component.spriteState = spriteState;
            }

            // Apply transition
            component.transition = transitionType;

            // Apply interactable state
            component.interactable = isInteractable;

            // Add click listeners
            foreach (var action in clickActions)
            {
                component.onClick.AddListener(action);
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
            HandleError($"Failed to add button to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion