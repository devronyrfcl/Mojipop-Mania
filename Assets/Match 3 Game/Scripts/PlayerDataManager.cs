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
using System.Security.Cryptography;
using System.Text;

[Serializable]
public class LevelListWrapper
{
    public List<LevelInfo> Levels;
}

public class PlayerDataManager : MonoBehaviour
{
    public PlayerData playerData;

    public bool isLaunched = false; 
    public bool isFoundName = false; 
    public bool isOnline = true;
    public bool isNameSame = false; 

    public string PlayFabPlayerID; 
    public string PlayFabPlayerName; 

    private StageManager stageManager;
    public int currentLevel = 1; 

    public static PlayerDataManager Instance { get; private set; }

    private Coroutine energyRegenCoroutine;
    private Coroutine internetCheckCoroutine;
    private const float INTERNET_CHECK_INTERVAL = 3f; 

    private string savePath; 

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
        isOnline = Application.internetReachability != NetworkReachability.NotReachable;
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject); 
            return; 
        }
        
        savePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        stageManager = FindObjectOfType<StageManager>();

        // Load local cached data immediately so we are initialized and never null on startup
        LoadLocalDataAsCache();
    }

    void Start()
    {
        LoginAsGuest();

        stageManager = FindObjectOfType<StageManager>();

        SavePlayerData();
        GetCurrentLevel(); 

        CalculateOfflineEnergyRegen();

        if (energyRegenCoroutine != null) StopCoroutine(energyRegenCoroutine);
        energyRegenCoroutine = StartCoroutine(EnergyRegenCoroutine());

        if (internetCheckCoroutine != null) StopCoroutine(internetCheckCoroutine);
        internetCheckCoroutine = StartCoroutine(InternetCheckCoroutine());
    }

    private void LoadLocalDataAsCache()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string rawData = File.ReadAllText(savePath);
                string decryptedJson = DecryptSaveData(rawData);
                playerData = JsonUtility.FromJson<PlayerData>(decryptedJson);
                GetCurrentLevel();
                stageManager?.RefreshLocalUI();
                Debug.Log("Loaded local data cache successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("Error loading local cache: " + e.Message);
            }
        }

        if (playerData == null)
        {
            CreateNewPlayer("Temp", Guid.NewGuid().ToString());
            GetCurrentLevel();
        }
    }

    #region "AES-256 Encryption & Security"
    // 256-bit Key & 128-bit IV derived securely
    private static readonly byte[] AesKey = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("MojiPopMania_SecureSaltKey_2026_@!#"));
    private static readonly byte[] AesIV = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("MojiPopMania_IV_2026!"));

    public static string EncryptAES(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        try
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = AesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                        cs.Write(plainBytes, 0, plainBytes.Length);
                        cs.FlushFinalBlock();
                    }
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"AES Encryption error: {e.Message}");
            return plainText;
        }
    }

    public static string DecryptAES(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;
        try
        {
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = AesKey;
                aes.IV = AesIV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (StreamReader reader = new StreamReader(cs, Encoding.UTF8))
                        {
                            return reader.ReadToEnd();
                        }
                    }
                }
            }
        }
        catch
        {
            return null; // Triggers fallback to legacy XOR for smooth migration
        }
    }

    // Decrypts with AES-256 first, with automatic backward-compatibility fallback to legacy XOR
    private string DecryptSaveData(string rawData)
    {
        if (string.IsNullOrEmpty(rawData)) return "";

        // 1. Try AES-256 first
        string aesDecrypted = DecryptAES(rawData);
        if (!string.IsNullOrEmpty(aesDecrypted) && aesDecrypted.Contains("CurrentLevelId"))
        {
            return aesDecrypted;
        }

        // 2. Backward compatibility fallback: Try legacy XOR
        try
        {
            string xorDecrypted = XorEncryptDecrypt(rawData);
            if (!string.IsNullOrEmpty(xorDecrypted) && xorDecrypted.Contains("CurrentLevelId"))
            {
                Debug.Log("Migrated legacy save file to AES-256.");
                return xorDecrypted;
            }
        }
        catch {}

        // 3. Fallback: plain JSON
        return rawData;
    }

    // Retained strictly for seamless backward-compatibility migration of existing installs
    private string XorEncryptDecrypt(string data, string key = "Heil")
    {
        char[] result = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (char)(data[i] ^ key[i % key.Length]);
        }
        return new string(result);
    }
    #endregion

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
            PlayerBombAbilityCount = 3,
            PlayerColorBombAbilityCount = 3,
            PlayerExtraMoveAbilityCount = 3,
            PlayerShuffleAbilityCount = 3,
            CurrentLevelId = 1,
            EnergyCount = 5,
            LastEnergyUpdateTime = GetCurrentUnixTime(),
            Levels = new List<LevelInfo>()
            {
                new LevelInfo { LevelID = 1, Stars = 0, XP = 0, LevelLocked = 0 },
                new LevelInfo { LevelID = 2, Stars = 0, XP = 0, LevelLocked = 1 },
                new LevelInfo { LevelID = 3, Stars = 0, XP = 0, LevelLocked = 1 }
            }
        };

        if (isOnline) 
        {
            SendPlayerDataToPlayFab();
        }
    }

    public void GetCurrentLevel()
    {
        if (playerData != null)
        {
            if (playerData.Levels != null && playerData.Levels.Count > 0)
            {
                int maxUnlocked = 1;
                foreach (var lvl in playerData.Levels)
                {
                    if (lvl != null && lvl.LevelLocked == 0 && lvl.LevelID > maxUnlocked)
                    {
                        maxUnlocked = lvl.LevelID;
                    }
                }
                if (playerData.CurrentLevelId < maxUnlocked)
                {
                    playerData.CurrentLevelId = maxUnlocked;
                }
            }

            currentLevel = playerData.CurrentLevelId;
            Debug.Log("Current Level: " + currentLevel);
        }
        else
        {
            Debug.LogWarning("Player data is null, cannot get current level.");
        }
    }

    public void SavePlayerData()
    {
        if (playerData != null)
        {
            string json = JsonUtility.ToJson(playerData, true);
            string encryptedJson = EncryptAES(json);
            File.WriteAllText(savePath, encryptedJson);
        }

        if (isOnline)
        {
            SendPlayerDataToPlayFab();
        }
    }

    public void LoadPlayerData()
    {
        if (isOnline && PlayFabClientAPI.IsClientLoggedIn())
        {
            PlayFabClientAPI.GetUserData(new GetUserDataRequest(), result =>
            {
                if (result.Data != null && result.Data.ContainsKey("Levels"))
                {
                    string levelsJson = result.Data["Levels"].Value;
                    int cloudLevel = result.Data.ContainsKey("CurrentLevelId") ? int.Parse(result.Data["CurrentLevelId"].Value) : 1;
                    var cloudLevelsList = JsonUtility.FromJson<LevelListWrapper>(levelsJson)?.Levels ?? new List<LevelInfo>();

                    // Smart Merge: If local progress is higher than cloud (e.g. offline play), preserve local progress and upload!
                    if (playerData != null && playerData.CurrentLevelId > cloudLevel)
                    {
                        Debug.Log($"Local progress (Level {playerData.CurrentLevelId}) is ahead of Cloud (Level {cloudLevel}). Syncing local to Cloud.");
                        playerData.Levels = MergeLevels(playerData.Levels, cloudLevelsList);
                        SavePlayerData();
                    }
                    else
                    {
                        var mergedLevels = (playerData != null) ? MergeLevels(cloudLevelsList, playerData.Levels) : cloudLevelsList;
                        playerData = new PlayerData
                        {
                            Name = result.Data.ContainsKey("PlayerName") ? result.Data["PlayerName"].Value : (playerData != null ? playerData.Name : "Temp"),
                            PlayerID = result.Data.ContainsKey("PlayerID") ? result.Data["PlayerID"].Value : (playerData != null ? playerData.PlayerID : Guid.NewGuid().ToString()),
                            CurrentLevelId = cloudLevel,
                            
                            PlayerBombAbilityCount = result.Data.ContainsKey("PlayerBombAbilityCount") ? int.Parse(result.Data["PlayerBombAbilityCount"].Value) : 3,
                            PlayerColorBombAbilityCount = result.Data.ContainsKey("PlayerColorBombAbilityCount") ? int.Parse(result.Data["PlayerColorBombAbilityCount"].Value) : 3,
                            PlayerExtraMoveAbilityCount = result.Data.ContainsKey("PlayerExtraMoveAbilityCount") ? int.Parse(result.Data["PlayerExtraMoveAbilityCount"].Value) : 3,
                            PlayerShuffleAbilityCount = result.Data.ContainsKey("PlayerShuffleAbilityCount") ? int.Parse(result.Data["PlayerShuffleAbilityCount"].Value) : 3,
                            
                            EnergyCount = result.Data.ContainsKey("PlayerEnergyCount") ? int.Parse(result.Data["PlayerEnergyCount"].Value) : 5,
                            LastEnergyUpdateTime = result.Data.ContainsKey("LastEnergyUpdateTime") ? long.Parse(result.Data["LastEnergyUpdateTime"].Value) : 0,
                            Levels = mergedLevels
                        };
                        Debug.Log("Player data synced from PlayFab.");
                    }

                    CalculateOfflineEnergyRegen();
                    GetCurrentLevel();
                    stageManager?.RefreshLocalUI();
                }
                else
                {
                    Debug.LogWarning("No data found on PlayFab. Checking for local data to upload...");
                    if (playerData == null)
                    {
                        CreateNewPlayer("Temp", Guid.NewGuid().ToString());
                    }
                    CalculateOfflineEnergyRegen();
                    SavePlayerData();
                    GetCurrentLevel();
                    stageManager?.RefreshLocalUI();
                }
            }, OnError);
        }
        else
        {
            Debug.LogWarning("Offline mode: Loading local JSON data.");
            if (File.Exists(savePath))
            {
                string rawData = File.ReadAllText(savePath);
                string decryptedJson = DecryptSaveData(rawData);
                playerData = JsonUtility.FromJson<PlayerData>(decryptedJson);
                CalculateOfflineEnergyRegen();
                GetCurrentLevel();
                stageManager?.RefreshLocalUI();
            }
            else
            {
                CreateNewPlayer("Temp", Guid.NewGuid().ToString());
                CalculateOfflineEnergyRegen();
                SavePlayerData();
                GetCurrentLevel();
                stageManager?.RefreshLocalUI();
            }
        }
    }

    public void SetLevelStars(int levelId, int stars, int xp)
    {
        if (stars < 0) stars = 0;
        if (stars > 3) stars = 3;

        LevelInfo level = playerData.Levels.Find(l => l.LevelID == levelId);
        if (level == null)
        {
            level = new LevelInfo { LevelID = levelId, Stars = stars, XP = xp, LevelLocked = 1 }; 
            playerData.Levels.Add(level);
        }
        else
        {
            if (stars > level.Stars) level.Stars = stars;
            level.XP += xp;
        }
        Debug.Log($"Level {levelId} updated: Stars={stars}, XP={xp}");
    }

    public void SetLevelLocked(int levelId, int lockedValue)
    {
        if (lockedValue != 0 && lockedValue != 1) return;

        LevelInfo level = playerData.Levels.Find(l => l.LevelID == levelId);
        if (level == null)
        {
            level = new LevelInfo { LevelID = levelId, Stars = 0, XP = 0, LevelLocked = lockedValue };
            playerData.Levels.Add(level);
        }
        else
        {
            level.LevelLocked = lockedValue;
        }
    }

    public void SetCurrentLevel(int levelId) { if (playerData != null) { playerData.CurrentLevelId = levelId; currentLevel = levelId; } }
    
    public void SetName(string newName)
    {
        playerData.Name = newName;
        PlayFabPlayerName = newName;
        isFoundName = true;
        stageManager?.RefreshLocalUI();
        if (isOnline) SetUserName(newName);
    }

    void OnUpdateUserNameSuccess(UpdateUserTitleDisplayNameResult result)
    {
        PlayFabPlayerName = result.DisplayName;
        isFoundName = true;
        SavePlayerData();
        stageManager?.RefreshLocalUI();
        stageManager.UserNameUpdated();
    }

    public void SetPlayerID(string newPlayerID) { playerData.PlayerID = newPlayerID; }

    public void SetPlayerBombAbilityCount(int count) { playerData.PlayerBombAbilityCount = count; }
    public void SetPlayerColorBombAbilityCount(int count) { playerData.PlayerColorBombAbilityCount = count; }
    public void SetPlayerExtraMoveAbilityCount(int count) { playerData.PlayerExtraMoveAbilityCount = count; }
    public void SetPlayerShuffleAbilityCount(int count) { playerData.PlayerShuffleAbilityCount = count; }

    public void SetAllData(int levelId, int lockedValue, int stars, int xp)
    {
        SetLevelLocked(levelId, lockedValue);
        SetLevelStars(levelId, stars, xp);
    }

    public void SendXP(int levelId, int xp)
    {
        LevelInfo level = playerData.Levels.Find(l => l.LevelID == levelId);
        if (level != null) level.XP = xp;
    }

    public void AddColorBombAbility(int count) { playerData.PlayerColorBombAbilityCount += count; }
    public void AddBombAbility(int count) { playerData.PlayerBombAbilityCount += count; }
    public void AddExtraMoveAbility(int count) { playerData.PlayerExtraMoveAbilityCount += count; }
    public void AddShuffleAbility(int count) { playerData.PlayerShuffleAbilityCount += count; }

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
    }

    void OnLoginSuccess(LoginResult result)
    {
        PlayFabPlayerID = result.PlayFabId;
        isLaunched = true;
        isOnline = true;

        LoadPlayerData();
        CheckAndSetPlayerName();

        var getRequest = new GetAccountInfoRequest();
        PlayFabClientAPI.GetAccountInfo(getRequest, accResult =>
        {
            PlayFabPlayerName = accResult.AccountInfo.TitleInfo.DisplayName;
            CheckAndSetPlayerName(); 
        }, OnError);
    }

    void OnError(PlayFabError error)
    {
        isOnline = false;
        // Fallback to loading offline local data if login fails on startup
        if (playerData == null)
        {
            LoadPlayerData();
        }
    }

    public void SetUserName(string name)
    {
        if (!string.IsNullOrEmpty(name) && PlayFabClientAPI.IsClientLoggedIn())
        {
            var request = new UpdateUserTitleDisplayNameRequest { DisplayName = name };
            PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnUpdateUserNameSuccess, OnError);
        }
    }

    public void SendPlayerDataToPlayFab()
    {
        if (!isOnline || !PlayFabClientAPI.IsClientLoggedIn() || playerData == null) return;

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
                { "LastEnergyUpdateTime", playerData.LastEnergyUpdateTime.ToString() },
                { "Levels", levelsJson }
            }
        };

        PlayFabClientAPI.UpdateUserData(request, OnUpdateUserDataSuccess, OnError);
    }

    void OnUpdateUserDataSuccess(UpdateUserDataResult result) {}

    public void CheckAndSetPlayerName()
    {
        if (!isOnline)
        {
            isFoundName = false;
            return;
        }

        if (string.IsNullOrEmpty(PlayFabPlayerName)) isFoundName = false;
        else
        {
            isFoundName = true;
            if (!string.IsNullOrEmpty(playerData.Name) && playerData.Name != PlayFabPlayerName)
            {
                playerData.Name = PlayFabPlayerName;
                SavePlayerData();
                stageManager?.RefreshLocalUI();
            }
        }
    }

    public void SendLeaderboard(int TotalXP)
    {
        if (!isOnline || !PlayFabClientAPI.IsClientLoggedIn()) return;

        var request = new UpdatePlayerStatisticsRequest
        {
            Statistics = new List<StatisticUpdate> { new StatisticUpdate { StatisticName = "XP", Value = TotalXP } }
        };
        PlayFabClientAPI.UpdatePlayerStatistics(request, OnLeaderboardUpdate, OnError);
    }

    public void GetLeaderboard()
    {
        if (!isOnline || !PlayFabClientAPI.IsClientLoggedIn()) return;

        var request = new GetLeaderboardRequest
        {
            StatisticName = "XP",
            StartPosition = 0,
            MaxResultsCount = 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
    }

    void OnLeaderboardUpdate(UpdatePlayerStatisticsResult result) {}

    public void OnLeaderboardGet(GetLeaderboardResult result) {}

    void CheckForOnline()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn()) return;
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnSuccess, OnError);
    }

    void OnSuccess(GetTitleDataResult result)
    {
        isOnline = true;
        SendPlayerDataToPlayFab();
    }
    #endregion

    private const int ENERGY_REGEN_MINUTES = 60;
    private const int MAX_ENERGY = 5;

    public void SetEnergyLevel(int energyCount)
    {
        playerData.EnergyCount = Mathf.Clamp(energyCount, 0, MAX_ENERGY);
        SavePlayerData();
    }

    public void AddEnergy(int amount)
    {
        if (playerData == null) return;
        playerData.EnergyCount = Mathf.Clamp(playerData.EnergyCount + amount, 0, MAX_ENERGY);
        playerData.LastEnergyUpdateTime = GetCurrentUnixTime();
        SavePlayerData();
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
        if (stageManager != null && stageManager.CurrentEnergyText != null)
        {
            stageManager.CurrentEnergyText.text = playerData.EnergyCount.ToString();
        }
        stageManager?.RefreshLocalUI();
        if (isOnline) SendPlayerDataToPlayFab();
    }

    public void RemoveEnergy(int amount)
    {
        if (playerData.EnergyCount >= MAX_ENERGY)
        {
            playerData.LastEnergyUpdateTime = GetCurrentUnixTime();
        }

        if (playerData.EnergyCount - amount < 0) 
            playerData.EnergyCount = 0;
        else 
            playerData.EnergyCount -= amount;

        SavePlayerData();
        if (isOnline) SendPlayerDataToPlayFab();
    }

    public int GetEnergyCount() 
    { 
        return playerData.EnergyCount; 
    }

    private void CalculateOfflineEnergyRegen()
    {
        if (playerData == null) return;
        if (playerData.EnergyCount >= MAX_ENERGY) return;

        long currentTime = GetCurrentUnixTime();
        long lastUpdateTime = playerData.LastEnergyUpdateTime;

        // If we have no record of when the last energy check occurred,
        // it means they've been gone for a while or it's a first load. Default to max energy!
        if (lastUpdateTime == 0)
        {
            playerData.EnergyCount = MAX_ENERGY;
            playerData.LastEnergyUpdateTime = currentTime;
            SavePlayerData();
            return;
        }

        long timeDifference = currentTime - lastUpdateTime;

        // Handle negative time shifts (timezone shifts or time cheating)
        if (timeDifference < 0)
        {
            playerData.LastEnergyUpdateTime = currentTime;
            SavePlayerData();
            return;
        }

        int minutesPassed = (int)(timeDifference / 60);

        if (minutesPassed >= ENERGY_REGEN_MINUTES)
        {
            int energyToAdd = minutesPassed / ENERGY_REGEN_MINUTES;
            int newEnergy = Mathf.Min(playerData.EnergyCount + energyToAdd, MAX_ENERGY);
            
            int actualEnergyAdded = newEnergy - playerData.EnergyCount;

            if (actualEnergyAdded > 0)
            {
                playerData.EnergyCount = newEnergy;
                
                // If they fully recharged to max energy, sync timer anchor to now.
                // Otherwise, increment the timer anchor forward by exactly the recharged amount.
                if (playerData.EnergyCount >= MAX_ENERGY)
                {
                    playerData.LastEnergyUpdateTime = currentTime;
                }
                else
                {
                    long timeUsedForRegen = actualEnergyAdded * ENERGY_REGEN_MINUTES * 60;
                    playerData.LastEnergyUpdateTime = lastUpdateTime + timeUsedForRegen;
                }
                
                SavePlayerData();
            }
        }
    }

    private IEnumerator EnergyRegenCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            if (playerData == null) continue;

            if (playerData.EnergyCount >= MAX_ENERGY) 
            {
                playerData.LastEnergyUpdateTime = GetCurrentUnixTime();
                continue;
            }

            long currentTime = GetCurrentUnixTime();
            long timeDifference = currentTime - playerData.LastEnergyUpdateTime;

            if (timeDifference < 0)
            {
                playerData.LastEnergyUpdateTime = currentTime;
                SavePlayerData();
                continue;
            }

            int minutesPassed = (int)(timeDifference / 60);

            if (minutesPassed >= ENERGY_REGEN_MINUTES)
            {
                int energyToAdd = minutesPassed / ENERGY_REGEN_MINUTES;
                int newEnergy = Mathf.Min(playerData.EnergyCount + energyToAdd, MAX_ENERGY);
                
                int actualEnergyAdded = newEnergy - playerData.EnergyCount;

                if (actualEnergyAdded > 0)
                {
                    playerData.EnergyCount = newEnergy;
                    
                    if (playerData.EnergyCount >= MAX_ENERGY)
                    {
                        playerData.LastEnergyUpdateTime = currentTime;
                    }
                    else
                    {
                        long timeUsedForRegen = actualEnergyAdded * ENERGY_REGEN_MINUTES * 60;
                        playerData.LastEnergyUpdateTime += timeUsedForRegen;
                    }
                    
                    SavePlayerData();

                    if (stageManager != null && stageManager.CurrentEnergyText != null)
                    {
                        stageManager.CurrentEnergyText.text = playerData.EnergyCount.ToString();
                    }
                }
            }
        }
    }

    public int GetTimeUntilNextEnergy()
    {
        if (playerData.EnergyCount >= MAX_ENERGY) return 0;
        
        long currentTime = GetCurrentUnixTime();
        long timeDifference = currentTime - playerData.LastEnergyUpdateTime;
        
        int secondsPassed = (int)timeDifference;
        int secondsPerEnergy = ENERGY_REGEN_MINUTES * 60;
        
        int remainingSeconds = secondsPerEnergy - (secondsPassed % secondsPerEnergy);
        return remainingSeconds < 0 ? 0 : remainingSeconds;
    }

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
        if (energyRegenCoroutine != null) StopCoroutine(energyRegenCoroutine);
        if (internetCheckCoroutine != null) StopCoroutine(internetCheckCoroutine);
    }

    public void SkipEnergyGenerateTime()
    {
        AddEnergy(1);
    }

    public void CheckInternetConnection()
    {
        isOnline = Application.internetReachability != NetworkReachability.NotReachable;
    }

    public int GetPlayerBombAbilityCount() { return playerData.PlayerBombAbilityCount; }
    public int GetPlayerColorBombAbilityCount() { return playerData.PlayerColorBombAbilityCount; }
    public int GetPlayerExtraMoveAbilityCount() { return playerData.PlayerExtraMoveAbilityCount; }
    public int GetPlayerShuffleAbilityCount() { return playerData.PlayerShuffleAbilityCount; }

    private List<LevelInfo> MergeLevels(List<LevelInfo> primary, List<LevelInfo> secondary)
    {
        Dictionary<int, LevelInfo> map = new Dictionary<int, LevelInfo>();
        if (primary != null)
        {
            foreach (var lvl in primary)
            {
                if (lvl != null) map[lvl.LevelID] = new LevelInfo { LevelID = lvl.LevelID, Stars = lvl.Stars, XP = lvl.XP, LevelLocked = lvl.LevelLocked };
            }
        }
        if (secondary != null)
        {
            foreach (var lvl in secondary)
            {
                if (lvl == null) continue;
                if (map.TryGetValue(lvl.LevelID, out var existing))
                {
                    existing.Stars = Mathf.Max(existing.Stars, lvl.Stars);
                    existing.XP = Mathf.Max(existing.XP, lvl.XP);
                    if (lvl.LevelLocked == 0) existing.LevelLocked = 0;
                }
                else
                {
                    map[lvl.LevelID] = new LevelInfo { LevelID = lvl.LevelID, Stars = lvl.Stars, XP = lvl.XP, LevelLocked = lvl.LevelLocked };
                }
            }
        }
        return map.Values.OrderBy(l => l.LevelID).ToList();
    }

    public void ReconnectAndSyncPlayFab(System.Action onSuccess = null, System.Action onFailure = null)
    {
        LoginAsGuest();
        if (isOnline) LoadPlayerData();

        stageManager = FindObjectOfType<StageManager>();
        SavePlayerData();
        GetCurrentLevel(); 
        CalculateOfflineEnergyRegen();
        energyRegenCoroutine = StartCoroutine(EnergyRegenCoroutine());
        internetCheckCoroutine = StartCoroutine(InternetCheckCoroutine());
    }
}
