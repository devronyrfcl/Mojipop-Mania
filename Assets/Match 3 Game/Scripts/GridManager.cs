using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
//For 



[System.Serializable]
public class SpriteData
{
    public Sprite sprite;
    public Vector3 targetScale = Vector3.one; // Default (1,1,1), you can customize per sprite
}


public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public GameObject[] piecePrefabs;// Array of piece prefabs to instantiate
    public GameObject[,] grid; // 2D array to hold the grid pieces
    public Piece[] pieces; // Array to hold all pieces in the game
    public LevelData levelData; // Reference to the LevelData ScriptableObject

    public LevelData[] levelDatas; // Array of LevelData ScriptableObjects for different levels
    public string missingBoosterType = ""; // Remembers what we need to reward

    public int currentLevelIndex = 0;
    public int levelIndexFromJson;
    private const string SelectedLevelIndexKey = "SelectedLevelIndex";


    public GameObject brickPrefab; // Prefab for the brick piece
    public GameObject particlePrefab; // Prefab for the particle effect
    public GameObject GridBackgroundBlock; // Array of background block prefabs for the grid
    public bool isPlacingBomb = false;
    public bool isPlacingColor = false;
    public bool canControl = true;
    public bool isGameOver = false;
    private bool isGameOverTriggered = false;

    private bool isRefilling = false; // Track if grid is currently refilling
    private bool needsAnotherRefill = false;
    private bool hasPendingMatches = false; // Track if matches were found during refill
    private float controlUnlockTimer = 0f;
    public int lastSwapX = -1;
    public int lastSwapY = -1;
    public int lastSwapX1 = -1;
    public int lastSwapY1 = -1;
    public int lastSwapX2 = -1;
    public int lastSwapY2 = -1;
    private bool specialSpawnClaimed;




    [Header("Main Game Visuals")]
    public GameObject EmojisImage;

    public TextMeshProUGUI timeText;
    //public int currentTime; // Current time in seconds
    public TextMeshProUGUI movesCountText;

    //Ability UI Elements
    public TextMeshProUGUI Ability_bombCountText;
    public TextMeshProUGUI Ability_ColorBombCountText;
    public TextMeshProUGUI Ability_extraMovesCountText;
    public TextMeshProUGUI Ability_shuffleCountText;
    /*public int Ability_bombStartAmount;
    public int Ability_colorBombStartAmount;
    public int Ability_extraMovesStartAmount;*/
    public int Ability_bombCurrentAmount;
    public int Ability_colorBombCurrentAmount;
    public int Ability_extraMovesCurrentAmount;
    public int Ability_shuffleCurrentAmount;
    public SpriteData[] sprites; // Each sprite has its own target scale
    public Image targetImage; // Target Image component
    public float scaleDuration = 0.3f; // How fast it scales
    public float holdDuration = 0.5f;  // Hold time before scaling back
    private Sequence currentSequence; // Track current tween sequence

    public Button RestartButton;
    public Button NextLevelButton;

    public GameObject WinMainMenuButton;
    public GameObject LoseMainMenuButton;



    /*private int bombAmount;
    private int colorAmount;
    private int extraMovesAmount;*/

    public float currentTime;
    public int currentMoves;
    private int currentTarget1;
    private int currentTarget2;

    public GameObject GameOverPanel;
    public TMP_Text gameOverTitleText; // Text to display game over title
    public GameObject Shine1;
    public GameObject Shine2;

    public TMP_Text gameOverText; // Text to display game over message
    public TMP_Text level_Count;
    public GameObject itemWarningPanel;
    public int stars = 0;
    public int XP = 0; // XP earned in the level
    public GameObject[] normalStars; // Empty stars
    public GameObject[] glowStars;   // Filled stars
    public TMP_Text xpAmount;

    public GameObject moveTargetUI;
    public GameObject timeTargetUI;

    //react transform of move image
    public RectTransform imageSpawm;
    public RectTransform imageTarget;
    public GameObject moveImage;

    public Canvas mainCanvas;

    public GameObject NoInternetConnectionPanel;
    public GameObject NoInternetPanelInside;

    public GameObject SettingsPanel;
    [Header("Warning Panel Dynamic Icons")]
    public Image warningPanelIcon; // The UI Image inside the popup that needs to change
    public Sprite bombSprite;
    public Sprite clownSprite;
    public Sprite movesSprite;
    public Sprite shuffleSprite;







    [Header("Targets Section")]
    public Sprite smilingFaceSprite;
    public Sprite smilingFaceWithTearSprite;
    public Sprite angryFaceSprite;
    public Sprite laughingFaceSprite;
    public Sprite smilingFaceWithHeartEyesSprite;
    public Sprite sleepingFaceSprite;
    public Sprite surprisedFaceSprite;
    public Sprite cryingFaceSprite;

    public Image target1Image; // Image to represent target1
    public Image target2Image; // Image to represent target2
    public TMP_Text target1CountText; // TextMeshPro text for target1 count
    public TMP_Text target2CountText; // TextMeshPro text for target2 count

    private int currentTarget1Count;
    private int currentTarget2Count;

    public GameObject horizontalClearParticle;
    public GameObject verticalClearParticle;

    public bool isTimerRunning = false;
    private bool isTimeExpired = false;
    private bool hasMadeFinalSwipeAfterTimeExpired = false;
    //public string fileName = "playerdata.json";

    //private string SavePath; // = Path.Combine(Application.persistentDataPath, "playerdata.json");



    #region "Common Region"
    // Start is called before the first frame update
    void Start()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        grid = new GameObject[levelData.gridWidth, levelData.gridHeight];
        pieces = new Piece[levelData.gridWidth * levelData.gridHeight];
        //SavePath = Path.Combine(Application.persistentDataPath, "playerdata.json");

        LoadLevel();

        //AudioManager.Instance.PlayMusic("MenuBG");
        GameOverPanel.transform.localScale = Vector3.zero; // Start from scale 0
        isGameOverTriggered = false;
        isGameOver = false;

        SpawnGridBackgroundBlock(); // Call the method to spawn background blocks
        //CreateGrid(); // Call the method to create the grid and place pieces

        //timescale will be 1
        //Time.timeScale = 0.2f;

        StartCoroutine(EmojiLoading()); // Start the emoji loading coroutine

        currentTime = levelData.timeLimit;
        currentMoves = levelData.movesCount;
        currentTarget1 = levelData.target1Count;
        currentTarget2 = levelData.target2Count;

        //get levelIndexFromJson




        UpdateUI();

        StartTimer();

        /*//ability start value
        Ability_bombCurrentAmount = Ability_bombStartAmount;
        Ability_colorBombCurrentAmount = Ability_colorBombStartAmount;
        Ability_extraMovesCurrentAmount = Ability_extraMovesStartAmount;*/



        if (levelData == null)
        {
            Debug.LogError("LevelData not found in GridManager!");
            return;
        }

        // Initialize counts from LevelData
        currentTarget1Count = levelData.target1Count;
        currentTarget2Count = levelData.target2Count;

        // Assign sprites based on LevelData piece types
        target1Image.sprite = GetSpriteForPiece(levelData.target1Piece);
        target2Image.sprite = GetSpriteForPiece(levelData.target2Piece);

        UpdateUI();


        LoadPlayerAbilities();

        if (levelData.isMovesLevel)
        {
            moveTargetUI.SetActive(true);
            timeTargetUI.SetActive(false);
            currentTime = Mathf.Infinity; // Set time to infinity for moves-based levels
        }
        else if (levelData.isTimedLevel)
        {
            moveTargetUI.SetActive(false);
            timeTargetUI.SetActive(true);
            currentMoves = 100000000;
        }
        else
        {
            moveTargetUI.SetActive(true);
            timeTargetUI.SetActive(true);
        }

        NextLevelButton.gameObject.SetActive(false); // Hide Next Level button initially
        RestartButton.gameObject.SetActive(false);
        WinMainMenuButton.SetActive(false);
        LoseMainMenuButton.SetActive(false);

        // Initialize No Internet Connection Panel as inactive & inside panel size 0
        NoInternetConnectionPanel.SetActive(false);
        NoInternetPanelInside.transform.localScale = Vector3.zero;

    }

    private void Awake()
    {
        if (targetImage != null)
            targetImage.transform.localScale = Vector3.zero; // Start hidden
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

    public void UpdateUI()
    {
        movesCountText.text = currentMoves.ToString();

        //ability UI
        Ability_bombCountText.text = Ability_bombCurrentAmount.ToString();
        Ability_ColorBombCountText.text = Ability_colorBombCurrentAmount.ToString();
        Ability_extraMovesCountText.text = Ability_extraMovesCurrentAmount.ToString();
        Ability_shuffleCountText.text = Ability_shuffleCurrentAmount.ToString();

        if (target1CountText != null)
            target1CountText.text = currentTarget1Count.ToString();
        if (target2CountText != null)
            target2CountText.text = currentTarget2Count.ToString();




    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (SettingsPanel != null) SettingsPanel.SetActive(true);
        }

        // Safety Watchdog: If board is not refilling and game is active, ensure control is never frozen
        if (!isRefilling && !canControl && !isGameOver && (GameOverPanel == null || !GameOverPanel.activeSelf))
        {
            controlUnlockTimer += Time.deltaTime;
            if (controlUnlockTimer > 1.2f)
            {
                canControl = true;
                controlUnlockTimer = 0f;
            }
        }
        else
        {
            controlUnlockTimer = 0f;
        }

        if (!isTimerRunning)
        {
            return;
        }

        currentTime -= Time.deltaTime;
        UpdateUI();

        if (currentTime <= 0)
        {
            currentTime = 0;
            isTimerRunning = false;
            GameOverLogic();
        }
    }
    public void GoToNextLevel()
    {
        // Unlock the next level
        PlayerDataManager.Instance.SetAllData(currentLevelIndex + 2, 0, 0, 0);

        // Set the current level to the next level
        //PlayerDataManager.Instance.SetCurrentLevel(currentLevelIndex + 2);

        // Get the previous current level from PlayerData
        PlayerData playerData = PlayerDataManager.Instance.playerData;
        int previousCurrentLevel = playerData.CurrentLevelId;

        // Only update current level if the new level is higher
        if (currentLevelIndex + 2 >= previousCurrentLevel)
        {
            PlayerDataManager.Instance.SetCurrentLevel(currentLevelIndex + 2);
        }

        // Update the selected level index in PlayerPrefs
        int nextLevelIndex = currentLevelIndex + 1;
        PlayerPrefs.SetInt(SelectedLevelIndexKey, nextLevelIndex);
        PlayerPrefs.Save(); // Save PlayerPrefs

        // Reload the current scene to load the new level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //back to main menu with same logic as above
    public void BackToMainMenuFromNextLevel()
    {
        // Unlock the next level
        PlayerDataManager.Instance.SetAllData(currentLevelIndex + 2, 0, 0, 0);
        // Set the current level to the next level
        //PlayerDataManager.Instance.SetCurrentLevel(currentLevelIndex + 2);
        // Get the previous current level from PlayerData
        PlayerData playerData = PlayerDataManager.Instance.playerData;
        int previousCurrentLevel = playerData.CurrentLevelId;
        // Only update current level if the new level is higher
        if (currentLevelIndex + 2 >= previousCurrentLevel)
        {
            PlayerDataManager.Instance.SetCurrentLevel(currentLevelIndex + 2);
        }
        // Update the selected level index in PlayerPrefs
        int nextLevelIndex = currentLevelIndex + 1;
        PlayerPrefs.SetInt(SelectedLevelIndexKey, nextLevelIndex);
        PlayerPrefs.Save(); // Save PlayerPrefs
        // Load main menu scene
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartCurrentLevel()
    {
        // If they restart mid-game (before Game Over screen was triggered)
        if (!isGameOverTriggered)
        {
            PlayerDataManager.Instance.RemoveEnergy(1);
        }
        // Reload the current scene to restart the level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GameOverLogic()
    {
        if (isGameOverTriggered)
        {
            return;
        }

        bool targetsMet = currentTarget1Count <= 0 && currentTarget2Count <= 0;
        bool outOfMovesOrTime = (currentTime <= 0 || currentMoves <= 0);

        if (targetsMet || outOfMovesOrTime)
        {
            isGameOverTriggered = true;
            isGameOver = true;
            canControl = false;
            StartCoroutine(GameOver());
        }
    }
    public void RefreshAbilities()
    {
        Ability_bombCurrentAmount = PlayerDataManager.Instance.GetPlayerBombAbilityCount();
        Ability_colorBombCurrentAmount = PlayerDataManager.Instance.GetPlayerColorBombAbilityCount();
        Ability_extraMovesCurrentAmount = PlayerDataManager.Instance.GetPlayerExtraMoveAbilityCount();
        Ability_shuffleCurrentAmount = PlayerDataManager.Instance.GetPlayerShuffleAbilityCount();
        
        // Automatically hide the warning panel since they just got the item!
        if (itemWarningPanel != null) 
        {
            itemWarningPanel.SetActive(false);
        }

        UpdateUI();
    }



    private void FixedUpdate()
    {
        UpdateTimeText();
    }


    void LoadLevel()
    {
        // Safety check: Ensure we have level data assigned
        if (levelDatas == null || levelDatas.Length == 0)
        {
            Debug.LogError("No LevelData assigned in GridManager!");
            return;
        }

        // Load the selected level index from PlayerPrefs (default to 0)
        currentLevelIndex = PlayerPrefs.GetInt(SelectedLevelIndexKey, 0);

        // Clamp the index to ensure it's valid
        if (currentLevelIndex < 0 || currentLevelIndex >= levelDatas.Length)
        {
            Debug.LogWarning($"Invalid level index ({currentLevelIndex}). Resetting to 0.");
            currentLevelIndex = 0;
        }

        // Assign the selected LevelData
        levelData = levelDatas[currentLevelIndex];

        // The selected level can have different dimensions from the inspector's
        // default LevelData, so allocate after selection rather than reusing it.
        grid = new GameObject[levelData.gridWidth, levelData.gridHeight];
        pieces = new Piece[levelData.gridWidth * levelData.gridHeight];
        CreateGrid();



    }

    /*void LoadPlayerAbilities()
    {
        //savePath = 

        // Load player abilities from the JSON file
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(json);
            Ability_bombCurrentAmount = playerData.PlayerBombAbilityCount;
            Ability_colorBombCurrentAmount = playerData.PlayerColorBombAbilityCount;
            Ability_extraMovesCurrentAmount = playerData.PlayerExtraMoveAbilityCount;
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("Save file not found. Using default ability values.");
        }
    }*/

    void LoadPlayerAbilities()
    {
        //savePath = 

        // Load player abilities from the JSON file
        /*if (File.Exists(SavePath))
        {
            // Read encrypted JSON
            string encryptedJson = File.ReadAllText(SavePath);

            // Decrypt JSON
            string decryptedJson = XorEncryptDecrypt(encryptedJson);

            // Parse into PlayerData
            PlayerData playerData = JsonUtility.FromJson<PlayerData>(decryptedJson);

            Ability_bombCurrentAmount = playerData.PlayerBombAbilityCount;
            Ability_colorBombCurrentAmount = playerData.PlayerColorBombAbilityCount;
            Ability_extraMovesCurrentAmount = playerData.PlayerExtraMoveAbilityCount;
            Ability_shuffleCurrentAmount = playerData.PlayerShuffleAbilityCount;

            UpdateUI();
            Debug.Log("Player abilities loaded (decrypted).");
        }
        else
        {
            Debug.LogWarning("Save file not found. Using default ability values.");
        }*/
        //load from PlayerDataManager
        Ability_bombCurrentAmount = PlayerDataManager.Instance.GetPlayerBombAbilityCount();
        Ability_colorBombCurrentAmount = PlayerDataManager.Instance.GetPlayerColorBombAbilityCount();
        Ability_extraMovesCurrentAmount = PlayerDataManager.Instance.GetPlayerExtraMoveAbilityCount();
        Ability_shuffleCurrentAmount = PlayerDataManager.Instance.GetPlayerShuffleAbilityCount();
        UpdateUI();


    }


    // Method to save new ability counts and update the UI. just change the values of ability counts.
    void SaveNewAbilityCounts(int bombCount, int colorBombCount, int extraMovesCount, int shuffleCount)
    {
        PlayerDataManager.Instance.SetPlayerBombAbilityCount(bombCount);
        PlayerDataManager.Instance.SetPlayerColorBombAbilityCount(colorBombCount);
        PlayerDataManager.Instance.SetPlayerExtraMoveAbilityCount(extraMovesCount);
        PlayerDataManager.Instance.SetPlayerShuffleAbilityCount(shuffleCount);
        PlayerDataManager.Instance.SavePlayerData(); // Save the updated player data to the JSON file

    }


    void GameOverHelper()
    {
        

        StartCoroutine(GameOver());
    }


    IEnumerator GameOver()
    {
        yield return new WaitForSeconds(0.8f);

        bool levelCompleted = currentTarget1Count <= 0 && currentTarget2Count <= 0;

        if (levelCompleted)
        {
            if (gameOverTitleText != null) gameOverTitleText.text = "Congratulations!";
            if (Shine1 != null) Shine1.SetActive(true);
            if (Shine2 != null) Shine2.SetActive(false);
            AudioManager.Instance?.PlaySFX("GameWin");
            if (NextLevelButton != null) NextLevelButton.gameObject.SetActive(true);
            if (WinMainMenuButton != null) WinMainMenuButton.SetActive(true);
            if (RestartButton != null) RestartButton.gameObject.SetActive(false);
            if (LoseMainMenuButton != null) LoseMainMenuButton.SetActive(false);

            Invoke("CalculateStarAndShow", 0.5f);
        }
        else
        {
            // Player lost the level, deduct 1 energy/life
            PlayerDataManager.Instance.RemoveEnergy(1);

            if (gameOverTitleText != null) gameOverTitleText.text = "Game Over!";
            if (Shine1 != null) Shine1.SetActive(false);
            if (Shine2 != null) Shine2.SetActive(true);
            AudioManager.Instance?.PlaySFX("GameLose");
            if (RestartButton != null) RestartButton.gameObject.SetActive(true);
            if (LoseMainMenuButton != null) LoseMainMenuButton.SetActive(true);
            if (NextLevelButton != null) NextLevelButton.gameObject.SetActive(false);
            if (WinMainMenuButton != null) WinMainMenuButton.SetActive(false);

            // On Game Over: Reset stars to 0 and XP to 0
            stars = 0;
            XP = 0;

            for (int i = 0; i < 3; i++)
            {
                if (glowStars != null && i < glowStars.Length && glowStars[i] != null)
                    glowStars[i].SetActive(false);
                if (normalStars != null && i < normalStars.Length && normalStars[i] != null)
                    normalStars[i].SetActive(true);
            }

            if (xpAmount != null)
                xpAmount.text = "0";
        }

        if (gameOverText != null) gameOverText.text = "Level :" + (currentLevelIndex + 1);
        if (level_Count != null) level_Count.text = (currentLevelIndex + 1).ToString();

        isTimerRunning = false;
        canControl = false;

        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);
            GameOverPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        }

        SaveNewAbilityCounts(Ability_bombCurrentAmount, Ability_colorBombCurrentAmount, Ability_extraMovesCurrentAmount, Ability_shuffleCurrentAmount);
    }
    public void CalculateStarAndShow()
    {
        if (currentTarget1Count > 0 || currentTarget2Count > 0)
        {
            stars = 0;
            XP = 0;
            Debug.Log("Targets not met - 0 stars.");
            return;
        }

        if (levelData.isTimedLevel)
        {
            float ratio = levelData.timeLimit > 0 ? (currentTime / (float)levelData.timeLimit) : 0f;
            if (ratio >= 0.5f)
            {
                stars = 3;
                XP = 20;
            }
            else if (ratio >= 0.25f)
            {
                stars = 2;
                XP = 10;
            }
            else
            {
                stars = 1;
                XP = 5;
            }
        }
        else // Moves level or default
        {
            float ratio = levelData.movesCount > 0 ? (currentMoves / (float)levelData.movesCount) : 0f;
            if (ratio >= 0.5f)
            {
                stars = 3;
                XP = 20;
            }
            else if (ratio >= 0.25f)
            {
                stars = 2;
                XP = 10;
            }
            else
            {
                stars = 1;
                XP = 5;
            }
        }

        // Show the stars UI
        for (int i = 0; i < 3; i++)
        {
            if (glowStars != null && i < glowStars.Length && glowStars[i] != null)
                glowStars[i].SetActive(i < stars);
            if (normalStars != null && i < normalStars.Length && normalStars[i] != null)
                normalStars[i].SetActive(i >= stars);
        }

        // Update XP UI
        if (xpAmount != null)
            xpAmount.text = XP.ToString();
        Debug.Log("Level Complete! Stars: " + stars + ", XP: " + XP);

        // Send star and xp data to PlayerDataManager
        SendStarXpDataToPlayerDataManager(currentLevelIndex + 1, 0, stars, XP);
    }
    void SendStarXpDataToPlayerDataManager(int levelId, int lockedValue, int stars, int xp)
    {

        PlayerDataManager.Instance.SetLevelStars(levelId, stars, xp);
        PlayerDataManager.Instance.SendXP(levelId, xp);

        PlayerDataManager.Instance.SetAllData(currentLevelIndex + 2, 0, 0, 0);// Set the next level data to 0 stars and 0 XP, and unlock it



        //PlayerDataManager.Instance.SetCurrentLevel(currentLevelIndex + 2);// Set the current level to the next level
        // Get the previous current level from PlayerData
        PlayerData playerData = PlayerDataManager.Instance.playerData;
        int previousCurrentLevel = playerData.CurrentLevelId;

        // Only update current level if the new level is higher
        if (currentLevelIndex + 2 >= previousCurrentLevel)
        {
            PlayerDataManager.Instance.SetCurrentLevel(currentLevelIndex + 2);
        }
        //PlayerDataManager.Instance.SetLevelLocked(currentLevelIndex + 2, 0); // Unlock the next level (currentLevelIndex + 2 because levels are 1-based in PlayerDataManager)


        PlayerDataManager.Instance.SavePlayerData(); // Save the updated player data to the JSON file



    }




    public void BackToMainMenu()
    {
        // Load the main menu scene
        StartCoroutine(EmojiLoading_2());

        PlayerDataManager.Instance.GetCurrentLevel(); // Initialize current level after creating new player

        //save player data
        PlayerDataManager.Instance.SavePlayerData();

    }


    IEnumerator EmojiLoading()
    {
        RectTransform emojiRect = EmojisImage != null ? EmojisImage.GetComponent<RectTransform>() : null;
        canControl = false;
        if (emojiRect != null)
        {
            emojiRect.transform.SetAsLastSibling();
            emojiRect.gameObject.SetActive(true);
            emojiRect.transform.localScale = Vector3.one;
            emojiRect.anchoredPosition = new Vector2(0f, 0f);
            yield return emojiRect.DOAnchorPosY(-3000f, 0.7f).SetEase(Ease.InOutQuad).WaitForCompletion();
            emojiRect.gameObject.SetActive(false);
        }
        canControl = true;
    }

    IEnumerator EmojiLoading_2()
    {
        RectTransform emojiRect = EmojisImage != null ? EmojisImage.GetComponent<RectTransform>() : null;
        if (emojiRect != null)
        {
            emojiRect.transform.SetAsLastSibling();
            emojiRect.gameObject.SetActive(true);
            emojiRect.transform.localScale = Vector3.one;
            emojiRect.anchoredPosition = new Vector2(0f, 3000f);
            yield return emojiRect.DOAnchorPosY(0f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitToMainMenu()
    {
        // If they quit mid-game (before Game Over screen was triggered)
        if (!isGameOverTriggered)
        {
            PlayerDataManager.Instance.RemoveEnergy(1);
        }
        StartCoroutine(EmojiLoading_2());
        StartCoroutine(ExitScene());
    }

    IEnumerator ExitScene()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("MainMenu");
    }


    #endregion

    #region "Grid System"


    //the grid and place pieces using seed from LevelData
    private List<GameObject> cachedActivePool;

    public void ResetActivePiecePool()
    {
        cachedActivePool = null;
    }

    private List<GameObject> GetActivePiecePool()
    {
        if (cachedActivePool != null && cachedActivePool.Count > 0)
            return cachedActivePool;

        cachedActivePool = new List<GameObject>();
        if (piecePrefabs == null || piecePrefabs.Length == 0)
            return cachedActivePool;

        // 1. Always prioritize target pieces so the level goals can be solved
        GameObject target1Prefab = null;
        GameObject target2Prefab = null;

        foreach (GameObject prefab in piecePrefabs)
        {
            if (prefab == null) continue;
            Piece p = prefab.GetComponent<Piece>();
            if (p == null) continue;

            if (levelData != null && p.pieceType == levelData.target1Piece && target1Prefab == null)
                target1Prefab = prefab;
            if (levelData != null && p.pieceType == levelData.target2Piece && target2Prefab == null)
                target2Prefab = prefab;
        }

        if (target1Prefab != null) cachedActivePool.Add(target1Prefab);
        if (target2Prefab != null && target2Prefab != target1Prefab) cachedActivePool.Add(target2Prefab);

        // 2. Add other piece types up to a friendly pool of 4-5 colors per level
        int maxColorsPerLevel = (levelData != null && levelData.colorCount >= 3) ? levelData.colorCount : 4;
        foreach (GameObject prefab in piecePrefabs)
        {
            if (prefab == null || cachedActivePool.Contains(prefab)) continue;

            if (cachedActivePool.Count < maxColorsPerLevel)
            {
                cachedActivePool.Add(prefab);
            }
        }

        if (cachedActivePool.Count == 0)
        {
            cachedActivePool.AddRange(piecePrefabs);
        }

        return cachedActivePool;
    }

    private GameObject GetRandomPiecePrefab(int gridX = -1, int gridY = -1)
    {
        List<GameObject> pool = GetActivePiecePool();
        if (pool == null || pool.Count == 0)
            return piecePrefabs[Random.Range(0, piecePrefabs.Length)];

        // When generating the initial board, prevent creating 3-in-a-row right away
        if (gridX >= 0 && gridY >= 0)
        {
            List<GameObject> validChoices = new List<GameObject>(pool);
            for (int i = validChoices.Count - 1; i >= 0; i--)
            {
                Piece p = validChoices[i].GetComponent<Piece>();
                if (p == null) continue;

                bool leftMatch = gridX >= 2 &&
                    grid[gridX - 1, gridY] != null && grid[gridX - 2, gridY] != null &&
                    grid[gridX - 1, gridY].GetComponent<Piece>()?.pieceType == p.pieceType &&
                    grid[gridX - 2, gridY].GetComponent<Piece>()?.pieceType == p.pieceType;

                bool bottomMatch = gridY >= 2 &&
                    grid[gridX, gridY - 1] != null && grid[gridX, gridY - 2] != null &&
                    grid[gridX, gridY - 1].GetComponent<Piece>()?.pieceType == p.pieceType &&
                    grid[gridX, gridY - 2].GetComponent<Piece>()?.pieceType == p.pieceType;

                if (leftMatch || bottomMatch)
                {
                    validChoices.RemoveAt(i);
                }
            }

            if (validChoices.Count > 0)
                return validChoices[Random.Range(0, validChoices.Count)];
        }

        return pool[Random.Range(0, pool.Count)];
    }

    private void CreateGrid()
    {
        Random.InitState(levelData.GridSeed);
        ResetActivePiecePool();

        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (IsBlocked(x, y))
                {
                    grid[x, y] = null; // Explicitly mark as blocked
                    GameObject brick = Instantiate(brickPrefab, new Vector2(x, y), Quaternion.identity);
                    brick.name = "Brick (" + x + ", " + y + ")";
                    continue;
                }

                GameObject selectedPrefab = GetRandomPiecePrefab(x, y);
                GameObject newPiece = ObjectPoolManager.Spawn(
                    selectedPrefab,
                    new Vector2(x, y + 1f),
                    Quaternion.identity
                );

                Piece pieceScript = newPiece.GetComponent<Piece>();
                pieceScript.SetPosition(x, y);
                newPiece.transform.SetParent(transform);
                newPiece.name = pieceScript.pieceType.ToString() + " (" + x + ", " + y + ")";
                newPiece.transform.localScale = Vector3.zero;
                grid[x, y] = newPiece;
                newPiece.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                newPiece.transform.DOMove(new Vector2(x, y), 0.3f).SetEase(Ease.OutBounce);
            }
        }
    }
    void SpawnGridBackgroundBlock()
    {
        //spawn background block prefabs in the grid and it will be used to fill the grid background
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                GameObject block = Instantiate(GridBackgroundBlock, new Vector2(x, y), Quaternion.identity);
                block.transform.SetParent(transform);
                block.name = "Block (" + x + ", " + y + ")";
                block.transform.localScale = Vector3.one; // Set scale to one for visibility
            }
        }
    }



    public void UpdateGrid()
    {
        if (isRefilling)
        {
            needsAnotherRefill = true;
            return;
        }

        StartCoroutine(RefillGridCoroutine());
    }

    private IEnumerator RefillGridCoroutine()
    {
        isRefilling = true;
        canControl = false;

        bool keepRefilling = true;
        while (keepRefilling)
        {
            needsAnotherRefill = false;

            yield return new WaitForSeconds(0.2f);

            // Phase 1: Gravity - existing upper pieces fall down into empty spaces below them
            for (int x = 0; x < levelData.gridWidth; x++)
            {
                int fallDelayIndex = 0;
                for (int y = 0; y < levelData.gridHeight; y++)
                {
                    if (grid[x, y] == null && !IsBlocked(x, y))
                    {
                        for (int upperY = y + 1; upperY < levelData.gridHeight; upperY++)
                        {
                            if (grid[x, upperY] != null && !IsBlocked(x, upperY))
                            {
                                GameObject fallingPiece = grid[x, upperY];
                                Piece pieceScript = fallingPiece.GetComponent<Piece>();
                                if (pieceScript != null)
                                {
                                    pieceScript.stickToGrid = false;
                                    grid[x, y] = fallingPiece;
                                    grid[x, upperY] = null;
                                    pieceScript.X = x;
                                    pieceScript.Y = y;

                                    Vector2 targetPos = new Vector2(x, y);
                                    float fallTime = 0.35f;
                                    float delay = fallDelayIndex * 0.05f;

                                    fallingPiece.transform.DOKill();
                                    fallingPiece.transform.localScale = Vector3.one;
                                    fallingPiece.transform.DOMove(targetPos, fallTime)
                                        .SetEase(Ease.InQuad)
                                        .SetDelay(delay);

                                    fallDelayIndex++;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.35f);

            // Phase 2: Spawn new pieces into all empty cells
            for (int x = 0; x < levelData.gridWidth; x++)
            {
                for (int y = 0; y < levelData.gridHeight; y++)
                {
                    if (grid[x, y] == null && !IsBlocked(x, y))
                    {
                        GameObject selectedPrefab = GetRandomPiecePrefab();
                        GameObject newPiece = ObjectPoolManager.Spawn(
                            selectedPrefab,
                            new Vector2(x, levelData.gridHeight + 1f),
                            Quaternion.identity
                        );
                        Piece pieceScript = newPiece.GetComponent<Piece>();
                        if (pieceScript != null)
                        {
                            pieceScript.stickToGrid = false;
                            pieceScript.X = x;
                            pieceScript.Y = y;
                            newPiece.transform.SetParent(transform);
                            newPiece.name = pieceScript.pieceType.ToString() + " (" + x + ", " + y + ")";
                            newPiece.transform.localScale = Vector3.zero;
                            grid[x, y] = newPiece;

                            newPiece.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                            newPiece.transform.DOMove(new Vector2(x, y), 0.35f).SetEase(Ease.OutBounce);
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);

            // Verify if any empty cells still exist on the board
            bool hasEmptyCells = false;
            for (int x = 0; x < levelData.gridWidth; x++)
            {
                for (int y = 0; y < levelData.gridHeight; y++)
                {
                    if (grid[x, y] == null && !IsBlocked(x, y))
                    {
                        hasEmptyCells = true;
                        break;
                    }
                }
                if (hasEmptyCells) break;
            }

            keepRefilling = hasEmptyCells || needsAnotherRefill;
        }

        // Re-enable stickToGrid and reset match states
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (grid[x, y] != null)
                {
                    Piece pieceScript = grid[x, y].GetComponent<Piece>();
                    if (pieceScript != null)
                    {
                        pieceScript.stickToGrid = true;
                        pieceScript.isMatched = false;
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.1f);

        // Check for cascade matches
        hasPendingMatches = false;
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (grid[x, y] != null)
                {
                    Piece piece = grid[x, y].GetComponent<Piece>();
                    if (piece != null && !piece.isMatched)
                    {
                        piece.CheckForMatchesWithoutAction();
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.2f);

        if (hasPendingMatches)
        {
            Debug.Log("Cascade match found! Executing...");
            for (int x = 0; x < levelData.gridWidth; x++)
            {
                for (int y = 0; y < levelData.gridHeight; y++)
                {
                    if (grid[x, y] != null)
                    {
                        Piece piece = grid[x, y].GetComponent<Piece>();
                        if (piece != null && piece.isMatched)
                        {
                            piece.ExecuteMatch();
                        }
                    }
                }
            }

            yield return new WaitForSeconds(0.4f);
            isRefilling = false;
            UpdateGrid();
        }
        else
        {
            isRefilling = false;
            canControl = true;
            if (!HasPossibleMove())
            {
                ShuffleBoard();
            }
            Debug.Log("Grid settled - control enabled");
        }
    }
    public void SetHasPendingMatches(bool value)
    {
        hasPendingMatches = value;
    }


    private bool IsBlocked(int x, int y)
    {
        if (levelData == null || levelData.blockedCells == null)
            return false;

        foreach (var blockedCell in levelData.blockedCells)
        {
            if (blockedCell.x == x && blockedCell.y == y)
            {
                return true; // Cell is blocked
            }
        }
        return false; // Cell is not blocked
    }


    // Method to spawn a particle effect at a specific(X,Y) position of grid
    public void SpawnParticleEffect(int x, int y)
    {
        /*if (IsBlocked(x, y))
        {
            Debug.LogWarning($"Trying to spawn particle effect at blocked cell ({x},{y})");
            return;
        }
        GameObject particle = Instantiate(particlePrefab, new Vector2(x, y), Quaternion.identity);
        particle.transform.SetParent(transform);
        particle.name = "Particle (" + x + ", " + y + ")";
        Destroy(particle, 1f); // Destroy after 1 second*/
    }



    public void RegisterNewPiece(GameObject newPiece, int x, int y)
    {
        if (IsBlocked(x, y))
        {
            //Debug.LogWarning($"Trying to register piece at blocked cell ({x},{y})");
            return;
        }

        // Update grid array
        grid[x, y] = newPiece;

        // Set the piece position in its script
        Piece pieceScript = newPiece.GetComponent<Piece>();
        if (pieceScript != null)
        {
            pieceScript.SetPosition(x, y);
            pieceScript.stickToGrid = true; // Enable grid sticking once registered
        }

        // Optionally update pieces array if you want (to keep it synced)
        int index = x + y * levelData.gridWidth;
        if (index >= 0 && index < pieces.Length)
        {
            pieces[index] = pieceScript;
        }
    }


    public void UnregisterPiece(GameObject piece, int x, int y)
    {
        if (IsBlocked(x, y))
        {
            //Debug.LogWarning($"Trying to unregister piece at blocked cell ({x},{y})");
            return;
        }
        // Clear the grid array at the specified position
        grid[x, y] = null;
        // Optionally clear the pieces array if you want
        int index = x + y * levelData.gridWidth;
        if (index >= 0 && index < pieces.Length)
        {
            pieces[index] = null;
        }
    }

    public void OnBombButtonClick()
    {
        if (!canControl) return;

        if (Ability_bombCurrentAmount > 0)
        {
            isPlacingBomb = true;
            isPlacingColor = false;
        }
        else
        {
            // ?? FIXED: Now passes 2 arguments!
            ItemWarningPanel(bombSprite, "Bomb");
        }
    }

    public void OnColorButtonClick()
    {
        if (!canControl) return;

        if (Ability_colorBombCurrentAmount > 0)
        {
            isPlacingColor = true;
            isPlacingBomb = false;
        }
        else
        {
            // ?? FIXED: Now passes 2 arguments!
            ItemWarningPanel(clownSprite, "Clown");
        }
    }

    public void OnMoveButtonClick()
    {
        // Check if player has enough extra moves before starting the animation
        if (Ability_extraMovesCurrentAmount > 0)
        {
            // ?? THE FIX: Removed the "for (i < 5)" loop so it only spawns ONE image!
            GameObject moveImg = Instantiate(moveImage, imageSpawm.position, Quaternion.identity, mainCanvas.transform);
            moveImg.transform.localScale = Vector3.zero; // Start from scale 0
            moveImg.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack); // Scale to normal size
            
            // Move to imageTarget position instantly without the loop delay
            moveImg.transform.DOMove(imageTarget.position, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                currentMoves += 1; // Exactly +1 Move!
                UpdateUI();
                AudioManager.Instance.PlaySFX("Pop_5");
                Destroy(moveImg); // Destroy after reaching target
            });

            // Deduct extra moves ability count by 1
            DeductAbility_ExtraMoves(1);
        }
        else
        {
            // If 0, show the warning panel with the Moves image!
            ItemWarningPanel(movesSprite, "Moves");
        }
    }


    public void OnReshuffleButtonClick()
    {
        
        
        //first loading image Y position will come to 2500.
        StartCoroutine(ReshuffleWithEmojiLoading());
    }

    IEnumerator ReshuffleWithEmojiLoading()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        canControl = false;
        if (emojiRect != null)
        {
            emojiRect.anchoredPosition = new Vector2(0f, 3000f);
            yield return emojiRect.DOAnchorPosY(0f, 0.5f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }
        Reshuffle();
        yield return new WaitForSeconds(0.2f);
        if (emojiRect != null)
        {
            yield return emojiRect.DOAnchorPosY(-3000f, 0.5f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }
        canControl = true;
    }



    public void Reshuffle()
    {
        //Shuffle ability logic
        if (Ability_shuffleCurrentAmount > 0)
        {
            DeductAbility_Shuffle(1);
        }
        else
        {
            // ?? FIXED: Now passes 2 arguments!
            ItemWarningPanel(shuffleSprite, "Shuffle"); 
            return; 
        }
        

        
        AudioManager.Instance.PlaySFX("GameStart");

        ShuffleBoard();
    }

    /// <summary>
    /// Tests only right and up neighbours. Swapping can only create a match at one
    /// of the two swapped cells, so this avoids allocating lists or moving objects.
    /// </summary>
    public bool HasPossibleMove()
    {
        if (grid == null || levelData == null) return false;

        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (grid[x, y] == null || IsBlocked(x, y)) continue;

                if (x + 1 < levelData.gridWidth && grid[x + 1, y] != null && !IsBlocked(x + 1, y) && WouldSwapCreateMatch(x, y, x + 1, y))
                    return true;
                if (y + 1 < levelData.gridHeight && grid[x, y + 1] != null && !IsBlocked(x, y + 1) && WouldSwapCreateMatch(x, y, x, y + 1))
                    return true;
            }
        }
        return false;
    }

    public void ShuffleBoard()
    {
        if (!isRefilling && gameObject.activeInHierarchy)
            StartCoroutine(ShuffleBoardCoroutine());
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        const int maxAttempts = 100;
        canControl = false;

        var piecesToShuffle = new List<Piece>();
        var cells = new List<Vector2Int>();
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                GameObject pieceObject = grid[x, y];
                if (pieceObject == null || IsBlocked(x, y)) continue;
                Piece piece = pieceObject.GetComponent<Piece>();
                if (piece == null) continue;
                piecesToShuffle.Add(piece);
                cells.Add(new Vector2Int(x, y));
            }
        }

        if (piecesToShuffle.Count < 3)
        {
            canControl = true;
            yield break;
        }

        var original = new List<Piece>(piecesToShuffle);
        bool validBoard = false;
        for (int attempt = 0; attempt < maxAttempts && !validBoard; attempt++)
        {
            for (int i = piecesToShuffle.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                Piece temp = piecesToShuffle[i];
                piecesToShuffle[i] = piecesToShuffle[j];
                piecesToShuffle[j] = temp;
            }

            AssignPiecesToCells(piecesToShuffle, cells, false);
            validBoard = !BoardHasPreExistingMatches() && HasPossibleMove();
        }

        if (!validBoard)
        {
            // If shuffling existing pieces couldn't produce a match, re-roll a few piece types
            List<GameObject> pool = GetActivePiecePool();
            for (int r = 0; r < 50 && !validBoard; r++)
            {
                // Re-roll 3 random pieces with active pool types
                for (int k = 0; k < 3; k++)
                {
                    int randIndex = Random.Range(0, piecesToShuffle.Count);
                    if (pool != null && pool.Count > 0)
                    {
                        GameObject prefab = pool[Random.Range(0, pool.Count)];
                        Piece p = prefab.GetComponent<Piece>();
                        if (p != null) piecesToShuffle[randIndex].pieceType = p.pieceType;
                    }
                }

                AssignPiecesToCells(piecesToShuffle, cells, false);
                validBoard = !BoardHasPreExistingMatches() && HasPossibleMove();
            }
        }
        AssignPiecesToCells(piecesToShuffle, cells, true);
        yield return new WaitForSeconds(0.35f);
        canControl = true;
    }

    private void AssignPiecesToCells(List<Piece> piecesToAssign, List<Vector2Int> cells, bool animate)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            Piece piece = piecesToAssign[i];
            grid[cell.x, cell.y] = piece.gameObject;
            piece.X = cell.x;
            piece.Y = cell.y;
            piece.isMatched = false;
            piece.stickToGrid = true;
            if (animate)
            {
                piece.transform.DOKill();
                piece.transform.DOMove(new Vector2(cell.x, cell.y), 0.3f).SetEase(Ease.InOutQuad);
            }
        }
    }

    private bool WouldSwapCreateMatch(int ax, int ay, int bx, int by)
    {
        if (grid[ax, ay] == null || grid[bx, by] == null) return false;
        if (IsBlocked(ax, ay) || IsBlocked(bx, by)) return false;

        GameObject a = grid[ax, ay];
        GameObject b = grid[bx, by];

        Piece pa = a.GetComponent<Piece>();
        Piece pb = b.GetComponent<Piece>();

        // Special pieces (Row, Column, Bomb, Color) can always be swapped to trigger their effects
        if (pa != null && (pa.IsSpecialBombPiece || pa.IsSpecialRowPiece || pa.IsSpecialColoumnPiece || pa.IsSpecialColorPiece))
            return true;
        if (pb != null && (pb.IsSpecialBombPiece || pb.IsSpecialRowPiece || pb.IsSpecialColoumnPiece || pb.IsSpecialColorPiece))
            return true;

        grid[ax, ay] = b;
        grid[bx, by] = a;

        bool createsMatch = HasMatchAt(ax, ay) || HasMatchAt(bx, by);

        grid[ax, ay] = a;
        grid[bx, by] = b;

        return createsMatch;
    }
    private bool BoardHasPreExistingMatches()
    {
        for (int x = 0; x < levelData.gridWidth; x++)
            for (int y = 0; y < levelData.gridHeight; y++)
                if (grid[x, y] != null && HasMatchAt(x, y)) return true;
        return false;
    }

    private bool HasMatchAt(int x, int y)
    {
        Piece piece = grid[x, y] != null ? grid[x, y].GetComponent<Piece>() : null;
        if (piece == null) return false;
        PieceType type = piece.pieceType;
        return CountSameType(x, y, -1, 0, type) + CountSameType(x, y, 1, 0, type) - 1 >= 3 ||
               CountSameType(x, y, 0, -1, type) + CountSameType(x, y, 0, 1, type) - 1 >= 3;
    }

    private int CountSameType(int x, int y, int dx, int dy, PieceType type)
    {
        int count = 0;
        while (x >= 0 && x < levelData.gridWidth && y >= 0 && y < levelData.gridHeight)
        {
            Piece piece = grid[x, y] != null ? grid[x, y].GetComponent<Piece>() : null;
            if (piece == null || piece.pieceType != type) break;
            count++;
            x += dx;
            y += dy;
        }
        return count;
    }

    public void RecordPlayerSwap(int x1, int y1, int x2 = -1, int y2 = -1)
    {
        lastSwapX = x1;
        lastSwapY = y1;
        lastSwapX1 = x1;
        lastSwapY1 = y1;
        lastSwapX2 = x2;
        lastSwapY2 = y2;
        specialSpawnClaimed = false;
    }

    public bool TryClaimSpecialSpawn(int x, int y)
    {
        if (specialSpawnClaimed || x != lastSwapX || y != lastSwapY) return false;
        specialSpawnClaimed = true;
        return true;
    }

    public bool TryGetPendingSpecialSource(out Piece piece)
    {
        piece = null;
        if (specialSpawnClaimed || lastSwapX < 0 || lastSwapY < 0 ||
            lastSwapX >= levelData.gridWidth || lastSwapY >= levelData.gridHeight)
            return false;

        GameObject source = grid[lastSwapX, lastSwapY];
        piece = source != null ? source.GetComponent<Piece>() : null;
        return piece != null && piece.isMatched;
    }




    #endregion

    #region "Game Management"

    public void CheckForInternet()
    {
        if (!PlayerDataManager.Instance.isOnline)
        {
            NoInternetConnectionPanel.SetActive(true);

            //NoInternetPanelInside will do tweening
            
            NoInternetPanelInside.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack); // Scale to normal size

        }
        else
        {
            
            NoInternetPanelInside.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).WaitForCompletion();
            NoInternetConnectionPanel.SetActive(false);
        }
    }
    public void ActiveNoInternetConnectionPanel()
    {

        NoInternetConnectionPanel.SetActive(true);

        //NoInternetPanelInside will do tweening

        NoInternetPanelInside.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack); // Scale to normal size

    }

    private void StartTimer()
    {
        isTimerRunning = true;
        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        int min = Mathf.FloorToInt(currentTime / 60);
        int sec = Mathf.FloorToInt(currentTime % 60);

        timeText.text = string.Format("{0:00}:{1:00}", min, sec);
    }



    public void DeductTarget1(int amount = 1)
    {
        currentTarget1Count = Mathf.Max(0, currentTarget1Count - amount);
        currentTarget1 = currentTarget1Count;
        UpdateUI();
    }

    public void DeductTarget2(int amount = 1)
    {
        currentTarget2Count = Mathf.Max(0, currentTarget2Count - amount);
        currentTarget2 = currentTarget2Count;
        UpdateUI();
    }
    public void DeductMove(int amount = 1)
    {
        currentMoves -= amount;
        if (currentMoves < 0)
        {
            currentMoves = 0;

        }
        UpdateUI();
    }



    private void OnTimeUp()
    {
        Debug.Log("Time is up!");
        // Wait for one final swipe before showing game over.
    }

    public void RegisterFinalSwipeAfterTimeExpired()
    {
        if (isTimeExpired)
        {
            hasMadeFinalSwipeAfterTimeExpired = true;
        }
    }

    public void ResetUI()
    {
        currentTime = levelData.timeLimit;
        currentMoves = levelData.movesCount;
        currentTarget1 = levelData.target1Count;
        currentTarget2 = levelData.target2Count;



        UpdateUI();
        StartTimer();
    }


    //ability methods
    public void AddAbility_Bomb(int amount = 1)
    {
        Ability_bombCurrentAmount += amount;
        if (Ability_bombCurrentAmount < 0)
        {
            Ability_bombCurrentAmount = 0;
        }
        UpdateUI();
    }

    public void DeductAbility_Bomb(int amount = 1)
    {
        Ability_bombCurrentAmount -= amount;
        if (Ability_bombCurrentAmount < 0)
        {
            Ability_bombCurrentAmount = 0;
            ItemWarningPanel(bombSprite, "Bomb");
        }
        else
        {
            // ?? THE FIX: Tell the save file you spent the item instantly!
            SaveNewAbilityCounts(Ability_bombCurrentAmount, Ability_colorBombCurrentAmount, Ability_extraMovesCurrentAmount, Ability_shuffleCurrentAmount);
        }
        UpdateUI();
    }

    public void AddAbility_ColorBomb(int amount = 1)
    {
        Ability_colorBombCurrentAmount += amount;
        if (Ability_colorBombCurrentAmount < 0)
        {
            Ability_colorBombCurrentAmount = 0;

        }
        UpdateUI();
    }

    public void DeductAbility_ColorBomb(int amount = 1)
    {
        Ability_colorBombCurrentAmount -= amount;
        if (Ability_colorBombCurrentAmount < 0)
        {
            Ability_colorBombCurrentAmount = 0;
            ItemWarningPanel(clownSprite, "Clown"); 
        }
        else
        {
            // ?? THE FIX: Tell the save file you spent the item instantly!
            SaveNewAbilityCounts(Ability_bombCurrentAmount, Ability_colorBombCurrentAmount, Ability_extraMovesCurrentAmount, Ability_shuffleCurrentAmount);
        }
        UpdateUI();
    }

    public void AddAbility_ExtraMoves(int amount = 1)
    {
        Ability_extraMovesCurrentAmount += amount;
        if (Ability_extraMovesCurrentAmount < 0)
        {
            Ability_extraMovesCurrentAmount = 0;

        }
        UpdateUI();
    }

    public void DeductAbility_ExtraMoves(int amount = 1)
    {
        Ability_extraMovesCurrentAmount -= amount;
        if (Ability_extraMovesCurrentAmount < 0)
        {
            Ability_extraMovesCurrentAmount = 0;
            ItemWarningPanel(movesSprite, "Moves");
        }
        else
        {
            // ?? THE FIX: Tell the save file you spent the item instantly!
            SaveNewAbilityCounts(Ability_bombCurrentAmount, Ability_colorBombCurrentAmount, Ability_extraMovesCurrentAmount, Ability_shuffleCurrentAmount);
        }   
        UpdateUI();
    }

    //shuffle ability methods
    public void DeductAbility_Shuffle(int amount = 1)
    {
        Ability_shuffleCurrentAmount -= amount;
        if (Ability_shuffleCurrentAmount < 0)
        {
            Ability_shuffleCurrentAmount = 0;
            ItemWarningPanel(shuffleSprite, "Shuffle");
        }
        else
        {
            // ?? THE FIX: Tell the save file you spent the item instantly!
            SaveNewAbilityCounts(Ability_bombCurrentAmount, Ability_colorBombCurrentAmount, Ability_extraMovesCurrentAmount, Ability_shuffleCurrentAmount);
        }
        UpdateUI();
    }


    // ?? THE FIX: Added ", string boosterType" inside the parentheses!
    public void ItemWarningPanel(Sprite iconToShow, string boosterType)
    {
        missingBoosterType = boosterType; // Remembers what we need to reward
        
        // Change the icon before showing the panel
        if (warningPanelIcon != null)
        {
            warningPanelIcon.sprite = iconToShow;
        }
        
        // Show the item warning panel
        itemWarningPanel.SetActive(true);
    }
    // Link your "WATCH VIDEO" UI button directly to this method!
// Link your "WATCH VIDEO" UI button directly to this method!
    public void OnClickWatchAdInGame()
    {
        // ?? We talk directly to the AdsManager now! No StageManager needed.
        
        if (missingBoosterType == "Bomb")
        {
            AdsManager.Instance.ShowRewardedAd(() => 
            {
                PlayerDataManager.Instance.AddBombAbility(1);
                PlayerDataManager.Instance.SavePlayerData(); 
                RefreshAbilities(); 
            });
        }
        else if (missingBoosterType == "Clown")
        {
            AdsManager.Instance.ShowRewardedAd(() => 
            {
                PlayerDataManager.Instance.AddColorBombAbility(1);
                PlayerDataManager.Instance.SavePlayerData(); 
                RefreshAbilities(); 
            });
        }
        else if (missingBoosterType == "Moves")
        {
            AdsManager.Instance.ShowRewardedAd(() => 
            {
                PlayerDataManager.Instance.AddExtraMoveAbility(1);
                PlayerDataManager.Instance.SavePlayerData();
                RefreshAbilities();
            });
        }
        else if (missingBoosterType == "Shuffle")
        {
            AdsManager.Instance.ShowRewardedAd(() => 
            {
                PlayerDataManager.Instance.AddShuffleAbility(1); 
                PlayerDataManager.Instance.SavePlayerData(); 
                RefreshAbilities();
            });
        }
    }




    #endregion

    #region "Target Management"

    private Sprite GetSpriteForPiece(PieceType type)
    {
        switch (type)
        {
            case PieceType.Smiling_Face: return smilingFaceSprite;
            case PieceType.Smiling_Face_with_Tear: return smilingFaceWithTearSprite;
            case PieceType.Angry_Face: return angryFaceSprite;
            case PieceType.Freeze_Face: return laughingFaceSprite;
            case PieceType.SunGlass_Face: return smilingFaceWithHeartEyesSprite;
            case PieceType.Jumbo_Angry: return sleepingFaceSprite;
            case PieceType.Surprised_Face: return surprisedFaceSprite;
            case PieceType.Sad_Face: return cryingFaceSprite;
            default: return null;
        }
    }


    // Call this when a piece is matched
    public void DeductTarget(PieceType type)
    {
        if (type == levelData.target1Piece)
        {
            currentTarget1Count = Mathf.Max(0, currentTarget1Count - 1);
        }
        else if (type == levelData.target2Piece)
        {
            currentTarget2Count = Mathf.Max(0, currentTarget2Count - 1);
        }

        UpdateUI();
    }

    // Individual functions for each PieceType (optional)
    public void Smiling_Face() => DeductTarget(PieceType.Smiling_Face);
    public void Smiling_Face_with_Tear() => DeductTarget(PieceType.Smiling_Face_with_Tear);
    public void Angry_Face() => DeductTarget(PieceType.Angry_Face);
    public void Laughing_Face() => DeductTarget(PieceType.Freeze_Face);
    public void Smiling_Face_With_Heart_Eyes() => DeductTarget(PieceType.SunGlass_Face);
    public void Sleeping_Face() => DeductTarget(PieceType.Jumbo_Angry);
    public void Surprised_Face() => DeductTarget(PieceType.Surprised_Face);
    public void Crying_Face() => DeductTarget(PieceType.Sad_Face);
    #endregion

    #region "Visual Effects and Sounds"
    public void SpawnHorizontalClear(int y)
    {

        GameObject particle = ObjectPoolManager.Spawn(horizontalClearParticle, new Vector2(levelData.gridWidth / 2f - 0.5f, y), Quaternion.identity);
        ObjectPoolManager.Despawn(particle, 1f);


    }

    public void SpawnVerticalClear(int x)
    {
        GameObject particle = ObjectPoolManager.Spawn(verticalClearParticle, new Vector2(x, levelData.gridHeight / 2f - 0.5f), Quaternion.identity);
        ObjectPoolManager.Despawn(particle, 1f);


    }


    //play random sfx sound(Pop_1, Pop_2, Pop_3, Pop_4) from AudioManager
    public void PlayRandomSFX()
    {
        /*int randomIndex = Random.Range(1, 5); // Random index between 1 and 4
        string sfxName = "Pop_" + randomIndex;
        AudioManager.Instance.PlaySFX(sfxName);*/
        AudioManager.Instance.PlaySFX("Pop_Main");
    }


    public void PlayEffect()
    {
        if (sprites == null || sprites.Length == 0) return;

        // Auto-find or create targetImage on Canvas if not assigned or missing
        if (targetImage == null)
        {
            Canvas canvas = mainCanvas != null ? mainCanvas.GetComponent<Canvas>() : FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject go = new GameObject("ActionTextPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(canvas.transform, false);
                targetImage = go.GetComponent<Image>();
                targetImage.raycastTarget = false;
            }
        }

        if (targetImage == null) return;

        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
        targetImage.transform.DOKill();

        // Pick a random sprite
        SpriteData data = sprites[Random.Range(0, sprites.Length)];
        if (data.sprite == null) return;

        targetImage.sprite = data.sprite;
        targetImage.SetNativeSize();
        targetImage.rectTransform.anchoredPosition = Vector2.zero; // Perfectly centered on screen
        targetImage.transform.SetAsLastSibling(); // Ensure it renders on top of all UI layers

        // Reset scale
        targetImage.transform.localScale = Vector3.zero;
        targetImage.enabled = true;
        targetImage.gameObject.SetActive(true);

        Vector3 endScale = new Vector3(1.2f, 1.2f, 1f);

        // Start new tween sequence
        currentSequence = DOTween.Sequence();
        currentSequence.Append(targetImage.transform.DOScale(endScale, scaleDuration).SetEase(Ease.OutBack));
        currentSequence.AppendInterval(holdDuration);
        currentSequence.Append(targetImage.transform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack));
    }
    #endregion

}
