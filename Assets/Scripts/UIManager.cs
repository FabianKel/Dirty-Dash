using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject pausePanel;
    public GameObject confirmationPanel;
    public WinPanelUI winPanel;

    [Header("Confirmation Text")]
    public TMPro.TextMeshProUGUI confirmationText;

    private string confirmationTarget;
    private bool _gameOver;

    [Header("Checkpoint UI")]
    public GameObject p1CheckpointText;
    public GameObject p2CheckpointText;
    public float checkpointDisplayTime = 2f; 

    private Coroutine _p1CheckpointCoroutine;
    private Coroutine _p2CheckpointCoroutine;
    private bool _settingsOpenedFromPause;
    private Image _settingsBackdrop;

    void Awake()
    {
        ResolveWinPanel();
        EnsureSettingsBackdrop();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (_gameOver) return;

        bool isMainMenuOpen = mainMenuPanel != null && mainMenuPanel.activeSelf;
        if (Input.GetKeyDown(KeyCode.Escape) && !isMainMenuOpen)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
                return;
            }

            bool isCurrentlyPaused = pausePanel != null && pausePanel.activeSelf;
            TogglePause(!isCurrentlyPaused);
        }
    }

    public void TogglePause(bool isPaused)
    {
        if (pausePanel == null) return;

        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void ResumeGame()
    {
        TogglePause(false);
    }

    public void ShowSettings()
    {
        if (settingsPanel == null) return;

        _settingsOpenedFromPause = pausePanel != null && pausePanel.activeSelf;
        if (_settingsOpenedFromPause && pausePanel != null)
            pausePanel.SetActive(false);

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        EnsureSettingsBackdrop();
        settingsPanel.SetActive(true);
        Time.timeScale = _settingsOpenedFromPause ? 0f : 1f;
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);

        if (_settingsOpenedFromPause && pausePanel != null && !_gameOver)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }

        _settingsOpenedFromPause = false;
    }

    public void ShowMainMenu()
    {
        ResetAllPanels();
        mainMenuPanel.SetActive(true);
    }

    private void ResetAllPanels()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (p1CheckpointText) p1CheckpointText.SetActive(false);
        if (p2CheckpointText) p2CheckpointText.SetActive(false);

        _settingsOpenedFromPause = false;
        Time.timeScale = 1f;
    }
    public void ShowWin(int winnerIndex)
    {
        if (_gameOver) return;
        _gameOver = true;

        ResolveWinPanel();

        pausePanel.SetActive(false);
        settingsPanel.SetActive(false);
        confirmationPanel.SetActive(false);

        if (winPanel != null) winPanel.ShowWinner(winnerIndex);

        foreach (var pc in Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            pc.enabled = false;
    }

    void ResolveWinPanel()
    {
        if (winPanel != null && winPanel.gameObject != null && winPanel.gameObject.scene.IsValid() && winPanel.gameObject.scene == gameObject.scene)
            return;

        winPanel = null;

        var panels = Object.FindObjectsByType<WinPanelUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        // Prefer same scene first (avoids picking a prefab-stage or DDOL panel).
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] == null) continue;
            if (!panels[i].gameObject.scene.IsValid()) continue;
            if (panels[i].gameObject.scene != gameObject.scene) continue;
            winPanel = panels[i];
            break;
        }

        // Fallback: any scene panel.
        if (winPanel == null)
        {
            for (int i = 0; i < panels.Length; i++)
            {
                if (panels[i] == null) continue;
                if (!panels[i].gameObject.scene.IsValid()) continue;
                winPanel = panels[i];
                break;
            }
        }
    }

    public void ShowCheckpointReached(int playerIndex)
    {
        if (_gameOver) return;

        if (playerIndex == 1)
        {
            if (_p1CheckpointCoroutine != null) StopCoroutine(_p1CheckpointCoroutine);
            _p1CheckpointCoroutine = StartCoroutine(CheckpointRoutine(p1CheckpointText));
        }
        else if (playerIndex == 2)
        {
            if (_p2CheckpointCoroutine != null) StopCoroutine(_p2CheckpointCoroutine);
            _p2CheckpointCoroutine = StartCoroutine(CheckpointRoutine(p2CheckpointText));
        }
    }

    private System.Collections.IEnumerator CheckpointRoutine(GameObject checkpointUI)
    {
        if (checkpointUI == null) yield break;

        checkpointUI.SetActive(true);

        yield return new WaitForSeconds(checkpointDisplayTime);

        checkpointUI.SetActive(false);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _gameOver = false;
        Time.timeScale = 1f;
        ResolveWinPanel();
        EnsureSettingsBackdrop();
        if (winPanel != null) winPanel.gameObject.SetActive(false);
        if (p1CheckpointText) p1CheckpointText.SetActive(false);
        if (p2CheckpointText) p2CheckpointText.SetActive(false);
    }

    void EnsureSettingsBackdrop()
    {
        if (settingsPanel == null) return;

        _settingsBackdrop = settingsPanel.GetComponent<Image>();
        if (_settingsBackdrop == null)
            _settingsBackdrop = settingsPanel.AddComponent<Image>();

        _settingsBackdrop.color = new Color(0f, 0f, 0f, 0.72f);
        _settingsBackdrop.raycastTarget = true;
    }
}

public class SceneAudioController : MonoBehaviour
{
    static readonly Dictionary<string, string> MusicByScene = new()
    {
        ["MainGame"] = "ost1",
        ["MainGame_Level2"] = "ost2",
        ["MainGame_Level3"] = "ost3",
    };

    static readonly Dictionary<string, string> FootstepsByScene = new()
    {
        ["MainGame"] = "metal_walk",
        ["MainGame_Level2"] = "grass_walk",
        ["MainGame_Level3"] = "grass_walk",
    };

    static readonly Dictionary<string, AudioClip> ClipCache = new();

    public static SceneAudioController Instance { get; private set; }

    private AudioSource _musicSource;
    private AudioClip _footstepClip;
    private AudioClip _jumpClip;
    private AudioClip _pickupClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance != null) return;

        SceneAudioController existing = Object.FindFirstObjectByType<SceneAudioController>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject go = new GameObject("SceneAudioController");
        go.AddComponent<SceneAudioController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _musicSource = gameObject.AddComponent<AudioSource>();
        _musicSource.playOnAwake = false;
        _musicSource.loop = true;
        _musicSource.spatialBlend = 0f;
        _musicSource.volume = SettingsManager.CurrentMusicVolume;

        _jumpClip = LoadClip("jump");
        _pickupClip = LoadClip("pickup");
        ConfigureSceneAudio(SceneManager.GetActiveScene().name);
        ApplyRuntimeVolumes();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SettingsManager.VolumesChanged += ApplyRuntimeVolumes;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SettingsManager.VolumesChanged -= ApplyRuntimeVolumes;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigureSceneAudio(scene.name);
        ApplyRuntimeVolumes();
    }

    public AudioClip GetFootstepClip() => _footstepClip;

    public void PlayJump(AudioSource source)
    {
        if (source == null || _jumpClip == null) return;
        source.volume = 0.75f * SettingsManager.CurrentSfxVolume;
        source.PlayOneShot(_jumpClip);
    }

    public void PlayPickup(AudioSource source)
    {
        if (source == null || _pickupClip == null) return;
        source.volume = 0.85f * SettingsManager.CurrentSfxVolume;
        source.PlayOneShot(_pickupClip);
    }

    void ConfigureSceneAudio(string sceneName)
    {
        _footstepClip = LoadClip(ResolveClipName(FootstepsByScene, sceneName));

        AudioClip musicClip = LoadClip(ResolveClipName(MusicByScene, sceneName));
        if (musicClip == null)
        {
            _musicSource.Stop();
            _musicSource.clip = null;
            return;
        }

        if (_musicSource.clip != musicClip)
        {
            _musicSource.Stop();
            _musicSource.clip = musicClip;
        }

        if (!_musicSource.isPlaying)
            _musicSource.Play();
    }

    void ApplyRuntimeVolumes()
    {
        AudioListener.volume = SettingsManager.CurrentMasterVolume;

        if (_musicSource != null)
            _musicSource.volume = SettingsManager.CurrentMusicVolume;
    }

    static string ResolveClipName(Dictionary<string, string> lookup, string sceneName)
    {
        return lookup.TryGetValue(sceneName, out string clipName) ? clipName : null;
    }

    static AudioClip LoadClip(string clipName)
    {
        if (string.IsNullOrWhiteSpace(clipName)) return null;
        if (ClipCache.TryGetValue(clipName, out AudioClip cached) && cached != null) return cached;

        AudioClip clip = Resources.Load<AudioClip>($"Music/{clipName}");
        if (clip != null)
            ClipCache[clipName] = clip;

        return clip;
    }
}
