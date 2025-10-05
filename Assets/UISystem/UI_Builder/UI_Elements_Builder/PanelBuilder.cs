using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

#region Panel Builder Implementation
public class PanelBuilder : UIElementBuilder<Image, PanelBuilder>
{
    // Use simple IUIBuilder interface for children collection
    private List<IUIBuilder> children = new List<IUIBuilder>();
    private Sprite backgroundSprite;
    private bool fillCenter = true;
    private Image.Type imageType = Image.Type.Sliced;
    private bool useOutline = false;
    private float outlineThickness = 1f;
    private Color outlineColor = Color.black;
    private bool roundedCorners = false;
    private bool useGradient = false;
    private Color gradientTopColor = Color.white;
    private Color gradientBottomColor = new Color(0.9f, 0.9f, 0.9f);
    private bool autoLayout = false;
    private bool isVerticalLayout = true;
    private float layoutSpacing = 10f;
    private int layoutPadding = 10;
    private TextAnchor layoutAlignment = TextAnchor.UpperLeft;

    public PanelBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            rectTransform = elementObject.GetComponent<RectTransform>();
            component = elementObject.GetComponent<Image>();

            rectTransform.sizeDelta = new Vector2(300, 200);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize PanelBuilder: {ex.Message}");
        }
    }

    public PanelBuilder WithBackgroundSprite(Sprite sprite, Image.Type imageType = Image.Type.Sliced)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            backgroundSprite = sprite;
            this.imageType = imageType;
        }, $"Failed to set background sprite for {elementName}");
    }

    public PanelBuilder WithBackgroundSprite(string resourcePath, Image.Type imageType = Image.Type.Sliced)
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

    public PanelBuilder WithFillCenter(bool fillCenter)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.fillCenter = fillCenter;
        }, $"Failed to set fill center for {elementName}");
    }

    public PanelBuilder WithOutline(bool useOutline, float thickness = 1f, Color color = default)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.useOutline = useOutline;
            this.outlineThickness = thickness;

            if (color != default)
            {
                this.outlineColor = color;
            }
        }, $"Failed to set outline for {elementName}");
    }

    public PanelBuilder WithRoundedCorners(bool rounded)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.roundedCorners = rounded;
        }, $"Failed to set rounded corners for {elementName}");
    }

    public PanelBuilder WithGradient(bool useGradient, Color topColor = default, Color bottomColor = default)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.useGradient = useGradient;

            if (topColor != default)
            {
                this.gradientTopColor = topColor;
            }

            if (bottomColor != default)
            {
                this.gradientBottomColor = bottomColor;
            }
        }, $"Failed to set gradient for {elementName}");
    }

    public PanelBuilder WithAutoLayout(bool isVertical, float spacing = 10f, int padding = 10, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.autoLayout = true;
            this.isVerticalLayout = isVertical;
            this.layoutSpacing = spacing;
            this.layoutPadding = padding;
            this.layoutAlignment = alignment;
        }, $"Failed to set auto layout for {elementName}");
    }

    // Simple method to add any builder as a child
    public PanelBuilder AddChild(IUIBuilder childBuilder)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (childBuilder != null)
            {
                children.Add(childBuilder);
            }
        }, $"Failed to add child for {elementName}");
    }

    public override Image AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            // Apply image properties
            if (backgroundSprite != null)
            {
                component.sprite = backgroundSprite;
                component.type = imageType;
            }
            component.fillCenter = fillCenter;

            // Apply auto layout if requested
            if (autoLayout)
            {
                if (isVerticalLayout)
                {
                    var layoutGroup = elementObject.GetComponent<VerticalLayoutGroup>();
                    if (layoutGroup == null)
                    {
                        layoutGroup = elementObject.AddComponent<VerticalLayoutGroup>();
                    }

                    layoutGroup.spacing = layoutSpacing;
                    layoutGroup.padding = new RectOffset(layoutPadding, layoutPadding, layoutPadding, layoutPadding);
                    layoutGroup.childAlignment = layoutAlignment;
                    layoutGroup.childControlWidth = true;
                    layoutGroup.childControlHeight = false;
                    layoutGroup.childForceExpandWidth = true;
                    layoutGroup.childForceExpandHeight = false;
                }
                else
                {
                    var layoutGroup = elementObject.GetComponent<HorizontalLayoutGroup>();
                    if (layoutGroup == null)
                    {
                        layoutGroup = elementObject.AddComponent<HorizontalLayoutGroup>();
                    }

                    layoutGroup.spacing = layoutSpacing;
                    layoutGroup.padding = new RectOffset(layoutPadding, layoutPadding, layoutPadding, layoutPadding);
                    layoutGroup.childAlignment = layoutAlignment;
                    layoutGroup.childControlWidth = false;
                    layoutGroup.childControlHeight = true;
                    layoutGroup.childForceExpandWidth = false;
                    layoutGroup.childForceExpandHeight = true;
                }

                // Add content size fitter for auto-sizing
                var sizeFitter = elementObject.GetComponent<ContentSizeFitter>();
                if (sizeFitter == null)
                {
                    sizeFitter = elementObject.AddComponent<ContentSizeFitter>();
                }

                if (isVerticalLayout)
                {
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                    sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                }
                else
                {
                    sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                    sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
                }
            }

            // Add outline if requested
            if (useOutline)
            {
                var outline = elementObject.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = elementObject.AddComponent<Outline>();
                }

                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(outlineThickness, outlineThickness);
            }

            // Add rounded corners if requested
            if (roundedCorners)
            {
                // Note: This would require a custom shader or mask to implement properly
                Debug.Log("Rounded corners requested - this would require a custom shader implementation");
            }

            // Add gradient if requested
            if (useGradient)
            {
                // Note: This would require a custom shader to implement properly
                Debug.Log("Gradient requested - this would require a custom shader implementation");
            }

            // Add all children
            foreach (var child in children)
            {
                child.AddTo(elementObject.transform);
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
            HandleError($"Failed to add panel to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion