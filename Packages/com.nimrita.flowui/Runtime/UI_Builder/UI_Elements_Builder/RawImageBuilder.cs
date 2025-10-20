using UnityEngine;
using UnityEngine.UI;
using System;

#region Raw Image Builder Implementation
public class RawImageBuilder : UIElementBuilder<RawImage, RawImageBuilder>
{
    private Texture texture;
    private Color color = Color.white;
    private bool preserveAspect = false;
    private bool raycastTarget = true;
    private Material material;
    private bool setNativeSize = false;
    private Rect uvRect = new Rect(0, 0, 1, 1);

    public RawImageBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            rectTransform = elementObject.GetComponent<RectTransform>();
            component = elementObject.GetComponent<RawImage>();

            rectTransform.sizeDelta = new Vector2(100, 100);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize RawImageBuilder: {ex.Message}");
        }
    }

    public RawImageBuilder WithTexture(Texture texture)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.texture = texture;
        }, $"Failed to set texture for {elementName}");
    }

    public RawImageBuilder WithTexture(string resourcePath)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            Texture loadedTexture = Resources.Load<Texture>(resourcePath);
            if (loadedTexture != null)
            {
                texture = loadedTexture;
            }
            else
            {
                Debug.LogWarning($"UIBuilder: Could not load texture from path '{resourcePath}'");
            }
        }, $"Failed to load texture from resources for {elementName}");
    }

    public RawImageBuilder WithUVRect(Rect uvRect)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.uvRect = uvRect;
        }, $"Failed to set UV rect for {elementName}");
    }

    public RawImageBuilder WithUVRect(float x, float y, float width, float height)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.uvRect = new Rect(x, y, width, height);
        }, $"Failed to set UV rect for {elementName}");
    }

    public RawImageBuilder WithMaterial(Material material)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.material = material;
        }, $"Failed to set material for {elementName}");
    }

    public RawImageBuilder WithPreserveAspect(bool preserveAspect)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.preserveAspect = preserveAspect;
        }, $"Failed to set preserve aspect for {elementName}");
    }

    public RawImageBuilder WithSetNativeSize(bool setNativeSize)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.setNativeSize = setNativeSize;
        }, $"Failed to set native size option for {elementName}");
    }

    public RawImageBuilder WithRaycastTarget(bool raycastTarget)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.raycastTarget = raycastTarget;
        }, $"Failed to set raycast target for {elementName}");
    }

    public override RawImage AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            component.texture = texture;
            component.color = color;
            component.raycastTarget = raycastTarget;
            component.uvRect = uvRect;

            if (material != null)
            {
                component.material = material;
            }

            if (setNativeSize && texture != null)
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
            HandleError($"Failed to add raw image to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion