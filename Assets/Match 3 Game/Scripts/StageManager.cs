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

    [Header("JSON Save File")]
    public string fileName = "playerdata.json";

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



    private PlayerData playerData;

    private string SavePath => Path.Combine(Application.persistentDataPath, fileName);

    private const string SelectedLevelIndexKey = "SelectedLevelIndex";

    private int selectedLevelIndex = 0; // 0-based, for the clicked button only

    public string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // test ad unit id

    private RewardedAd rewardedAd;



    void Start()
    {
        mapHomeButton.ShowButton(); // Show the map home button
        AudioManager.Instance.PlayMusic("MenuBG");
        LoadPlayerData();
        ApplyDataToButtons();
        ShowTotalXPandTotalStars();

        Application.targetFrameRate = 60;

        LoadRewardedAd();

        namePanel.SetActive(false); // Hide name panel initially
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

    //lock 60 fps


    private void Update()
    {
        GetCurrentLevelInt();

        
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
        
        
        if (File.Exists(SavePath))
        {
            //string json = File.ReadAllText(SavePath);

            string encryptedJson = File.ReadAllText(SavePath);

            // Decrypt before loading
            string decryptedJson = XorEncryptDecrypt(encryptedJson);

            playerData = JsonUtility.FromJson<PlayerData>(decryptedJson);
            Debug.Log("StageManager: Player data loaded.");
        }
        else
        {
            Debug.LogWarning("StageManager: Save file not found at " + SavePath);
            playerData = new PlayerData(); // empty fallback
        }
    }


    /*public void LoadPlayerData()
    {
        if (File.Exists(savePath))
        {
            

            playerData = JsonUtility.FromJson<PlayerData>(decryptedJson);
            Debug.Log("Player data loaded (decrypted).");

            GetCurrentLevel();
        }
        else
        {
            Debug.LogWarning("Save file not found, creating new player...");
            CreateNewPlayer("Temp", Guid.NewGuid().ToString());
            SavePlayerData();
            GetCurrentLevel();
        }
    }*/



    private void ApplyDataToButtons()
    {

        

        /*//enable btn.isCurrentLevel=true if current level
        for (int i = 0; i < levelButtons.Length; i++)
        {
            LevelButtonManager btn = levelButtons[i];
            btn.isCurrentLevel = (i + 1 == currentLevel); // Levels start from 1
            btn.SetInteractable(!btn.isCurrentLevel); // Disable interaction for current level button
        }*/


        //int currentLevelIndex = GetCurrentLevelIndex();

        for (int i = 0; i < levelButtons.Length; i++)
        {
            LevelButtonManager btn = levelButtons[i];
            btn.SetLevelId(i + 1); // Levels start from 1

            btn.isCurrentLevel = (i + 1 == currentLevel); // Levels start from 1
            //btn.SetInteractable(!btn.isCurrentLevel); // Disable interaction for current level button
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

    /*private int GetCurrentLevelIndex()
    {
        // Example: first unlocked level with <3 stars
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (!levelButtons[i].isLocked)
            {
                return i;
            }
        }

        // If all locked, fallback to first one
        return 0;
    }*/

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
            //CheckAndShowNamePanel();
            FetchPlayerDataFromPlayFab();
        }




        




    }
    public void SelectLevel(LevelButtonManager clickedButton)
    {
        StartCoroutine(SelectLevelCoroutine(clickedButton));
    }

    /*private IEnumerator SelectLevelCoroutine(LevelButtonManager clickedButton)
    {
        // Run Emoji animation first
        yield return StartCoroutine(EmojiLoading());

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

        if (isLocked)
        {
            OnLockedLevelClicked(clickedButton.levelId); // call separate function
            yield break;
        }

        // ✅ Level is unlocked → save and load scene
        selectedLevelIndex = clickedIndex;
        PlayerPrefs.SetInt(SelectedLevelIndexKey, clickedButton.levelId - 1);
        PlayerPrefs.Save();

        Debug.Log($"StageManager: Selected level saved as {clickedButton.levelId - 1}");

        SceneManager.LoadScene("MainGame");

        // Deduct 1 energy when level is selected
    }*/


    private IEnumerator SelectLevelCoroutine(LevelButtonManager clickedButton)
    {
        // Run Emoji animation first

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
        if(!PlayerDataManager.Instance.isOnline)
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

        // ✅ Deduct energy before loading the level
        //PlayerDataManager.Instance.RemoveEnergy(1);

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

        //reset sce

        
        // You can also show a UI message or popup here
    }

    /*public void ShowTotalXPandTotalStars()
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
        Name.text = playerData.Name; // Display player name

        //show current vibes
        CurrentEnergyText.text = PlayerDataManager.Instance.GetEnergyCount().ToString();


        //show ability counts
        bombAbilityCount.text = playerData.PlayerBombAbilityCount.ToString();
        colorBombAbilityCount.text = playerData.PlayerColorBombAbilityCount.ToString();
        extraMoveAbilityCount.text = playerData.PlayerExtraMoveAbilityCount.ToString();
        ShuffleAbilityCount.text = playerData.PlayerShuffleAbilityCount.ToString();
    }*/

    // This function remains the same - it just reads from playerData
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
        /*if (playerData == null || string.IsNullOrEmpty(playerData.Name))
        {
            namePanel.SetActive(true); // Show name panel if no name found
        }
        else
        {
            namePanel.SetActive(false); // Hide if name exists
        }

        //if player data name is null or empty then show name panel. also check 
        */

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


    /*public void RefreashData()
    {
               // Reload player data and update buttons
        CheckForInternetConnection();
        LoadPlayerData();
        PlayerDataManager.Instance.CheckAndSetPlayerName();
        ApplyDataToButtons();
        ShowTotalXPandTotalStars();
        CheckAndShowNamePanel(); // Ensure name panel visibility is updated
        
    }*/

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
        var adRequest = new AdRequest();

        RewardedAd.Load(rewardedAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("Failed to load rewarded ad: " + error);
                rewardedAd = null;
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

        if (rewardedAd != null)
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                //add clown ability count by 1
                PlayerDataManager.Instance.SendColorBombAbility(1);
                ColorBombGetFromAdsPanel.SetActive(true); // Show the panel

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
        if (rewardedAd != null)
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                //add clown ability count by 1
                PlayerDataManager.Instance.SendBombAbility(1);
                BombGetFromAdsPanel.SetActive(true); // Show the panel

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
        if (rewardedAd != null)
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                //add clown ability count by 1
                PlayerDataManager.Instance.SendExtraMoveAbility(1);
                ExtraMovesGetFromAdsPanel.SetActive(true); // Show the panel

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
        if (rewardedAd != null)
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);
                //add clown ability count by 1
                PlayerDataManager.Instance.SkipEnergyGenerateTime();
                NoEnergyLeftPanel.SetActive(false); // Hide the no energy panel
                energyTimerUI.UpdateUI();



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

        CheckForInternetConnection(); // ✅ Optionally add this to immediately show panel if still offline
    }

}
