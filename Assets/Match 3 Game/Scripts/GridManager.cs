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

    private bool isRefilling = false; // Track if grid is currently refilling
    private bool hasPendingMatches = false; // Track if matches were found during refill




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
    public string fileName = "playerdata.json";

    private string SavePath; // = Path.Combine(Application.persistentDataPath, "playerdata.json");



    #region "Common Region"
    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;

        grid = new GameObject[levelData.gridWidth, levelData.gridHeight];
        SavePath = Path.Combine(Application.persistentDataPath, "playerdata.json");

        LoadLevel();

        //AudioManager.Instance.PlayMusic("MenuBG");


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

    private void UpdateUI()
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
        if (!isTimerRunning)
        {
            return;
        }

        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            isTimerRunning = false;
            OnTimeUp();
        }

        //GameOverLogic();


        /*if(isGameOver)
        {
            GameOverHelper();
        }*/

        //if i click on any bomb then call bomb on that piece. use mouse position raycast to get the piece
        /*if (Input.GetMouseButtonDown(0) && canControl)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                Piece piece = hit.collider.GetComponent<Piece>();
                if (piece != null)
                {
                    if (isPlacingBomb && Ability_bombCurrentAmount > 0)
                    {
                        piece.ActivateBomb();
                        DeductAbility_Bomb(1);
                        isPlacingBomb = false;
                    }
                    
                }
            }
        }*/

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

    public void RestartCurrentLevel()
    {
        // Reload the current scene to restart the level
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GameOverLogic()
    {



        //optimised version of above code   
        if ((currentTime <= 0 || currentMoves <= 0) && (currentTarget1Count > 0 || currentTarget2Count > 0))
        {
            // Game over condition: Time or moves are up, but targets are not met
            isGameOver = true;
            Debug.Log("Game Over! Time is up or no moves left.");
        }
        else if (currentTarget1Count <= 0 && currentTarget2Count <= 0)
        {
            // Level completed condition: All targets met
            Debug.Log("Level Completed!");
            isGameOver = true;
        }

        if (isGameOver)
        {
            GameOverHelper();
            StartCoroutine(TurnOfisGameOver());

        }
    }

    //wait for few sec befor isGameOver = false
    private IEnumerator TurnOfisGameOver()
    {
        //wait for 1 sec
        yield return new WaitForSeconds(0.2f);
        isGameOver = false;
    }



    private void FixedUpdate()
    {
        UpdateTimeText();



        //pieces objects will be the spawned pieces in the game
        pieces = new Piece[levelData.gridWidth * levelData.gridHeight];
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (grid[x, y] != null)
                {
                    Piece pieceScript = grid[x, y].GetComponent<Piece>();
                    pieces[x + y * levelData.gridWidth] = pieceScript; // Store the piece in the pieces array
                }
            }
        }
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

        // Recreate the grid with the new LevelData
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
        if (File.Exists(SavePath))
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
        }
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
        //wait for 1 second
        yield return new WaitForSeconds(1f);

        if (currentTarget1Count <= 0 && currentTarget2Count <= 0)
        {
            gameOverTitleText.text = "Congratulations!";
            Shine1.SetActive(true);
            Shine2.SetActive(false);
            AudioManager.Instance.PlaySFX("GameWin");
            NextLevelButton.gameObject.SetActive(true);
        }
        else
        {
            gameOverTitleText.text = "Game Over!";
            Shine1.SetActive(false);
            Shine2.SetActive(true);
            AudioManager.Instance.PlaySFX("GameLose");
            PlayerDataManager.Instance.RemoveEnergy(1);
            RestartButton.gameObject.SetActive(true);
        }

        //gameOverText will be = level + currentLevelIndex + 1
        gameOverText.text = "Level :" + (currentLevelIndex + 1);
        level_Count.text = (currentLevelIndex + 1).ToString(); // Update level count text
                                                               //gameOverTitleText will be = Contratulations if currentTarget1Count and currentTarget2Count are 0

        // Handle game over logic here
        // For example, show a game over screen or reset the game
        Debug.Log("Game Over! You can implement your game over logic here.");
        isTimerRunning = false; // Stop the timer
        canControl = false; // Disable player controls
        //game over panel will be shown using do tweening
        GameOverPanel.SetActive(true);
        GameOverPanel.transform.localScale = Vector3.zero; // Start from scale 0
        GameOverPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack); // Scale to normal size
        // Optionally, you can also reset the game state or show options to restart or exit
        // Reset the grid and pieces
        //CalculateStarAndShow();
        //delay CalculateStarAndShow() for 0.5 seconds
        Invoke("CalculateStarAndShow", 0.5f);



        SaveNewAbilityCounts(Ability_bombCurrentAmount, Ability_colorBombCurrentAmount, Ability_extraMovesCurrentAmount, Ability_shuffleCurrentAmount);

    }

    public void CalculateStarAndShow()
    {
        //if  level datas Target1Count and Target2Count are 0, then no stars and no XP
        if (levelData.target1Count == 0 && levelData.target2Count == 0)
        {
            stars = 0; // No stars
            XP = 0; // No XP
            Debug.Log("No stars and no XP because targets are 0.");
        }
        // Calculate stars based on moves and time left. more moves and move time left, more stars

        else if (currentMoves > 0 && currentTime > 0)
        {
            if (currentMoves >= levelData.movesCount * 0.25f && currentTime >= levelData.timeLimit * 0.25f)
            {
                stars = 3; // Full stars
                Debug.Log("3 stars and 100XP earned!");

                //calculate XP based on moves left multiplied by left times
                //XP = Mathf.FloorToInt((currentMoves / (float)levelData.movesCount) * 100) + Mathf.FloorToInt((currentTime / (float)levelData.timeLimit) * 100);
                //if levelData.isTimedLevel is true then XP will be left time x 10
                XP = Mathf.FloorToInt((currentTime / (float)levelData.timeLimit) * 10);
                if (levelData.isTimedLevel)
                {
                    XP = 20;//Mathf.FloorToInt((currentTime / (float)levelData.timeLimit) * 10);

                }
                else if (levelData.isMovesLevel)
                {
                    XP = 20;//Mathf.FloorToInt((currentMoves / (float)levelData.timeLimit) * 10);
                }
                else
                {
                    Debug.Log("error getting XP data for levelData bools");
                }

            }
            else if (currentMoves >= levelData.movesCount * 0.2f && currentTime >= levelData.timeLimit * 0.2f)
            {
                stars = 2; // Two stars
                Debug.Log("2 stars and 50 XP earned!");
                //XP = Mathf.FloorToInt((currentMoves / (float)levelData.movesCount) * 100) + Mathf.FloorToInt((currentTime / (float)levelData.timeLimit) * 100);
                if (levelData.isTimedLevel)
                {
                    XP = 10;//Mathf.FloorToInt((currentTime / (float)levelData.timeLimit) * 5);

                }
                else if (levelData.isMovesLevel)
                {
                    XP = 10;//Mathf.FloorToInt((currentMoves / (float)levelData.timeLimit) * 5);
                }
                else
                {
                    Debug.Log("error getting XP data for levelData bools");
                }

            }
            else
            {
                stars = 1; // One star
                Debug.Log("1 star and 20 XP earned!");
                //XP = Mathf.FloorToInt((currentMoves / (float)levelData.movesCount) * 100) + Mathf.FloorToInt((currentTime / (float)levelData.timeLimit) * 100);
                if (levelData.isTimedLevel)
                {
                    XP = 5;//Mathf.FloorToInt((currentTime / (float)levelData.timeLimit) * 2);

                }
                else if (levelData.isMovesLevel)
                {
                    XP = 5;//Mathf.FloorToInt((currentMoves / (float)levelData.timeLimit) * 2);
                }
                else
                {
                    Debug.Log("error getting XP data for levelData bools");
                }

            }
        }




        // Show the stars UI. By default, all Glowing stars are hidden and normal stars are shown
        for (int i = 0; i < 3; i++)
        {
            if (i < stars)
            {
                glowStars[i].SetActive(true); // Show glowing stars
                normalStars[i].SetActive(false); // Hide normal stars
            }
            else
            {
                glowStars[i].SetActive(false); // Hide glowing stars
                normalStars[i].SetActive(true); // Show normal stars
                Debug.Log("No stars earned for star index: " + i);
            }
        }

        // Update XP UI
        xpAmount.text = XP.ToString();
        Debug.Log("Stars: " + stars + ", XP: " + XP);

        //send star and xp data to PlayerDataManager
        SendStarXpDataToPlayerDataManager(currentLevelIndex + 1, 0, stars, XP);

        //PlayerDataManager.Instance.SendLeaderboardScore(stars, XP); // Send the score to the leaderboard

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

    }


    IEnumerator EmojiLoading()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        //AudioManager.Instance.PlaySFX("GameStart");
        canControl = false; // Disable player controls during loading
        // Move EmojisImage into view (Y: -1250 to 2500)
        yield return emojiRect.DOAnchorPosY(2500f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();
        canControl = true; // Re-enable player controls after loading


    }

    IEnumerator EmojiLoading_2()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        // Move EmojisImage into view (Y: 2500 to -1250)

        yield return emojiRect.DOAnchorPosY(-1250f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();


        SceneManager.LoadScene("MainMenu");
    }

    public void ExitToMainMenu()
    {
        StartCoroutine(EmojiLoading_2());
        StartCoroutine(ExitScene());
        //vibe or energy will lose 1 on exit to main menu
        PlayerDataManager.Instance.RemoveEnergy(1);

    }

    IEnumerator ExitScene()
    {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene("MainMenu");
    }


    #endregion

    #region "Grid System"


    //the grid and place pieces using seed from LevelData
    private void CreateGrid()
    {
        // Use the seed from LevelData to ensure consistent piece placement
        Random.InitState(levelData.GridSeed);

        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (IsBlocked(x, y))
                {
                    grid[x, y] = null; // Explicitly mark as blocked
                    //spawn brick prefab in blocked cells
                    GameObject brick = Instantiate(brickPrefab, new Vector2(x, y), Quaternion.identity);
                    //brick.transform.SetParent(transform);
                    //brick.transform.localScale = new Vector2(x, y);
                    brick.name = "Brick (" + x + ", " + y + ")";
                    continue; // Skip to the next cell


                }

                int randomIndex = Random.Range(0, piecePrefabs.Length);
                GameObject newPiece = Instantiate(
                    piecePrefabs[randomIndex],
                    new Vector2(x, y + 1f),
                    Quaternion.identity
                );

                Piece pieceScript = newPiece.GetComponent<Piece>();
                pieceScript.SetPosition(x, y); //GameObject.SetPosition(Vector2)
                newPiece.transform.SetParent(transform);
                newPiece.name = pieceScript.pieceType.ToString() + " (" + x + ", " + y + ")";
                newPiece.transform.localScale = Vector3.zero;
                grid[x, y] = newPiece;
                newPiece.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                newPiece.transform.DOMove(new Vector2(x, y), 0.3f).SetEase(Ease.OutBounce);
            }
        }

        //Debug.Log("Grid created with seed: " + levelData.GridSeed);
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
        if (!isRefilling)
        {
            canControl = false; // Disable control at the start
            StartCoroutine(RefillGridCoroutine());
        }
    }


    private IEnumerator RefillGridCoroutine()
    {
        isRefilling = true;
        canControl = false;

        yield return new WaitForSeconds(0.2f);

        // Pieces fall down to fill empty spaces
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

                            // Disable grid sticking during fall
                            pieceScript.stickToGrid = false;

                            // Update grid references
                            grid[x, y] = fallingPiece;
                            grid[x, upperY] = null;

                            // Update logical position
                            pieceScript.X = x;
                            pieceScript.Y = y;

                            // Animate fall
                            Vector2 targetPos = new Vector2(x, y);
                            float fallTime = 0.5f;
                            float delay = fallDelayIndex * 0.06f;

                            fallingPiece.transform.DOMove(targetPos, fallTime)
                                .SetEase(Ease.InQuad)
                                .SetDelay(delay);

                            fallDelayIndex++;
                            break;
                        }
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.35f);

        Random.InitState(levelData.GridSeed);

        // Refill empty cells with new pieces
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (grid[x, y] == null && !IsBlocked(x, y))
                {
                    int randomIndex = Random.Range(0, piecePrefabs.Length);
                    GameObject newPiece = Instantiate(
                        piecePrefabs[randomIndex],
                        new Vector2(x, levelData.gridHeight + 1f),
                        Quaternion.identity
                    );
                    Piece pieceScript = newPiece.GetComponent<Piece>();

                    // Disable grid sticking during spawn animation
                    pieceScript.stickToGrid = false;

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

        // Wait for all animations to finish
        yield return new WaitForSeconds(0.5f);

        // Re-enable stickToGrid for all pieces
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
                        pieceScript.isMatched = false; // Reset match state for checking
                    }
                }
            }
        }

        yield return new WaitForSeconds(0.1f);

        // Check for matches after refill
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

        // If matches found, execute them and refill again
        if (hasPendingMatches)
        {
            Debug.Log("Cascade match found! Executing...");
            
            // Execute all matched pieces
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
            
            // Wait for destruction animations
            yield return new WaitForSeconds(0.5f);
            
            // Reset and refill again
            isRefilling = false;
            UpdateGrid();
        }
        else
        {
            // No matches found, safe to enable control
            isRefilling = false;
            canControl = true;
            Debug.Log("Grid settled - control enabled");
        }
    }

    // Add this helper method to check if any matches exist:
    public void SetHasPendingMatches(bool value)
    {
        hasPendingMatches = value;
    }


    private bool IsBlocked(int x, int y)
    {
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
        isPlacingBomb = true;
    }

    public void OnColorButtonClick()
    {
        isPlacingColor = true;
    }

    //OnMoveButtonClick function call moveimage will spawn on spawn image and then go to target image using dotween
    public void OnMoveButtonClick()
    {
        // Instantiate moveImage at imageSpawm position
        //GameObject moveImg = Instantiate(moveImage, imageSpawm.position, Quaternion.identity, transform);
        //spawn it as child of canvas
        /*GameObject moveImg = Instantiate(moveImage, imageSpawm.position, Quaternion.identity, mainCanvas.transform);
        moveImg.transform.localScale = Vector3.zero; // Start from scale 0
        moveImg.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack); // Scale to normal size
        // Move to imageTarget position
        moveImg.transform.DOMove(imageTarget.position, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            
            Destroy(moveImg); // Destroy after reaching target
        });*/

        //increase currentMoves int by 5

        //spawn 5 move images that move to target image using dotween
        for (int i = 0; i < 5; i++)
        {
            GameObject moveImg = Instantiate(moveImage, imageSpawm.position, Quaternion.identity, mainCanvas.transform);
            moveImg.transform.localScale = Vector3.zero; // Start from scale 0
            moveImg.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack); // Scale to normal size
            // Move to imageTarget position
            moveImg.transform.DOMove(imageTarget.position, 0.5f).SetEase(Ease.InOutQuad).SetDelay(i * 0.1f).OnComplete(() =>
            {
                currentMoves += 1;
                UpdateUI();
                //play Pop_5 sound
                AudioManager.Instance.PlaySFX("Pop_5");
                Destroy(moveImg); // Destroy after reaching target

            });

        }

        //Diduct extra moves ability count by 1
        DeductAbility_ExtraMoves(1);




    }


    public void OnReshuffleButtonClick()
    {
        
        
        //first loading image Y position will come to 2500.
        StartCoroutine(ReshuffleWithEmojiLoading());
    }

    IEnumerator ReshuffleWithEmojiLoading()
    {
        //first loading image Y position will come to 2500.
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        
        canControl = false; // Disable player controls during loading
        // Move EmojisImage out of view (Y: 2500 to -1250
        yield return emojiRect.DOAnchorPosY(-1250f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();
        Reshuffle();
        
        waitforseconds: yield return new WaitForSeconds(0.5f);

        // Move EmojisImage into view (Y: -1250 to 2500)
        yield return emojiRect.DOAnchorPosY(2500f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();
        canControl = true; // Re-enable player controls after loading
        
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
            ItemWarningPanel();
            return; // Exit if no reshuffle ability left
        }
        
        AudioManager.Instance.PlaySFX("GameStart");

        // Reshuffle the grid pieces
        List<Piece> allPieces = new List<Piece>();
        // Collect all pieces from the grid
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (grid[x, y] != null)
                {
                    Piece pieceScript = grid[x, y].GetComponent<Piece>();
                    allPieces.Add(pieceScript);
                }
            }
        }
        // Shuffle the list of pieces
        for (int i = 0; i < allPieces.Count; i++)
        {
            Piece temp = allPieces[i];
            int randomIndex = Random.Range(0, allPieces.Count);
            allPieces[i] = allPieces[randomIndex];
            allPieces[randomIndex] = temp;
        }
        // Reassign pieces back to the grid
        int index = 0;
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                if (grid[x, y] != null)
                {
                    Piece pieceScript = allPieces[index];
                    pieceScript.SetPosition(x, y);
                    grid[x, y] = pieceScript.gameObject;
                    index++;
                }
            }
        }
        // Deduct reshuffle ability count by 1
        //DeductAbility_ColorBomb(1);
        // Update UI after reshuffle
        UpdateUI();
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
        currentTarget1 -= amount;
        if (currentTarget1 < 0)
        {
            currentTarget1 = 0;

        }
        UpdateUI();
    }

    public void DeductTarget2(int amount = 1)
    {
        currentTarget2 -= amount;
        if (currentTarget2 < 0)
        {
            currentTarget2 = 0;

        }
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
        // maybe add fail screen logic here or something
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
            ItemWarningPanel();
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
            ItemWarningPanel();
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
            ItemWarningPanel();
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
            ItemWarningPanel();
        }
        UpdateUI();
    }


    public void ItemWarningPanel()
    {
        // Show the item warning panel
        itemWarningPanel.SetActive(true);
        /*itemWarningPanel.transform.localScale = Vector3.zero; // Start from scale 0
        itemWarningPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack); // Scale to normal size*/
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

        GameObject particle = Instantiate(horizontalClearParticle, new Vector2(levelData.gridWidth / 2f - 0.5f, y), Quaternion.identity);
        particle.transform.SetParent(transform);
        particle.name = "HorizontalClear (" + y + ")";
        Destroy(particle, 1f); // Destroy after 1 second
    }

    public void SpawnVerticalClear(int x)
    {
        GameObject particle = Instantiate(verticalClearParticle, new Vector2(x, levelData.gridHeight / 2f - 0.5f), Quaternion.identity);
        particle.transform.SetParent(transform);
        particle.name = "VerticalClear (" + x + ")";
        Destroy(particle, 1f); // Destroy after 1 second
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
        if (sprites.Length == 0 || targetImage == null) return;

        // If a previous tween is running, tween it back to zero immediately
        if (currentSequence != null && currentSequence.IsActive() && currentSequence.IsPlaying())
        {
            currentSequence.Kill();
            targetImage.transform.DOScale(Vector3.zero, scaleDuration * 0.5f).SetEase(Ease.InBack);
        }

        // Pick a random sprite
        SpriteData data = sprites[Random.Range(0, sprites.Length)];
        targetImage.sprite = data.sprite;

        // Reset scale
        targetImage.transform.localScale = Vector3.zero;

        // Start new tween sequence
        currentSequence = DOTween.Sequence();
        currentSequence.Append(targetImage.transform.DOScale(data.targetScale, scaleDuration).SetEase(Ease.OutBack));
        currentSequence.AppendInterval(holdDuration);
        currentSequence.Append(targetImage.transform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack));
    }

    #endregion

}