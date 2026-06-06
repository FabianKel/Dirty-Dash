using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelSelectPanel : MonoBehaviour
{
    [System.Serializable]
    public class LevelCard
    {
        public Image      levelImage;   // screenshot del nivel
        public TextMeshProUGUI levelName;
        public Button     btnSelect;
    }

    [Header("Cards (en orden: nivel 1, 2, 3)")]
    public LevelCard[] cards = new LevelCard[3];

    [Header("Contenido por nivel")]
    public Sprite[] levelPreviews;                          // screenshots
    public string[] levelNames = { "Level 1", "Level 2", "Level 3" };

    [Header("Highlight de la card seleccionada")]
    public Color normalBorderColor    = new Color(1,1,1,0.15f);
    public Color selectedBorderColor  = new Color(1,1,1,0.9f);
    public Button closeButton;

    void Awake()
    {
        EnsureCloseButton();

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < levelPreviews.Length && cards[i].levelImage != null)
                cards[i].levelImage.sprite = levelPreviews[i];

            if (i < levelNames.Length && cards[i].levelName != null)
                cards[i].levelName.text = levelNames[i];

            int levelIndex = i + 1;
            cards[i].btnSelect.onClick.AddListener(() => SelectLevel(levelIndex));
        }
    }

    void OnEnable()
    {
        SetHighlight(-1);
    }

    void EnsureCloseButton()
    {
        if (closeButton == null)
            closeButton = GetComponentInChildren<Button>(true);

        if (closeButton == null || closeButton.transform.parent != transform)
            closeButton = FindTopRightCloseButton();

        if (closeButton == null)
            closeButton = CreateCloseButton();

        closeButton.onClick.RemoveListener(ReturnToMainMenu);
        closeButton.onClick.AddListener(ReturnToMainMenu);
    }

    Button FindTopRightCloseButton()
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == "ExitButton")
                return buttons[i];
        }

        return null;
    }

    Button CreateCloseButton()
    {
        GameObject buttonGO = new GameObject("ExitButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(UIButtonAudioHook));
        buttonGO.layer = gameObject.layer;
        buttonGO.transform.SetParent(transform, false);

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
        rect.sizeDelta = new Vector2(40f, 40f);

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(1f, 0.5235849f, 0.5235849f, 1f);

        Button button = buttonGO.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.6886792f, 0.36058205f, 0.36058205f, 1f);
        colors.pressedColor = new Color(0.78431374f, 0.78431374f, 0.78431374f, 1f);
        button.colors = colors;

        GameObject textGO = new GameObject("Text (TMP)", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.layer = gameObject.layer;
        textGO.transform.SetParent(buttonGO.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(40f, 40f);

        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.text = "X";
        text.fontSize = 32f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        text.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSharedMaterial = TMP_Settings.defaultFontAsset.material;
        }

        return button;
    }

    void SelectLevel(int level)
    {
        SetHighlight(level - 1);

        if (GameDataManager.Instance != null)
            GameDataManager.Instance.selectedLevel = level;

        var ui = FindAnyObjectByType<UIMainMenuManager>();
        if (ui != null)
            ui.ShowCharacterSelect();
        else
            Debug.LogError("UIMainMenuManager no encontrado.");
    }

    void SetHighlight(int selectedIndex)
    {
        for (int i = 0; i < cards.Length; i++)
        {
            var cardImage = cards[i].btnSelect.transform.parent?.GetComponent<Image>();
            if (cardImage != null)
                cardImage.color = (i == selectedIndex) ? selectedBorderColor : normalBorderColor;
        }
    }

    void ReturnToMainMenu()
    {
        var ui = FindAnyObjectByType<UIMainMenuManager>();
        if (ui != null)
            ui.ShowMainMenu();
    }
}
