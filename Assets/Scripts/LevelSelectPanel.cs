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

    void Awake()
    {
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
}