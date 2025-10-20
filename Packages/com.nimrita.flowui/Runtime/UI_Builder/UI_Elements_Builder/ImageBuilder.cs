using UnityEngine;
using UnityEngine.UI;
using System;

#region Image Builder Implementation
public class ImageBuilder : UIElementBuilder<Image, ImageBuilder>
{
    private Sprite sprite;
    private Color color = Color.white;
    private Image.Type imageType = Image.Type.Simple;
    private bool preserveAspect = false;
    private bool fillCenter = true;
    private Image.FillMethod fillMethod = Image.FillMethod.Radial360;
    private float fillAmount = 1f;
    private bool fillClockwise = true;
    private int fillOrigin = 0;
    private bool raycastTarget = true;
    private Material material;
    private bool setNativeSize = false;

    public ImageBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            rectTransform = elementObject.GetComponent<RectTransform>();
            component = elementObject.GetComponent<Image>();

            rectTransform.sizeDelta = new Vector2(100, 100);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize ImageBuilder: {ex.Message}");
        }
    }

    public ImageBuilder WithSprite(Sprite sprite)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.sprite = sprite;
        }, $"Failed to set sprite for {elementName}");
    }

    public ImageBuilder WithSprite(string resourcePath)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            Sprite loadedSprite = LoadSpriteFromResources(resourcePath);
            if (loadedSprite != null)
            {
                sprite = loadedSprite;
            }
        }, $"Failed to load sprite from resources for {elementName}");
    }

    public ImageBuilder WithMaterial(Material material)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.material = material;
        }, $"Failed to set material for {elementName}");
    }

    public ImageBuilder WithImageType(Image.Type type)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            imageType = type;
        }, $"Failed to set image type for {elementName}");
    }

    public ImageBuilder WithPreserveAspect(bool preserveAspect)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.preserveAspect = preserveAspect;
        }, $"Failed to set preserve aspect for {elementName}");
    }

    public ImageBuilder WithFillCenter(bool fillCenter)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.fillCenter = fillCenter;
        }, $"Failed to set fill center for {elementName}");
    }

    public ImageBuilder WithSetNativeSize(bool setNativeSize)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.setNativeSize = setNativeSize;
        }, $"Failed to set native size option for {elementName}");
    }

    public ImageBuilder WithFillMethod(Image.FillMethod method, float amount = 1f, bool clockwise = true, int origin = 0)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            fillMethod = method;
            fillAmount = Mathf.Clamp01(amount);
            fillClockwise = clockwise;
            fillOrigin = origin;
        }, $"Failed to set fill method for {elementName}");
    }

    public ImageBuilder WithRaycastTarget(bool raycastTarget)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.raycastTarget = raycastTarget;
        }, $"Failed to set raycast target for {elementName}");
    }

    public override Image AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            component.sprite = sprite;
            component.color = color;
            component.type = imageType;
            component.preserveAspect = preserveAspect;
            component.fillCenter = fillCenter;
            component.fillMethod = fillMethod;
            component.fillAmount = fillAmount;
            component.fillClockwise = fillClockwise;
            component.fillOrigin = fillOrigin;
            component.raycastTarget = raycastTarget;

            if (material != null)
            {
                component.material = material;
            }

            if (setNativeSize && sprite != null)
            {
                component.SetNativeSize();
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
            HandleError($"Failed to add image to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion