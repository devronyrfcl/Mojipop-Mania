using DG.Tweening; // ✅ Needed for DOTween animations
using System.Collections; // ✅ Needed for coroutines
using System.IO;
using TMPro; // ✅ Needed for text display
using UnityEngine;
using UnityEngine.SceneManagement; // ✅ Needed for scene loading
using UnityEngine.UI; // ✅ Needed for UI components
using PlayFab;
using PlayFab.ClientModels;
using GoogleMobileAds.Api;

public class StageManager : MonoBehaviour
{
    [Header("Level Button References (Order matters)")]
    public LevelButtonManager[] levelButtons; // Assign in order in Inspector

    public TMP_Text TotalStar;
    public TMP_Text TotalXP;
    public TMP_Text Name;
    public GameObject EmojisImage; // Reference to the emojis image GameObject

    public TMP_Text bombAbilityCount;
    public TMP_Text colorBombAbilityCount;
    public TMP_Text extraMoveAbilityCount;
    public TMP_Text ShuffleAbilityCount;

    public GameObject namePanel; // Reference to the name panel GameObject

    public int currentLevel;

    public HomeButtonManager mapHomeButton; // Reference to the HomeButtonManager
    public HomeButtonManager spinButton;

    public GameObject shopUI;

    public int totalStars;
    public int totalXP;
    public TMP_InputField userNameInput; // Reference to the input field for username

    public GameObject UserNameUpdatedPanel; // Panel to show when username is updated

    public GameObject ColorBombGetFromAdsPanel; // Panel to show when color bomb is rewarded from ads
    public GameObject BombGetFromAdsPanel;
    public GameObject ExtraMovesGetFromAdsPanel;

    public TMP_Text CurrentEnergyText; // Text to show current energy
    public GameObject NoEnergyLeftPanel; // Panel to show when no energy left

    public EnergyTimerUI energyTimerUI; // Reference to the EnergyTimerUI script

    public GameObject NoInternetConnectionPanel; // Panel to show when no internet connection

    public GameObject ExitPanel;

    private PlayerData playerData;

    private const string SelectedLevelIndexKey = "SelectedLevelIndex";

    private int selectedLevelIndex = 0; // 0-based, for the clicked button only

    private const string rewardedAdUnitId =
#if UNITY_ANDROID
        "ca-app-pub-5068573171198161/1757128667";
#elif UNITY_IOS
        "ca-app-pub-5068573171198161/3280588859";
#else
        "";
#endif

    private const string bannerAdUnitId =
#if UNITY_ANDROID
            "ca-app-pub-5068573171198161/2958529693";
#elif UNITY_IOS
            "ca-app-pub-5068573171198161/6281852798";
#else
            "";
#endif

    private RewardedAd rewardedAd;
    private BannerView bannerView;

    // 🔥 THIS IS NEW: Forces the UI to update whenever this script's GameObject is turned on
    private void OnEnable()
    {
        RefreshLocalUI();
    }

    void Start()
    {
        mapHomeButton.ShowButton(); 
        AudioManager.Instance.PlayMusic("MenuBG");
        LoadPlayerData();
        ApplyDataToButtons();
        ShowTotalXPandTotalStars();

        Application.targetFrameRate = 60;

        // 🔥 THE FIX: Use standard MobileAds.Initialize
        MobileAds.Initialize((InitializationStatus status) =>
        {
            LoadRewardedAd();
            LoadBannerAd();
            InvokeRepeating(nameof(AdWatchdog), 10f, 10f);
        });

        namePanel.SetActive(false); 
    }
    // 🔥 THE FIX: The background watchdog
    private void AdWatchdog()
    {
        // If the ad failed to load, was destroyed, or hasn't loaded yet, fetch it automatically!
        if (rewardedAd == null)
        {
            LoadRewardedAd();
        }
    }
    // 🔥 THE FIX: Instantly updates StageManager's local memory and redraws the text!
    public void SyncLocalBooster(string type, int amount)
    {
        if (playerData != null)
        {
            if (type == "Bomb") playerData.PlayerBombAbilityCount += amount;
            else if (type == "ColorBomb") playerData.PlayerColorBombAbilityCount += amount;
            else if (type == "Moves") playerData.PlayerExtraMoveAbilityCount += amount;
            else if (type == "Shuffle") playerData.PlayerShuffleAbilityCount += amount;
        }
        ShowTotalXPandTotalStars(); // Force the text to redraw instantly
    }

    private void LoadBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        if (string.IsNullOrEmpty(bannerAdUnitId))
        {
            Debug.LogWarning("Banner ad unit ID is not set for this platform.");
            return;
        }

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Top);
        var adRequest = new AdRequest();
        bannerView.LoadAd(adRequest);
    }

    private void OnDestroy()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }
    }

    private string XorEncryptDecrypt(string data, string key = "Heil")
    {
        char[] result = new char[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            result[i] = (char)(data[i] ^ key[i % key.Length]);
        }
        return new string(result);
    }

    private void Update()
    {
        GetCurrentLevelInt();

        //if press back button on android then active exit panel
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitPanel.SetActive(true);
        }
    }

    void CheckForInternetConnection()
    {
        if (PlayerDataManager.Instance.isOnline == false)
        {
            ActiveNoInternetPanel();
        }
        else
        {
            NoInternetConnectionPanel.SetActive(false);
        }
    }

    public void CheckForInternetConnectionForUIButton()
    {
        PlayerDataManager.Instance.ReconnectAndSyncPlayFab();
        SceneManager.LoadScene("MainMenu");
    }

    private void LoadPlayerData()
    {
        //load player data from playfab if online. no json file used here
        if (PlayerDataManager.Instance.isOnline)
        {
            Debug.Log("StageManager: Online mode - loading data from PlayFab.");
            FetchPlayerDataFromPlayFab(); // Moved to Start() to avoid nested calls
        }
        else
        {
            Debug.Log("StageManager: Offline mode - loading local JSON data.");
            string savePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
            if (File.Exists(savePath))
            {
                string encryptedJson = File.ReadAllText(savePath);
                // Decrypt before loading
                string decryptedJson = XorEncryptDecrypt(encryptedJson);
                playerData = JsonUtility.FromJson<PlayerData>(decryptedJson);
                Debug.Log("Player data loaded (decrypted).");
                GetCurrentLevelInt();
            }
            else
            {
                Debug.LogWarning("Save file not found, creating new player...");
                PlayerDataManager.Instance.CreateNewPlayer("Rookie", System.Guid.NewGuid().ToString());
                PlayerDataManager.Instance.SavePlayerData();
                GetCurrentLevelInt();
            }
        }
    }

    private void ApplyDataToButtons()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            LevelButtonManager btn = levelButtons[i];
            btn.SetLevelId(i + 1); // Levels start from 1

            btn.isCurrentLevel = (i + 1 == currentLevel); // Levels start from 1
            LevelInfo levelInfo = playerData.Levels.Find(l => l.LevelID == btn.levelId);
            if (levelInfo != null)
            {
                btn.SetStar(levelInfo.Stars);
                btn.SetLocked(levelInfo.LevelLocked == 1);

                // 🔥 Make button not interactable if locked
                btn.GetComponent<Button>().interactable = (levelInfo.LevelLocked == 0);
            }
            else
            {
                // If level not found in JSON, default: locked & 0 stars
                btn.SetStar(0);
                btn.SetLocked(true);

                btn.GetComponent<Button>().interactable = false; // 🔒
            }
        }
        SendDataToLeaderBoard();
    }

    IEnumerator EmojiLoading()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();

        // ✅ Move EmojisImage into view (Y: 2150 → -1777)
        yield return emojiRect.DOAnchorPosY(-1250f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();

        CheckForInternetConnection(); // ✅ Optionally add this to immediately show panel if still offline

        // ✅ Wait 1 second
        yield return new WaitForSeconds(1f);

        //if no internet then active no internet panel. if found internet then check for internet connection. 
        if (PlayerDataManager.Instance.isOnline == false)
        {
            ActiveNoInternetPanel();
        }
        else
        {
            NoInternetConnectionPanel.SetActive(false);
            FetchPlayerDataFromPlayFab();
        }
    }

    public void SelectLevel(LevelButtonManager clickedButton)
    {
        StartCoroutine(SelectLevelCoroutine(clickedButton));
    }

    private IEnumerator SelectLevelCoroutine(LevelButtonManager clickedButton)
    {
        PlayerDataManager.Instance.CheckInternetConnection();

        int clickedIndex = -1;

        // Find index of clicked button
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == clickedButton)
            {
                clickedIndex = i;
                break;
            }
        }

        if (clickedIndex == -1)
            yield break; // safety check

        // Check if the level is locked
        LevelInfo levelInfo = playerData.Levels.Find(l => l.LevelID == clickedButton.levelId);
        bool isLocked = levelInfo != null && levelInfo.LevelLocked == 1;

        //if no internet then active no internet panel. if found internet then just continue
        if (!PlayerDataManager.Instance.isOnline)
        {
            ActiveNoInternetPanel();
            yield break; // Exit without loading the level
        }

        if (isLocked)
        {
            OnLockedLevelClicked(clickedButton.levelId); // call separate function
            yield break;
        }

        // ✅ Check if player has enough energy
        int currentEnergy = PlayerDataManager.Instance.GetEnergyCount();
        if (currentEnergy <= 0)
        {
            Debug.Log("Not enough energy to play this level!");
            NoEnergyLeftPanel.SetActive(true); // Show no energy panel
            yield break; // Exit without loading the level
        }

        yield return StartCoroutine(EmojiLoading());

        // Update the energy display
        CurrentEnergyText.text = PlayerDataManager.Instance.GetEnergyCount().ToString();

        // ✅ Level is unlocked & energy deducted → save and load scene
        selectedLevelIndex = clickedIndex;
        PlayerPrefs.SetInt(SelectedLevelIndexKey, clickedButton.levelId - 1);
        PlayerPrefs.Save();

        Debug.Log($"StageManager: Selected level saved as {clickedButton.levelId - 1}");

        SceneManager.LoadScene("MainGame");
    }

    void OnLockedLevelClicked(int levelId)
    {
        // Handle locked level click (e.g., show message)
        Debug.Log($"StageManager: Level {levelId} is locked. Please unlock it first.");
    }

    // 🔥 THIS IS NEW: Grabs the latest local save data and forces the UI to redraw
    public void RefreshLocalUI()
    {
        string savePath = Path.Combine(Application.persistentDataPath, "playerdata.json");
        if (File.Exists(savePath))
        {
            string encryptedJson = File.ReadAllText(savePath);
            string decryptedJson = XorEncryptDecrypt(encryptedJson);
            
            // Update the StageManager's active data structure
            playerData = JsonUtility.FromJson<PlayerData>(decryptedJson);
            
            // Force the visual text to redraw with the new numbers
            ShowTotalXPandTotalStars(); 
        }
        // else if (PlayerDataManager.Instance != null)
        // {
        //      // Fallback if the file isn't written yet
        //      bombAbilityCount.text = PlayerDataManager.Instance.playerBombCount.ToString();
        //      colorBombAbilityCount.text = PlayerDataManager.Instance.playerClownCount.ToString();
        //      extraMoveAbilityCount.text = PlayerDataManager.Instance.playerExtraMoveCount.ToString();
        //      ShuffleAbilityCount.text = PlayerDataManager.Instance.playerShuffleCount.ToString();
        //      CurrentEnergyText.text = PlayerDataManager.Instance.GetEnergyCount().ToString();
        // }
    }

    public void ShowTotalXPandTotalStars()
    {
        if (playerData == null)
        {
            Debug.LogError("StageManager: No player data available.");
            return;
        }
        totalXP = 0;
        totalStars = 0;
        foreach (LevelInfo level in playerData.Levels)
        {
            totalXP += level.XP;
            totalStars += level.Stars;
        }
        TotalXP.text = $"{totalXP}";
        TotalStar.text = $"{totalStars}";
        Name.text = playerData.Name;
        CurrentEnergyText.text = PlayerDataManager.Instance.GetEnergyCount().ToString();
        bombAbilityCount.text = playerData.PlayerBombAbilityCount.ToString();
        colorBombAbilityCount.text = playerData.PlayerColorBombAbilityCount.ToString();
        extraMoveAbilityCount.text = playerData.PlayerExtraMoveAbilityCount.ToString();
        ShuffleAbilityCount.text = playerData.PlayerShuffleAbilityCount.ToString();
    }

    private void FetchPlayerDataFromPlayFab()
    {
        if (!PlayerDataManager.Instance.isOnline)
        {
            Debug.Log("Offline mode: Cannot fetch from PlayFab. Using local JSON.");
            LoadPlayerData(); // Fallback to local JSON
            return;
        }

        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, OnPlayFabDataReceived, OnPlayFabError);
    }

    private void OnPlayFabDataReceived(GetUserDataResult result)
    {
        Debug.Log("Player data fetched from PlayFab successfully.");

        // Parse the data from PlayFab
        if (result.Data != null)
        {
            // Initialize playerData if null
            if (playerData == null)
                playerData = new PlayerData();

            // Get basic info
            if (result.Data.ContainsKey("PlayerName"))
                playerData.Name = result.Data["PlayerName"].Value;

            if (result.Data.ContainsKey("PlayerID"))
                playerData.PlayerID = result.Data["PlayerID"].Value;

            if (result.Data.ContainsKey("CurrentLevelId"))
                playerData.CurrentLevelId = int.Parse(result.Data["CurrentLevelId"].Value);

            if (result.Data.ContainsKey("PlayerBombAbilityCount"))
                playerData.PlayerBombAbilityCount = int.Parse(result.Data["PlayerBombAbilityCount"].Value);

            if (result.Data.ContainsKey("PlayerColorBombAbilityCount"))
                playerData.PlayerColorBombAbilityCount = int.Parse(result.Data["PlayerColorBombAbilityCount"].Value);

            if (result.Data.ContainsKey("PlayerExtraMoveAbilityCount"))
                playerData.PlayerExtraMoveAbilityCount = int.Parse(result.Data["PlayerExtraMoveAbilityCount"].Value);

            if (result.Data.ContainsKey("PlayerShuffleAbilityCount"))
                playerData.PlayerShuffleAbilityCount = int.Parse(result.Data["PlayerShuffleAbilityCount"].Value);

            if (result.Data.ContainsKey("PlayerEnergyCount"))
                playerData.EnergyCount = int.Parse(result.Data["PlayerEnergyCount"].Value);

            // Parse Levels JSON
            if (result.Data.ContainsKey("Levels"))
            {
                string levelsJson = result.Data["Levels"].Value;
                LevelListWrapper wrapper = JsonUtility.FromJson<LevelListWrapper>(levelsJson);
                playerData.Levels = wrapper.Levels;
            }

            // Now update UI with fetched data
            ApplyDataToButtons();
            ShowTotalXPandTotalStars();
            CheckAndShowNamePanel();
        }
        else
        {
            Debug.LogWarning("No data found in PlayFab. Using local JSON as fallback.");
            LoadPlayerData();
        }
    }

    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError("Error fetching PlayFab data: " + error.GenerateErrorReport());

        // Fallback to local JSON
        LoadPlayerData();
        ApplyDataToButtons();
        ShowTotalXPandTotalStars();
    }

    //if PlayerDataManager.isFoundName = false , then show name panel
    public void CheckAndShowNamePanel()
    {
        Debug.Log("isFoundName: " + PlayerDataManager.Instance.isFoundName);

        if (!PlayerDataManager.Instance.isFoundName)
        {
            namePanel.SetActive(true); // Show name panel if no name found
        }
        else
        {
            namePanel.SetActive(false); // Hide if name exists
        }
    }

    public void RefreashData()
    {
        CheckForInternetConnection();

        // 🔥 Fetch from PlayFab if online
        if (PlayerDataManager.Instance.isOnline)
        {
            FetchPlayerDataFromPlayFab();
        }
        else
        {
            LoadPlayerData();
            ApplyDataToButtons();
            ShowTotalXPandTotalStars();
        }

        PlayerDataManager.Instance.CheckAndSetPlayerName();
        CheckAndShowNamePanel();
    }

    void GetCurrentLevelInt()
    {
        // Get the current level from PlayerPrefs
        currentLevel = PlayerDataManager.Instance.currentLevel;
    }

    public void OnClickAbilityButton()
    {
        //if bomb or color bomb or extra move ability count is 0 then debug log "No abilities left"
        if (playerData.PlayerBombAbilityCount <= 0 && playerData.PlayerColorBombAbilityCount <= 0 && playerData.PlayerExtraMoveAbilityCount <= 0)
        {
            spinButton.ShowButton(); // Show the spin button
            return;
        }
        else
        {
            shopUI.SetActive(true); // Show the shop UI
        }
    }

    public void SendDataToLeaderBoard()
    {
        PlayerDataManager.Instance.SendLeaderboard(totalXP); // Send the score to the leaderboard
    }

    public void GetDataFromLeaderboard()
    {
        PlayerDataManager.Instance.GetLeaderboard();
    }

    public void SetUserName()
    {
        string userName = userNameInput.text.Trim();
        if (string.IsNullOrEmpty(userName))
        {
            Debug.LogWarning("Username cannot be empty.");
            return;
        }

        PlayerDataManager.Instance.SetName(userName);
        PlayerDataManager.Instance.SavePlayerData();
        RefreashData();
        UserNameUpdated();
    }

    //public exit function
    public void OnClickExitButton()
    {
        Debug.Log("Exit button clicked. Quitting application...");
        //quit application for android and ios
        Application.Quit();
    }

    public void UserNameUpdated()
    {
        UserNameUpdatedPanel.SetActive(true);
    }

    void LoadRewardedAd()
    {
        // Clean up old instance first
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var adRequest = new AdRequest();

        RewardedAd.Load(rewardedAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("Failed to load rewarded ad: " + error);
                rewardedAd = null;
                Invoke(nameof(LoadRewardedAd), 5f); // Auto retry
                return;
            }

            rewardedAd = ad;
            Debug.Log("Rewarded ad loaded successfully");

            // Register callbacks
            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Ad closed. Reloading new ad...");
                LoadRewardedAd(); // Load next ad
            };

            rewardedAd.OnAdFullScreenContentFailed += (AdError err) =>
            {
                Debug.LogError("Ad failed to show: " + err);
                LoadRewardedAd();
            };

            rewardedAd.OnAdPaid += (AdValue value) =>
            {
                Debug.Log("Rewarded Ad revenue: " + value.Value);
            };
        });
    }

    public void ShowRewardedAd_Clown()
    {
        CheckForInternetConnection();

        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                
                // 🔥 FIX 1: Use 'Add' instead of 'Send' (matches Spin.cs)
                PlayerDataManager.Instance.AddColorBombAbility(1);
                PlayerDataManager.Instance.SavePlayerData(); // Force a save

                // 🔥 FIX 2: Manually increment the StageManager's active tracker
                if (playerData != null) playerData.PlayerColorBombAbilityCount += 1;

                ColorBombGetFromAdsPanel.SetActive(true); 
                ShowTotalXPandTotalStars(); // 🔥 Force the text to redraw instantly!
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready. Reloading...");
            LoadRewardedAd();
        }
    }

    public void ShowRewardedAd_Bomb()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                
                // 🔥 FIX 1
                PlayerDataManager.Instance.AddBombAbility(1);
                PlayerDataManager.Instance.SavePlayerData(); 

                // 🔥 FIX 2
                if (playerData != null) playerData.PlayerBombAbilityCount += 1;

                BombGetFromAdsPanel.SetActive(true); 
                ShowTotalXPandTotalStars(); 
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready. Reloading...");
            LoadRewardedAd();
        }
    }

    public void ShowRewardedAd_Moves()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                
                // 🔥 FIX 1
                PlayerDataManager.Instance.AddExtraMoveAbility(1);
                PlayerDataManager.Instance.SavePlayerData();

                // 🔥 FIX 2
                if (playerData != null) playerData.PlayerExtraMoveAbilityCount += 1;

                ExtraMovesGetFromAdsPanel.SetActive(true); 
                ShowTotalXPandTotalStars(); 
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready. Reloading...");
            LoadRewardedAd();
        }
    }

    public void ShowRewardedAd_SkipEnergyGenerateTime()
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                
                PlayerDataManager.Instance.SkipEnergyGenerateTime();
                PlayerDataManager.Instance.SavePlayerData(); // Force a save

                NoEnergyLeftPanel.SetActive(false); 
                energyTimerUI.UpdateUI();
                ShowTotalXPandTotalStars(); 
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready. Reloading...");
            LoadRewardedAd();
        }
    }

    public void ActiveNoInternetPanel()
    {
        NoInternetConnectionPanel.SetActive(true);
    }

    public void RetryConnection()
    {
        NoInternetConnectionPanel.SetActive(false);
        PlayerDataManager.Instance.CheckInternetConnection();

        //login with guest
        PlayerDataManager.Instance.LoginAsGuest();

        CheckForInternetConnection(); 
    }
}