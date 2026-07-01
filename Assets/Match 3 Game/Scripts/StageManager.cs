using DG.Tweening; 
using System.Collections;
using System.Collections.Generic; 
using System.IO;
using TMPro; 
using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.UI; 
using PlayFab;
using PlayFab.ClientModels;

public class StageManager : MonoBehaviour
{
    [Header("Level Button References (Order matters)")]
    public LevelButtonManager[] levelButtons; 

    public TMP_Text TotalStar;
    public TMP_Text TotalXP;
    public TMP_Text Name;
    public GameObject EmojisImage; 

    public TMP_Text bombAbilityCount;
    public TMP_Text colorBombAbilityCount;
    public TMP_Text extraMoveAbilityCount;
    public TMP_Text ShuffleAbilityCount;

    public GameObject namePanel; 
    public int currentLevel;

    public HomeButtonManager mapHomeButton; 
    public HomeButtonManager spinButton;
    public GameObject shopUI;

    public int totalStars;
    public int totalXP;
    public TMP_InputField userNameInput; 

    public GameObject UserNameUpdatedPanel; 

    public GameObject ColorBombGetFromAdsPanel; 
    public GameObject BombGetFromAdsPanel;
    public GameObject ExtraMovesGetFromAdsPanel;

    public TMP_Text CurrentEnergyText; 
    public GameObject NoEnergyLeftPanel; 
    public EnergyTimerUI energyTimerUI; 

    public GameObject NoInternetConnectionPanel; 
    public GameObject ExitPanel;

    private const string SelectedLevelIndexKey = "SelectedLevelIndex";
    private int selectedLevelIndex = 0; 

    private void OnEnable()
    {
        RefreshLocalUI();
    }

    void Start()
    {
        mapHomeButton.ShowButton(); 
        AudioManager.Instance.PlayMusic("MenuBG");
        
        LoadPlayerData(); 

        Application.targetFrameRate = 60;
        namePanel.SetActive(false); 
    }

    public void SyncLocalBooster(string type, int amount)
    {
        ShowTotalXPandTotalStars(); 
    }

    private void Update()
    {
        GetCurrentLevelInt();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitPanel.SetActive(true);
        }
    }

    void CheckForInternetConnection()
    {
        if (PlayerDataManager.Instance.isOnline == false)
            ActiveNoInternetPanel();
        else
            NoInternetConnectionPanel.SetActive(false);
    }

    public void CheckForInternetConnectionForUIButton()
    {
        PlayerDataManager.Instance.ReconnectAndSyncPlayFab();
        SceneManager.LoadScene("MainMenu");
    }

    private void LoadPlayerData()
    {
        if (PlayerDataManager.Instance.isOnline)
        {
            FetchPlayerDataFromPlayFab(); 
        }
        else
        {
            ApplyDataToButtons();
            ShowTotalXPandTotalStars();
        }
    }

private void ApplyDataToButtons()
    {
        // Safety check
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.playerData == null) return;

        var pData = PlayerDataManager.Instance.playerData;

        // 🔥 THE CLOUD REPAIR: If PlayFab gives us a corrupted list, recreate it!
        if (pData.Levels == null)
        {
            pData.Levels = new List<LevelInfo>();
        }

        // 🔥 FORCE UNLOCK LEVEL 1: Check if Level 1 exists in the save file
        LevelInfo level1 = pData.Levels.Find(l => l.LevelID == 1);
        if (level1 == null)
        {
            // If it's completely missing, add it and unlock it
            pData.Levels.Add(new LevelInfo { LevelID = 1, Stars = 0, XP = 0, LevelLocked = 0 });
            PlayerDataManager.Instance.SavePlayerData(); // Save the repair to PlayFab
        }
        else if (level1.LevelLocked == 1)
        {
            // If it exists but is accidentally locked, force it open
            level1.LevelLocked = 0;
            PlayerDataManager.Instance.SavePlayerData(); // Save the repair to PlayFab
        }

        // Draw the buttons on the map
        for (int i = 0; i < levelButtons.Length; i++)
        {
            LevelButtonManager btn = levelButtons[i];
            btn.SetLevelId(i + 1); 
            btn.isCurrentLevel = (i + 1 == currentLevel); 
            
            LevelInfo levelInfo = pData.Levels.Find(l => l.LevelID == btn.levelId);
            if (levelInfo != null)
            {
                btn.SetStar(levelInfo.Stars);
                btn.SetLocked(levelInfo.LevelLocked == 1);
                btn.GetComponent<Button>().interactable = (levelInfo.LevelLocked == 0);
            }
            else
            {
                // Any levels not found in the save file default to locked
                btn.SetStar(0);
                btn.SetLocked(true);
                btn.GetComponent<Button>().interactable = false; 
            }
        }
        SendDataToLeaderBoard();
    }

    IEnumerator EmojiLoading()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        yield return emojiRect.DOAnchorPosY(-1250f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();

        CheckForInternetConnection(); 
        yield return new WaitForSeconds(1f);

        if (PlayerDataManager.Instance.isOnline == false)
            ActiveNoInternetPanel();
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
        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == clickedButton)
            {
                clickedIndex = i;
                break;
            }
        }

        if (clickedIndex == -1) yield break; 

        LevelInfo levelInfo = PlayerDataManager.Instance.playerData.Levels.Find(l => l.LevelID == clickedButton.levelId);
        bool isLocked = levelInfo != null && levelInfo.LevelLocked == 1;

        if (!PlayerDataManager.Instance.isOnline)
        {
            ActiveNoInternetPanel();
            yield break; 
        }

        if (isLocked)
        {
            OnLockedLevelClicked(clickedButton.levelId); 
            yield break;
        }

        int currentEnergy = PlayerDataManager.Instance.GetEnergyCount();
        if (currentEnergy <= 0)
        {
            NoEnergyLeftPanel.SetActive(true); 
            yield break; 
        }

        PlayerDataManager.Instance.RemoveEnergy(1);
        if (CurrentEnergyText != null)
        {
            CurrentEnergyText.text = PlayerDataManager.Instance.GetEnergyCount().ToString();
        }

        yield return StartCoroutine(EmojiLoading());

        selectedLevelIndex = clickedIndex;
        PlayerPrefs.SetInt(SelectedLevelIndexKey, clickedButton.levelId - 1);
        PlayerPrefs.Save();

        SceneManager.LoadScene("MainGame");
    }

    void OnLockedLevelClicked(int levelId)
    {
        Debug.Log($"StageManager: Level {levelId} is locked. Please unlock it first.");
    }

    public void RefreshLocalUI()
    {
        ApplyDataToButtons();
        ShowTotalXPandTotalStars(); 
    }

    public void ShowTotalXPandTotalStars()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.playerData == null)
        {
            Invoke(nameof(ShowTotalXPandTotalStars), 0.5f);
            return;
        }

        var pData = PlayerDataManager.Instance.playerData;

        totalXP = 0;
        totalStars = 0;
        
        if (pData.Levels != null)
        {
            foreach (LevelInfo level in pData.Levels)
            {
                totalXP += level.XP;
                totalStars += level.Stars;
            }
        }
        
        TotalXP.text = $"{totalXP}";
        TotalStar.text = $"{totalStars}";
        Name.text = pData.Name;
        CurrentEnergyText.text = pData.EnergyCount.ToString();
        
        // This is now guaranteed to match exactly what is in the save file!
        bombAbilityCount.text = pData.PlayerBombAbilityCount.ToString();
        colorBombAbilityCount.text = pData.PlayerColorBombAbilityCount.ToString();
        extraMoveAbilityCount.text = pData.PlayerExtraMoveAbilityCount.ToString();
        ShuffleAbilityCount.text = pData.PlayerShuffleAbilityCount.ToString();
    }

    private void FetchPlayerDataFromPlayFab()
    {
        if (!PlayerDataManager.Instance.isOnline) return;
        var request = new GetUserDataRequest();
        PlayFabClientAPI.GetUserData(request, OnPlayFabDataReceived, OnPlayFabError);
    }

    private void OnPlayFabDataReceived(GetUserDataResult result)
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.playerData == null) return;
        var pData = PlayerDataManager.Instance.playerData;

        if (result.Data != null)
        {
            if (result.Data.ContainsKey("PlayerName")) pData.Name = result.Data["PlayerName"].Value;
            if (result.Data.ContainsKey("PlayerID")) pData.PlayerID = result.Data["PlayerID"].Value;
            if (result.Data.ContainsKey("CurrentLevelId")) pData.CurrentLevelId = int.Parse(result.Data["CurrentLevelId"].Value);
            if (result.Data.ContainsKey("PlayerEnergyCount")) pData.EnergyCount = int.Parse(result.Data["PlayerEnergyCount"].Value);

            // First-time players should start with 3 boosters if PlayFab has no saved value yet.
            pData.PlayerBombAbilityCount = result.Data.ContainsKey("PlayerBombAbilityCount") ? int.Parse(result.Data["PlayerBombAbilityCount"].Value) : 3;
            pData.PlayerColorBombAbilityCount = result.Data.ContainsKey("PlayerColorBombAbilityCount") ? int.Parse(result.Data["PlayerColorBombAbilityCount"].Value) : 3;
            pData.PlayerExtraMoveAbilityCount = result.Data.ContainsKey("PlayerExtraMoveAbilityCount") ? int.Parse(result.Data["PlayerExtraMoveAbilityCount"].Value) : 3;
            pData.PlayerShuffleAbilityCount = result.Data.ContainsKey("PlayerShuffleAbilityCount") ? int.Parse(result.Data["PlayerShuffleAbilityCount"].Value) : 3;

            if (result.Data.ContainsKey("Levels"))
            {
                string levelsJson = result.Data["Levels"].Value;
                LevelListWrapper wrapper = JsonUtility.FromJson<LevelListWrapper>(levelsJson);
                pData.Levels = wrapper.Levels;
            }
            
            PlayerDataManager.Instance.SavePlayerData();

            ApplyDataToButtons();
            ShowTotalXPandTotalStars();
            CheckAndShowNamePanel();
        }
    }

    private void OnPlayFabError(PlayFabError error)
    {
        Debug.LogError("Error fetching PlayFab data: " + error.GenerateErrorReport());
        ApplyDataToButtons();
        ShowTotalXPandTotalStars();
    }

    public void CheckAndShowNamePanel()
    {
        if (!PlayerDataManager.Instance.isFoundName)
            namePanel.SetActive(true); 
        else
            namePanel.SetActive(false); 
    }

    public void RefreashData()
    {
        CheckForInternetConnection();

        if (PlayerDataManager.Instance.isOnline)
            FetchPlayerDataFromPlayFab();
        else
        {
            ApplyDataToButtons();
            ShowTotalXPandTotalStars();
        }

        PlayerDataManager.Instance.CheckAndSetPlayerName();
        CheckAndShowNamePanel();
    }

    void GetCurrentLevelInt()
    {
        currentLevel = PlayerDataManager.Instance.currentLevel;
    }

    public void OnClickAbilityButton()
    {
        if (PlayerDataManager.Instance == null || PlayerDataManager.Instance.playerData == null) return;
        var pData = PlayerDataManager.Instance.playerData;

        if (pData.PlayerBombAbilityCount <= 0 && pData.PlayerColorBombAbilityCount <= 0 && pData.PlayerExtraMoveAbilityCount <= 0)
            spinButton.ShowButton(); 
        else
            shopUI.SetActive(true); 
    }

    public void SendDataToLeaderBoard()
    {
        PlayerDataManager.Instance.SendLeaderboard(totalXP); 
    }

    public void GetDataFromLeaderboard()
    {
        PlayerDataManager.Instance.GetLeaderboard();
    }

    public void SetUserName()
    {
        string userName = userNameInput.text.Trim();
        if (string.IsNullOrEmpty(userName)) return;

        PlayerDataManager.Instance.SetName(userName);
        PlayerDataManager.Instance.SavePlayerData();
        UserNameUpdated();
    }

    public void OnClickExitButton()
    {
        Application.Quit();
    }

    public void UserNameUpdated()
    {
        UserNameUpdatedPanel.SetActive(true);
    }

    // ==========================================
    // REWARDED ADS SECTION (Via AdsManager)
    // ==========================================
    public void ShowRewardedAd_Bomb()
    {
        AdsManager.Instance.ShowRewardedAd(() => 
        {
            PlayerDataManager.Instance.AddBombAbility(1);
            PlayerDataManager.Instance.SavePlayerData(); 
            
            if (BombGetFromAdsPanel != null) BombGetFromAdsPanel.SetActive(true); 
            
            ShowTotalXPandTotalStars();
            GridManager grid = FindObjectOfType<GridManager>();
            if (grid != null) grid.RefreshAbilities(); 
        });
    }

    public void ShowRewardedAd_Clown()
    {
        AdsManager.Instance.ShowRewardedAd(() => 
        {
            PlayerDataManager.Instance.AddColorBombAbility(1);
            PlayerDataManager.Instance.SavePlayerData(); 

            if (ColorBombGetFromAdsPanel != null) ColorBombGetFromAdsPanel.SetActive(true); 

            ShowTotalXPandTotalStars(); 
            GridManager grid = FindObjectOfType<GridManager>();
            if (grid != null) grid.RefreshAbilities(); 
        });
    }

    public void ShowRewardedAd_Moves()
    {
        AdsManager.Instance.ShowRewardedAd(() => 
        {
            PlayerDataManager.Instance.AddExtraMoveAbility(1);
            PlayerDataManager.Instance.SavePlayerData();

            if (ExtraMovesGetFromAdsPanel != null) ExtraMovesGetFromAdsPanel.SetActive(true); 

            ShowTotalXPandTotalStars(); 
            GridManager grid = FindObjectOfType<GridManager>();
            if (grid != null) grid.RefreshAbilities();
        });
    }

    public void ShowRewardedAd_SkipEnergyGenerateTime()
    {
        AdsManager.Instance.ShowRewardedAd(() => 
        {
            PlayerDataManager.Instance.SkipEnergyGenerateTime();
            PlayerDataManager.Instance.SavePlayerData(); 

            if (NoEnergyLeftPanel != null) NoEnergyLeftPanel.SetActive(false);
            if (energyTimerUI != null) energyTimerUI.UpdateUI();
            
            ShowTotalXPandTotalStars(); 
        });
    }

    public void ShowRewardedAd_Shuffle()
    {
        AdsManager.Instance.ShowRewardedAd(() => 
        {
            PlayerDataManager.Instance.AddShuffleAbility(1); 
            PlayerDataManager.Instance.SavePlayerData(); 
            
            ShowTotalXPandTotalStars(); 
            GridManager grid = FindObjectOfType<GridManager>();
            if (grid != null) grid.RefreshAbilities();
        });
    }

    public void ActiveNoInternetPanel()
    {
        NoInternetConnectionPanel.SetActive(true);
    }

    public void RetryConnection()
    {
        NoInternetConnectionPanel.SetActive(false);
        PlayerDataManager.Instance.CheckInternetConnection();
        PlayerDataManager.Instance.LoginAsGuest();
        CheckForInternetConnection(); 
    }
}