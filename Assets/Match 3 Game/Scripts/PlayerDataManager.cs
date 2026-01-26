using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using UnityEngine.UIElements;



[Serializable]
public class LevelListWrapper
{
    public List<LevelInfo> Levels;
}


public class PlayerDataManager : MonoBehaviour
{
    public PlayerData playerData;

    public bool isLaunched = false; // Flag to check if the game has been launched
    public bool isFoundName = false; // Flag to check if player name is found
    public bool isOnline = false; // Flag to check if online mode is active

    public bool isNameSame = false; // Flag to check if playerData name is same as PlayFabManager player name

    public string PlayFabPlayerID; // PlayFab Player ID
    public string PlayFabPlayerName; // PlayFab Player Name

    private StageManager stageManager;



    public int currentLevel = 1; // 🔥 Universal current level tracker

    public static PlayerDataManager Instance { get; private set; }

    private const int ENERGY_REGEN_MINUTES = 59; // Time to regenerate 1 energy
    private Coroutine energyRegenCoroutine;

    private Coroutine internetCheckCoroutine;
    private const float INTERNET_CHECK_INTERVAL = 3f; // Check every 5 seconds



    public int TotalXP
    {
        get { return playerData != null ? playerData.Levels.Sum(l => l.XP) : 0; }
    }
    public int TotalStars
    {
        get { return playerData != null ? playerData.Levels.Sum(l => l.Stars) : 0; }
    }


    #region "Offline JSON"
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist between scenes
        }
        else
        {
            Destroy(gameObject); // Avoid duplicates
        }
        //CheckForOnline();

        stageManager = FindObjectOfType<StageManager>();

        

    }

    void Start()
    {
        LoginAsGuest();

        if(isOnline)
        {
            //LoadPlayerData();
        }

        stageManager = FindObjectOfType<StageManager>();


        SavePlayerData();
        GetCurrentLevel(); // Initialize current level from player data

        // ✅ NEW: Calculate offline energy regeneration
        CalculateOfflineEnergyRegen();

        // ✅ NEW: Start continuous energy regeneration coroutine
        energyRegenCoroutine = StartCoroutine(EnergyRegenCoroutine());

        // ✅ NEW: Start internet connection check coroutine
        internetCheckCoroutine = StartCoroutine(InternetCheckCoroutine());

    }

    /*private string XorEncryptDecrypt(string data, string key = "Heil")
    {
        char[] result = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (char)(data[i] ^ key[i % key.Length]);
        }
        return new string(result);
    }*/


    /// <summary>
    /// Continuously checks internet connection every 5 seconds
    /// </summary>
    private IEnumerator InternetCheckCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(INTERNET_CHECK_INTERVAL);

            CheckInternetConnection();
        }
    }

    

    public void CreateNewPlayer(string name, string playerId)
    {
        playerData = new PlayerData
        {
            Name = name,
            PlayerID = playerId,
            PlayerBombAbilityCount = 20,
            PlayerColorBombAbilityCount = 20,
            PlayerExtraMoveAbilityCount = 20,
            PlayerShuffleAbilityCount = 3,
            CurrentLevelId = 1,
            EnergyCount = 5,
            Levels = new List<LevelInfo>()
        {
            new LevelInfo { LevelID = 1, Stars = 0, XP = 0, LevelLocked = 0 },
            new LevelInfo { LevelID = 2, Stars = 0, XP = 0, LevelLocked = 1 },
            new LevelInfo { LevelID = 3, Stars = 0, XP = 0, LevelLocked = 1 }
        }
        };

        if (isOnline) // 🔥 Only send if online
        {
            SendPlayerDataToPlayFab();
        }
    }

    public void GetCurrentLevel()
    {
        if (playerData != null)
        {
            currentLevel = playerData.CurrentLevelId;
            Debug.Log("Current Level: " + currentLevel);
        }
        else
        {
            Debug.LogWarning("Player data is null, cannot get current level.");
        }
    }

    /*public void SavePlayerData()
    {
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Player data saved: " + savePath);

        SendPlayerDataToPlayFab(); // Send data to PlayFab after saving locally

    }

    public void SavePlayerData()
    {
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
        Debug.Log("Player data saved locally: " + savePath);

        if (isOnline) // 🔥 Only send to PlayFab if online
        {
            SendPlayerDataToPlayFab();
        }
    }*/

    public void SavePlayerData()
    {
        

        if (isOnline)
        {
            SendPlayerDataToPlayFab();
        }
    }



    /*public void LoadPlayerData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            playerData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("Player data loaded.");
            GetCurrentLevel(); // Initialize current level from loaded data
        }
        else
        {
            Debug.LogWarning("Save file not found, creating new player...");
            CreateNewPlayer("Temp", Guid.NewGuid().ToString());
            SavePlayerData();
            GetCurrentLevel(); // Initialize current level after creating new player

        }
    }*/

    public void LoadPlayerData()
    {
        
        //load data from PlayFab if online
        if (isOnline)
        {
            PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
            {
                if (result.Data != null && result.Data.ContainsKey("Levels"))
                {
                    string levelsJson = result.Data["Levels"].Value;
                    playerData = new PlayerData
                    {
                        Name = result.Data.ContainsKey("PlayerName") ? result.Data["PlayerName"].Value : "Temp",
                        PlayerID = result.Data.ContainsKey("PlayerID") ? result.Data["PlayerID"].Value : Guid.NewGuid().ToString(),
                        CurrentLevelId = result.Data.ContainsKey("CurrentLevelId") ? int.Parse(result.Data["CurrentLevelId"].Value) : 1,
                        PlayerBombAbilityCount = result.Data.ContainsKey("PlayerBombAbilityCount") ? int.Parse(result.Data["PlayerBombAbilityCount"].Value) : 20,
                        PlayerColorBombAbilityCount = result.Data.ContainsKey("PlayerColorBombAbilityCount") ? int.Parse(result.Data["PlayerColorBombAbilityCount"].Value) : 20,
                        PlayerExtraMoveAbilityCount = result.Data.ContainsKey("PlayerExtraMoveAbilityCount") ? int.Parse(result.Data["PlayerExtraMoveAbilityCount"].Value) : 20,
                        PlayerShuffleAbilityCount = result.Data.ContainsKey("PlayerShuffleAbilityCount") ? int.Parse(result.Data["PlayerShuffleAbilityCount"].Value) : 3,
                        EnergyCount = result.Data.ContainsKey("PlayerEnergyCount") ? int.Parse(result.Data["PlayerEnergyCount"].Value) : 5,
                        Levels = JsonUtility.FromJson<LevelListWrapper>(levelsJson).Levels
                    };
                    Debug.Log("Player data loaded from PlayFab.");
                    GetCurrentLevel();
                }
                else
                {
                    Debug.LogWarning("No data found on PlayFab, creating new player...");
                    CreateNewPlayer("Temp", Guid.NewGuid().ToString());
                    SavePlayerData();
                    GetCurrentLevel();
                }
            }, OnError);
        }
        else
        {
            Debug.LogWarning("Offline mode: Cannot load data from PlayFab.");
        }



    }



    //if playerData name is not matched with PlayFabManager player name, then set PlayfabManager player name to playerData name


    // 🔥 Set level stars and XP
    public void SetLevelStars(int levelId, int stars, int xp)
    {
        if (stars < 0) stars = 0;
        if (stars > 3) stars = 3;

        LevelInfo level = playerData.Levels.Find(l => l.LevelID == levelId);
        if (level == null)
        {
            level = new LevelInfo { LevelID = levelId, Stars = stars, XP = xp, LevelLocked = 1 }; // Default locked
            playerData.Levels.Add(level);
        }
        else
        {
            if (stars > level.Stars) level.Stars = stars;
            level.XP += xp;
        }

        //playerData.TotalXP += xp;
        Debug.Log($"Level {levelId} updated: Stars={stars}, XP={xp}");
    }

    public void SetLevelLocked(int levelId, int lockedValue)
    {
        if (lockedValue != 0 && lockedValue != 1)
        {
            Debug.LogWarning("Locked value must be 0 (unlocked) or 1 (locked).");
            return;
        }

        LevelInfo level = playerData.Levels.Find(l => l.LevelID == levelId);
        if (level == null)
        {
            // If level doesn't exist, create it with 0 stars and locked state
            level = new LevelInfo { LevelID = levelId, Stars = 0, XP = 0, LevelLocked = lockedValue };
            playerData.Levels.Add(level);
        }
        else
        {
            level.LevelLocked = lockedValue;
        }

        Debug.Log($"Level {levelId} lock state changed to: {(lockedValue == 1 ? "Locked" : "Unlocked")}");
    }

    // 🔥 NEW - Set universal current level
    public void SetCurrentLevel(int levelId)
    {
        playerData.CurrentLevelId = levelId;
        Debug.Log($"Current Level set to: {levelId}");
    }

    // Ability setters
    /*public void SetName(string newName)
    {
        playerData.Name = newName;
        SetUserName(newName);
    }*/

    public void SetName(string newName)
    {
        playerData.Name = newName;
        if (isOnline) // 🔥 Only update PlayFab if online
        {
            SetUserName(newName);
        }
    }

    //On Name Update debug log the new name
    void OnUpdateUserNameSuccess(UpdateUserTitleDisplayNameResult result)
    {
        PlayFabPlayerName = result.DisplayName;
        Debug.Log("User name updated to: " + PlayFabPlayerName);

        stageManager.UserNameUpdated();
    }



    public void SetPlayerID(string newPlayerID)
    {
        playerData.PlayerID = newPlayerID;
    }

    public void SetPlayerBombAbilityCount(int count)
    {
        playerData.PlayerBombAbilityCount = count;
    }

    public void SetPlayerColorBombAbilityCount(int count)
    {
        playerData.PlayerColorBombAbilityCount = count;
    }

    public void SetPlayerExtraMoveAbilityCount(int count)
    {
        playerData.PlayerExtraMoveAbilityCount = count;
    }

    //set shuffle ability count
    public void SetPlayerShuffleAbilityCount(int count)
    {
        playerData.PlayerShuffleAbilityCount = count;
    }

    public void SetAllData(int levelId, int lockedValue, int stars, int xp)
    {
        SetLevelLocked(levelId, lockedValue);
        SetLevelStars(levelId, stars, xp);
    }

    //send xp of specific level
    public void SendXP(int levelId, int xp)
    {
        LevelInfo level = playerData.Levels.Find(l => l.LevelID == levelId);
        if (level != null)
        {
            level.XP = xp;
            //playerData.TotalXP += xp;
            Debug.Log($"XP for Level {levelId} updated: {xp}");
        }
        else
        {
            Debug.LogWarning($"Level {levelId} not found to send XP.");
        }
    }


    public void AddColorBombAbility(int count)
    {
        playerData.PlayerColorBombAbilityCount += count;
        //Debug.Log($"Added {count} Color Bombs. Total: {playerData.PlayerColorBombAbilityCount}");
    }
    public void AddBombAbility(int count)
    {
        playerData.PlayerBombAbilityCount += count;
        //Debug.Log($"Added {count} Bombs. Total: {playerData.PlayerBombAbilityCount}");
    }
    public void AddExtraMoveAbility(int count)
    {
        playerData.PlayerExtraMoveAbilityCount += count;
        //Debug.Log($"Added {count} Extra Moves. Total: {playerData.PlayerExtraMoveAbilityCount}");
    }

    //add shuffle ability
    public void AddShuffleAbility(int count)
    {
        playerData.PlayerShuffleAbilityCount += count;
        //Debug.Log($"Added {count} Shuffles. Total: {playerData.PlayerShuffleAbilityCount}");
    }


    #endregion


    #region "PlayFab Integration"
    public void LoginAsGuest()
    {
        var request = new LoginWithCustomIDRequest
        {
            CustomId = SystemInfo.deviceUniqueIdentifier,
            CreateAccount = true
        };
        PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);

        //GetUserName();
    }
    /*void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successfully logged in as guest");
        // Handle successful login

        PlayFabPlayerID = result.PlayFabId;
        Invoke("CheckAndSetPlayerName", 2f); // Delay to ensure playerName is set after login
        isLaunched = true;
        isOnline = true;
    }*/

    void OnLoginSuccess(LoginResult result)
    {
        Debug.Log("Successfully logged in as guest");

        PlayFabPlayerID = result.PlayFabId;

        //Invoke("CheckAndSetPlayerName", 2f);
        isLaunched = true;
        isOnline = true;

        // 🔥 When login succeeds, always sync local JSON data
        //LoadPlayerData();
        SendPlayerDataToPlayFab();
        CheckAndSetPlayerName();

        //CheckAndShowNamePanel();

        //get player display name and set PlayFabPlayerName
        var getRequest = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(getRequest, result =>
        {
            PlayFabPlayerName = result.AccountInfo.TitleInfo.DisplayName;
            Debug.Log("Fetched PlayFab player name: " + PlayFabPlayerName);
            CheckAndSetPlayerName(); // Ensure names are checked after fetching
        }, OnError);
    }


    

    void OnError(PlayFabError error)
    {
        Debug.LogError("Error during PlayFab operation: " + error.GenerateErrorReport());
        // Handle error
        isOnline = false;
    }

    

    //get user name. if user name not found, then loadscene.isFoundName = false;

    // Set user name using PlayFab API from another script (e.g., loadscene.cs)
    public void SetUserName(string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var request = new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = name
            };
            PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnUpdateUserNameSuccess, OnError);
        }
        else
        {
            Debug.LogError("User name input is empty");
        }
    }


    /*void OnUpdateUserNameSuccess(UpdateUserTitleDisplayNameResult result)
    {
        PlayFabPlayerName = result.DisplayName;
        Debug.Log("User name updated to: " + PlayFabPlayerName);
        
    }*/


    // send player level data and PlayerExtraMoveAbilityCount, PlayerColorBombAbilityCount, PlayerBombAbilityCount to PlayFab
    /*public void SendPlayerDataToPlayFab()
    {
        if (playerData == null)
        {
            Debug.LogError("Player data is null, cannot send to PlayFab.");
            return;
        }

        // 🔥 Serialize levels list to JSON
        string levelsJson = JsonUtility.ToJson(new LevelListWrapper { Levels = playerData.Levels }, true);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "PlayerName", playerData.Name },
            { "PlayerID", playerData.PlayerID },
            { "CurrentLevelId", playerData.CurrentLevelId.ToString() },
            { "PlayerBombAbilityCount", playerData.PlayerBombAbilityCount.ToString() },
            { "PlayerColorBombAbilityCount", playerData.PlayerColorBombAbilityCount.ToString() },
            { "PlayerExtraMoveAbilityCount", playerData.PlayerExtraMoveAbilityCount.ToString() },
            { "Levels", levelsJson }
        }
        };

        PlayFabClientAPI.UpdateUserData(request, OnUpdateUserDataSuccess, OnError);
    }*/

    public void SendPlayerDataToPlayFab()
    {
        if (!isOnline)
        {
            Debug.Log("Offline mode: Skipping PlayFab upload.");
            return;
        }

        if (playerData == null)
        {
            Debug.LogError("Player data is null, cannot send to PlayFab.");
            return;
        }

        string levelsJson = JsonUtility.ToJson(new LevelListWrapper { Levels = playerData.Levels }, true);

        var request = new UpdateUserDataRequest
        {
            Data = new Dictionary<string, string>
        {
            { "PlayerName", playerData.Name },
            { "PlayerID", playerData.PlayerID },
            { "CurrentLevelId", playerData.CurrentLevelId.ToString() },
            { "PlayerBombAbilityCount", playerData.PlayerBombAbilityCount.ToString() },
            { "PlayerColorBombAbilityCount", playerData.PlayerColorBombAbilityCount.ToString() },
            { "PlayerExtraMoveAbilityCount", playerData.PlayerExtraMoveAbilityCount.ToString() },
            { "PlayerShuffleAbilityCount", playerData.PlayerShuffleAbilityCount.ToString() },
            { "PlayerEnergyCount", playerData.EnergyCount.ToString() },
            { "Levels", levelsJson }
        }
        };

        PlayFabClientAPI.UpdateUserData(request, OnUpdateUserDataSuccess, OnError);
    }


    void OnUpdateUserDataSuccess(UpdateUserDataResult result)
    {
        Debug.Log("Player data sent to PlayFab successfully.");
    }

    //if playerData name is not matched with PlayFabManager player name, then set PlayfabManager player name to playerData name also isFoundName = false;
    public void CheckAndSetPlayerName()
    {
        // 🔥 Online-only feature - ignore offline mode entirely
        if (!isOnline)
        {
            isFoundName = false;
            Debug.Log("Offline mode → isFoundName = false (PlayFab name check requires online connection)");
            return;
        }

        // 🔥 Check only PlayFab name, ignore local JSON name
        if (string.IsNullOrEmpty(PlayFabPlayerName))
        {
            isFoundName = false;
            Debug.Log("PlayFab player name is empty or null → isFoundName = false");
        }
        else
        {
            isFoundName = true;
            Debug.Log($"PlayFab name found: '{PlayFabPlayerName}' → isFoundName = true");

            // Optional: Update local JSON with PlayFab name for display purposes only
            if (playerData.Name != PlayFabPlayerName)
            {
                playerData.Name = PlayFabPlayerName;
                SavePlayerData();
                Debug.Log($"Local name updated to match PlayFab: {PlayFabPlayerName}");
            }
        }
    }



    /*public void SendLeaderboard(int TotalXP)
    {
        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
            {
                new StatisticUpdate
                {
                    StatisticName = "XP",
                    Value = TotalXP
                }
            }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnLeaderboardUpdate, OnError);
    }
    
     
     
     public void GetLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "XP",
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
        //ShowNameOnLeaderboard();
    }*/


    public void SendLeaderboard(int TotalXP)
    {
        if (!isOnline)
        {
            Debug.Log("Offline mode: Skipping leaderboard update.");
            return;
        }

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate>
        {
            new StatisticUpdate
            {
                StatisticName = "XP",
                Value = TotalXP
            }
        }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnLeaderboardUpdate, OnError);
    }

    public void GetLeaderboard()
    {
        if (!isOnline)
        {
            Debug.Log("Offline mode: Leaderboard not available.");
            return;
        }

        var request = new GetLeaderboardRequest
        {
            StatisticName = "XP",
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
    }

    void OnLeaderboardUpdate(UpdatePlayerStatisticsResult result)
    {
        Debug.Log("Successfully updated leaderboard");
    }

    

    public void OnLeaderboardGet(GetLeaderboardResult result)
    {

        //Show leaderboard entries
        if (result.Leaderboard != null && result.Leaderboard.Count > 0)
        {
            foreach (var entry in result.Leaderboard)
            {
                Debug.Log($"Rank: {entry.Position + 1}, Player: {entry.DisplayName}, XP: {entry.StatValue}");
            }
        }
        else
        {
            Debug.Log("No leaderboard entries found.");
        }

    }

    void CheckForOnline()
    {
        PlayFabClientAPI.GetTitleData(
            new GetTitleDataRequest(),
            OnSuccess,
            OnError
        );
        
    }

    

    void OnSuccess(GetTitleDataResult result)
    {
        isOnline = true;
        Debug.Log("Online mode active. Syncing local data to PlayFab...");

        // 🔥 Sync latest local data to PlayFab
        //LoadPlayerData();  // Make sure JSON is loaded
        SendPlayerDataToPlayFab();
    }



    #endregion

    //send xp of specific level
    /*public void SendXP(int levelId, int xp)
    {
        LevelInfo level = playerData.Levels.Find(l => l.LevelID == levelId);
        if (level != null)
        {
            level.XP += xp;
            playerData.TotalXP += xp;
            Debug.Log($"XP for Level {levelId} updated: {xp}, Total XP: {playerData.TotalXP}");
        }
        else
        {
            Debug.LogWarning($"Level {levelId} not found to send XP.");
        }
    }*/

    

    public void SendBombAbility(int bombCount)
    {
        playerData.PlayerBombAbilityCount = bombCount;
        Debug.Log($"Bomb Ability Count updated: {bombCount}");
    }

    public void SendColorBombAbility(int colorBombCount)
    {
        playerData.PlayerColorBombAbilityCount = colorBombCount;
        Debug.Log($"Color Bomb Ability Count updated: {colorBombCount}");
    }

    public void SendExtraMoveAbility(int extraMoveCount)
    {
        playerData.PlayerExtraMoveAbilityCount = extraMoveCount;
        Debug.Log($"Extra Move Ability Count updated: {extraMoveCount}");
    }



    public void SetEnergyLevel(int energyCount)
    {
        playerData.EnergyCount = energyCount;
        Debug.Log($"Energy Count updated: {energyCount}");
        //Send to PlayFab if online
        if (isOnline)
        {
            SendPlayerDataToPlayFab();
        }

        //save to local json
        SavePlayerData();
    }

    public void AddEnergy(int amount)
    {
        playerData.EnergyCount += amount;
        Debug.Log($"Added {amount} energy. Total Energy: {playerData.EnergyCount}");
        // Send to PlayFab if online
        if (isOnline)
        {
            SendPlayerDataToPlayFab();
        }
        // Save to local JSON
        SavePlayerData();
    }

    public void RemoveEnergy(int amount)
    {

        //if energy is less than 0 set to 0. else remove amount
        if (playerData.EnergyCount - amount < 0)
        {
            playerData.EnergyCount = 0;
        }
        else
        {
            playerData.EnergyCount -= amount;
        }


        //playerData.EnergyCount -= amount;

        // ✅ Update timestamp when energy is spent
        playerData.LastEnergyUpdateTime = GetCurrentUnixTime();

        Debug.Log($"Removed {amount} energy. Total Energy: {playerData.EnergyCount}");

        if (isOnline)
        {
            SendPlayerDataToPlayFab();
        }
        SavePlayerData();
    }

    public int GetEnergyCount()
    {
        return playerData.EnergyCount;
    }


    /// <summary>
    /// Calculates how much energy should be regenerated based on offline time
    /// </summary>
    private void CalculateOfflineEnergyRegen()
    {
        if (playerData.EnergyCount >= playerData.MaxEnergy)
        {
            Debug.Log("Energy is already full. No regeneration needed.");
            return;
        }

        long currentTime = GetCurrentUnixTime();
        long lastUpdateTime = playerData.LastEnergyUpdateTime;

        // First time setup
        if (lastUpdateTime == 0)
        {
            playerData.LastEnergyUpdateTime = currentTime;
            SavePlayerData();
            return;
        }

        long timeDifference = currentTime - lastUpdateTime;
        int minutesPassed = (int)(timeDifference / 60);

        if (minutesPassed >= ENERGY_REGEN_MINUTES)
        {
            int energyToAdd = minutesPassed / ENERGY_REGEN_MINUTES;
            int newEnergy = Mathf.Min(playerData.EnergyCount + energyToAdd, playerData.MaxEnergy);

            int actualEnergyAdded = newEnergy - playerData.EnergyCount;

            if (actualEnergyAdded > 0)
            {
                playerData.EnergyCount = newEnergy;

                // Update timestamp to account for regenerated energy
                long timeUsedForRegen = actualEnergyAdded * ENERGY_REGEN_MINUTES * 60;
                playerData.LastEnergyUpdateTime = lastUpdateTime + timeUsedForRegen;

                Debug.Log($"Offline energy regeneration: +{actualEnergyAdded} energy. Total: {playerData.EnergyCount}");
                SavePlayerData();

                if (isOnline)
                {
                    SendPlayerDataToPlayFab();
                }
            }
        }
    }

    /// <summary>
    /// Continuous energy regeneration while game is open
    /// </summary>
    private IEnumerator EnergyRegenCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); // Check every minute

            if (playerData.EnergyCount >= playerData.MaxEnergy)
            {
                continue; // Already at max, no need to regenerate
            }

            long currentTime = GetCurrentUnixTime();
            long timeDifference = currentTime - playerData.LastEnergyUpdateTime;
            int minutesPassed = (int)(timeDifference / 60);

            if (minutesPassed >= ENERGY_REGEN_MINUTES)
            {
                int energyToAdd = minutesPassed / ENERGY_REGEN_MINUTES;
                int newEnergy = Mathf.Min(playerData.EnergyCount + energyToAdd, playerData.MaxEnergy);

                int actualEnergyAdded = newEnergy - playerData.EnergyCount;

                if (actualEnergyAdded > 0)
                {
                    playerData.EnergyCount = newEnergy;

                    // Update timestamp
                    long timeUsedForRegen = actualEnergyAdded * ENERGY_REGEN_MINUTES * 60;
                    playerData.LastEnergyUpdateTime += timeUsedForRegen;

                    Debug.Log($"Energy regenerated: +{actualEnergyAdded}. Total: {playerData.EnergyCount}");
                    SavePlayerData();

                    if (isOnline)
                    {
                        SendPlayerDataToPlayFab();
                    }

                    // ✅ Update UI if StageManager exists
                    if (stageManager != null && stageManager.CurrentEnergyText != null)
                    {
                        stageManager.CurrentEnergyText.text = playerData.EnergyCount.ToString();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Gets time remaining until next energy regeneration (in seconds)
    /// </summary>
    public int GetTimeUntilNextEnergy()
    {
        if (playerData.EnergyCount >= playerData.MaxEnergy)
            return 0;

        long currentTime = GetCurrentUnixTime();
        long timeDifference = currentTime - playerData.LastEnergyUpdateTime;
        int secondsPassed = (int)timeDifference;

        int secondsPerEnergy = ENERGY_REGEN_MINUTES * 60;
        int secondsUntilNext = secondsPerEnergy - (secondsPassed % secondsPerEnergy);

        return secondsUntilNext;
    }

    /// <summary>
    /// Format time remaining as MM:SS
    /// </summary>
    public string GetFormattedTimeUntilNextEnergy()
    {
        int seconds = GetTimeUntilNextEnergy();
        int minutes = seconds / 60;
        int secs = seconds % 60;
        return $"{minutes:00}:{secs:00}";
    }

    private long GetCurrentUnixTime()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    private void OnDestroy()
    {
        // Stop coroutine when game closes
        if (energyRegenCoroutine != null)
        {
            StopCoroutine(energyRegenCoroutine);
        }
    }


    public void SkipEnergyGenerateTime()
    {

        // Fast forward last energy update time by 5 minutes
        playerData.LastEnergyUpdateTime -= ENERGY_REGEN_MINUTES * 60;
        SavePlayerData();
    }


    public void CheckInternetConnection()
    {
        PlayFabClientAPI.GetTitleData(
            new GetTitleDataRequest(),
            result =>
            {
                isOnline = true;
                Debug.Log("Internet connection verified.");
            },
            error =>
            {
                isOnline = false;
                Debug.LogWarning("No internet connection.");
            }
        );
    }

    //create a function to get GetPlayerBombAbilityCount
    public int GetPlayerBombAbilityCount()
    {
        return playerData.PlayerBombAbilityCount;
    }
    //create a function to get GetPlayerColorBombAbilityCount
    public int GetPlayerColorBombAbilityCount()
    {
        return playerData.PlayerColorBombAbilityCount;
    }
    //create a function to get GetPlayerExtraMoveAbilityCount
    public int GetPlayerExtraMoveAbilityCount()
    {
        return playerData.PlayerExtraMoveAbilityCount;
    }
    //create a function to get GetPlayerShuffleAbilityCount
    public int GetPlayerShuffleAbilityCount()
    {
        return playerData.PlayerShuffleAbilityCount;
    }




    /// <summary>
    /// Reconnects to PlayFab, syncs all data, and refreshes connection state
    /// Call this when internet connection is restored or when user manually retries
    /// </summary>
    public void ReconnectAndSyncPlayFab(System.Action onSuccess = null, System.Action onFailure = null)
    {

        LoginAsGuest();

        if (isOnline)
        {
            LoadPlayerData();
        }

        stageManager = FindObjectOfType<StageManager>();


        SavePlayerData();
        GetCurrentLevel(); // Initialize current level from player data

        // ✅ NEW: Calculate offline energy regeneration
        CalculateOfflineEnergyRegen();

        // ✅ NEW: Start continuous energy regeneration coroutine
        energyRegenCoroutine = StartCoroutine(EnergyRegenCoroutine());

        // ✅ NEW: Start internet connection check coroutine
        internetCheckCoroutine = StartCoroutine(InternetCheckCoroutine());

    }

    
}
