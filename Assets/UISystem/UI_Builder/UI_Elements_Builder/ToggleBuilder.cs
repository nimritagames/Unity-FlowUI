using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;

#region Toggle Builder Implementation
public class ToggleBuilder : UIElementBuilder<Toggle, ToggleBuilder>
{
    private string text = "";
    private bool isOn = false;
    private bool interactable = true;
    private Color textColor = Color.white;
    private Sprite backgroundSprite;
    private Sprite checkmarkSprite;
    private bool useTMP = false;
    private List<UnityAction<bool>> valueChangedActions = new List<UnityAction<bool>>();
    private ColorBlock colors;
    private bool useCustomColors = false;
    private ToggleGroup toggleGroup;
    private Selectable.Transition transition = Selectable.Transition.ColorTint;
    private SpriteState spriteState;
    private bool useSpriteState = false;
    private int fontSize = 14;
    private FontStyle fontStyle = FontStyle.Normal;

    public ToggleBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            rectTransform = elementObject.GetComponent<RectTransform>();
            component = elementObject.GetComponent<Toggle>();

            rectTransform.sizeDelta = new Vector2(160, 30);
            colors = component.colors;

            // Create required structure for toggle
            SetupToggleStructure();
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize ToggleBuilder: {ex.Message}");
        }
    }

    private void SetupToggleStructure()
    {
        // Create the background image
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(elementObject.transform, false);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.5f);
        bgRect.anchorMax = new Vector2(0, 0.5f);
        bgRect.sizeDelta = new Vector2(20, 20);
        bgRect.anchoredPosition = new Vector2(10, 0);

        // Create the checkmark image (child of background)
        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(background.transform, false);
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.anchorMin = Vector2.zero;
        checkRect.anchorMax = Vector2.one;
        checkRect.offsetMin = new Vector2(2, 2);
        checkRect.offsetMax = new Vector2(-2, -2);

        // Create the label
        GameObject label;
        if (useTMP)
        {
            label = new GameObject("Label", typeof(RectTransform), typeof(TMP_Text));
            label.transform.SetParent(elementObject.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.offsetMax = Vector2.zero;
            TMP_Text labelText = label.GetComponent<TMP_Text>();
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.text = "Toggle";
            labelText.color = textColor;
            labelText.fontSize = fontSize;
            labelText.fontStyle = (TMPro.FontStyles)fontStyle;

            // Set default font for TMP
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                labelText.font = TMPro.TMP_Settings.defaultFontAsset;
            }
        }
        else
        {
            label = new GameObject("Label", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(elementObject.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(30, 0);
            labelRect.offsetMax = Vector2.zero;
            Text labelText = label.GetComponent<Text>();
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.text = "Toggle";
            labelText.color = textColor;
            labelText.fontSize = fontSize;
            labelText.fontStyle = fontStyle;
            labelText.font = GetDefaultFont();
        }

        // Assign the components
        component.graphic = checkmark.GetComponent<Image>();
        component.targetGraphic = background.GetComponent<Image>();
    }

    public ToggleBuilder WithText(string text, bool useTMP = false)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
        this.text = text;
        this.useTMP = useTMP;

        if (elementObject != null)
        {
            Transform labelTransform = elementObject.transform.Find("Label");
            if (labelTransform != null)
            {
                if (useTMP)
                {
                    // Replace Text with TMP_Text if needed
                    Text oldText = labelTransform.GetComponent<Text>();
                    if (oldText != null)
                    {
                        UnityEngine.Object.DestroyImmediate(oldText);
                    }

                    if (labelTransform.GetComponent<TMP_Text>() == null)
                    {
                            TMP_Text tmpText = labelTransform.gameObject.AddComponent<TMP_Text>();
                            tmpText.text = text;
                            tmpText.alignment = TextAlignmentOptions.Left;
                            tmpText.color = textColor;
                            tmpText.fontSize = fontSize;
                            tmpText.fontStyle = (TMPro.FontStyles)fontStyle;

                            // Set default font for TMP
                            if (TMPro.TMP_Settings.defaultFontAsset != null)
                            {
                                tmpText.font = TMPro.TMP_Settings.defaultFontAsset;
                            }
                        }
                        else
                        {
                            TMP_Text tmpText = labelTransform.GetComponent<TMP_Text>();
                            tmpText.text = text;
                            tmpText.color = textColor;
                            tmpText.fontSize = fontSize;
                            tmpText.fontStyle = (TMPro.FontStyles)fontStyle;
                        }
                    }
                    else
                    {
                        // Use standard Text component
                        TMP_Text oldTmpText = labelTransform.GetComponent<TMP_Text>();
                        if (oldTmpText != null)
                        {
                            UnityEngine.Object.DestroyImmediate(oldTmpText);
                        }

                        Text labelComponent = labelTransform.GetComponent<Text>();
                        if (labelComponent == null)
                        {
                            labelComponent = labelTransform.gameObject.AddComponent<Text>();
                        }

                        labelComponent.text = text;
                        labelComponent.color = textColor;
                        labelComponent.fontSize = fontSize;
                        labelComponent.fontStyle = fontStyle;
                        labelComponent.font = GetDefaultFont();
                    }
                }
            }
        }, $"Failed to set text for {elementName}");
    }

    public ToggleBuilder WithIsOn(bool isOn)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.isOn = isOn;
            if (component != null)
            {
                component.isOn = isOn;
            }
        }, $"Failed to set isOn for {elementName}");
    }

    public ToggleBuilder WithInteractable(bool interactable)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.interactable = interactable;
            if (component != null)
            {
                component.interactable = interactable;
            }
        }, $"Failed to set interactable for {elementName}");
    }

    public ToggleBuilder WithTextColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            textColor = color;
            if (elementObject != null)
            {
                Transform labelTransform = elementObject.transform.Find("Label");
                if (labelTransform != null)
                {
                    Text textComponent = labelTransform.GetComponent<Text>();
                    if (textComponent != null)
                    {
                        textComponent.color = color;
                    }

                    TMP_Text tmpText = labelTransform.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        tmpText.color = color;
                    }
                }
            }
        }, $"Failed to set text color for {elementName}");
    }

    public ToggleBuilder WithFontSize(int size)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fontSize = size;
            if (elementObject != null)
            {
                Transform labelTransform = elementObject.transform.Find("Label");
                if (labelTransform != null)
                {
                    Text textComponent = labelTransform.GetComponent<Text>();
                    if (textComponent != null)
                    {
                        textComponent.fontSize = size;
                    }

                    TMP_Text tmpText = labelTransform.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        tmpText.fontSize = size;
                    }
                }
            }
        }, $"Failed to set font size for {elementName}");
    }

    public ToggleBuilder WithFontStyle(FontStyle style)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fontStyle = style;
            if (elementObject != null)
            {
                Transform labelTransform = elementObject.transform.Find("Label");
                if (labelTransform != null)
                {
                    Text textComponent = labelTransform.GetComponent<Text>();
                    if (textComponent != null)
                    {
                        textComponent.fontStyle = style;
                    }

                    TMP_Text tmpText = labelTransform.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        tmpText.fontStyle = (TMPro.FontStyles)style;
                    }
                }
            }
        }, $"Failed to set font style for {elementName}");
    }

    public ToggleBuilder WithBackgroundSprite(Sprite sprite)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            backgroundSprite = sprite;
            if (elementObject != null)
            {
                Transform bgTransform = elementObject.transform.Find("Background");
                if (bgTransform != null)
                {
                    Image bgImage = bgTransform.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.sprite = sprite;
                    }
                }
            }
        }, $"Failed to set background sprite for {elementName}");
    }

    public ToggleBuilder WithCheckmarkSprite(Sprite sprite)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            checkmarkSprite = sprite;
            if (elementObject != null)
            {
                Transform bgTransform = elementObject.transform.Find("Background");
                if (bgTransform != null)
                {
                    Transform checkTransform = bgTransform.Find("Checkmark");
                    if (checkTransform != null)
                    {
                        Image checkImage = checkTransform.GetComponent<Image>();
                        if (checkImage != null)
                        {
                            checkImage.sprite = sprite;
                        }
                    }
                }
            }
        }, $"Failed to set checkmark sprite for {elementName}");
    }

    public ToggleBuilder WithColors(Color normalColor, Color highlightedColor, Color pressedColor, Color disabledColor)
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

    public ToggleBuilder WithToggleGroup(ToggleGroup group)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            toggleGroup = group;
        }, $"Failed to set toggle group for {elementName}");
    }

    public ToggleBuilder WithTransition(Selectable.Transition transition)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.transition = transition;
        }, $"Failed to set transition for {elementName}");
    }

    public ToggleBuilder WithSpriteState(Sprite highlightedSprite, Sprite pressedSprite, Sprite disabledSprite)
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

    public ToggleBuilder OnValueChanged(UnityAction<bool> action)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (action != null)
            {
                valueChangedActions.Add(action);
            }
        }, $"Failed to add value changed action for {elementName}");
    }

    public override Toggle AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            // Apply properties
            component.isOn = isOn;
            component.interactable = interactable;
            component.transition = transition;

            if (toggleGroup != null)
            {
                component.group = toggleGroup;
            }

            // Apply colors
            if (useCustomColors)
            {
                component.colors = colors;
            }

            // Apply sprite state
            if (useSpriteState)
            {
                component.spriteState = spriteState;
            }

            // Set sprites if provided
            if (backgroundSprite != null)
            {
                Transform bgTransform = elementObject.transform.Find("Background");
                if (bgTransform != null)
                {
                    Image bgImage = bgTransform.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.sprite = backgroundSprite;
                    }
                }
            }

            if (checkmarkSprite != null)
            {
                Transform bgTransform = elementObject.transform.Find("Background");
                if (bgTransform != null)
                {
                    Transform checkTransform = bgTransform.Find("Checkmark");
                    if (checkTransform != null)
                    {
                        Image checkImage = checkTransform.GetComponent<Image>();
                        if (checkImage != null)
                        {
                            checkImage.sprite = checkmarkSprite;
                        }
                    }
                }
            }

            // Update text if needed
            Transform labelTransform = elementObject.transform.Find("Label");
            if (labelTransform != null)
            {
                if (useTMP)
                {
                    TMP_Text tmpText = labelTransform.GetComponent<TMP_Text>();
                    if (tmpText != null)
                    {
                        tmpText.text = text;
                        tmpText.color = textColor;
                    }
                }
                else
                {
                    Text labelComponent = labelTransform.GetComponent<Text>();
                    if (labelComponent != null)
                    {
                        labelComponent.text = text;
                        labelComponent.color = textColor;
                    }
                }
            }

            // Add value changed listeners
            foreach (var action in valueChangedActions)
            {
                component.onValueChanged.AddListener(action);
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
            HandleError($"Failed to add toggle to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion