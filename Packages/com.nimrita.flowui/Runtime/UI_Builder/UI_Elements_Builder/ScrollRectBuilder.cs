using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

#region ScrollRect Builder Implementation
public class ScrollRectBuilder : UIElementBuilder<ScrollRect, ScrollRectBuilder>
{
    private bool horizontal = true;
    private bool vertical = true;
    private float elasticity = 0.1f;
    private bool inertia = true;
    private float decelerationRate = 0.135f;
    private float scrollSensitivity = 1.0f;
    private RectTransform viewport;
    private RectTransform content;
    private Scrollbar horizontalScrollbar;
    private Scrollbar verticalScrollbar;
    private ScrollRect.ScrollbarVisibility horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    private ScrollRect.ScrollbarVisibility verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    private float horizontalScrollbarSpacing = 0;
    private float verticalScrollbarSpacing = 0;
    private ScrollRect.MovementType movementType = ScrollRect.MovementType.Elastic;
    private List<RectTransform> contentItems = new List<RectTransform>();
    private bool createDefaultViewport = true;
    private bool createDefaultContent = true;
    private bool createHorizontalScrollbar = false;
    private bool createVerticalScrollbar = true;
    private RectOffset viewportPadding = new RectOffset(0, 0, 0, 0);
    private Color viewportColor = new Color(1, 1, 1, 0);
    private Color contentBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.1f);
    private Color scrollbarBackgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.3f);
    private Color scrollbarHandleColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    private bool maskViewport = true;

    public ScrollRectBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            rectTransform = elementObject.GetComponent<RectTransform>();
            component = elementObject.GetComponent<ScrollRect>();

            rectTransform.sizeDelta = new Vector2(300, 200);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize ScrollRectBuilder: {ex.Message}");
        }
    }

    private void SetupScrollRectStructure()
    {
        if (createDefaultViewport)
        {
            // Create viewport
            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
            viewportObj.transform.SetParent(elementObject.transform, false);

            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;

            // Apply viewport padding
            if (vertical && createVerticalScrollbar)
            {
                viewportRect.offsetMax = new Vector2(-16 - verticalScrollbarSpacing, 0);
            }
            else
            {
                viewportRect.offsetMax = new Vector2(0, 0);
            }

            if (horizontal && createHorizontalScrollbar)
            {
                viewportRect.offsetMin = new Vector2(0, 16 + horizontalScrollbarSpacing);
            }
            else
            {
                viewportRect.offsetMin = new Vector2(0, 0);
            }

            // Apply additional padding
            viewportRect.offsetMin = new Vector2(
                viewportRect.offsetMin.x + viewportPadding.left,
                viewportRect.offsetMin.y + viewportPadding.bottom
            );

            viewportRect.offsetMax = new Vector2(
                viewportRect.offsetMax.x - viewportPadding.right,
                viewportRect.offsetMax.y - viewportPadding.top
            );

            // Set viewport image properties
            Image viewportImage = viewportObj.GetComponent<Image>();
            viewportImage.color = viewportColor;

            // Add mask if requested
            if (maskViewport)
            {
                Mask mask = viewportObj.AddComponent<Mask>();
                mask.showMaskGraphic = false;
            }

            viewport = viewportRect;
        }

        if (createDefaultContent)
        {
            // Create content
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(Image));
            contentObj.transform.SetParent(viewport != null ? viewport : elementObject.transform, false);

            RectTransform contentRect = contentObj.GetComponent<RectTransform>();

            // Set different anchors based on scroll direction
            if (vertical && !horizontal)
            {
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);
            }
            else if (horizontal && !vertical)
            {
                contentRect.anchorMin = new Vector2(0, 0);
                contentRect.anchorMax = new Vector2(0, 1);
                contentRect.pivot = new Vector2(0, 0.5f);
            }
            else
            {
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(0, 1);
                contentRect.pivot = new Vector2(0, 1);
            }

            contentRect.anchoredPosition = Vector2.zero;

            // Set initial size based on scroll direction
            if (vertical && !horizontal)
            {
                // Vertical scrolling - make it tall
                contentRect.sizeDelta = new Vector2(0, 400);
            }
            else if (horizontal && !vertical)
            {
                // Horizontal scrolling - make it wide
                contentRect.sizeDelta = new Vector2(500, 0);
            }
            else
            {
                // Both directions - make it bigger in both
                contentRect.sizeDelta = new Vector2(500, 400);
            }

            // Set content image properties
            Image contentImage = contentObj.GetComponent<Image>();
            contentImage.color = contentBackgroundColor;

            content = contentRect;
        }

        if (createHorizontalScrollbar)
        {
            // Create horizontal scrollbar
            GameObject hScrollbarObj = new GameObject("Horizontal Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
            hScrollbarObj.transform.SetParent(elementObject.transform, false);

            RectTransform hScrollbarRect = hScrollbarObj.GetComponent<RectTransform>();
            hScrollbarRect.anchorMin = new Vector2(0, 0);
            hScrollbarRect.anchorMax = new Vector2(1, 0);
            hScrollbarRect.pivot = new Vector2(0, 0);
            hScrollbarRect.sizeDelta = new Vector2(-(vertical ? 16 : 0), 16);
            hScrollbarRect.anchoredPosition = Vector2.zero;

            // Set scrollbar properties
            Scrollbar hScrollbar = hScrollbarObj.GetComponent<Scrollbar>();
            hScrollbar.direction = Scrollbar.Direction.LeftToRight;

            // Set background image
            Image hScrollbarImage = hScrollbarObj.GetComponent<Image>();
            hScrollbarImage.color = scrollbarBackgroundColor;

            // Create scrollbar parts
            GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(hScrollbarObj.transform, false);

            RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(10, 10);
            slidingAreaRect.offsetMax = new Vector2(-10, -10);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;

            // Set handle image
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = scrollbarHandleColor;

            // Connect scrollbar parts
            hScrollbar.handleRect = handleRect;
            hScrollbar.targetGraphic = handleImage;

            horizontalScrollbar = hScrollbar;
        }

        if (createVerticalScrollbar)
        {
            // Create vertical scrollbar
            GameObject vScrollbarObj = new GameObject("Vertical Scrollbar", typeof(RectTransform), typeof(Scrollbar), typeof(Image));
            vScrollbarObj.transform.SetParent(elementObject.transform, false);

            RectTransform vScrollbarRect = vScrollbarObj.GetComponent<RectTransform>();
            vScrollbarRect.anchorMin = new Vector2(1, 0);
            vScrollbarRect.anchorMax = new Vector2(1, 1);
            vScrollbarRect.pivot = new Vector2(1, 1);
            vScrollbarRect.sizeDelta = new Vector2(16, -(horizontal ? 16 : 0));
            vScrollbarRect.anchoredPosition = Vector2.zero;

            // Set scrollbar properties
            Scrollbar vScrollbar = vScrollbarObj.GetComponent<Scrollbar>();
            vScrollbar.direction = Scrollbar.Direction.BottomToTop;

            // Set background image
            Image vScrollbarImage = vScrollbarObj.GetComponent<Image>();
            vScrollbarImage.color = scrollbarBackgroundColor;

            // Create scrollbar parts
            GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(vScrollbarObj.transform, false);

            RectTransform slidingAreaRect = slidingArea.GetComponent<RectTransform>();
            slidingAreaRect.anchorMin = Vector2.zero;
            slidingAreaRect.anchorMax = Vector2.one;
            slidingAreaRect.offsetMin = new Vector2(10, 10);
            slidingAreaRect.offsetMax = new Vector2(-10, -10);

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(slidingArea.transform, false);

            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.sizeDelta = Vector2.zero;

            // Set handle image
            Image handleImage = handle.GetComponent<Image>();
            handleImage.color = scrollbarHandleColor;

            // Connect scrollbar parts
            vScrollbar.handleRect = handleRect;
            vScrollbar.targetGraphic = handleImage;

            verticalScrollbar = vScrollbar;
        }

        // Add content items if any
        if (content != null && contentItems.Count > 0)
        {
            foreach (var item in contentItems)
            {
                if (item != null)
                {
                    item.SetParent(content, false);
                }
            }
        }
    }

    public ScrollRectBuilder WithDirection(bool horizontal, bool vertical)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.horizontal = horizontal;
            this.vertical = vertical;
        }, $"Failed to set direction for {elementName}");
    }

    public ScrollRectBuilder WithMovementType(ScrollRect.MovementType movementType, float elasticity = 0.1f)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.movementType = movementType;
            this.elasticity = elasticity;
        }, $"Failed to set movement type for {elementName}");
    }

    public ScrollRectBuilder WithScrollSettings(bool inertia, float decelerationRate = 0.135f, float sensitivity = 1.0f)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.inertia = inertia;
            this.decelerationRate = decelerationRate;
            this.scrollSensitivity = sensitivity;
        }, $"Failed to set scroll settings for {elementName}");
    }

    public ScrollRectBuilder WithScrollbarSettings(bool showHorizontal, bool showVertical,
                                                ScrollRect.ScrollbarVisibility horizontalVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport,
                                                ScrollRect.ScrollbarVisibility verticalVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport,
                                                float horizontalSpacing = 0, float verticalSpacing = 0)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.createHorizontalScrollbar = showHorizontal;
            this.createVerticalScrollbar = showVertical;
            this.horizontalScrollbarVisibility = horizontalVisibility;
            this.verticalScrollbarVisibility = verticalVisibility;
            this.horizontalScrollbarSpacing = horizontalSpacing;
            this.verticalScrollbarSpacing = verticalSpacing;
        }, $"Failed to set scrollbar settings for {elementName}");
    }

    public ScrollRectBuilder WithViewport(RectTransform viewport, bool maskViewport = true)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.viewport = viewport;
            this.createDefaultViewport = false;
            this.maskViewport = maskViewport;
        }, $"Failed to set viewport for {elementName}");
    }

    public ScrollRectBuilder WithViewportPadding(int left, int right, int top, int bottom)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.viewportPadding = new RectOffset(left, right, top, bottom);
        }, $"Failed to set viewport padding for {elementName}");
    }

    public ScrollRectBuilder WithContent(RectTransform content)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.content = content;
            this.createDefaultContent = false;
        }, $"Failed to set content for {elementName}");
    }

    public ScrollRectBuilder AddContentItem(RectTransform item)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (item != null)
            {
                contentItems.Add(item);
            }
        }, $"Failed to add content item for {elementName}");
    }

    public ScrollRectBuilder WithColors(Color viewportColor, Color contentBackgroundColor,
                                      Color scrollbarBackgroundColor, Color scrollbarHandleColor)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.viewportColor = viewportColor;
            this.contentBackgroundColor = contentBackgroundColor;
            this.scrollbarBackgroundColor = scrollbarBackgroundColor;
            this.scrollbarHandleColor = scrollbarHandleColor;
        }, $"Failed to set colors for {elementName}");
    }

    public override ScrollRect AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            // Setup structure
            SetupScrollRectStructure();

            // Configure ScrollRect component
            component.horizontal = horizontal;
            component.vertical = vertical;
            component.movementType = movementType;
            component.elasticity = elasticity;
            component.inertia = inertia;
            component.decelerationRate = decelerationRate;
            component.scrollSensitivity = scrollSensitivity;

            // Assign references
            component.viewport = viewport;
            component.content = content;
            component.horizontalScrollbar = horizontalScrollbar;
            component.verticalScrollbar = verticalScrollbar;
            component.horizontalScrollbarVisibility = horizontalScrollbarVisibility;
            component.verticalScrollbarVisibility = verticalScrollbarVisibility;
            component.horizontalScrollbarSpacing = horizontalScrollbarSpacing;
            component.verticalScrollbarSpacing = verticalScrollbarSpacing;

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
            HandleError($"Failed to add scroll rect to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion