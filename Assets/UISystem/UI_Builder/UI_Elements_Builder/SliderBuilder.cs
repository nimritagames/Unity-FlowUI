using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;

#region Slider Builder Implementation
public class SliderBuilder : UIElementBuilder<Slider, SliderBuilder>
{
    private float minValue = 0f;
    private float maxValue = 1f;
    private float value = 0.5f;
    private bool wholeNumbers = false;
    private Slider.Direction direction = Slider.Direction.LeftToRight;
    private Sprite backgroundSprite;
    private Sprite fillSprite;
    private Sprite handleSprite;
    private Color backgroundColor = Color.white;
    private Color fillColor = Color.white;
    private Color handleColor = Color.white;
    private List<UnityAction<float>> valueChangedActions = new List<UnityAction<float>>();
    private bool interactable = true;
    private ColorBlock colors;
    private bool useCustomColors = false;
    private Selectable.Transition transition = Selectable.Transition.ColorTint;
    private SpriteState spriteState;
    private bool useSpriteState = false;
    private bool showSliderValueText = false;
    private bool useTMPForValueText = false;
    private string valueTextFormat = "{0:0.0}";
    private int valueTextFontSize = 12;
    private Color valueTextColor = Color.black;

    public SliderBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform), typeof(Slider));
            rectTransform = elementObject.GetComponent<RectTransform>();
            component = elementObject.GetComponent<Slider>();

            rectTransform.sizeDelta = new Vector2(200, 30);
            colors = component.colors;

            // Create required structure for slider
            SetupSliderStructure();
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize SliderBuilder: {ex.Message}");
        }
    }

    private void SetupSliderStructure()
    {
        // Create the background
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(elementObject.transform, false);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(0, 12);
        bgRect.offsetMax = new Vector2(0, -12);

        // Create the fill area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(elementObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.offsetMin = new Vector2(5, 0);
        fillAreaRect.offsetMax = new Vector2(-5, 0);

        // Create the fill
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1);
        fillRect.pivot = new Vector2(0, 0.5f);
        fillRect.sizeDelta = Vector2.zero;

        // Create the handle area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(elementObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        // Create the handle
        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(20, 20);
        handleRect.anchoredPosition = Vector2.zero;

        // Assign components to slider
        component.fillRect = fillRect;
        component.handleRect = handleRect;
        component.targetGraphic = handle.GetComponent<Image>();
    }

    private void SetupValueText()
    {
        if (!showSliderValueText) return;

        // Check if value text already exists
        Transform existingValueText = elementObject.transform.Find("Value Text");
        if (existingValueText != null)
        {
            UnityEngine.Object.DestroyImmediate(existingValueText.gameObject);
        }

        // Create value text
        GameObject valueTextObj;
        if (useTMPForValueText)
        {
            valueTextObj = new GameObject("Value Text", typeof(RectTransform), typeof(TMP_Text));
            valueTextObj.transform.SetParent(elementObject.transform, false);
            RectTransform valueTextRect = valueTextObj.GetComponent<RectTransform>();
            valueTextRect.anchorMin = new Vector2(1, 0.5f);
            valueTextRect.anchorMax = new Vector2(1, 0.5f);
            valueTextRect.pivot = new Vector2(0, 0.5f);
            valueTextRect.sizeDelta = new Vector2(40, 20);
            valueTextRect.anchoredPosition = new Vector2(10, 0);

            TMP_Text tmpText = valueTextObj.GetComponent<TMP_Text>();
            tmpText.text = string.Format(valueTextFormat, value);
            tmpText.color = valueTextColor;
            tmpText.fontSize = valueTextFontSize;
            tmpText.alignment = TextAlignmentOptions.Left;

            // Set default font for TMP
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                tmpText.font = TMPro.TMP_Settings.defaultFontAsset;
            }
        }
        else
        {
            valueTextObj = new GameObject("Value Text", typeof(RectTransform), typeof(Text));
            valueTextObj.transform.SetParent(elementObject.transform, false);
            RectTransform valueTextRect = valueTextObj.GetComponent<RectTransform>();
            valueTextRect.anchorMin = new Vector2(1, 0.5f);
            valueTextRect.anchorMax = new Vector2(1, 0.5f);
            valueTextRect.pivot = new Vector2(0, 0.5f);
            valueTextRect.sizeDelta = new Vector2(40, 20);
            valueTextRect.anchoredPosition = new Vector2(10, 0);

            Text text = valueTextObj.GetComponent<Text>();
            text.text = string.Format(valueTextFormat, value);
            text.color = valueTextColor;
            text.fontSize = valueTextFontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.font = GetDefaultFont();
        }
    }

    public SliderBuilder WithRange(float min, float max, float value = float.MinValue)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            minValue = min;
            maxValue = max;
            if (value != float.MinValue)
            {
                this.value = Mathf.Clamp(value, min, max);
            }
            else
            {
                this.value = min + (max - min) * 0.5f;
            }

            if (component != null)
            {
                component.minValue = minValue;
                component.maxValue = maxValue;
                component.value = this.value;
            }
        }, $"Failed to set range for {elementName}");
    }

    public SliderBuilder WithValue(float value)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.value = Mathf.Clamp(value, minValue, maxValue);
            if (component != null)
            {
                component.value = this.value;
            }
        }, $"Failed to set value for {elementName}");
    }

    public SliderBuilder WithWholeNumbers(bool wholeNumbers)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.wholeNumbers = wholeNumbers;
            if (component != null)
            {
                component.wholeNumbers = wholeNumbers;
            }
        }, $"Failed to set whole numbers for {elementName}");
    }

    public SliderBuilder WithDirection(Slider.Direction direction)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.direction = direction;
            if (component != null)
            {
                component.direction = direction;
            }
        }, $"Failed to set direction for {elementName}");
    }

    public SliderBuilder WithBackgroundSprite(Sprite sprite, Color color = default)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            backgroundSprite = sprite;
            if (color != default)
            {
                backgroundColor = color;
            }

            if (elementObject != null)
            {
                Transform bgTransform = elementObject.transform.Find("Background");
                if (bgTransform != null)
                {
                    Image bgImage = bgTransform.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.sprite = sprite;
                        if (color != default)
                        {
                            bgImage.color = color;
                        }
                    }
                }
            }
        }, $"Failed to set background sprite for {elementName}");
    }

    public SliderBuilder WithFillSprite(Sprite sprite, Color color = default)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fillSprite = sprite;
            if (color != default)
            {
                fillColor = color;
            }

            if (elementObject != null)
            {
                Transform fillAreaTransform = elementObject.transform.Find("Fill Area");
                if (fillAreaTransform != null)
                {
                    Transform fillTransform = fillAreaTransform.Find("Fill");
                    if (fillTransform != null)
                    {
                        Image fillImage = fillTransform.GetComponent<Image>();
                        if (fillImage != null)
                        {
                            fillImage.sprite = sprite;
                            if (color != default)
                            {
                                fillImage.color = color;
                            }
                        }
                    }
                }
            }
        }, $"Failed to set fill sprite for {elementName}");
    }

    public SliderBuilder WithHandleSprite(Sprite sprite, Color color = default)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            handleSprite = sprite;
            if (color != default)
            {
                handleColor = color;
            }

            if (elementObject != null)
            {
                Transform handleAreaTransform = elementObject.transform.Find("Handle Slide Area");
                if (handleAreaTransform != null)
                {
                    Transform handleTransform = handleAreaTransform.Find("Handle");
                    if (handleTransform != null)
                    {
                        Image handleImage = handleTransform.GetComponent<Image>();
                        if (handleImage != null)
                        {
                            handleImage.sprite = sprite;
                            if (color != default)
                            {
                                handleImage.color = color;
                            }
                        }
                    }
                }
            }
        }, $"Failed to set handle sprite for {elementName}");
    }

    public SliderBuilder WithColors(Color normalColor, Color highlightedColor, Color pressedColor, Color disabledColor)
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

    public SliderBuilder WithTransition(Selectable.Transition transition)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.transition = transition;
        }, $"Failed to set transition for {elementName}");
    }

    public SliderBuilder WithSpriteState(Sprite highlightedSprite, Sprite pressedSprite, Sprite disabledSprite)
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

    public SliderBuilder WithInteractable(bool interactable)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.interactable = interactable;
        }, $"Failed to set interactable for {elementName}");
    }

    public SliderBuilder WithValueText(bool show, bool useTMP = false, string format = "{0:0.0}", int fontSize = 12, Color color = default)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            showSliderValueText = show;
            useTMPForValueText = useTMP;
            valueTextFormat = format;
            valueTextFontSize = fontSize;

            if (color != default)
            {
                valueTextColor = color;
            }
        }, $"Failed to set value text for {elementName}");
    }

    public SliderBuilder OnValueChanged(UnityAction<float> action)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (action != null)
            {
                valueChangedActions.Add(action);
            }
        }, $"Failed to add value changed action for {elementName}");
    }

    public override Slider AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            // Apply properties
            component.minValue = minValue;
            component.maxValue = maxValue;
            component.value = value;
            component.wholeNumbers = wholeNumbers;
            component.direction = direction;
            component.interactable = interactable;
            component.transition = transition;

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

            // Apply sprites
            if (backgroundSprite != null)
            {
                Transform bgTransform = elementObject.transform.Find("Background");
                if (bgTransform != null)
                {
                    Image bgImage = bgTransform.GetComponent<Image>();
                    if (bgImage != null)
                    {
                        bgImage.sprite = backgroundSprite;
                        bgImage.color = backgroundColor;
                    }
                }
            }

            if (fillSprite != null)
            {
                Transform fillAreaTransform = elementObject.transform.Find("Fill Area");
                if (fillAreaTransform != null)
                {
                    Transform fillTransform = fillAreaTransform.Find("Fill");
                    if (fillTransform != null)
                    {
                        Image fillImage = fillTransform.GetComponent<Image>();
                        if (fillImage != null)
                        {
                            fillImage.sprite = fillSprite;
                            fillImage.color = fillColor;
                        }
                    }
                }
            }

            if (handleSprite != null)
            {
                Transform handleAreaTransform = elementObject.transform.Find("Handle Slide Area");
                if (handleAreaTransform != null)
                {
                    Transform handleTransform = handleAreaTransform.Find("Handle");
                    if (handleTransform != null)
                    {
                        Image handleImage = handleTransform.GetComponent<Image>();
                        if (handleImage != null)
                        {
                            handleImage.sprite = handleSprite;
                            handleImage.color = handleColor;
                        }
                    }
                }
            }

            // Set up value text if requested
            if (showSliderValueText)
            {
                SetupValueText();

                // Add special event listener to update the value text
                component.onValueChanged.AddListener((val) => {
                    Transform valueTextTrans = elementObject.transform.Find("Value Text");
                    if (valueTextTrans != null)
                    {
                        // Update either Text or TMP_Text component
                        Text textComp = valueTextTrans.GetComponent<Text>();
                        if (textComp != null)
                        {
                            textComp.text = string.Format(valueTextFormat, val);
                        }

                        TMP_Text tmpTextComp = valueTextTrans.GetComponent<TMP_Text>();
                        if (tmpTextComp != null)
                        {
                            tmpTextComp.text = string.Format(valueTextFormat, val);
                        }
                    }
                });
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
            HandleError($"Failed to add slider to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion