using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private TMP_InputField slopeInputField;
    [SerializeField] private TMP_Text pointAText;
    [SerializeField] private TMP_Text pointBText;
    [SerializeField] private TMP_Text pointATextOUT;
    [SerializeField] private TMP_Text pointBTextOUT;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text coinsTextShop;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private GameObject levelPopup;
    [SerializeField] private GameObject nextPanel;

    [Header("Settings")]
    [SerializeField] private PointSelector pointSelector;

    private Vector2Int pointA;
    private Vector2Int pointB;

    private int attemptsRemaining = 3;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private IEnumerator Start()
    {
        // Wait until cloud data is fully loaded
        while (!GameManager.IsCloudDataLoaded)
            yield return null;

        // Now cloud data is available — use it safely
        UpdateLevelText();
        SetupStage();
        UpdateScoreUI();
        UpdateCoinsUI();

        if (GameManager.Instance.currentLevel == 2 && PlayerPrefs.GetInt("Level2PopupShown", 0) == 1)
        {
            ShowLevelPopup();
        }
    }


    private void SetupStage()
    {
        slopeInputField.gameObject.SetActive(false);
      //  checkAnswerButton.gameObject.SetActive(false);
        //feedbackText.text = "";
        nextPanel.SetActive(false);

        pointAText.text = "Point A:";
        pointBText.text = "Point B:";
        pointATextOUT.text = "Point A:";
        pointBTextOUT.text = "Point B:";

        SetupQuestion();
    }

    private void SetupQuestion()
    {
        int questionNumber = GameManager.Instance.currentQuestion + 1;
        int maxQuestions = GameManager.Instance.LevelQue;
        instructionText.text = $"Question {questionNumber}/{maxQuestions}: Select two points.";
      //  feedbackText.text = "";
        slopeInputField.text = "";
        slopeInputField.gameObject.SetActive(false);
       // checkAnswerButton.gameObject.SetActive(false);
        nextPanel.SetActive(false);
        pointSelector.ClearPoints();
    }

    public void ReceivePoints(System.Collections.Generic.List<GameObject> points)
    {
        if (points == null || points.Count != 2)
        {
            feedbackText.text = "Please select two points.";
            return;
        }

        Vector3 worldA = points[0].transform.position;
        Vector3 worldB = points[1].transform.position;

        Vector3 localA = pointSelector.OriginTransform.InverseTransformPoint(worldA);
        Vector3 localB = pointSelector.OriginTransform.InverseTransformPoint(worldB);

        pointA = new Vector2Int(Mathf.RoundToInt(localA.x), Mathf.RoundToInt(localA.y));
        pointB = new Vector2Int(Mathf.RoundToInt(localB.x), Mathf.RoundToInt(localB.y));

        pointAText.text = $"Point A: ({pointA.x}, {pointA.y})";
        pointBText.text = $"Point B: ({pointB.x}, {pointB.y})";


        pointATextOUT.text = $"Point A: ({pointA.x}, {pointA.y})";
        pointBTextOUT.text = $"Point B: ({pointB.x}, {pointB.y})";

        instructionText.text = $"What is the slope between A({pointA.x},{pointA.y}) and B({pointB.x},{pointB.y})?";
        slopeInputField.gameObject.SetActive(true);
      //  checkAnswerButton.gameObject.SetActive(true);
        nextPanel.SetActive(true);
       // feedbackText.text = "";
        attemptsRemaining = 3;
    }

    public void CheckAnswer()
    {
        if (GameManager.Instance.currentLevel == 1 && slopeInputField.text.Contains("."))
        {
            feedbackText.text = "Enter a whole number only (no decimals).";
            return;
        }

        if (!float.TryParse(slopeInputField.text, out float studentSlope))
        {
            feedbackText.text = "Please enter a valid number.";
            return;
        }

        int dx = pointB.x - pointA.x;
        int dy = pointB.y - pointA.y;

        if (dx == 0)
        {
            feedbackText.text = "Vertical slope (undefined).";
            return;
        }

        float preciseSlope = (float)dy / dx;
        float actualSlope = GameManager.Instance.currentLevel == 1
                          ? Mathf.Round(preciseSlope)
                          : Mathf.Round(preciseSlope * 100) / 100;

        if (Mathf.Approximately(studentSlope, actualSlope))
        {
            feedbackText.text = "Correct!";
            int reward = GameManager.Instance.currentLevel == 1 ? 10 : 20;
            GameManager.Instance.AddScore(reward);
            GameManager.Instance.AddCoins(reward);
            GameManager.Instance.OnCorrect();
            UpdateScoreUI();
            UpdateCoinsUI();
            NextQuestion();
            
        }
        else
        {
            attemptsRemaining--;
            if (attemptsRemaining > 0)
            {
                feedbackText.text = $"Incorrect. Try again ({attemptsRemaining} attempts left).";
            }
            else
            {
                GameManager.Instance.OnWrong();
                feedbackText.text = $"Wrong! The correct slope was {actualSlope}.";
                NextQuestion();
                
            }
        }
    }

    private void NextQuestion()
    {
        slopeInputField.text = "";
        slopeInputField.gameObject.SetActive(false);
      //  checkAnswerButton.gameObject.SetActive(false);
        nextPanel.SetActive(false);
        pointSelector.ClearPoints();

        GameManager.Instance.AdvanceQuestion();
        GameManager.Instance.SaveProgress();
        UpdateLevelText();
        SetupQuestion();
    }

    public void ShowLevelPopup()
    {
        if (levelPopup != null)
        {
            levelPopup.SetActive(true);
        }
    }

    public void CloseLevelPopup()
    {
        if (levelPopup != null)
        {
            levelPopup.SetActive(false);
        }
    }

    private void UpdateLevelText()
    {
        levelText.text = $"Level {GameManager.Instance.currentLevel}";
    }

    private void UpdateScoreUI()
    {
        scoreText.text = $"Score: {GameManager.Instance.CurrentScore}";
    }

    public void UpdateCoinsUI()
    {
        coinsText.text = $"{GameManager.Instance.Coins}";
        coinsTextShop.text = $"{GameManager.Instance.Coins}";
    }

    public void OnBackToScan()
    {
        GameManager.Instance.BackFromInput();
    }
}
