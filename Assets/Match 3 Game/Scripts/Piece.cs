using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum PieceType
{
    Smiling_Face,
    Smiling_Face_with_Tear,
    Angry_Face,
    Freeze_Face,
    SunGlass_Face,
    Jumbo_Angry,
    Surprised_Face,
    Sad_Face,
}

public class Piece : MonoBehaviour
{
    public int X;// X position in the grid
    public int Y; // Y position in the grid

    private Vector2 firstTouchPosition; 
    private Vector2 finalTouchPosition; 

    public GameObject otherPiece; 
    private Vector2 tempPosition; 
    private float swipeAngle; 

    public PieceType pieceType; 

    public bool IsSpecialBombPiece = false; 
    public bool IsSpecialRowPiece = false; 
    public bool IsSpecialColoumnPiece = false; 
    public bool IsSpecialColorPiece = false; 
    public bool preventSwipeBack = false; 

    private GridManager gridManager; 

    public bool isMatched = false; 

    private Vector2 originalWorldPosition;
    private int originalX, originalY;

    private LevelData levelData;

    public bool stickToGrid = true; 

    public GameObject ColoumnPiece;
    public GameObject RowPiece;
    public GameObject BombPiece;
    public GameObject ColorPiece;

    public Animator pieceAnimator; 

    public GameObject matchedParticle;
    public GameObject bombParticle1;
    public GameObject bombParticle2;
    public GameObject bombParticle3;

    public void SetPosition(int x, int y)
    {
        X = x;
        Y = y;
        transform.position = new Vector2(x, y); 
    }

    void Start()
    {
        gridManager = FindObjectOfType<GridManager>(); 

        Invoke(nameof(FindMatches), 0.5f); 

        if (gridManager != null)
        {
            levelData = gridManager.levelData; 
        }
        else
        {
            Debug.LogError("GridManager not found in the scene.");
        }
        
        StartCoroutine(AnimatePiece()); 
        stickToGrid = true; 
    }

    private void FixedUpdate()
    {
        UpdateTargetPosition();
        SpecialPieceCall();
    }

    void SpecialPieceCall()
    {
        if (IsSpecialBombPiece && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Bomb(X, Y); 
                AudioManager.Instance.PlaySFX("Bomb");
                
                int randomEffect = Random.Range(1, 4);
                if (randomEffect == 1) Instantiate(bombParticle1, transform.position, Quaternion.identity);
                else if (randomEffect == 2) Instantiate(bombParticle2, transform.position, Quaternion.identity);
                else if (randomEffect == 3) Instantiate(bombParticle3, transform.position, Quaternion.identity);
            }
        }

        if (IsSpecialRowPiece && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ClearRow(Y); 
            }
        }

        if (IsSpecialColoumnPiece && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ClearColoumn(X); 
            }
        }
    }

    void UpdateTargetPosition()
    {
        if (gridManager == null || PlayerDataManager.Instance == null) return;

        if(!PlayerDataManager.Instance.isOnline)
        {
            gridManager.ActiveNoInternetConnectionPanel();
            return;
        }

        if (!gridManager.canControl)
        {
            finalTouchPosition = Vector2.zero; 
            firstTouchPosition = Vector2.zero; 
            return;
        }

        if (finalTouchPosition == Vector2.zero)
            return;

        float dx = finalTouchPosition.x - firstTouchPosition.x;
        float dy = finalTouchPosition.y - firstTouchPosition.y;

        if (Mathf.Abs(dx) < 0.2f && Mathf.Abs(dy) < 0.2f)
            return; 

        int targetX = X;
        int targetY = Y;

        if (Mathf.Abs(dx) > Mathf.Abs(dy))
        {
            if (dx > 0) targetX = X + 1; 
            else targetX = X - 1; 
        }
        else
        {
            if (dy > 0) targetY = Y + 1; 
            else targetY = Y - 1; 
        }

        if (targetX < 0 || targetX >= levelData.gridWidth || targetY < 0 || targetY >= levelData.gridHeight)
            return;

        GameObject targetPieceObj = gridManager.grid[targetX, targetY];
        if (targetPieceObj == null)
            return;

        Piece targetPiece = targetPieceObj.GetComponent<Piece>();
        if (targetPiece == null)
            return;

        otherPiece = targetPieceObj;

        gridManager.RegisterFinalSwipeAfterTimeExpired();

        Vector2 myTarget = targetPiece.transform.position;
        Vector2 otherTarget = transform.position;
        float swipeTime = 0.3f;

        originalWorldPosition = transform.position;
        originalX = X;
        originalY = Y;

        targetPiece.originalWorldPosition = targetPiece.transform.position;
        targetPiece.originalX = targetPiece.X;
        targetPiece.originalY = targetPiece.Y;

        transform.DOMove(myTarget, swipeTime);
        targetPiece.transform.DOMove(otherTarget, swipeTime);

        gridManager.grid[X, Y] = targetPieceObj;
        gridManager.grid[targetPiece.X, targetPiece.Y] = this.gameObject;

        int tempX = X;
        int tempY = Y;
        X = targetPiece.X;
        Y = targetPiece.Y;
        targetPiece.X = tempX;
        targetPiece.Y = tempY;

        AudioManager.Instance.PlaySFX("Swing_1");

        gridManager.canControl = false; 
        gridManager.DeductMove();

        if (this.IsSpecialColorPiece)
        {
            targetPiece.ClearColour(targetPiece.pieceType);
            this.isMatched = true;
            MarkAndDestroyColorPiece(this);
        }
        else if (targetPiece.IsSpecialColorPiece)
        {
            this.ClearColour(this.pieceType);
            targetPiece.isMatched = true;
            MarkAndDestroyColorPiece(targetPiece);
        }

        Invoke(nameof(FindMatches), 0.5f);
        targetPiece.Invoke(nameof(FindMatches), 0.5f);

        finalTouchPosition = Vector2.zero;
    }

    void CalculateAngle()
    {
        swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
    }

    private void OnMouseDown()
    {
        firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        if (gridManager.isPlacingBomb)
        {
            StartCoroutine(ReplaceWithBomb()); 
            gridManager.isPlacingBomb = false; 
            return; 
        }

        if (gridManager.isPlacingColor)
        {
            StartCoroutine(ReplaceWithColor()); 
            gridManager.isPlacingColor = false; 
            return;
        }
    }

    private void OnMouseUp()
    {
        finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        CalculateAngle();
    }

    public void CheckForMatchesWithoutAction()
    {
        if (gridManager == null || gridManager.grid == null || isMatched) return;

        List<Piece> horizontalMatches = new List<Piece>();
        List<Piece> verticalMatches = new List<Piece>();

        horizontalMatches.Add(this);

        for (int i = 1; X - i >= 0; i++)
        {
            Piece next = gridManager.grid[X - i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                horizontalMatches.Add(next);
            else break;
        }

        for (int i = 1; X + i < levelData.gridWidth; i++)
        {
            Piece next = gridManager.grid[X + i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                horizontalMatches.Add(next);
            else break;
        }

        verticalMatches.Add(this);

        for (int i = 1; Y - i >= 0; i++)
        {
            Piece next = gridManager.grid[X, Y - i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                verticalMatches.Add(next);
            else break;
        }

        for (int i = 1; Y + i < levelData.gridHeight; i++)
        {
            Piece next = gridManager.grid[X, Y + i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                verticalMatches.Add(next);
            else break;
        }

        if (horizontalMatches.Count >= 3)
        {
            foreach (var piece in horizontalMatches)
            {
                if (piece != null && !piece.isMatched)
                {
                    piece.isMatched = true;
                    gridManager.SetHasPendingMatches(true);
                }
            }
        }

        if (verticalMatches.Count >= 3)
        {
            foreach (var piece in verticalMatches)
            {
                if (piece != null && !piece.isMatched)
                {
                    piece.isMatched = true;
                    gridManager.SetHasPendingMatches(true);
                }
            }
        }
    }

    public void ExecuteMatch()
    {
        if (!isMatched || this == null) return;

        List<Piece> horizontalMatches = new List<Piece>();
        List<Piece> verticalMatches = new List<Piece>();

        horizontalMatches.Add(this);
        for (int i = 1; X - i >= 0; i++)
        {
            Piece next = gridManager.grid[X - i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) horizontalMatches.Add(next);
            else break;
        }
        for (int i = 1; X + i < levelData.gridWidth; i++)
        {
            Piece next = gridManager.grid[X + i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) horizontalMatches.Add(next);
            else break;
        }

        verticalMatches.Add(this);
        for (int i = 1; Y - i >= 0; i++)
        {
            Piece next = gridManager.grid[X, Y - i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) verticalMatches.Add(next);
            else break;
        }
        for (int i = 1; Y + i < levelData.gridHeight; i++)
        {
            Piece next = gridManager.grid[X, Y + i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) verticalMatches.Add(next);
            else break;
        }

        bool shouldSpawnSpecial = false;
        GameObject specialPieceToSpawn = null;

        if (horizontalMatches.Count >= 6 || verticalMatches.Count >= 6)
        {
            shouldSpawnSpecial = true;
            specialPieceToSpawn = ColorPiece;
        }
        else if (horizontalMatches.Count >= 5)
        {
            shouldSpawnSpecial = true;
            specialPieceToSpawn = RowPiece;
        }
        else if (verticalMatches.Count >= 5)
        {
            shouldSpawnSpecial = true;
            specialPieceToSpawn = ColoumnPiece;
        }
        else if (horizontalMatches.Count >= 4)
        {
            shouldSpawnSpecial = true;
            specialPieceToSpawn = RowPiece;
        }
        else if (verticalMatches.Count >= 4)
        {
            shouldSpawnSpecial = true;
            specialPieceToSpawn = ColoumnPiece;
        }

        HashSet<Piece> allMatches = new HashSet<Piece>();
        if (horizontalMatches.Count >= 3)
        {
            foreach (var p in horizontalMatches) allMatches.Add(p);
        }
        if (verticalMatches.Count >= 3)
        {
            foreach (var p in verticalMatches) allMatches.Add(p);
        }

        bool isFirstPiece = true;
        Vector2 spawnPosition = transform.position;

        foreach (var piece in allMatches)
        {
            if (piece != null)
            {
                if (isFirstPiece && shouldSpawnSpecial)
                {
                    spawnPosition = piece.transform.position;
                    isFirstPiece = false;
                }

                MarkAsMatched(piece);
            }
        }

        if (shouldSpawnSpecial && specialPieceToSpawn != null)
        {
            StartCoroutine(SpawnSpecialPieceDelayed(specialPieceToSpawn, spawnPosition));
        }

        if (allMatches.Count >= 3)
        {
        }
    }

    private IEnumerator SpawnSpecialPieceDelayed(GameObject specialPrefab, Vector2 position)
    {
        yield return new WaitForSeconds(0.35f);

        GameObject spawnedPiece = Instantiate(specialPrefab, position, Quaternion.identity);
        spawnedPiece.transform.SetParent(gridManager.transform);

        int gridX = Mathf.RoundToInt(position.x);
        int gridY = Mathf.RoundToInt(position.y);

        gridManager.RegisterNewPiece(spawnedPiece, gridX, gridY);
    }

    public void FindMatches()
    {
        if (gridManager == null || gridManager.grid == null) return;

        CheckForMatchesWithoutAction();

        if (isMatched)
        {
            Invoke(nameof(ExecuteMatch), 0.1f);
        }
        else
        {
            StartCoroutine(SwipeBackAfterDelay());
        }
    }

    void MarkAsMatched(Piece piece)
    {
        piece.preventSwipeBack = true; 
        
        if (otherPiece != null)
        {
            Piece other = otherPiece.GetComponent<Piece>();
            if (other != null)
            {
                other.preventSwipeBack = true;
                other.StartCoroutine(other.ActiveSwapBack());
            }
        }

        Collider2D collider = piece.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        TriggerGridUpdate();
        gridManager.PlayEffect();

        gridManager.grid[piece.X, piece.Y] = null;
        TriggerPieceMatchedEvent(piece.pieceType);

        if (piece.IsSpecialRowPiece)
        {
            gridManager.SpawnHorizontalClear(piece.Y);
        }
        else if (piece.IsSpecialColoumnPiece)
        {
            gridManager.SpawnVerticalClear(piece.X);
        }
        else
        {
            if (matchedParticle != null)
            {
                Instantiate(matchedParticle, piece.transform.position, Quaternion.identity);
            }
        }

        // 🔥 FIX 3: Use piece.X and piece.Y so it spawns particles in the correct spot!
        piece.transform.DOScale(Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gridManager.SpawnParticleEffect(piece.X, piece.Y);
            gridManager.GameOverLogic();
            Destroy(piece.gameObject);
            gridManager.PlayRandomSFX();
        });
    }

    void TriggerGridUpdate()
    {
        gridManager.UpdateGrid();
    }

    private IEnumerator SwipeBackAfterDelay(float delay = 0.3f)
    {
        yield return new WaitForSeconds(delay);

        if (isMatched || otherPiece == null || otherPiece.GetComponent<Piece>()?.isMatched == true || preventSwipeBack)
            yield break;

        Piece other = otherPiece.GetComponent<Piece>();
        if (other == null) yield break;

        AudioManager.Instance?.PlaySFX("Swing_1");

        const float swipeTime = 0.3f;

        transform.DOMove(originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);
        otherPiece.transform.DOMove(other.originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);

        X = originalX;
        Y = originalY;
        other.X = other.originalX;
        other.Y = other.originalY;

        if (gridManager != null && gridManager.grid != null)
        {
            gridManager.grid[X, Y] = gameObject;
            gridManager.grid[other.X, other.Y] = otherPiece;
        }

        gridManager.GameOverLogic();
        gridManager.canControl = true;
    }

    void ClearColoumn(int coloumnIndex)
    {
        for (int y = 0; y < levelData.gridHeight; y++)
        {
            Piece piece = gridManager.grid[coloumnIndex, y]?.GetComponent<Piece>();
            if (piece != null && !piece.isMatched)
            {
                piece.isMatched = true;
                MarkAsMatched(piece);
                AudioManager.Instance.PlaySFX("ColoumnClear");
            }
        }
    }

    void ClearRow(int rowIndex)
    {
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            Piece piece = gridManager.grid[x, rowIndex]?.GetComponent<Piece>();
            if (piece != null && !piece.isMatched)
            {
                piece.isMatched = true;
                MarkAsMatched(piece);
                AudioManager.Instance.PlaySFX("RowClear");
            }
        }
    }

    void ClearColour(PieceType type)
    {
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                Piece piece = gridManager.grid[x, y]?.GetComponent<Piece>();
                if (piece != null && piece.pieceType == type && !piece.isMatched)
                {
                    piece.isMatched = true;
                    // 🔥 FIX 2: We only call MarkAsMatched here! I deleted the extra DOScale and Destroy 
                    // that was causing the massive missing reference errors!
                    MarkAsMatched(piece);
                }
            }
        }
    }

    void Bomb(int x, int y)
    {
        vibrateDevice(); 
        
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int targetX = x + i;
                int targetY = y + j;
                if (targetX >= 0 && targetX < levelData.gridWidth && targetY >= 0 && targetY < levelData.gridHeight)
                {
                    Piece piece = gridManager.grid[targetX, targetY]?.GetComponent<Piece>();
                    if (piece != null && !piece.isMatched)
                    {
                        piece.isMatched = true;
                        MarkAsMatched(piece);
                    }
                }
            }
        }
    }

    private void MarkAndDestroyColorPiece(Piece colorPiece)
    {
        colorPiece.isMatched = true;   
        gridManager.grid[colorPiece.X, colorPiece.Y] = null; 

        colorPiece.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
            .OnComplete(() => Destroy(colorPiece.gameObject));
    }

    void StickToTheGrid()
    {
        if (stickToGrid)
        {
            Vector2 snappedPosition = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));
            transform.position = snappedPosition;
        }
    }

    public void SetStickToGrid(float duration)
    {
        stickToGrid = false; 
        Invoke(nameof(EnableStickToGrid), duration); 
    }
    
    private void EnableStickToGrid()
    {
        stickToGrid = false; 
        if (otherPiece != null)
        {
            Piece other = otherPiece.GetComponent<Piece>();
            if (other != null)
            {
                other.stickToGrid = false; 
            }
        }
    }

    private IEnumerator ReplaceWithBomb()
    {
        // 🔥 FIX 4: Simplified the coroutine so it safely spawns the new item FIRST, then destroys itself.
        if (!gridManager.canControl || gridManager.Ability_bombCurrentAmount <= 0) yield break;

        yield return null;

        if (stickToGrid)
        {
            GameObject bomb = Instantiate(BombPiece, transform.position, Quaternion.identity);
            Piece bombPiece = bomb.GetComponent<Piece>();
            if (bombPiece != null)
            {
                bombPiece.SetPosition(X, Y); 
                bombPiece.IsSpecialBombPiece = true; 
                gridManager.RegisterNewPiece(bomb, X, Y); 
                gridManager.DeductAbility_Bomb(1); 
            }
            
            // Destroy this piece safely at the very end
            Destroy(gameObject); 
        }
    }

    private IEnumerator ReplaceWithColor()
    {
        // 🔥 FIX 4: Safety fixes applied here too!
        if (!gridManager.canControl || gridManager.Ability_colorBombCurrentAmount <= 0) yield break;

        yield return null;

        if (stickToGrid)
        {
            GameObject colorPiece = Instantiate(ColorPiece, transform.position, Quaternion.identity);
            Piece colorPieceScript = colorPiece.GetComponent<Piece>();
            if (colorPieceScript != null)
            {
                colorPieceScript.SetPosition(X, Y); 
                colorPieceScript.IsSpecialColorPiece = true; 
                gridManager.RegisterNewPiece(colorPiece, X, Y); 
                gridManager.DeductAbility_ColorBomb(1); 
            }
            
            Destroy(gameObject); 
        }
    }

    private void TriggerPieceMatchedEvent(PieceType type)
    {
        switch (type)
        {
            case PieceType.Smiling_Face: OnSmilingFaceMatched(); break;
            case PieceType.Smiling_Face_with_Tear: OnSmilingFaceWithTearMatched(); break;
            case PieceType.Angry_Face: OnAngryFaceMatched(); break;
            case PieceType.Freeze_Face: OnLaughingFaceMatched(); break;
            case PieceType.SunGlass_Face: OnSmilingFaceWithHeartEyesMatched(); break;
            case PieceType.Jumbo_Angry: OnSleepingFaceMatched(); break;
            case PieceType.Surprised_Face: OnSurprisedFaceMatched(); break;
            case PieceType.Sad_Face: OnCryingFaceMatched(); break;
        }
    }

    private void OnSmilingFaceMatched() => gridManager.Smiling_Face();
    private void OnSmilingFaceWithTearMatched() => gridManager.Smiling_Face_with_Tear();
    private void OnAngryFaceMatched() => gridManager.Angry_Face();
    public void OnLaughingFaceMatched() => gridManager.Laughing_Face();
    public void OnSleepingFaceMatched() => gridManager.Sleeping_Face();
    public void OnSurprisedFaceMatched() => gridManager.Surprised_Face();
    public void OnCryingFaceMatched() => gridManager.Crying_Face();
    private void OnSmilingFaceWithHeartEyesMatched() => gridManager.Smiling_Face_With_Heart_Eyes();

    public IEnumerator AnimatePiece()
    {
        if (pieceAnimator != null)
        {
            //pieceAnimator.SetTrigger("2ndMotion"); 
        }
        yield return new WaitForSeconds(Random.Range(1f, 5f)); 
        StartCoroutine(AnimatePiece()); 
    }

    public void ActivateBomb()
    {
        if (IsSpecialBombPiece)
        {
            Bomb(X, Y);
            isMatched = true;
            MarkAndDestroyColorPiece(this); 
            gridManager.DeductAbility_Bomb(1); 
            AudioManager.Instance.PlaySFX("Bomb_1");
        }
    }
    
    public IEnumerator ActiveSwapBack()
    {
        yield return new WaitForSeconds(0.1f); 
        preventSwipeBack = false; 
    }

    void vibrateDevice(float duration = 0.1f)
    {
        #if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
        #endif
    }
} // 🔥 FIX 1: Added the missing closing bracket to finish the script!