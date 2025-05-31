using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Data")]
    public int currentLevel { get; private set; } = 1;
    public int currentQuestion { get; private set; } = 0;
    public int CurrentScore { get; private set; } = 0;
    public bool ColoredPointsPurchased { get; private set; } = false;
    public int Coins { get; private set; } = 0;
    public int CorrectAnswers { get; private set; } = 0;
    public int WrongAnswers { get; private set; } = 0;


    public int LevelQue = 10; // questions per level

    [Header("UI Panels")]
    public GameObject mainMenuUI;
    public GameObject scanUI;
    public GameObject inputUI;
    public GameObject settingsUI;
    

    [Header("Settings")]
    public Slider hapticSlider;
    public Slider audioSlider;
    private float hapticIntensity = 1.0f;
    private float audioVolume = 1.0f;

    private bool accessingSettingsFromMainMenu = false;

    [Header("Player Info")]
    public GameObject playerInputPanel;
    public TMP_InputField playerNameInputField;
    public Button submitPlayerNameButton;
    public TMP_Text playerNameText;

    private const string playerIDKey = "PlayerID";
    public string PlayerID { get; private set; }

    public static bool IsNewGame = false;
    public static bool IsCloudDataLoaded = false;


    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            await InitializeUnityServicesAsync();
            await LoadDataFromCloud();
            LoadProgress();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private async Task InitializeUnityServicesAsync()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in anonymously as: " + AuthenticationService.Instance.PlayerId);
            }
            catch (AuthenticationException ex)
            {
                Debug.LogError($"Authentication failed: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("Already signed in as: " + AuthenticationService.Instance.PlayerId);
        }
    }

    private void Start()
    {
        PlayerID = PlayerPrefs.GetString(playerIDKey, "");
        AnalyticsManager.Instance.LogGameSessionStart();

        if (string.IsNullOrEmpty(PlayerID))
        {
            playerInputPanel.SetActive(true);
            mainMenuUI.SetActive(false);
            playerNameText.text = "";
            submitPlayerNameButton.onClick.RemoveAllListeners();
            submitPlayerNameButton.onClick.AddListener(SubmitPlayerName);
        }
        else
        {
            playerInputPanel.SetActive(false);
            playerNameText.text = $"Player: {PlayerID}";

            if (IsNewGame)
            {
                ShowScanUI();
                IsNewGame = false;
            }
            else
            {
                ShowMainMenu();
            }
        }

        InitializeSettings();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(PlayerID))
        {
            playerInputPanel.SetActive(true);
            mainMenuUI.SetActive(false);
            playerNameText.text = "";
        }
        else if (IsNewGame)
        {
            ShowScanUI();
            IsNewGame = false;
        }
        else
        {
            ShowMainMenu();
        }
    }

    private void SubmitPlayerName()
    {
        string enteredName = playerNameInputField.text.Trim();

        if (!string.IsNullOrEmpty(enteredName))
        {
            PlayerID = enteredName;
            PlayerPrefs.SetString(playerIDKey, PlayerID);
            PlayerPrefs.Save();

            playerInputPanel.SetActive(false);
            playerNameText.text = $"Player: {PlayerID}";
            ShowMainMenu();
        }
    }

    // UI navigation methods
    public void ShowMainMenu()
    {
        mainMenuUI.SetActive(true);
        scanUI.SetActive(false);
        inputUI.SetActive(false);
        settingsUI.SetActive(false);
    }

    public void ShowScanUI()
    {
        mainMenuUI.SetActive(false);
        scanUI.SetActive(true);
        inputUI.SetActive(false);
        settingsUI.SetActive(false);
    }

    public void ShowInputUI()
    {
        scanUI.SetActive(false);
        inputUI.SetActive(true);
    }

    public void ShowSettingsUI()
    {
        accessingSettingsFromMainMenu = mainMenuUI.activeSelf;

        mainMenuUI.SetActive(false);
        scanUI.SetActive(false);
        inputUI.SetActive(false);
        settingsUI.SetActive(true);
    }

    public void BackFromSettings()
    {
        if (accessingSettingsFromMainMenu)
            ShowMainMenu();
        else
            ShowScanUI();
    }

    public void BackFromInput()
    {
        ShowScanUI();
    }

    public async void ContinueGame()
    {
        if (string.IsNullOrEmpty(PlayerID))
            return;

        Debug.Log("Continue Game button clicked");
        LoadProgress(); 
        await LoadDataFromCloud(); // now includes PlayerPrefs sync

        // Don't call LoadProgress() here anymore
        ShowScanUI();
    }


    public void GoToMainMenu()
    {
        SaveProgress();
        ShowMainMenu();
    }

    // Settings
    private void InitializeSettings()
    {
        hapticIntensity = PlayerPrefs.GetFloat("HapticIntensity", 1.0f);
        audioVolume = PlayerPrefs.GetFloat("AudioVolume", 1.0f);

        hapticSlider.value = hapticIntensity;
        audioSlider.value = audioVolume;

        hapticSlider.onValueChanged.RemoveAllListeners();
        audioSlider.onValueChanged.RemoveAllListeners();

        hapticSlider.onValueChanged.AddListener(UpdateHapticIntensity);
        audioSlider.onValueChanged.AddListener(UpdateAudioVolume);

        AdjustGameAudio();
    }

    public void UpdateHapticIntensity(float value)
    {
        hapticIntensity = value;
        PlayerPrefs.SetFloat("HapticIntensity", hapticIntensity);
        PlayerPrefs.Save();
    }

    public void UpdateAudioVolume(float value)
    {
        audioVolume = value;
        PlayerPrefs.SetFloat("AudioVolume", audioVolume);
        PlayerPrefs.Save();
        AdjustGameAudio();
    }

    private void AdjustGameAudio()
    {
        AudioSource[] audioSources = FindObjectsOfType<AudioSource>();
        foreach (var source in audioSources)
        {
            source.volume = audioVolume;
        }
    }

    // Game Data Management
    public void SaveProgress()
    {
        PlayerPrefs.SetInt("CurrentQuestion", currentQuestion);
        PlayerPrefs.SetInt("Score", CurrentScore);
        PlayerPrefs.SetInt("ColoredPointsPurchased", ColoredPointsPurchased ? 1 : 0);
        PlayerPrefs.SetInt("Coins", Coins);
        PlayerPrefs.SetFloat("HapticIntensity", hapticIntensity);
        PlayerPrefs.SetFloat("AudioVolume", audioVolume);
        PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        PlayerPrefs.SetInt("CorrectAnswers", CorrectAnswers);
        PlayerPrefs.SetInt("WrongAnswers", WrongAnswers);


        PlayerPrefs.Save();
        SaveDataToCloud();
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        SaveProgress();
    }

    public void AddCoins(int amount)
    {
        Coins += amount;
        SaveProgress();
    }

    public bool SpendCoins(int amount)
    {
        if (Coins >= amount)
        {
            Coins -= amount;
            StageManager.Instance.UpdateCoinsUI();
            SaveProgress();
            return true;
        }
        return false;
    }


    private void LoadProgress()
    {
        currentQuestion = PlayerPrefs.GetInt("CurrentQuestion", 0);
        CurrentScore = PlayerPrefs.GetInt("Score", 0);
        ColoredPointsPurchased = PlayerPrefs.GetInt("ColoredPointsPurchased", 0) == 1;
        Coins = PlayerPrefs.GetInt("Coins", 0);
        hapticIntensity = PlayerPrefs.GetFloat("HapticIntensity", 1.0f);
        currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        audioVolume = PlayerPrefs.GetFloat("AudioVolume", 1.0f);
        CorrectAnswers = PlayerPrefs.GetInt("CorrectAnswers", 0);
        WrongAnswers = PlayerPrefs.GetInt("WrongAnswers", 0);
        AdjustGameAudio();
    }
    public async void SaveDataToCloud()
    {
        var data = new Dictionary<string, object>
          {
        { "nickname", PlayerID },
        { "currentLevel", currentLevel },
        { "correctAnswers", CorrectAnswers },
        { "wrongAnswers", WrongAnswers },
        { "score", CurrentScore }
         };

        await CloudSaveService.Instance.Data.ForceSaveAsync(data);
    }
    public async Task LoadDataFromCloud()
    {
        var data = await CloudSaveService.Instance.Data.LoadAllAsync();

        if (string.IsNullOrEmpty(PlayerID) && data.TryGetValue("nickname", out var name))
        {
            PlayerID = name.ToString();
            PlayerPrefs.SetString(playerIDKey, PlayerID);
        }

        if (data.TryGetValue("currentLevel", out var level))
        {
            currentLevel = Convert.ToInt32(level);
            PlayerPrefs.SetInt("CurrentLevel", currentLevel);
        }

        if (data.TryGetValue("correctAnswers", out var correct))
        {
            CorrectAnswers = Convert.ToInt32(correct);
            PlayerPrefs.SetInt("CorrectAnswers", CorrectAnswers);
        }

        if (data.TryGetValue("wrongAnswers", out var wrong))
        {
            WrongAnswers = Convert.ToInt32(wrong);
            PlayerPrefs.SetInt("WrongAnswers", WrongAnswers);
        }

        if (data.TryGetValue("score", out var score))
        {
            CurrentScore = Convert.ToInt32(score);
            PlayerPrefs.SetInt("Score", CurrentScore);
        }

        PlayerPrefs.Save(); //Save all loaded cloud data to local prefs
        PlayerPrefs.Save();
        IsCloudDataLoaded = true;

    }


    public void StartNewGame()
    {
        Debug.Log("Start New Game button clicked");

        ResetGameDataButKeepPlayerID();

        currentLevel = 1;
        currentQuestion = 0;
        CurrentScore = 0;
        Coins = 0;
        ColoredPointsPurchased = false;

        PlayerPrefs.SetInt("Level2PopupShown", 0);
        PlayerPrefs.Save();

        SaveProgress();

        IsNewGame = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void ResetGameDataButKeepPlayerID()
    {
        string savedPlayerID = PlayerPrefs.GetString(playerIDKey, "");
        PlayerPrefs.DeleteAll();
        PlayerPrefs.SetString(playerIDKey, savedPlayerID);
        PlayerPrefs.SetInt("Level2PopupShown", 0);
        PlayerPrefs.Save();
    }

    public void AdvanceQuestion()
    {
        currentQuestion++;

        if (currentQuestion >= LevelQue)
        {
            AnalyticsManager.Instance?.LogLevelComplete(currentLevel,CorrectAnswers,WrongAnswers,CurrentScore);
            currentLevel++;
            AnalyticsManager.Instance?.LogLevelStart(currentLevel);
            currentQuestion = 0;

            if (currentLevel == 2 && PlayerPrefs.GetInt("Level2PopupShown", 0) == 0)
            {
                PlayerPrefs.SetInt("Level2PopupShown", 1);
                PlayerPrefs.Save();

                if (StageManager.Instance != null)
                {
                    StageManager.Instance.ShowLevelPopup();
                }
            }
        }

        SaveProgress();
    }

    public void OnCorrect()
    {
        CorrectAnswers++;
        AnalyticsManager.Instance?.LogQuestionAnswered(currentLevel,currentQuestion,true);
        SaveProgress();
    }

    public void OnWrong()
    {
        AnalyticsManager.Instance?.LogQuestionAnswered(currentLevel, currentQuestion,false);

        WrongAnswers++;
        SaveProgress();
    }
}