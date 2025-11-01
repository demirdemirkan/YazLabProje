using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT || ENABLE_TMP
using TMPro;
#endif

[ExecuteAlways]
public class PauseMenuBeautify : MonoBehaviour
{
    [Header("Root")]
    public RectTransform card;              // Orta kart (opsiyonel)
    public RectTransform buttonsParent;     // Butonlarýn parent'ý (Vertical Layout Group)
    public Button[] buttons;                // Devam Et, Çýkýþ

    [Header("Sizes")]
    public Vector2 cardSize = new Vector2(640, 380);
    public float containerWidth = 560f;
    public Vector2 buttonSize = new Vector2(520f, 76f);
    public int fontSize = 36;

    [Header("Colors")]
    public Color overlay = new Color(0, 0, 0, 0.7f);
    public Color cardColor = new Color32(15, 23, 42, 235);   // #0F172A, A~0.92
    public Color btnNormal = new Color32(30, 41, 59, 230);   // #1E293B
    public Color btnHighlighted = new Color32(51, 65, 85, 242); // #334155
    public Color btnPressed = new Color32(71, 85, 105, 255); // #475569
    public Color textColor = Color.white;

    void OnEnable() { Apply(); }
    void OnValidate() { Apply(); }

    public void Apply()
    {
        // Panel arka plan (overlay)
        var img = GetComponent<Image>();
        if (!img) img = gameObject.AddComponent<Image>();
        img.color = overlay;
        // Card
        if (card)
        {
            var imgCard = card.GetComponent<Image>() ?? card.gameObject.AddComponent<Image>();
            imgCard.color = cardColor;

            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = cardSize;

            var shadow = card.GetComponent<Shadow>() ?? card.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(0, -2);
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
        }

        // Buttons container
        if (buttonsParent)
        {
            var vlg = buttonsParent.GetComponent<VerticalLayoutGroup>() ?? buttonsParent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 18;
            vlg.padding = new RectOffset(24, 24, 16, 16);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var csf = buttonsParent.GetComponent<ContentSizeFitter>() ?? buttonsParent.gameObject.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            buttonsParent.anchorMin = buttonsParent.anchorMax = new Vector2(0.5f, 0.5f);
            buttonsParent.sizeDelta = new Vector2(containerWidth, buttonsParent.sizeDelta.y);
            buttonsParent.anchoredPosition = new Vector2(0, -10);
        }

        // Buttons
        foreach (var b in buttons)
        {
            if (!b) continue;

            // arkaplan
            var bg = b.GetComponent<Image>() ?? b.gameObject.AddComponent<Image>();
            bg.color = btnNormal;

            // transition renkleri
            var colors = b.colors;
            colors.normalColor = btnNormal;
            colors.highlightedColor = btnHighlighted;
            colors.pressedColor = btnPressed;
            colors.selectedColor = btnHighlighted;
            colors.disabledColor = new Color(btnNormal.r, btnNormal.g, btnNormal.b, 0.5f);
            b.colors = colors;

            // boyutlandýrma / layout
            var rt = b.GetComponent<RectTransform>();
            rt.sizeDelta = buttonSize;

            var le = b.GetComponent<LayoutElement>() ?? b.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = buttonSize.x;
            le.preferredHeight = buttonSize.y;

            // text ayarlarý (TMP varsa onu, yoksa klasik Text)
#if TMP_PRESENT || ENABLE_TMP
            var tmp = b.GetComponentInChildren<TMP_Text>();
            if (tmp)
            {
                tmp.fontSize = fontSize;
                tmp.color = textColor;
                tmp.alignment = TextAlignmentOptions.Center;
            }
#else
            var t = b.GetComponentInChildren<Text>();
            if (t)
            {
                t.fontSize = fontSize;
                t.color = textColor;
                t.alignment = TextAnchor.MiddleCenter;
            }
#endif
        }
    }
}
