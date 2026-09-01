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
    private bool isStartingLevel = false;

    private void OnEnable()
    {
        isStartingLevel = false;
        RefreshLocalUI();
    }

    void Start()
    {
        mapHomeButton.ShowButton(); 
        AudioManager.Instance.PlayMusic("MenuBG");
        
        LoadPlayerData(); 

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        namePanel.SetActive(false); 
    }

    public void SyncLocalBooster(string type, int amount)
    {
        ShowTotalXPandTotalStars(); 
    }

    private void Update()
    {
        // Optimized: GetCurrentLevelInt called on data refresh instead of per-frame

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

        // ?? THE CLOUD REPAIR: If PlayFab gives us a corrupted list, recreate it!
        if (pData.Levels == null)
        {
            pData.Levels = new List<LevelInfo>();
        }

        // ?? FORCE UNLOCK LEVEL 1: Check if Level 1 exists in the save file
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
            if (btn == null) continue;

            btn.SetLevelId(i + 1); 
            btn.SetCurrentLevel(i + 1 == currentLevel);
            
            LevelInfo levelInfo = pData.Levels != null ? pData.Levels.Find(l => l.LevelID == btn.levelId) : null;
            bool isUnlocked = (levelInfo != null && levelInfo.LevelLocked == 0) || (btn.levelId <= currentLevel);

            btn.SetStar(levelInfo != null ? levelInfo.Stars : 0);
            btn.SetLocked(!isUnlocked);

            Button uiButton = btn.GetComponent<Button>();
            if (uiButton != null)
            {
                uiButton.interactable = isUnlocked;
            }
        }

        FindObjectOfType<DynamicMapScroller>()?.UpdateMapHeight();
        SendDataToLeaderBoard();
    }

    IEnumerator EmojiLoading()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        if (emojiRect != null)
        {
            emojiRect.anchoredPosition = new Vector2(0f, 3000f);
            yield return emojiRect.DOAnchorPosY(0f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }

        CheckForInternetConnection(); 
        yield return new WaitForSeconds(0.2f);

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
        if (isStartingLevel) return;
        StartCoroutine(SelectLevelCoroutine(clickedButton));
    }

    private IEnumerator SelectLevelCoroutine(LevelButtonManager clickedButton)
    {
        if (isStartingLevel) yield break;

        // Check internet connection
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            ActiveNoInternetPanel();
            yield break;
        }

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

        LevelInfo levelInfo = PlayerDataManager.Instance.playerData.Levels != null ?
            PlayerDataManager.Instance.playerData.Levels.Find(l => l.LevelID == clickedButton.levelId) : null;
        bool isLocked = levelInfo != null ? (levelInfo.LevelLocked == 1) : (clickedButton.levelId > PlayerDataManager.Instance.currentLevel);

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

        // Debounce lock
        isStartingLevel = true;

        // Energy is now only subtracted if the player loses or exits/restarts mid-game!
        // PlayerDataManager.Instance.RemoveEnergy(1);
        // if (CurrentEnergyText != null)
        // {
        //     CurrentEnergyText.text = PlayerDataManager.Instance.GetEnergyCount().ToString();
        // }

        // Smooth emoji curtain slide down to cover screen
        RectTransform emojiRect = EmojisImage != null ? EmojisImage.GetComponent<RectTransform>() : null;
            emojiRect.transform.SetAsLastSibling();
            emojiRect.gameObject.SetActive(true);
        if (emojiRect != null)
        {
            emojiRect.transform.localScale = Vector3.one;
            emojiRect.anchoredPosition = new Vector2(0f, 3000f);
            yield return emojiRect.DOAnchorPosY(0f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }

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
        // Centralized through PlayerDataManager to prevent race conditions and duplicate queries
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.LoadPlayerData();
        }
        else
        {
            RefreshLocalUI();
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
            PlayerDataManager.Instance.AddEnergy(1);
            if (CurrentEnergyText != null)
            {
                CurrentEnergyText.text = PlayerDataManager.Instance.GetEnergyCount().ToString();
            }
            if (NoEnergyLeftPanel != null)
            {
                NoEnergyLeftPanel.SetActive(false);
            }
            RefreshLocalUI();
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
