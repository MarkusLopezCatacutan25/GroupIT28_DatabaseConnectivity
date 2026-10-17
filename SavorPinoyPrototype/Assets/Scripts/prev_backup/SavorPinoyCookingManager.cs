// Historical backup preserved but excluded from Unity compilation.
#if false
using System;
using System.Collections.Generic;
using System.Linq;
using Firebase;
using Firebase.Database;
using Firebase.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavorPinoyCookingManager : MonoBehaviour
{
    [Header("Firebase Configuration")]
    [SerializeField] private string databaseUrl = "PASTE_YOUR_FIREBASE_REALTIME_DATABASE_URL_HERE";

    [Header("UI Inputs & Displays")]
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text potContentsText;
    [SerializeField] private TMP_Text leaderboardText;

    [Header("Ingredient Buttons (Adobo sa Dilaw)")]
    [SerializeField] private Button addTurmericBtn;
    [SerializeField] private Button addGarlicBtn;
    [SerializeField] private Button addMeatBtn;
    [SerializeField] private Button addWrongIngredientBtn;

    [Header("Action Buttons")]
    [SerializeField] private Button cookDishBtn;
    [SerializeField] private Button saveScoreBtn;
    [SerializeField] private Button loadLeaderboardBtn;

    private DatabaseReference databaseReference;
    private bool firebaseReady = false;
    private int currentScore = 0;
    private bool stageFinished = false;
    private List<string> potIngredients = new List<string>();

    private const string DISH_NAME = "Adobo sa Dilaw";
    private const string PROVINCE = "Batangas";

    private void Start()
    {
        currentScore = 0;
        stageFinished = false;
        potIngredients.Clear();

        UpdateScoreUI();
        UpdatePotUI();
        saveScoreBtn.interactable = false;
        SetStatus("Initializing Firebase...");

        addTurmericBtn.onClick.AddListener(() => AddIngredient("Luyang Dilaw (Turmeric)", 25));
        addGarlicBtn.onClick.AddListener(() => AddIngredient("Garlic", 25));
        addMeatBtn.onClick.AddListener(() => AddIngredient("Chicken/Pork", 25));
        addWrongIngredientBtn.onClick.AddListener(() => AddIngredient("Soy Sauce (Incorrect for Dilaw)", -10));

        cookDishBtn.onClick.AddListener(FinishCooking);
        saveScoreBtn.onClick.AddListener(SaveGameRecord);
        loadLeaderboardBtn.onClick.AddListener(LoadLeaderboardRecords);

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (string.IsNullOrWhiteSpace(databaseUrl) || databaseUrl.Contains("PASTE_YOUR"))
        {
            SetStatus("Error: Paste your Realtime Database URL in Inspector.");
            return;
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                databaseReference = FirebaseDatabase.GetInstance(FirebaseApp.DefaultInstance, databaseUrl).RootReference;
                firebaseReady = true;
                SetStatus("Connected to Firebase. Select authentic Batangas ingredients.");
            }
            else
            {
                SetStatus("Firebase dependency error: " + task.Result);
            }
        });
    }

    private void AddIngredient(string name, int scoreValue)
    {
        if (stageFinished) return;

        potIngredients.Add(name);
        currentScore = Mathf.Max(0, currentScore + scoreValue);
        UpdatePotUI();
        UpdateScoreUI();
        SetStatus($"Added {name} to pot.");
    }

    private void FinishCooking()
    {
        if (stageFinished) return;
        if (potIngredients.Count == 0)
        {
            SetStatus("Add ingredients before cooking!");
            return;
        }

        stageFinished = true;
        cookDishBtn.interactable = false;
        saveScoreBtn.interactable = true;

        if (potIngredients.Contains("Luyang Dilaw (Turmeric)") && !potIngredients.Contains("Soy Sauce (Incorrect for Dilaw)"))
        {
            currentScore += 25;
            SetStatus("Authentic Adobo sa Dilaw prepared! Click 'Save Score'.");
        }
        else
        {
            SetStatus("Dish prepared without regional authenticity. Click 'Save Score'.");
        }
        UpdateScoreUI();
    }

    private void SaveGameRecord()
    {
        if (!firebaseReady)
        {
            SetStatus("Database not ready.");
            return;
        }

        string playerName = playerNameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            SetStatus("Please enter your name before saving.");
            return;
        }

        saveScoreBtn.interactable = false;
        SetStatus("Saving record...");

        string remarks = currentScore >= 80 ? "Master Heritage Chef" : "Novice Cook";
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        PlayerScoreRecord record = new PlayerScoreRecord(playerName, currentScore, DISH_NAME, PROVINCE, remarks, timestamp);
        string json = JsonUtility.ToJson(record);
        string key = databaseReference.Child("player_scores").Push().Key;

        databaseReference.Child("player_scores").Child(key).SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                saveScoreBtn.interactable = true;
                if (task.IsFaulted)
                {
                    SetStatus("Save failed. Check rules & connectivity.");
                    return;
                }

                SetStatus("Score saved successfully!");
                LoadLeaderboardRecords();
            });
    }

    private void LoadLeaderboardRecords()
    {
        if (!firebaseReady)
        {
            SetStatus("Database not ready.");
            return;
        }

        leaderboardText.text = "Fetching records...";
        loadLeaderboardBtn.interactable = false;

        databaseReference.Child("player_scores")
            .OrderByChild("score")
            .LimitToLast(5)
            .GetValueAsync()
            .ContinueWithOnMainThread(task =>
            {
                loadLeaderboardBtn.interactable = true;
                if (task.IsFaulted)
                {
                    leaderboardText.text = "Failed to load leaderboard.";
                    return;
                }

                DataSnapshot snapshot = task.Result;
                if (!snapshot.Exists)
                {
                    leaderboardText.text = "No saved records found.";
                    return;
                }

                List<PlayerScoreRecord> recordsList = new List<PlayerScoreRecord>();
                foreach (DataSnapshot child in snapshot.Children)
                {
                    string pName = child.Child("player_name").Value?.ToString() ?? "Unknown";
                    int pScore = int.TryParse(child.Child("score").Value?.ToString(), out int s) ? s : 0;
                    string pDish = child.Child("dish_name").Value?.ToString() ?? "";
                    string pLevel = child.Child("level").Value?.ToString() ?? "";
                    string pRemarks = child.Child("remarks").Value?.ToString() ?? "";
                    string pDate = child.Child("created_at").Value?.ToString() ?? "";

                    recordsList.Add(new PlayerScoreRecord(pName, pScore, pDish, pLevel, pRemarks, pDate));
                }

                recordsList = recordsList.OrderByDescending(r => r.score).ToList();

                string displayOutput = "=== REGIONAL LEADERBOARD ===\n";
                for (int i = 0; i < recordsList.Count; i++)
                {
                    displayOutput += $"{i + 1}. {recordsList[i].player_name} - {recordsList[i].score} pts ({recordsList[i].dish_name})\n";
                }

                leaderboardText.text = displayOutput;
                SetStatus("Leaderboard refreshed.");
            });
    }

    private void UpdateScoreUI() => scoreText.text = $"Score: {currentScore}";
    private void UpdatePotUI() => potContentsText.text = "Pot: " + (potIngredients.Count > 0 ? string.Join(", ", potIngredients) : "[Empty]");
    private void SetStatus(string msg) => statusText.text = msg;
}
#endif
