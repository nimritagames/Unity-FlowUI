using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Common interface for all UI builders.
/// </summary>
public interface IUIBuilder
{
    Component AddTo(Transform parent);
    GameObject GameObject { get; }
    Transform Transform { get; }
}

/// <summary>
/// Enum for common anchor presets.
/// </summary>
public enum AnchorPreset
{
    TopLeft,
    TopCenter,
    TopRight,
    MiddleLeft,
    MiddleCenter,
    MiddleRight,
    BottomLeft,
    BottomCenter,
    BottomRight,
    StretchHorizontal,
    StretchVertical,
    StretchAll
}

/// <summary>
/// Enum for animation types.
/// </summary>
public enum UIAnimationType
{
    None,
    FadeIn,
    FadeOut,
    SlideFromLeft,
    SlideFromRight,
    SlideFromTop,
    SlideFromBottom,
    ScaleUp,
    ScaleDown,
    Pulse,
    Bounce,
    Rotate
}

#region UI Manager Extension Methods
// UI Manager Extension Methods - Enhanced builder creation with error handling
public static class UIManagerBuilderExtensions
{
    // Method to check if the manager is valid before creating builders
    private static bool ValidateManager(UIManager uiManager, string builderType)
    {
        if (uiManager == null)
        {
            Debug.LogError($"Cannot create {builderType}Builder: UIManager is null");
            return false;
        }
        return true;
    }

    public static ButtonBuilder CreateButton(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "Button"))
            return null;

        return new ButtonBuilder(uiManager, name);
    }

    public static TextBuilder CreateText(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "Text"))
            return null;

        return new TextBuilder(uiManager, name);
    }

    public static ImageBuilder CreateImage(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "Image"))
            return null;

        return new ImageBuilder(uiManager, name);
    }

    public static RawImageBuilder CreateRawImage(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "RawImage"))
            return null;

        return new RawImageBuilder(uiManager, name);
    }

    public static InputFieldBuilder CreateInputField(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "InputField"))
            return null;

        return new InputFieldBuilder(uiManager, name);
    }

    public static ToggleBuilder CreateToggle(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "Toggle"))
            return null;

        return new ToggleBuilder(uiManager, name);
    }

    public static SliderBuilder CreateSlider(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "Slider"))
            return null;

        return new SliderBuilder(uiManager, name);
    }

    public static DropdownBuilder CreateDropdown(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "Dropdown"))
            return null;

        return new DropdownBuilder(uiManager, name);
    }

    public static ScrollRectBuilder CreateScrollRect(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "ScrollRect"))
            return null;

        return new ScrollRectBuilder(uiManager, name);
    }

    public static PanelBuilder CreatePanel(this UIManager uiManager, string name)
    {
        if (!ValidateManager(uiManager, "Panel"))
            return null;

        return new PanelBuilder(uiManager, name);
    }
}

// Helper class for common UI builder operations
public static class UIBuilderHelpers
{
    /// <summary>
    /// Ensures a Canvas exists in the scene, creating one if needed.
    /// </summary>
    /// <returns>Reference to an existing or newly created Canvas</returns>
    public static Canvas EnsureCanvas()
    {
        Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            // Add an event system if one doesn't exist
            if (UnityEngine.Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        return canvas;
    }

    /// <summary>
    /// Positions a UI element relative to the screen dimensions.
    /// </summary>
    /// <param name="builder">The UI builder to position</param>
    /// <param name="screenPosition">Position as percentage of screen (0-1, 0-1)</param>
    /// <returns>The original builder for method chaining</returns>
    public static T PositionRelativeToScreen<T>(this T builder, Vector2 screenPosition) where T : UIElementBuilder<Component, T>
    {
        Canvas canvas = EnsureCanvas();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        if (canvasRect != null && builder.GameObject != null)
        {
            RectTransform rectTransform = builder.GameObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                Vector2 position = new Vector2(
                    canvasRect.rect.width * screenPosition.x - canvasRect.rect.width * 0.5f,
                    canvasRect.rect.height * screenPosition.y - canvasRect.rect.height * 0.5f
                );

                rectTransform.anchoredPosition = position;
            }
        }

        return builder;
    }

    /// <summary>
    /// Applies a predefined style to a UI element for consistent appearance.
    /// </summary>
    /// <param name="builder">The UI builder to style</param>
    /// <param name="styleName">The name of the predefined style to apply</param>
    /// <returns>The original builder for method chaining</returns>
    public static T ApplyStyle<T>(this T builder, string styleName) where T : UIElementBuilder<Component, T>
    {
        switch (styleName.ToLowerInvariant())
        {
            case "primary":
                if (builder is ButtonBuilder buttonBuilder)
                {
                    return (T)(object)buttonBuilder
                        .WithColors(
                            new Color(0.2f, 0.4f, 0.8f), // normal
                            new Color(0.3f, 0.5f, 0.9f), // highlighted
                            new Color(0.1f, 0.3f, 0.7f), // pressed
                            new Color(0.5f, 0.5f, 0.5f)) // disabled
                        .WithTextColor(Color.white);
                }
                else if (builder is ImageBuilder imageBuilder)
                {
                    return (T)(object)imageBuilder.WithColor(new Color(0.2f, 0.4f, 0.8f));
                }
                break;

            case "secondary":
                if (builder is ButtonBuilder secondaryButtonBuilder)
                {
                    return (T)(object)secondaryButtonBuilder
                        .WithColors(
                            new Color(0.5f, 0.5f, 0.5f), // normal
                            new Color(0.6f, 0.6f, 0.6f), // highlighted
                            new Color(0.4f, 0.4f, 0.4f), // pressed
                            new Color(0.3f, 0.3f, 0.3f)) // disabled
                        .WithTextColor(Color.white);
                }
                break;

            case "warning":
                if (builder is ButtonBuilder warningButtonBuilder)
                {
                    return (T)(object)warningButtonBuilder
                        .WithColors(
                            new Color(0.9f, 0.6f, 0.1f), // normal
                            new Color(1.0f, 0.7f, 0.2f), // highlighted
                            new Color(0.8f, 0.5f, 0.0f), // pressed
                            new Color(0.5f, 0.5f, 0.5f)) // disabled
                        .WithTextColor(Color.white);
                }
                break;

            case "danger":
                if (builder is ButtonBuilder dangerButtonBuilder)
                {
                    return (T)(object)dangerButtonBuilder
                        .WithColors(
                            new Color(0.8f, 0.2f, 0.2f), // normal
                            new Color(0.9f, 0.3f, 0.3f), // highlighted
                            new Color(0.7f, 0.1f, 0.1f), // pressed
                            new Color(0.5f, 0.5f, 0.5f)) // disabled
                        .WithTextColor(Color.white);
                }
                break;

            case "success":
                if (builder is ButtonBuilder successButtonBuilder)
                {
                    return (T)(object)successButtonBuilder
                        .WithColors(
                            new Color(0.2f, 0.7f, 0.2f), // normal
                            new Color(0.3f, 0.8f, 0.3f), // highlighted
                            new Color(0.1f, 0.6f, 0.1f), // pressed
                            new Color(0.5f, 0.5f, 0.5f)) // disabled
                        .WithTextColor(Color.white);
                }
                break;
        }

        return builder;
    }

    /// <summary>
    /// Creates a group of related UI elements with consistent styling and spacing.
    /// </summary>
    /// <param name="parent">The parent transform to add the group to</param>
    /// <param name="groupName">Name for the group GameObject</param>
    /// <param name="spacing">Spacing between elements in the group</param>
    /// <param name="isVertical">Whether to arrange elements vertically (true) or horizontally (false)</param>
    /// <returns>The parent transform of the group for adding elements</returns>
    public static Transform CreateUIGroup(Transform parent, string groupName, float spacing = 10f, bool isVertical = true)
    {
        GameObject groupObject = new GameObject(groupName, typeof(RectTransform));
        groupObject.transform.SetParent(parent, false);

        RectTransform rectTransform = groupObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;

        if (isVertical)
        {
            VerticalLayoutGroup layout = groupObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }
        else
        {
            HorizontalLayoutGroup layout = groupObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        ContentSizeFitter sizeFitter = groupObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = isVertical ? ContentSizeFitter.FitMode.Unconstrained : ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = isVertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;

        return groupObject.transform;
    }

    /// <summary>
    /// Scales a UI element to fit a percentage of the screen.
    /// </summary>
    /// <param name="builder">The UI builder to scale</param>
    /// <param name="widthPercent">Width as percentage of screen width (0-100)</param>
    /// <param name="heightPercent">Height as percentage of screen height (0-100)</param>
    /// <returns>The original builder for method chaining</returns>
    public static T ScaleToScreenPercent<T>(this T builder, float widthPercent, float heightPercent) where T : UIElementBuilder<Component, T>
    {
        Canvas canvas = EnsureCanvas();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        if (canvasRect != null && builder.GameObject != null)
        {
            RectTransform rectTransform = builder.GameObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                float width = canvasRect.rect.width * (widthPercent / 100f);
                float height = canvasRect.rect.height * (heightPercent / 100f);

                rectTransform.sizeDelta = new Vector2(width, height);
            }
        }

        return builder;
    }
}
#endregion