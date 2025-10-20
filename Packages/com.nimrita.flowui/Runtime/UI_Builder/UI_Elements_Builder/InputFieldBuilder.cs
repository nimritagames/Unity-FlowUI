using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System;
using System.Collections.Generic;

#region InputField Builder Implementation
public class InputFieldBuilder : UIElementBuilder<InputField, InputFieldBuilder>
{
    private string text = "";
    private string placeholder = "Enter text...";
    private bool interactable = true;
    private int characterLimit = 0;
    private InputField.CharacterValidation characterValidation = InputField.CharacterValidation.None;
    private InputField.ContentType contentType = InputField.ContentType.Standard;
    private InputField.LineType lineType = InputField.LineType.SingleLine;
    private InputField.InputType inputType = InputField.InputType.Standard;
    private TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default;
    private bool useTMP = false;
    private List<UnityAction<string>> valueChangedActions = new List<UnityAction<string>>();
    private List<UnityAction<string>> endEditActions = new List<UnityAction<string>>();
    private bool readOnly = false;
    private Color selectionColor = new Color(0.5f, 0.5f, 1f, 0.4f);
    private Color textColor = Color.black;
    private Color placeholderColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
    private int fontSize = 14;
    private FontStyle fontStyle = FontStyle.Normal;

    // Reference to TMP component if used
    private TMP_InputField tmpInputField;

    public InputFieldBuilder(UIManager uiManager, string name) : base(uiManager, name)
    {
        try
        {
            elementObject = new GameObject(name, typeof(RectTransform));
            rectTransform = elementObject.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 40);

            // Defer structure creation until we know if TMP will be used
        }
        catch (Exception ex)
        {
            HandleError($"Failed to initialize InputFieldBuilder: {ex.Message}");
        }
    }

    private void SetupInputFieldStructure()
    {
        if (useTMP)
        {
            if (elementObject.GetComponent<TMP_InputField>() == null)
            {
                tmpInputField = elementObject.AddComponent<TMP_InputField>();
            }
            else
            {
                tmpInputField = elementObject.GetComponent<TMP_InputField>();
            }

            // Create background
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(elementObject.transform, false);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Create text area
            GameObject textArea = new GameObject("Text Area", typeof(RectTransform));
            textArea.transform.SetParent(elementObject.transform, false);
            RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -6);

            // Create text component
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TMP_Text));
            textObj.transform.SetParent(textArea.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            TMP_Text textComponent = textObj.GetComponent<TMP_Text>();
            textComponent.text = text;
            textComponent.color = textColor;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = (TMPro.FontStyles)fontStyle;
            textComponent.alignment = TextAlignmentOptions.Left;

            // Set default font for TMP
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                textComponent.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            // Create placeholder
            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(TMP_Text));
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;
            TMP_Text placeholderComponent = placeholderObj.GetComponent<TMP_Text>();
            placeholderComponent.text = placeholder;
            placeholderComponent.color = placeholderColor;
            placeholderComponent.fontSize = fontSize;
            placeholderComponent.fontStyle = (TMPro.FontStyles)fontStyle;
            placeholderComponent.alignment = TextAlignmentOptions.Left;

            // Set default font for placeholder TMP
            if (TMPro.TMP_Settings.defaultFontAsset != null)
            {
                placeholderComponent.font = TMPro.TMP_Settings.defaultFontAsset;
            }

            // Assign references
            tmpInputField.textComponent = textComponent;
            tmpInputField.placeholder = placeholderComponent;
            tmpInputField.targetGraphic = background.GetComponent<Image>();
            tmpInputField.textViewport = textAreaRect;

            // Set properties
            tmpInputField.text = text;
            tmpInputField.characterLimit = characterLimit;
            tmpInputField.interactable = interactable;
            tmpInputField.readOnly = readOnly;
            tmpInputField.selectionColor = selectionColor;

            // Set input type settings - map from InputField to TMP_InputField types
            switch (contentType)
            {
                case InputField.ContentType.Standard:
                    tmpInputField.contentType = TMP_InputField.ContentType.Standard;
                    break;
                case InputField.ContentType.Autocorrected:
                    tmpInputField.contentType = TMP_InputField.ContentType.Autocorrected;
                    break;
                case InputField.ContentType.IntegerNumber:
                    tmpInputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                    break;
                case InputField.ContentType.DecimalNumber:
                    tmpInputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                    break;
                case InputField.ContentType.Alphanumeric:
                    tmpInputField.contentType = TMP_InputField.ContentType.Alphanumeric;
                    break;
                case InputField.ContentType.Name:
                    tmpInputField.contentType = TMP_InputField.ContentType.Name;
                    break;
                case InputField.ContentType.EmailAddress:
                    tmpInputField.contentType = TMP_InputField.ContentType.EmailAddress;
                    break;
                case InputField.ContentType.Password:
                    tmpInputField.contentType = TMP_InputField.ContentType.Password;
                    break;
                case InputField.ContentType.Pin:
                    tmpInputField.contentType = TMP_InputField.ContentType.Pin;
                    break;
                case InputField.ContentType.Custom:
                    tmpInputField.contentType = TMP_InputField.ContentType.Custom;
                    break;
            }

            // Set keyboard type
            tmpInputField.keyboardType = keyboardType;

            // Set line type
            switch (lineType)
            {
                case InputField.LineType.SingleLine:
                    tmpInputField.lineType = TMP_InputField.LineType.SingleLine;
                    break;
                case InputField.LineType.MultiLineSubmit:
                    tmpInputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
                    break;
                case InputField.LineType.MultiLineNewline:
                    tmpInputField.lineType = TMP_InputField.LineType.MultiLineNewline;
                    break;
            }

            // Set character validation
            switch (characterValidation)
            {
                case InputField.CharacterValidation.None:
                    tmpInputField.characterValidation = TMP_InputField.CharacterValidation.None;
                    break;
                case InputField.CharacterValidation.Integer:
                    tmpInputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
                    break;
                case InputField.CharacterValidation.Decimal:
                    tmpInputField.characterValidation = TMP_InputField.CharacterValidation.Decimal;
                    break;
                case InputField.CharacterValidation.Alphanumeric:
                    tmpInputField.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
                    break;
                case InputField.CharacterValidation.Name:
                    tmpInputField.characterValidation = TMP_InputField.CharacterValidation.Name;
                    break;
                case InputField.CharacterValidation.EmailAddress:
                    tmpInputField.characterValidation = TMP_InputField.CharacterValidation.EmailAddress;
                    break;
            }
        }
        else
        {
            if (elementObject.GetComponent<InputField>() == null)
            {
                component = elementObject.AddComponent<InputField>();
            }
            else
            {
                component = elementObject.GetComponent<InputField>();
            }

            // Create background
            GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(elementObject.transform, false);
            RectTransform bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Create text component
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(elementObject.transform, false);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 6);
            textRect.offsetMax = new Vector2(-10, -6);
            Text textComponent = textObj.GetComponent<Text>();
            textComponent.text = text;
            textComponent.color = textColor;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = TextAnchor.MiddleLeft;
            textComponent.font = GetDefaultFont();

            // Create placeholder
            GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            placeholderObj.transform.SetParent(elementObject.transform, false);
            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10, 6);
            placeholderRect.offsetMax = new Vector2(-10, -6);
            Text placeholderComponent = placeholderObj.GetComponent<Text>();
            placeholderComponent.text = placeholder;
            placeholderComponent.color = placeholderColor;
            placeholderComponent.fontSize = fontSize;
            placeholderComponent.fontStyle = fontStyle;
            placeholderComponent.alignment = TextAnchor.MiddleLeft;
            placeholderComponent.font = GetDefaultFont();

            // Assign references
            component.textComponent = textComponent;
            component.placeholder = placeholderComponent;
            component.targetGraphic = background.GetComponent<Image>();

            // Set properties
            component.text = text;
            component.characterLimit = characterLimit;
            component.contentType = contentType;
            component.lineType = lineType;
            component.inputType = inputType;
            component.keyboardType = keyboardType;
            component.characterValidation = characterValidation;
            component.interactable = interactable;
            component.readOnly = readOnly;
            component.selectionColor = selectionColor;
        }
    }

    public InputFieldBuilder WithTMP(bool useTMP)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.useTMP = useTMP;
        }, $"Failed to set TMP mode for {elementName}");
    }

    public InputFieldBuilder WithText(string text)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.text = text;
        }, $"Failed to set text for {elementName}");
    }

    public InputFieldBuilder WithPlaceholder(string placeholder)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.placeholder = placeholder;
        }, $"Failed to set placeholder for {elementName}");
    }

    public InputFieldBuilder WithCharacterLimit(int limit)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.characterLimit = limit;
        }, $"Failed to set character limit for {elementName}");
    }

    public InputFieldBuilder WithContentType(InputField.ContentType contentType)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.contentType = contentType;
        }, $"Failed to set content type for {elementName}");
    }

    public InputFieldBuilder WithLineType(InputField.LineType lineType)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.lineType = lineType;
        }, $"Failed to set line type for {elementName}");
    }

    public InputFieldBuilder WithInputType(InputField.InputType inputType)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.inputType = inputType;
        }, $"Failed to set input type for {elementName}");
    }

    public InputFieldBuilder WithKeyboardType(TouchScreenKeyboardType keyboardType)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.keyboardType = keyboardType;
        }, $"Failed to set keyboard type for {elementName}");
    }

    public InputFieldBuilder WithCharacterValidation(InputField.CharacterValidation validation)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.characterValidation = validation;
        }, $"Failed to set character validation for {elementName}");
    }

    public InputFieldBuilder WithInteractable(bool interactable)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.interactable = interactable;
        }, $"Failed to set interactable for {elementName}");
    }

    public InputFieldBuilder WithReadOnly(bool readOnly)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.readOnly = readOnly;
        }, $"Failed to set read only for {elementName}");
    }

    public InputFieldBuilder WithSelectionColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.selectionColor = color;
        }, $"Failed to set selection color for {elementName}");
    }

    public InputFieldBuilder WithTextColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.textColor = color;
        }, $"Failed to set text color for {elementName}");
    }

    public InputFieldBuilder WithPlaceholderColor(Color color)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.placeholderColor = color;
        }, $"Failed to set placeholder color for {elementName}");
    }

    public InputFieldBuilder WithFontSize(int fontSize)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.fontSize = fontSize;
        }, $"Failed to set font size for {elementName}");
    }

    public InputFieldBuilder WithFontStyle(FontStyle fontStyle)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            this.fontStyle = fontStyle;
        }, $"Failed to set font style for {elementName}");
    }

    public InputFieldBuilder OnValueChanged(UnityAction<string> action)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (action != null)
                valueChangedActions.Add(action);
        }, $"Failed to add value changed action for {elementName}");
    }

    public InputFieldBuilder OnEndEdit(UnityAction<string> action)
    {
        if (hasFailed) return this;

        return SafeExecute(() => {
            if (action != null)
                endEditActions.Add(action);
        }, $"Failed to add end edit action for {elementName}");
    }

    public override InputField AddTo(Transform parent)
    {
        if (hasFailed || elementObject == null) return null;

        try
        {
            elementObject.transform.SetParent(parent, false);

            // Create the structure now that all properties are set
            SetupInputFieldStructure();

            if (useTMP && tmpInputField != null)
            {
                // Add event listeners
                foreach (var action in valueChangedActions)
                {
                    tmpInputField.onValueChanged.AddListener(action);
                }

                foreach (var action in endEditActions)
                {
                    tmpInputField.onEndEdit.AddListener(action);
                }

                // TMP is used, so regular InputField isn't available
                component = null;
            }
            else if (component != null)
            {
                // Add event listeners
                foreach (var action in valueChangedActions)
                {
                    component.onValueChanged.AddListener(action);
                }

                foreach (var action in endEditActions)
                {
                    component.onEndEdit.AddListener(action);
                }
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
            HandleError($"Failed to add input field to parent: {ex.Message}");
            return null;
        }

        return component;
    }
}
#endregion