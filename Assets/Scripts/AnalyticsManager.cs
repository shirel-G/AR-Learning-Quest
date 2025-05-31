using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Analytics;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AnalyticsManager : MonoBehaviour
{
    public static AnalyticsManager Instance;

    private async void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            await InitializeUnityServicesAsync();
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
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        AnalyticsService.Instance.StartDataCollection();
    }

    public void LogLevelStart(int level)
    {
        var customEvent = new CustomEvent("level_start")
        {
            { "level", level },
            { "playerID", GameManager.Instance.PlayerID }
        };
        AnalyticsService.Instance.RecordEvent(customEvent);
    }

    public void LogQuestionAnswered(int level, int questionNumber, bool isCorrect)
    {
        var customEvent = new CustomEvent("question_answered")
        {
            { "level", level },
            { "questionNumber", questionNumber },
            { "isCorrect", isCorrect },
            { "playerID", GameManager.Instance.PlayerID }
        };
        AnalyticsService.Instance.RecordEvent(customEvent);
    }

    public void LogLevelComplete(int level, int correctAnswers, int wrongAnswers, int score)
    {   
        var customEvent = new CustomEvent("level_complete")
        {
            { "level", level },
            { "correctAnswers", correctAnswers },
            { "wrongAnswers", wrongAnswers },
            { "score", score },
            { "playerID", GameManager.Instance.PlayerID }
        };
        AnalyticsService.Instance.RecordEvent(customEvent);
    }

    public void LogGameSessionStart()
    {
        var customEvent = new CustomEvent("game_session_start")
        {
            { "playerID", GameManager.Instance.PlayerID }
        };
        AnalyticsService.Instance.RecordEvent(customEvent);
    }

    public void LogGameSessionEnd(int totalScore)
    {
        var customEvent = new CustomEvent("game_session_end")
        {
            { "playerID", GameManager.Instance.PlayerID },
            { "totalScore", totalScore }
        };
        AnalyticsService.Instance.RecordEvent(customEvent);
    }
}
