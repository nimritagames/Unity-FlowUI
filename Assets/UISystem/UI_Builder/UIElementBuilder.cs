using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// Base UIElementBuilder class using self-referential generics for fluent interfaces.
/// </summary>
public abstract class UIElementBuilder<TComponent, TBuilder> : IUIBuilder
    where TComponent : Component
    where TBuilder : UIElementBuilder<TComponent, TBuilder>
{
    protected UIManager uiManager;
    protected GameObject elementObject;
    protected RectTransform rectTransform;
    protected TComponent component;
    protected string elementName;
    protected bool hasFailed = false;
    protected string errorMessage = string.Empty;
    protected List<Action<TBuilder>> customizationActions = new List<Action<TBuilder>>();

    // Public access to GameObject and Transform
    public GameObject GameObject => elementObject;
    public Transform Transform => elementObject?.transform;

    // Error handling properties
    public bool HasFailed => hasFailed;
    public string ErrorMessage => errorMessage;

    protected UIElementBuilder(UIManager uiManager, string name)
    {
        this.uiManager = uiManager;
        this.elementName = name;
    }

    // Error handling method
    protected TBuilder HandleError(string message)
    {
        hasFailed = true;
        errorMessage = message;
        Debug.LogError($"UI Builder Error: {message}");
        return (TBuilder)this;
    }

    // Safe execution wrapper
    protected TBuilder SafeExecute(Action action, string errorMessage)
    {
        if (hasFailed) return (TBuilder)this;

        try
        {
            action();
        }
        catch (Exception ex)
        {
            return HandleError($"{errorMessage}: {ex.Message}");
        }

        return (TBuilder)this;
    }

    // Enhanced size method with validation
    public TBuilder WithSize(float width, float height)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform == null)
                throw new InvalidOperationException("RectTransform is null");

            if (width < 0 || height < 0)
                Debug.LogWarning($"UI Builder Warning: Negative size ({width}, {height}) specified for {elementName}");

            rectTransform.sizeDelta = new Vector2(width, height);
        }, $"Failed to set size for {elementName}");
    }

    // Relative sizing method
    public TBuilder WithRelativeSize(float widthPercent, float heightPercent)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform == null)
                throw new InvalidOperationException("RectTransform is null");

            if (rectTransform.parent == null)
                throw new InvalidOperationException("Cannot set relative size without a parent");

            RectTransform parentRect = rectTransform.parent as RectTransform;
            if (parentRect == null)
                throw new InvalidOperationException("Parent is not a RectTransform");

            float parentWidth = parentRect.rect.width;
            float parentHeight = parentRect.rect.height;

            rectTransform.sizeDelta = new Vector2(
                parentWidth * (widthPercent / 100f),
                parentHeight * (heightPercent / 100f)
            );
        }, $"Failed to set relative size for {elementName}");
    }

    public TBuilder WithPosition(Vector2 position)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform != null)
                rectTransform.anchoredPosition = position;
        }, $"Failed to set position for {elementName}");
    }

    public TBuilder WithAnchors(Vector2 min, Vector2 max)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform != null)
            {
                rectTransform.anchorMin = min;
                rectTransform.anchorMax = max;
            }
        }, $"Failed to set anchors for {elementName}");
    }

    public TBuilder WithPivot(Vector2 pivot)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform != null)
                rectTransform.pivot = pivot;
        }, $"Failed to set pivot for {elementName}");
    }

    // Method for common anchor presets
    public TBuilder WithAnchorPreset(AnchorPreset preset, bool setPivot = true)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform == null)
                throw new InvalidOperationException("RectTransform is null");

            Vector2 minAnchor = Vector2.zero;
            Vector2 maxAnchor = Vector2.zero;
            Vector2 pivot = Vector2.zero;

            switch (preset)
            {
                case AnchorPreset.TopLeft:
                    minAnchor = new Vector2(0, 1);
                    maxAnchor = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    break;
                case AnchorPreset.TopCenter:
                    minAnchor = new Vector2(0.5f, 1);
                    maxAnchor = new Vector2(0.5f, 1);
                    pivot = new Vector2(0.5f, 1);
                    break;
                case AnchorPreset.TopRight:
                    minAnchor = new Vector2(1, 1);
                    maxAnchor = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    break;
                case AnchorPreset.MiddleLeft:
                    minAnchor = new Vector2(0, 0.5f);
                    maxAnchor = new Vector2(0, 0.5f);
                    pivot = new Vector2(0, 0.5f);
                    break;
                case AnchorPreset.MiddleCenter:
                    minAnchor = new Vector2(0.5f, 0.5f);
                    maxAnchor = new Vector2(0.5f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.MiddleRight:
                    minAnchor = new Vector2(1, 0.5f);
                    maxAnchor = new Vector2(1, 0.5f);
                    pivot = new Vector2(1, 0.5f);
                    break;
                case AnchorPreset.BottomLeft:
                    minAnchor = new Vector2(0, 0);
                    maxAnchor = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    break;
                case AnchorPreset.BottomCenter:
                    minAnchor = new Vector2(0.5f, 0);
                    maxAnchor = new Vector2(0.5f, 0);
                    pivot = new Vector2(0.5f, 0);
                    break;
                case AnchorPreset.BottomRight:
                    minAnchor = new Vector2(1, 0);
                    maxAnchor = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    break;
                case AnchorPreset.StretchHorizontal:
                    minAnchor = new Vector2(0, 0.5f);
                    maxAnchor = new Vector2(1, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.StretchVertical:
                    minAnchor = new Vector2(0.5f, 0);
                    maxAnchor = new Vector2(0.5f, 1);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case AnchorPreset.StretchAll:
                    minAnchor = Vector2.zero;
                    maxAnchor = Vector2.one;
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
            }

            rectTransform.anchorMin = minAnchor;
            rectTransform.anchorMax = maxAnchor;

            if (setPivot)
                rectTransform.pivot = pivot;
        }, $"Failed to set anchor preset for {elementName}");
    }

    public TBuilder WithStretch(float left = 0, float right = 0, float top = 0, float bottom = 0)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform != null)
            {
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = new Vector2(left, bottom);
                rectTransform.offsetMax = new Vector2(-right, -top);
            }
        }, $"Failed to set stretch for {elementName}");
    }

    public TBuilder WithRotation(float zRotation)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform != null)
                rectTransform.localRotation = Quaternion.Euler(0, 0, zRotation);
        }, $"Failed to set rotation for {elementName}");
    }

    public TBuilder WithScale(Vector3 scale)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (rectTransform != null)
                rectTransform.localScale = scale;
        }, $"Failed to set scale for {elementName}");
    }

    public TBuilder WithName(string name)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                elementObject.name = name;
                elementName = name;
            }
        }, $"Failed to set name for {elementName}");
    }

    public TBuilder WithColor(Color color)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var graphic = elementObject.GetComponent<Graphic>();
                if (graphic != null)
                    graphic.color = color;
            }
        }, $"Failed to set color for {elementName}");
    }

    public TBuilder WithAlpha(float alpha)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var graphic = elementObject.GetComponent<Graphic>();
                if (graphic != null)
                {
                    Color color = graphic.color;
                    color.a = alpha;
                    graphic.color = color;
                }
            }
        }, $"Failed to set alpha for {elementName}");
    }

    public TBuilder WithCanvasGroup(float alpha = 1f, bool interactable = true, bool blocksRaycasts = true)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var canvasGroup = elementObject.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = elementObject.AddComponent<CanvasGroup>();

                canvasGroup.alpha = alpha;
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = blocksRaycasts;
            }
        }, $"Failed to set canvas group for {elementName}");
    }

    public TBuilder WithLayoutElement(float? minWidth = null, float? minHeight = null,
                                      float? preferredWidth = null, float? preferredHeight = null,
                                      float? flexibleWidth = null, float? flexibleHeight = null,
                                      int? layoutPriority = null)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var layoutElement = elementObject.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = elementObject.AddComponent<LayoutElement>();

                if (minWidth.HasValue) layoutElement.minWidth = minWidth.Value;
                if (minHeight.HasValue) layoutElement.minHeight = minHeight.Value;
                if (preferredWidth.HasValue) layoutElement.preferredWidth = preferredWidth.Value;
                if (preferredHeight.HasValue) layoutElement.preferredHeight = preferredHeight.Value;
                if (flexibleWidth.HasValue) layoutElement.flexibleWidth = flexibleWidth.Value;
                if (flexibleHeight.HasValue) layoutElement.flexibleHeight = flexibleHeight.Value;
                if (layoutPriority.HasValue) layoutElement.layoutPriority = layoutPriority.Value;
            }
        }, $"Failed to set layout element for {elementName}");
    }

    public TBuilder WithContentSizeFitter(ContentSizeFitter.FitMode horizontalFit = ContentSizeFitter.FitMode.Unconstrained,
                                        ContentSizeFitter.FitMode verticalFit = ContentSizeFitter.FitMode.Unconstrained)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var sizeFitter = elementObject.GetComponent<ContentSizeFitter>();
                if (sizeFitter == null)
                    sizeFitter = elementObject.AddComponent<ContentSizeFitter>();

                sizeFitter.horizontalFit = horizontalFit;
                sizeFitter.verticalFit = verticalFit;
            }
        }, $"Failed to set content size fitter for {elementName}");
    }

    public TBuilder WithHorizontalLayoutGroup(float spacing = 0, int padding = 0,
                                           TextAnchor childAlignment = TextAnchor.UpperLeft,
                                           bool childControlWidth = true, bool childControlHeight = true,
                                           bool childScaleWidth = false, bool childScaleHeight = false,
                                           bool childForceExpandWidth = true, bool childForceExpandHeight = true)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var layoutGroup = elementObject.GetComponent<HorizontalLayoutGroup>();
                if (layoutGroup == null)
                    layoutGroup = elementObject.AddComponent<HorizontalLayoutGroup>();

                layoutGroup.spacing = spacing;
                layoutGroup.padding = new RectOffset(padding, padding, padding, padding);
                layoutGroup.childAlignment = childAlignment;
                layoutGroup.childControlWidth = childControlWidth;
                layoutGroup.childControlHeight = childControlHeight;
                layoutGroup.childScaleWidth = childScaleWidth;
                layoutGroup.childScaleHeight = childScaleHeight;
                layoutGroup.childForceExpandWidth = childForceExpandWidth;
                layoutGroup.childForceExpandHeight = childForceExpandHeight;
            }
        }, $"Failed to set horizontal layout group for {elementName}");
    }

    public TBuilder WithVerticalLayoutGroup(float spacing = 0, int padding = 0,
                                         TextAnchor childAlignment = TextAnchor.UpperLeft,
                                         bool childControlWidth = true, bool childControlHeight = true,
                                         bool childScaleWidth = false, bool childScaleHeight = false,
                                         bool childForceExpandWidth = true, bool childForceExpandHeight = true)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var layoutGroup = elementObject.GetComponent<VerticalLayoutGroup>();
                if (layoutGroup == null)
                    layoutGroup = elementObject.AddComponent<VerticalLayoutGroup>();

                layoutGroup.spacing = spacing;
                layoutGroup.padding = new RectOffset(padding, padding, padding, padding);
                layoutGroup.childAlignment = childAlignment;
                layoutGroup.childControlWidth = childControlWidth;
                layoutGroup.childControlHeight = childControlHeight;
                layoutGroup.childScaleWidth = childScaleWidth;
                layoutGroup.childScaleHeight = childScaleHeight;
                layoutGroup.childForceExpandWidth = childForceExpandWidth;
                layoutGroup.childForceExpandHeight = childForceExpandHeight;
            }
        }, $"Failed to set vertical layout group for {elementName}");
    }

    public TBuilder WithGridLayoutGroup(Vector2 cellSize, Vector2 spacing,
                                      GridLayoutGroup.Corner startCorner = GridLayoutGroup.Corner.UpperLeft,
                                      GridLayoutGroup.Axis startAxis = GridLayoutGroup.Axis.Horizontal,
                                      TextAnchor childAlignment = TextAnchor.UpperLeft,
                                      GridLayoutGroup.Constraint constraint = GridLayoutGroup.Constraint.Flexible,
                                      int constraintCount = 2)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var layoutGroup = elementObject.GetComponent<GridLayoutGroup>();
                if (layoutGroup == null)
                    layoutGroup = elementObject.AddComponent<GridLayoutGroup>();

                layoutGroup.cellSize = cellSize;
                layoutGroup.spacing = spacing;
                layoutGroup.startCorner = startCorner;
                layoutGroup.startAxis = startAxis;
                layoutGroup.childAlignment = childAlignment;
                layoutGroup.constraint = constraint;
                layoutGroup.constraintCount = constraintCount;
            }
        }, $"Failed to set grid layout group for {elementName}");
    }

    public TBuilder WithAnimation(UIAnimationType animationType, float duration = 0.3f, bool playOnStart = true, bool destroyOnComplete = false)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var animator = elementObject.GetComponent<UIAnimationController>();
                if (animator == null)
                    animator = elementObject.AddComponent<UIAnimationController>();

                animator.animationType = animationType;
                animator.duration = duration;
                animator.playOnStart = playOnStart;
                animator.destroyOnComplete = destroyOnComplete;
            }
        }, $"Failed to add animation to {elementName}");
    }

    public TBuilder WithNavigation(Navigation.Mode mode = Navigation.Mode.Automatic,
                                Selectable selectOnUp = null,
                                Selectable selectOnDown = null,
                                Selectable selectOnLeft = null,
                                Selectable selectOnRight = null)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var selectable = elementObject.GetComponent<Selectable>();
                if (selectable != null)
                {
                    var navigation = selectable.navigation;
                    navigation.mode = mode;

                    if (mode == Navigation.Mode.Explicit)
                    {
                        navigation.selectOnUp = selectOnUp;
                        navigation.selectOnDown = selectOnDown;
                        navigation.selectOnLeft = selectOnLeft;
                        navigation.selectOnRight = selectOnRight;
                    }

                    selectable.navigation = navigation;
                }
            }
        }, $"Failed to set navigation for {elementName}");
    }

    public TBuilder WithMask(bool showMaskGraphic = true)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (elementObject != null)
            {
                var mask = elementObject.GetComponent<Mask>();
                if (mask == null)
                    mask = elementObject.AddComponent<Mask>();

                mask.showMaskGraphic = showMaskGraphic;
            }
        }, $"Failed to add mask to {elementName}");
    }

    public TBuilder WithCustomization(Action<TBuilder> customization)
    {
        if (hasFailed) return (TBuilder)this;

        return SafeExecute(() => {
            if (customization != null)
                customizationActions.Add(customization);
        }, $"Failed to add customization for {elementName}");
    }

    public abstract TComponent AddTo(Transform parent);

    // Explicit interface implementation
    Component IUIBuilder.AddTo(Transform parent)
    {
        return AddTo(parent);
    }

    // Helper method to register the UI element with the UIManager
    protected void RegisterWithUIManager()
    {
        if (elementObject != null && uiManager != null)
        {
            try
            {
                uiManager.AddUIReference(elementObject);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to register {elementName} with UIManager: {ex.Message}");
            }
        }
    }

    // Helper method to get the default font with improved fallbacks
    protected Font GetDefaultFont()
    {
        Font defaultFont = null;

        // Prioritized approaches to get a default font
        try
        {
            // First try: Resources.GetBuiltinResource
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont != null) return defaultFont;

            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont != null) return defaultFont;

            // Second try: Find common fonts by name
            string[] commonFontNames = { "Arial", "Helvetica", "Verdana", "Tahoma", "Roboto", "Open Sans", "LegacyRuntime" };
            foreach (string fontName in commonFontNames)
            {
                try
                {
                    defaultFont = Font.CreateDynamicFontFromOSFont(fontName, 14);
                    if (defaultFont != null) return defaultFont;
                }
                catch
                {
                    // Ignore and try next font
                }
            }

            // Third try: Find any font in the scene
            defaultFont = Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
            if (defaultFont != null) return defaultFont;

            // Last resort: Create a dynamic font
            defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"UIBuilder: Could not find or create a default font. {ex.Message}");
        }

        return defaultFont;
    }

    // Helper method to load sprite from resources
    protected Sprite LoadSpriteFromResources(string path)
    {
        try
        {
            if (string.IsNullOrEmpty(path))
                return null;

            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
                Debug.LogWarning($"UIBuilder: Could not load sprite from path '{path}'");

            return sprite;
        }
        catch (Exception ex)
        {
            Debug.LogError($"UIBuilder: Error loading sprite: {ex.Message}");
            return null;
        }
    }
}