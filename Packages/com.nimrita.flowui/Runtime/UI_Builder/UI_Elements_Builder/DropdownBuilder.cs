using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;

#region Dropdown Builder Implementation
public class DropdownBuilder : UIElementBuilder<Dropdown, DropdownBuilder>
{
    private List<string> options = new List<string>();
    private int selectedIndex = 0;
    private bool interactable = true;
    private bool useTMP = false;
    private List<UnityAction<int>> valueChangedActions = new List<UnityAction<int>>();
    private ColorBlock colors;
    private bool useCustomColors = false;
    private Sprite backgroundSprite;
    private Sprite checkmarkSprite;
    private Sprite arrowSprite;
    private Selectable.Transition transition = Selectable.Transition.ColorTint;
    private SpriteState spriteState;
    private bool useSpriteState = false;
    private Color textColor = Color.black;
    private Color dropdownBackgroundColor = Color.white;
    private int fontSize = 14;
    private FontStyle fontStyle = FontStyle.Normal;
    private bool showDropdownScrollbar = true;
    private float dropdownItemHeight = 20f;
    private int maxDropdownItems = 8;

    // Reference to TMP component if used
    private TMP_Dropdown tmpDropdown;

    public DropdownBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform));
            rectTransform = elementObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(160, 30);

            // Defer creation of dropdown until we know which type to use
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize DropdownBuilder: {ex.Message}");
        }
    }

    private void SetupDropdownStructure()
    {
        if (useTMP)
        {
            // Remove regular dropdown if it exists
            Dropdown regularDropdown = elementObject.GetComponent<Dropdown>();
            if (regularDropdown != null)
            {
                UnityEngine.Object.DestroyImmediate(regularDropdown);
            }

            // Add TMP dropdown
            if (elementObject.GetComponent<TMP_Dropdown>() == null)
            {
                tmpDropdown = elementObject.AddComponent<TMP_Dropdown>();

                // Create required hierarchy
                // Background
                GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
                background.transform.SetParent(elementObject.transform, false);
                RectTransform bgRect = background.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                Image bgImage = background.GetComponent<Image>();
                bgImage.color = dropdownBackgroundColor;

                // Label
                GameObject label = new GameObject("Label", typeof(RectTransform), typeof(TMP_Text));
                label.transform.SetParent(background.transform, false);
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10, 6);
                labelRect.offsetMax = new Vector2(-25, -7);
                TMP_Text labelText = label.GetComponent<TMP_Text>();
                labelText.text = options.Count > 0 ? options[0] : "Select...";
                labelText.color = textColor;
                labelText.fontSize = fontSize;
                labelText.fontStyle = (TMPro.FontStyles)fontStyle;
                labelText.alignment = TextAlignmentOptions.Left;

                // Set default font for TMP
                if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    labelText.font = TMPro.TMP_Settings.defaultFontAsset;
                }

                // Arrow
                GameObject arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                arrow.transform.SetParent(background.transform, false);
                RectTransform arrowRect = arrow.GetComponent<RectTransform>();
                arrowRect.anchorMin = new Vector2(1, 0.5f);
                arrowRect.anchorMax = new Vector2(1, 0.5f);
                arrowRect.pivot = new Vector2(1, 0.5f);
                arrowRect.sizeDelta = new Vector2(20, 20);
                arrowRect.anchoredPosition = new Vector2(-5, 0);

                // Template
                GameObject template = new GameObject("Template", typeof(RectTransform), typeof(ScrollRect));
                template.transform.SetParent(elementObject.transform, false);
                template.SetActive(false);
                RectTransform templateRect = template.GetComponent<RectTransform>();
                templateRect.anchorMin = new Vector2(0, 0);
                templateRect.anchorMax = new Vector2(1, 0);
                templateRect.pivot = new Vector2(0.5f, 1);
                templateRect.anchoredPosition = new Vector2(0, 2);
                templateRect.sizeDelta = new Vector2(0, dropdownItemHeight * maxDropdownItems);

                // Template background
                GameObject templateBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                templateBg.transform.SetParent(template.transform, false);
                RectTransform templateBgRect = templateBg.GetComponent<RectTransform>();
                templateBgRect.anchorMin = Vector2.zero;
                templateBgRect.anchorMax = Vector2.one;
                templateBgRect.sizeDelta = Vector2.zero;
                Image templateBgImage = templateBg.GetComponent<Image>();
                templateBgImage.color = dropdownBackgroundColor;

                // Add mask
                if (templateBg.GetComponent<Mask>() == null)
                {
                    Mask mask = templateBg.AddComponent<Mask>();
                    mask.showMaskGraphic = true;
                }

                // Add viewport
                GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(template.transform, false);
                RectTransform viewportRect = viewport.GetComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.sizeDelta = new Vector2(-18, 0);
                viewportRect.pivot = new Vector2(0, 1);

                // Add content
                GameObject content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                RectTransform contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0, dropdownItemHeight * options.Count);

                // Add scrollbar if requested
                GameObject scrollbar = null;
                if (showDropdownScrollbar)
                {
                    scrollbar = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
                    scrollbar.transform.SetParent(template.transform, false);
                    RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
                    scrollbarRect.anchorMin = new Vector2(1, 0);
                    scrollbarRect.anchorMax = new Vector2(1, 1);
                    scrollbarRect.pivot = new Vector2(1, 1);
                    scrollbarRect.sizeDelta = new Vector2(18, 0);
                    scrollbarRect.anchoredPosition = Vector2.zero;

                    GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
                    slidingArea.transform.SetParent(scrollbar.transform, false);
                    RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
                    slidingAreaRect.anchorMin = Vector2.zero;
                    slidingAreaRect.anchorMax = Vector2.one;
                    slidingAreaRect.offsetMin = new Vector2(1, 1);
                    slidingAreaRect.offsetMax = new Vector2(-1, -1);

                    GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                    handle.transform.SetParent(slidingArea.transform, false);
                    RectTransform handleRect = handle.GetComponent<RectTransform>();
                    handleRect.anchorMin = Vector2.zero;
                    handleRect.anchorMax = Vector2.one;
                    handleRect.sizeDelta = Vector2.zero;

                    // Setup scrollbar component
                    Scrollbar scrollbarComp = scrollbar.GetComponent<Scrollbar>();
                    scrollbarComp.direction = Scrollbar.Direction.BottomToTop;
                    scrollbarComp.handleRect = handleRect;
                    scrollbarComp.targetGraphic = handle.GetComponent<Image>();
                }

                // Setup scroll rect
                ScrollRect scrollRect = template.GetComponent<ScrollRect>();
                scrollRect.content = contentRect;
                scrollRect.viewport = viewportRect;
                scrollRect.horizontal = false;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.verticalScrollbar = showDropdownScrollbar ? scrollbar.GetComponent<Scrollbar>() : null;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                scrollRect.verticalScrollbarSpacing = -3f;

                // Item template
                GameObject itemTemplate = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
                itemTemplate.transform.SetParent(content.transform, false);
                RectTransform itemTemplateRect = itemTemplate.GetComponent<RectTransform>();
                itemTemplateRect.anchorMin = new Vector2(0, 0.5f);
                itemTemplateRect.anchorMax = new Vector2(1, 0.5f);
                itemTemplateRect.sizeDelta = new Vector2(0, dropdownItemHeight);
                itemTemplateRect.anchoredPosition = Vector2.zero;

                // Item background
                GameObject itemBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                itemBg.transform.SetParent(itemTemplate.transform, false);
                RectTransform itemBgRect = itemBg.GetComponent<RectTransform>();
                itemBgRect.anchorMin = Vector2.zero;
                itemBgRect.anchorMax = Vector2.one;
                itemBgRect.sizeDelta = Vector2.zero;

                // Item checkmark
                GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkmark.transform.SetParent(itemTemplate.transform, false);
                RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
                checkmarkRect.anchorMin = new Vector2(0, 0.5f);
                checkmarkRect.anchorMax = new Vector2(0, 0.5f);
                checkmarkRect.sizeDelta = new Vector2(20, 20);
                checkmarkRect.anchoredPosition = new Vector2(10, 0);

                // Item label
                GameObject itemLabel = new GameObject("Label", typeof(RectTransform), typeof(TMP_Text));
                itemLabel.transform.SetParent(itemTemplate.transform, false);
                RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
                itemLabelRect.anchorMin = Vector2.zero;
                itemLabelRect.anchorMax = Vector2.one;
                itemLabelRect.offsetMin = new Vector2(30, 0);
                itemLabelRect.offsetMax = Vector2.zero;
                TMP_Text itemLabelText = itemLabel.GetComponent<TMP_Text>();
                itemLabelText.fontSize = fontSize;
                itemLabelText.fontStyle = (TMPro.FontStyles)fontStyle;
                itemLabelText.alignment = TextAlignmentOptions.Left;
                itemLabelText.color = textColor;

                // Set default font for TMP
                if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    itemLabelText.font = TMPro.TMP_Settings.defaultFontAsset;
                }

                // Configure the toggle
                Toggle itemToggle = itemTemplate.GetComponent<Toggle>();
                itemToggle.targetGraphic = itemBg.GetComponent<Image>();
                itemToggle.graphic = checkmark.GetComponent<Image>();
                itemToggle.isOn = false;

                // Configure the TMP dropdown component
                tmpDropdown.template = templateRect;
                tmpDropdown.captionText = labelText;
                tmpDropdown.itemText = itemLabelText;
                tmpDropdown.value = selectedIndex;

                // Add options
                if (options.Count > 0)
                {
                    List<TMP_Dropdown.OptionData> optionData = new List<TMP_Dropdown.OptionData>();
                    foreach (string option in options)
                    {
                        optionData.Add(new TMP_Dropdown.OptionData(option));
                    }
                    tmpDropdown.options = optionData;

                    // Make sure content size is updated
                    contentRect.sizeDelta = new Vector2(0, dropdownItemHeight * options.Count);
                }
            }
            else
            {
                tmpDropdown = elementObject.GetComponent<TMP_Dropdown>();
            }
        }
        else
        {
            // Remove TMP dropdown if it exists
            TMP_Dropdown existingTmpDropdown = elementObject.GetComponent<TMP_Dropdown>();
            if (existingTmpDropdown != null)
            {
                UnityEngine.Object.DestroyImmediate(existingTmpDropdown);
            }

            // Add regular dropdown
            if (elementObject.GetComponent<Dropdown>() == null)
            {
                component = elementObject.AddComponent<Dropdown>();

                // Create required hierarchy
                // Background
                GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
                background.transform.SetParent(elementObject.transform, false);
                RectTransform bgRect = background.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.sizeDelta = Vector2.zero;
                Image bgImage = background.GetComponent<Image>();
                bgImage.color = dropdownBackgroundColor;

                // Label
                GameObject label = new GameObject("Label", typeof(RectTransform), typeof(Text));
                label.transform.SetParent(background.transform, false);
                RectTransform labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(10, 6);
                labelRect.offsetMax = new Vector2(-25, -7);
                Text labelText = label.GetComponent<Text>();
                labelText.text = options.Count > 0 ? options[0] : "Select...";
                labelText.color = textColor;
                labelText.fontSize = fontSize;
                labelText.fontStyle = fontStyle;
                labelText.alignment = TextAnchor.MiddleLeft;
                labelText.font = GetDefaultFont();

                // Arrow
                GameObject arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                arrow.transform.SetParent(background.transform, false);
                RectTransform arrowRect = arrow.GetComponent<RectTransform>();
                arrowRect.anchorMin = new Vector2(1, 0.5f);
                arrowRect.anchorMax = new Vector2(1, 0.5f);
                arrowRect.pivot = new Vector2(1, 0.5f);
                arrowRect.sizeDelta = new Vector2(20, 20);
                arrowRect.anchoredPosition = new Vector2(-5, 0);

                // Template
                GameObject template = new GameObject("Template", typeof(RectTransform), typeof(ScrollRect));
                template.transform.SetParent(elementObject.transform, false);
                template.SetActive(false);
                RectTransform templateRect = template.GetComponent<RectTransform>();
                templateRect.anchorMin = new Vector2(0, 0);
                templateRect.anchorMax = new Vector2(1, 0);
                templateRect.pivot = new Vector2(0.5f, 1);
                templateRect.anchoredPosition = new Vector2(0, 2);
                templateRect.sizeDelta = new Vector2(0, dropdownItemHeight * maxDropdownItems);

                // Template background
                GameObject templateBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                templateBg.transform.SetParent(template.transform, false);
                RectTransform templateBgRect = templateBg.GetComponent<RectTransform>();
                templateBgRect.anchorMin = Vector2.zero;
                templateBgRect.anchorMax = Vector2.one;
                templateBgRect.sizeDelta = Vector2.zero;
                Image templateBgImage = templateBg.GetComponent<Image>();
                templateBgImage.color = dropdownBackgroundColor;

                // Add mask
                if (templateBg.GetComponent<Mask>() == null)
                {
                    Mask mask = templateBg.AddComponent<Mask>();
                    mask.showMaskGraphic = true;
                }

                // Add viewport
                GameObject viewport = new GameObject("Viewport", typeof(RectTransform));
                viewport.transform.SetParent(template.transform, false);
                RectTransform viewportRect = viewport.GetComponent<RectTransform>();
                viewportRect.anchorMin = Vector2.zero;
                viewportRect.anchorMax = Vector2.one;
                viewportRect.sizeDelta = new Vector2(-18, 0);
                viewportRect.pivot = new Vector2(0, 1);

                // Add content
                GameObject content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(viewport.transform, false);
                RectTransform contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = new Vector2(0, dropdownItemHeight * options.Count);

                // Add scrollbar if requested
                GameObject scrollbar = null;
                if (showDropdownScrollbar)
                {
                    scrollbar = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
                    scrollbar.transform.SetParent(template.transform, false);
                    RectTransform scrollbarRect = scrollbar.GetComponent<RectTransform>();
                    scrollbarRect.anchorMin = new Vector2(1, 0);
                    scrollbarRect.anchorMax = new Vector2(1, 1);
                    scrollbarRect.pivot = new Vector2(1, 1);
                    scrollbarRect.sizeDelta = new Vector2(18, 0);
                    scrollbarRect.anchoredPosition = Vector2.zero;

                    GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
                    slidingArea.transform.SetParent(scrollbar.transform, false);
                    RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
                    slidingAreaRect.anchorMin = Vector2.zero;
                    slidingAreaRect.anchorMax = Vector2.one;
                    slidingAreaRect.offsetMin = new Vector2(1, 1);
                    slidingAreaRect.offsetMax = new Vector2(-1, -1);

                    GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                    handle.transform.SetParent(slidingArea.transform, false);
                    RectTransform handleRect = handle.GetComponent<RectTransform>();
                    handleRect.anchorMin = Vector2.zero;
                    handleRect.anchorMax = Vector2.one;
                    handleRect.sizeDelta = Vector2.zero;

                    // Setup scrollbar component
                    Scrollbar scrollbarComp = scrollbar.GetComponent<Scrollbar>();
                    scrollbarComp.direction = Scrollbar.Direction.BottomToTop;
                    scrollbarComp.handleRect = handleRect;
                    scrollbarComp.targetGraphic = handle.GetComponent<Image>();
                }

                // Setup scroll rect
                ScrollRect scrollRect = template.GetComponent<ScrollRect>();
                scrollRect.content = contentRect;
                scrollRect.viewport = viewportRect;
                scrollRect.horizontal = false;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.verticalScrollbar = showDropdownScrollbar ? scrollbar.GetComponent<Scrollbar>() : null;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
                scrollRect.verticalScrollbarSpacing = -3f;

                // Item template
                GameObject itemTemplate = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
                itemTemplate.transform.SetParent(content.transform, false);
                RectTransform itemTemplateRect = itemTemplate.GetComponent<RectTransform>();
                itemTemplateRect.anchorMin = new Vector2(0, 0.5f);
                itemTemplateRect.anchorMax = new Vector2(1, 0.5f);
                itemTemplateRect.sizeDelta = new Vector2(0, dropdownItemHeight);
                itemTemplateRect.anchoredPosition = Vector2.zero;

                // Item background
                GameObject itemBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
                itemBg.transform.SetParent(itemTemplate.transform, false);
                RectTransform itemBgRect = itemBg.GetComponent<RectTransform>();
                itemBgRect.anchorMin = Vector2.zero;
                itemBgRect.anchorMax = Vector2.one;
                itemBgRect.sizeDelta = Vector2.zero;

                // Item checkmark
                GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkmark.transform.SetParent(itemTemplate.transform, false);
                RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
                checkmarkRect.anchorMin = new Vector2(0, 0.5f);
                checkmarkRect.anchorMax = new Vector2(0, 0.5f);
                checkmarkRect.sizeDelta = new Vector2(20, 20);
                checkmarkRect.anchoredPosition = new Vector2(10, 0);

                // Item label
                GameObject itemLabel = new GameObject("Label", typeof(RectTransform), typeof(Text));
                itemLabel.transform.SetParent(itemTemplate.transform, false);
                RectTransform itemLabelRect = itemLabel.GetComponent<RectTransform>();
                itemLabelRect.anchorMin = Vector2.zero;
                itemLabelRect.anchorMax = Vector2.one;
                itemLabelRect.offsetMin = new Vector2(30, 0);
                itemLabelRect.offsetMax = Vector2.zero;
                Text itemLabelText = itemLabel.GetComponent<Text>();
                itemLabelText.fontSize = fontSize;
                itemLabelText.fontStyle = fontStyle;
                itemLabelText.alignment = TextAnchor.MiddleLeft;
                itemLabelText.color = textColor;
                itemLabelText.font = GetDefaultFont();

                // Configure the toggle
                Toggle itemToggle = itemTemplate.GetComponent<Toggle>();
                itemToggle.targetGraphic = itemBg.GetComponent<Image>();
                itemToggle.graphic = checkmark.GetComponent<Image>();
                itemToggle.isOn = false;

                // Configure the dropdown component
                component.template = templateRect;
                component.captionText = labelText;
                component.itemText = itemLabelText;
                component.value = selectedIndex;

                // Add options
                if (options.Count > 0)
                {
                    List<Dropdown.OptionData> optionData = new List<Dropdown.OptionData>();
                    foreach (string option in options)
                    {
                        optionData.Add(new Dropdown.OptionData(option));
                    }
                    component.options = optionData;

                    // Make sure content size is updated
                    contentRect.sizeDelta = new Vector2(0, dropdownItemHeight * options.Count);
                }
            }
            else
            {
                component = elementObject.GetComponent<Dropdown>();
            }
        }
    }

    public DropdownBuilder WithTMP(bool useTMP)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.useTMP = useTMP;
        }, $"Failed to set TMP mode for {elementName}");
    }

    public DropdownBuilder WithOptions(params string[] options)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.options.Clear();
            this.options.AddRange(options);
        }, $"Failed to set options for {elementName}");
    }

    public DropdownBuilder WithOptions(List<string> options)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.options.Clear();
            this.options.AddRange(options);
        }, $"Failed to set options for {elementName}");
    }

    public DropdownBuilder WithSelectedIndex(int index)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (options.Count > 0)
            {
                selectedIndex = Mathf.Clamp(index, 0, options.Count - 1);
            }
            else
            {
                selectedIndex = 0;
            }
        }, $"Failed to set selected index for {elementName}");
    }

    public DropdownBuilder WithInteractable(bool interactable)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.interactable = interactable;
        }, $"Failed to set interactable for {elementName}");
    }

    public DropdownBuilder WithColors(Color normalColor, Color highlightedColor, Color pressedColor, Color disabledColor)
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

    public DropdownBuilder WithBackgroundSprite(Sprite sprite)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            backgroundSprite = sprite;
        }, $"Failed to set background sprite for {elementName}");
    }

    public DropdownBuilder WithArrowSprite(Sprite sprite)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            arrowSprite = sprite;
        }, $"Failed to set arrow sprite for {elementName}");
    }

    public DropdownBuilder WithCheckmarkSprite(Sprite sprite)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            checkmarkSprite = sprite;
        }, $"Failed to set checkmark sprite for {elementName}");
    }

    public DropdownBuilder WithTransition(Selectable.Transition transition)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.transition = transition;
        }, $"Failed to set transition for {elementName}");
    }

    public DropdownBuilder WithSpriteState(Sprite highlightedSprite, Sprite pressedSprite, Sprite disabledSprite)
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

    public DropdownBuilder WithTextColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            textColor = color;
        }, $"Failed to set text color for {elementName}");
    }

    public DropdownBuilder WithBackgroundColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            dropdownBackgroundColor = color;
        }, $"Failed to set background color for {elementName}");
    }

    public DropdownBuilder WithFontSize(int size)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fontSize = size;
        }, $"Failed to set font size for {elementName}");
    }

    public DropdownBuilder WithFontStyle(FontStyle style)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fontStyle = style;
        }, $"Failed to set font style for {elementName}");
    }

    public DropdownBuilder WithDropdownSettings(bool showScrollbar, float itemHeight = 20f, int maxItems = 8)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            showDropdownScrollbar = showScrollbar;
            dropdownItemHeight = itemHeight;
            maxDropdownItems = maxItems;
        }, $"Failed to set dropdown settings for {elementName}");
    }

    public DropdownBuilder OnValueChanged(UnityAction<int> action)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (action != null)
            {
                valueChangedActions.Add(action);
            }
        }, $"Failed to add value changed action for {elementName}");
    }

    public override Dropdown AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            // Create the structure now that all properties are set
            SetupDropdownStructure();

            if (useTMP && tmpDropdown != null)
            {
                // Apply properties
                tmpDropdown.interactable = interactable;
                tmpDropdown.transition = transition;
                tmpDropdown.value = selectedIndex;

                // Apply colors
                if (useCustomColors)
                {
                    tmpDropdown.colors = colors;
                }

                // Apply sprite state
                if (useSpriteState)
                {
                    tmpDropdown.spriteState = spriteState;
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
                        }
                    }
                }

                if (arrowSprite != null)
                {
                    Transform bgTransform = elementObject.transform.Find("Background");
                    if (bgTransform != null)
                    {
                        Transform arrowTransform = bgTransform.Find("Arrow");
                        if (arrowTransform != null)
                        {
                            Image arrowImage = arrowTransform.GetComponent<Image>();
                            if (arrowImage != null)
                            {
                                arrowImage.sprite = arrowSprite;
                            }
                        }
                    }
                }

                if (checkmarkSprite != null)
                {
                    Transform templateTransform = elementObject.transform.Find("Template");
                    if (templateTransform != null)
                    {
                        Transform viewportTransform = templateTransform.Find("Viewport");
                        if (viewportTransform != null)
                        {
                            Transform contentTransform = viewportTransform.Find("Content");
                            if (contentTransform != null && contentTransform.childCount > 0)
                            {
                                Transform itemTransform = contentTransform.GetChild(0);
                                if (itemTransform != null)
                                {
                                    Transform checkmarkTransform = itemTransform.Find("Checkmark");
                                    if (checkmarkTransform != null)
                                    {
                                        Image checkmarkImage = checkmarkTransform.GetComponent<Image>();
                                        if (checkmarkImage != null)
                                        {
                                            checkmarkImage.sprite = checkmarkSprite;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Add value changed listeners
                foreach (var action in valueChangedActions)
                {
                    tmpDropdown.onValueChanged.AddListener(action);
                }

                // Apply custom customizations
                foreach (var customization in customizationActions)
                {
                    customization(this);
                }

                // Regular Dropdown component isn't available when using TMP
                component = null;
            }
            else if (component != null)
            {
                // Apply properties
                component.interactable = interactable;
                component.transition = transition;
                component.value = selectedIndex;

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
                        }
                    }
                }

                if (arrowSprite != null)
                {
                    Transform bgTransform = elementObject.transform.Find("Background");
                    if (bgTransform != null)
                    {
                        Transform arrowTransform = bgTransform.Find("Arrow");
                        if (arrowTransform != null)
                        {
                            Image arrowImage = arrowTransform.GetComponent<Image>();
                            if (arrowImage != null)
                            {
                                arrowImage.sprite = arrowSprite;
                            }
                        }
                    }
                }

                if (checkmarkSprite != null)
                {
                    Transform templateTransform = elementObject.transform.Find("Template");
                    if (templateTransform != null)
                    {
                        Transform viewportTransform = templateTransform.Find("Viewport");
                        if (viewportTransform != null)
                        {
                            Transform contentTransform = viewportTransform.Find("Content");
                            if (contentTransform != null && contentTransform.childCount > 0)
                            {
                                Transform itemTransform = contentTransform.GetChild(0);
                                if (itemTransform != null)
                                {
                                    Transform checkmarkTransform = itemTransform.Find("Checkmark");
                                    if (checkmarkTransform != null)
                                    {
                                        Image checkmarkImage = checkmarkTransform.GetComponent<Image>();
                                        if (checkmarkImage != null)
                                        {
                                            checkmarkImage.sprite = checkmarkSprite;
                                        }
                                    }
                                }
                            }
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
            }

            // Register with UIManager
            RegisterWithUIManager();
        }
        catch (Exception ex)
        {
            HandleError($"Failed to add dropdown to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion