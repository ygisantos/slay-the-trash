using TMPro;
using UnityEngine;

public class DynamicText : MonoBehaviour
{
    public enum TextType
    {
        Normal,
        Success,
        Error,
        Warning,
        Info,
        Highlight
    }

    private TMP_Text text;

    private Color normalColor = Color.white;
    private Color successColor = new Color(0.2f, 0.8f, 0.3f);
    private Color errorColor = new Color(1f, 0.25f, 0.25f);
    private Color warningColor = new Color(1f, 0.7f, 0.15f);
    private Color infoColor = new Color(0.25f, 0.65f, 1f);
    private Color highlightColor = new Color(0.8f, 0.4f, 1f);

    public string Text => text != null ? text.text : string.Empty;

    private void Awake()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();
    }

    // =========================================================
    // TEXT
    // =========================================================

    public void SetText(string value)
    {
        if (text == null)
            return;

        text.text = value;
    }

    public void SetText(string value, TextType type)
    {
        SetText(value);
        SetType(type);
    }

    // =========================================================
    // TYPE
    // =========================================================

    public void SetType(TextType type)
    {
        if (text == null)
        {
            Debug.Log("Text component is not assigned.");
            return;
        }

        text.color = GetColor(type);
    }

    public void SetTypeAndText(TextType type, string value)
    {
        SetText(value);
        SetType(type);
    }

    // =========================================================
    // COLOR
    // =========================================================

    public void SetColor(Color color)
    {
        if (text == null)
            return;

        text.color = color;
    }

    public Color GetColor(TextType type)
    {
        return type switch
        {
            TextType.Success => successColor,
            TextType.Error => errorColor,
            TextType.Warning => warningColor,
            TextType.Info => infoColor,
            TextType.Highlight => highlightColor,
            _ => normalColor
        };
    }

    // =========================================================
    // CONVENIENCE METHODS
    // =========================================================

    public void Success(string value)
    {
        SetTextAndType(value, TextType.Success);
    }

    public void Error(string value)
    {
        SetTextAndType(value, TextType.Error);
    }

    public void Warning(string value)
    {
        SetTextAndType(value, TextType.Warning);
    }

    public void Info(string value)
    {
        SetTextAndType(value, TextType.Info);
    }

    public void Highlight(string value)
    {
        SetTextAndType(value, TextType.Highlight);
    }

    public void Normal(string value)
    {
        SetTextAndType(value, TextType.Normal);
    }

    private void SetTextAndType(string value, TextType type)
    {
        SetText(value);
        SetType(type);
    }

    // =========================================================
    // ENABLE / DISABLE
    // =========================================================

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // =========================================================
    // DIRECT TMP ACCESS
    // =========================================================

    public TMP_Text GetTextComponent()
    {
        return text;
    }
}