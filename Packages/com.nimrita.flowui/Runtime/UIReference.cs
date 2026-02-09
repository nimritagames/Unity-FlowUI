using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Represents a reference to a UI element in the scene.
/// </summary>
[System.Serializable]
public class UIReference : ISerializationCallbackReceiver
{
    public string name;
    public string fullPath;
    [SerializeField]
    private int instanceId;
    [FormerlySerializedAs("instanceID")]
    [SerializeField]
    private string legacyInstanceId;
    public GameObject uiElement;
    public UIElementType elementType;

    public int instanceID
    {
        get => instanceId;
        set => instanceId = value;
    }

    public void OnBeforeSerialize()
    {
        legacyInstanceId = null;
    }

    public void OnAfterDeserialize()
    {
        if (instanceId != 0)
        {
            return;
        }

        if (int.TryParse(legacyInstanceId, out int parsedId))
        {
            instanceId = parsedId;
        }
    }
}

/// <summary>
/// Enum representing different types of UI elements.
/// </summary>
public enum UIElementType
{
    Panel,
    Button,
    Text,
    TMP_Text,
    Toggle,
    InputField,
    TMP_InputField,
    Slider,
    Dropdown,
    ScrollView,
    Image,
    RawImage,
    Mask,
    Canvas,
    CanvasGroup,
    Unknown
}

