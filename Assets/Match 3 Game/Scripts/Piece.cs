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
    private static Camera cachedMainCamera;
    private static Camera MainCam => cachedMainCamera != null ? cachedMainCamera : (cachedMainCamera = Camera.main);
    public int X; // X position in the grid
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
    // True only for a special placed by an inventory/rewarded-ad booster.
    public bool IsUiBoosterPiece = false;
    public bool preventSwipeBack = false; 

    [HideInInspector] public GridManager gridManager;

    public bool isMatched = false; 
    private bool specialEffectPlayed;

    private Vector2 originalWorldPosition;
    private int originalX, originalY;

    [HideInInspector] public LevelData levelData;

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

    // Event-driven: polling Update removed for high mobile performance
    // (Swipe & tap logic executed directly on OnMouseUp)


    void UpdateTargetPosition()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null || PlayerDataManager.Instance == null) return;
        if (levelData == null && gridManager != null) levelData = gridManager.levelData;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            if (PlayerDataManager.Instance != null) PlayerDataManager.Instance.isOnline = false;
            gridManager.ActiveNoInternetConnectionPanel();
            finalTouchPosition = Vector2.zero;
            firstTouchPosition = Vector2.zero;
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
        {
            // Tap-to-blast on tap release without dragging
            if (IsSpecialBombPiece && gridManager != null && gridManager.canControl)
            {
                finalTouchPosition = Vector2.zero;
                firstTouchPosition = Vector2.zero;
                Bomb(X, Y);
                DestroySpecialPiece(this);
                return;
            }

            finalTouchPosition = Vector2.zero;
            firstTouchPosition = Vector2.zero;
            return;
        }

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
        {
            finalTouchPosition = Vector2.zero;
            firstTouchPosition = Vector2.zero;
            return;
        }

        GameObject targetPieceObj = gridManager.grid[targetX, targetY];
        if (targetPieceObj == null)
        {
            finalTouchPosition = Vector2.zero;
            firstTouchPosition = Vector2.zero;
            return;
        }

        Piece targetPiece = targetPieceObj.GetComponent<Piece>();
        if (targetPiece == null)
        {
            finalTouchPosition = Vector2.zero;
            firstTouchPosition = Vector2.zero;
            return;
        }

        otherPiece = targetPieceObj;
        targetPiece.otherPiece = gameObject;
        preventSwipeBack = false;
        targetPiece.preventSwipeBack = false;

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

        // Record swap position for special creation
        // Record both swapped positions for special creation
        gridManager.RecordPlayerSwap(X, Y, targetPiece.X, targetPiece.Y);

        AudioManager.Instance?.PlaySFX("Swing_1");

        gridManager.canControl = false; 
        gridManager.DeductMove(); // Client requirement: Deduct move on swap

        // Execute unified swap sequence (handles animation, special triggers, match check, and guaranteed swipe back)
        StartCoroutine(HandleSwapSequence(targetPiece, swipeTime));

        finalTouchPosition = Vector2.zero;
        firstTouchPosition = Vector2.zero;
    }

    private bool IsSpecialSwap(Piece targetPiece)
    {
        if (targetPiece == null) return false;
        return IsSpecialColorPiece || targetPiece.IsSpecialColorPiece ||
               IsSpecialBombPiece || targetPiece.IsSpecialBombPiece ||
               IsSpecialRowPiece || targetPiece.IsSpecialRowPiece ||
               IsSpecialColoumnPiece || targetPiece.IsSpecialColoumnPiece;
    }

    private IEnumerator HandleSwapSequence(Piece targetPiece, float swipeTime)
    {
        yield return new WaitForSeconds(swipeTime);

        // Check for special activations after completing the visual swipe move
        if (IsSpecialSwap(targetPiece))
        {
            ActivateSpecialSwap(targetPiece);
            yield break;
        }

        // Reset match flags on both swapped pieces before evaluating
        isMatched = false;
        if (targetPiece != null) targetPiece.isMatched = false;

        CheckForMatchesWithoutAction();
        if (targetPiece != null) targetPiece.CheckForMatchesWithoutAction();

        // If either piece created a match, execute the matches
        if (isMatched || (targetPiece != null && targetPiece.isMatched))
        {
            if (isMatched) ExecuteMatch();
            if (targetPiece != null && targetPiece.isMatched) targetPiece.ExecuteMatch();
            yield break;
        }

        // NO MATCH -> GUARANTEED SWAP BACK TO ORIGINAL POSITIONS
        AudioManager.Instance?.PlaySFX("Swing_1");

        const float returnTime = 0.25f;
        transform.DOMove(originalWorldPosition, returnTime).SetEase(Ease.OutQuad);
        if (targetPiece != null)
            targetPiece.transform.DOMove(targetPiece.originalWorldPosition, returnTime).SetEase(Ease.OutQuad);

        // Restore grid coordinates and array entries
        X = originalX;
        Y = originalY;
        if (targetPiece != null)
        {
            targetPiece.X = targetPiece.originalX;
            targetPiece.Y = targetPiece.originalY;
        }

        if (gridManager != null && gridManager.grid != null)
        {
            gridManager.grid[X, Y] = gameObject;
            if (targetPiece != null)
                gridManager.grid[targetPiece.X, targetPiece.Y] = targetPiece.gameObject;

        }

        yield return new WaitForSeconds(returnTime);

        otherPiece = null;
        if (targetPiece != null) targetPiece.otherPiece = null;

        if (gridManager != null)
        {
            gridManager.canControl = true;
        }
    }

    private bool ActivateSpecialSwap(Piece targetPiece)
    {
        // 1. Color + Color Combo: Wipes the entire board!
        if (IsSpecialColorPiece && targetPiece.IsSpecialColorPiece)
        {
            ClearAllPieces();
            DestroySpecialPiece(this);
            DestroySpecialPiece(targetPiece);
            return true;
        }

        // 2. Color + Bomb Combo: Transforms all pieces of that color into bombs and explodes them!
        if ((IsSpecialColorPiece && targetPiece.IsSpecialBombPiece) || (IsSpecialBombPiece && targetPiece.IsSpecialColorPiece))
        {
            PieceType targetType = IsSpecialColorPiece ? targetPiece.pieceType : pieceType;
            ConvertAndDetonateColorCombo(targetType, true);
            DestroySpecialPiece(this);
            DestroySpecialPiece(targetPiece);
            return true;
        }

        // 3. Color + Stripe (Row/Column) Combo: Transforms all pieces of that color into lasers and fires them!
        if ((IsSpecialColorPiece && (targetPiece.IsSpecialRowPiece || targetPiece.IsSpecialColoumnPiece)) ||
            ((IsSpecialRowPiece || IsSpecialColoumnPiece) && targetPiece.IsSpecialColorPiece))
        {
            PieceType targetType = IsSpecialColorPiece ? targetPiece.pieceType : pieceType;
            ConvertAndDetonateColorCombo(targetType, false);
            DestroySpecialPiece(this);
            DestroySpecialPiece(targetPiece);
            return true;
        }

        // 4. Color + Normal Piece: Clears all pieces of that color from the board
        if (IsSpecialColorPiece)
        {
            ClearColour(targetPiece.pieceType);
            isMatched = true;
            MarkAndDestroyColorPiece(this);
            return true;
        }

        if (targetPiece.IsSpecialColorPiece)
        {
            ClearColour(pieceType);
            targetPiece.isMatched = true;
            MarkAndDestroyColorPiece(targetPiece);
            return true;
        }

        bool isThisSpecial = IsSpecialRowPiece || IsSpecialColoumnPiece || IsSpecialBombPiece;
        bool isTargetSpecial = targetPiece.IsSpecialRowPiece || targetPiece.IsSpecialColoumnPiece || targetPiece.IsSpecialBombPiece;

        // 5. Special + Special Combos:
        if (isThisSpecial && isTargetSpecial)
        {
            // Stripe + Stripe (Cross Combo)
            if ((IsSpecialRowPiece && targetPiece.IsSpecialColoumnPiece) || (IsSpecialColoumnPiece && targetPiece.IsSpecialRowPiece))
            {
                gridManager.SpawnHorizontalClear(Y);
                ClearRow(Y);
                gridManager.SpawnVerticalClear(X);
                ClearColoumn(X);
                DestroySpecialPiece(this);
                DestroySpecialPiece(targetPiece);
                return true;
            }

            // Row + Row (Double Row Clear)
            if (IsSpecialRowPiece && targetPiece.IsSpecialRowPiece)
            {
                gridManager.SpawnHorizontalClear(Y);
                ClearRow(Y);
                gridManager.SpawnHorizontalClear(targetPiece.Y);
                ClearRow(targetPiece.Y);
                DestroySpecialPiece(this);
                DestroySpecialPiece(targetPiece);
                return true;
            }

            // Column + Column (Double Column Clear)
            if (IsSpecialColoumnPiece && targetPiece.IsSpecialColoumnPiece)
            {
                gridManager.SpawnVerticalClear(X);
                ClearColoumn(X);
                gridManager.SpawnVerticalClear(targetPiece.X);
                ClearColoumn(targetPiece.X);
                DestroySpecialPiece(this);
                DestroySpecialPiece(targetPiece);
                return true;
            }

            // Bomb + Row Stripe (Mega Row Blast)
            if ((IsSpecialBombPiece && targetPiece.IsSpecialRowPiece) || (IsSpecialRowPiece && targetPiece.IsSpecialBombPiece))
            {
                gridManager.SpawnHorizontalClear(Y);
                ClearRow(Y);
                if (Y - 1 >= 0) { gridManager.SpawnHorizontalClear(Y - 1); ClearRow(Y - 1); }
                if (Y + 1 < levelData.gridHeight) { gridManager.SpawnHorizontalClear(Y + 1); ClearRow(Y + 1); }
                Bomb(X, Y);
                DestroySpecialPiece(this);
                DestroySpecialPiece(targetPiece);
                return true;
            }

            // Bomb + Column Stripe (Mega Column Blast)
            if ((IsSpecialBombPiece && targetPiece.IsSpecialColoumnPiece) || (IsSpecialColoumnPiece && targetPiece.IsSpecialBombPiece))
            {
                gridManager.SpawnVerticalClear(X);
                ClearColoumn(X);
                if (X - 1 >= 0) { gridManager.SpawnVerticalClear(X - 1); ClearColoumn(X - 1); }
                if (X + 1 < levelData.gridWidth) { gridManager.SpawnVerticalClear(X + 1); ClearColoumn(X + 1); }
                Bomb(X, Y);
                DestroySpecialPiece(this);
                DestroySpecialPiece(targetPiece);
                return true;
            }

            // Bomb + Bomb (Dual Mega Blast)
            if (IsSpecialBombPiece || targetPiece.IsSpecialBombPiece)
            {
                Bomb(X, Y);
                targetPiece.Bomb(targetPiece.X, targetPiece.Y);
                DestroySpecialPiece(this);
                DestroySpecialPiece(targetPiece);
                return true;
            }
        }

        // 6. Single Special piece swapped with a normal piece:
        if (IsSpecialRowPiece)
        {
            gridManager.SpawnHorizontalClear(Y);
            ClearRow(Y);
            DestroySpecialPiece(this);
            return true;
        }
        if (IsSpecialColoumnPiece)
        {
            gridManager.SpawnVerticalClear(X);
            ClearColoumn(X);
            DestroySpecialPiece(this);
            return true;
        }
        if (IsSpecialBombPiece)
        {
            Bomb(X, Y);
            DestroySpecialPiece(this);
            return true;
        }

        if (targetPiece.IsSpecialRowPiece)
        {
            gridManager.SpawnHorizontalClear(targetPiece.Y);
            targetPiece.ClearRow(targetPiece.Y);
            DestroySpecialPiece(targetPiece);
            return true;
        }
        if (targetPiece.IsSpecialColoumnPiece)
        {
            gridManager.SpawnVerticalClear(targetPiece.X);
            targetPiece.ClearColoumn(targetPiece.X);
            DestroySpecialPiece(targetPiece);
            return true;
        }
        if (targetPiece.IsSpecialBombPiece)
        {
            targetPiece.Bomb(targetPiece.X, targetPiece.Y);
            DestroySpecialPiece(targetPiece);
            return true;
        }

        return false;
    }

    private void ConvertAndDetonateColorCombo(PieceType type, bool isBomb)
    {
        if (levelData == null || gridManager == null || gridManager.grid == null) return;

        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                Piece piece = gridManager.grid[x, y]?.GetComponent<Piece>();
                if (piece != null && piece.pieceType == type && !piece.isMatched)
                {
                    piece.isMatched = true;
                    if (isBomb)
                    {
                        piece.IsSpecialBombPiece = true;
                        piece.PlayBombEffect();
                        piece.Bomb(x, y);
                    }
                    else
                    {
                        if (Random.value > 0.5f)
                        {
                            gridManager.SpawnHorizontalClear(y);
                            piece.ClearRow(y);
                        }
                        else
                        {
                            gridManager.SpawnVerticalClear(x);
                            piece.ClearColoumn(x);
                        }
                    }
                    MarkPieceDestroyed(piece);
                }
            }
        }
        gridManager.UpdateGrid();
    }

    private void DestroySpecialPiece(Piece piece)
    {
        if (piece == null) return;
        piece.isMatched = true;
        if (gridManager != null && gridManager.grid != null)
            gridManager.grid[piece.X, piece.Y] = null;
        TriggerPieceMatchedEvent(piece.pieceType);
        Collider2D col = piece.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
        piece.transform.DOKill();
        piece.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                gridManager?.GameOverLogic();
                gridManager?.UpdateGrid();
                Destroy(piece.gameObject);
            });
    }

    void CalculateAngle()
    {
        swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
    }

    private void OnMouseDown()
    {
        firstTouchPosition = MainCam != null ? (Vector2)MainCam.ScreenToWorldPoint(Input.mousePosition) : Vector2.zero;
        
        if (gridManager != null && gridManager.isPlacingBomb)
        {
            StartCoroutine(ReplaceWithBomb()); 
            gridManager.isPlacingBomb = false; 
            return; 
        }

        if (gridManager != null && gridManager.isPlacingColor)
        {
            StartCoroutine(ReplaceWithColor()); 
            gridManager.isPlacingColor = false; 
            return; 
        }
    }

    private void OnMouseUp()
    {
        finalTouchPosition = MainCam != null ? (Vector2)MainCam.ScreenToWorldPoint(Input.mousePosition) : Vector2.zero;
        CalculateAngle();
        UpdateTargetPosition(); // Trigger swipe processing directly on finger release
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

    public int GetHorizontalMatchCount()
    {
        if (gridManager == null || gridManager.grid == null) return 1;
        int count = 1;
        for (int i = 1; X - i >= 0; i++)
        {
            Piece next = gridManager.grid[X - i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) count++;
            else break;
        }
        for (int i = 1; X + i < levelData.gridWidth; i++)
        {
            Piece next = gridManager.grid[X + i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) count++;
            else break;
        }
        return count;
    }

    public int GetVerticalMatchCount()
    {
        if (gridManager == null || gridManager.grid == null) return 1;
        int count = 1;
        for (int i = 1; Y - i >= 0; i++)
        {
            Piece next = gridManager.grid[X, Y - i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) count++;
            else break;
        }
        for (int i = 1; Y + i < levelData.gridHeight; i++)
        {
            Piece next = gridManager.grid[X, Y + i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched) count++;
            else break;
        }
        return count;
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

        HashSet<Piece> allMatches = new HashSet<Piece>();
        if (horizontalMatches.Count >= 3)
        {
            foreach (var p in horizontalMatches) allMatches.Add(p);
        }
        if (verticalMatches.Count >= 3)
        {
            foreach (var p in verticalMatches) allMatches.Add(p);
        }

        // T-Shape & L-Shape Detection across entire cluster
        Piece tShapeIntersection = null;
        int maxH = horizontalMatches.Count >= 3 ? horizontalMatches.Count : 0;
        int maxV = verticalMatches.Count >= 3 ? verticalMatches.Count : 0;

        List<Piece> initialCheck = new List<Piece>(allMatches);
        foreach (Piece p in initialCheck)
        {
            if (p != null)
            {
                int pH = p.GetHorizontalMatchCount();
                int pV = p.GetVerticalMatchCount();

                if (pH >= 3)
                {
                    maxH = Mathf.Max(maxH, pH);
                    for (int i = 1; p.X - i >= 0; i++)
                    {
                        Piece next = gridManager.grid[p.X - i, p.Y]?.GetComponent<Piece>();
                        if (next != null && next.pieceType == pieceType && next.isMatched) allMatches.Add(next);
                        else break;
                    }
                    for (int i = 1; p.X + i < levelData.gridWidth; i++)
                    {
                        Piece next = gridManager.grid[p.X + i, p.Y]?.GetComponent<Piece>();
                        if (next != null && next.pieceType == pieceType && next.isMatched) allMatches.Add(next);
                        else break;
                    }
                }

                if (pV >= 3)
                {
                    maxV = Mathf.Max(maxV, pV);
                    for (int i = 1; p.Y - i >= 0; i++)
                    {
                        Piece next = gridManager.grid[p.X, p.Y - i]?.GetComponent<Piece>();
                        if (next != null && next.pieceType == pieceType && next.isMatched) allMatches.Add(next);
                        else break;
                    }
                    for (int i = 1; p.Y + i < levelData.gridHeight; i++)
                    {
                        Piece next = gridManager.grid[p.X, p.Y + i]?.GetComponent<Piece>();
                        if (next != null && next.pieceType == pieceType && next.isMatched) allMatches.Add(next);
                        else break;
                    }
                }

                if (pH >= 3 && pV >= 3)
                {
                    tShapeIntersection = p;
                }
            }
        }

        if (allMatches.Count < 3) return;

        int hCount = maxH;
        int vCount = maxV;

        Piece specialSource = this;

        // 1. If any piece in this match was one of the swapped tiles, prefer spawning at that tile
        Piece swappedPieceInMatch = null;
        foreach (Piece p in allMatches)
        {
            if (p != null && ((p.X == gridManager.lastSwapX1 && p.Y == gridManager.lastSwapY1) ||
                              (p.X == gridManager.lastSwapX2 && p.Y == gridManager.lastSwapY2)))
            {
                swappedPieceInMatch = p;
                break;
            }
        }

        if (swappedPieceInMatch != null)
        {
            specialSource = swappedPieceInMatch;
        }
        else if (tShapeIntersection != null)
        {
            specialSource = tShapeIntersection;
        }
        else
        {
            // For cascade / automatic matches, pick the middle piece of the run
            if (hCount >= 4 && horizontalMatches.Count >= 4)
            {
                specialSource = horizontalMatches[horizontalMatches.Count / 2];
            }
            else if (vCount >= 4 && verticalMatches.Count >= 4)
            {
                specialSource = verticalMatches[verticalMatches.Count / 2];
            }
        }

        GameObject specialPrefab = GetSpecialPrefab(hCount, vCount);
        bool createSpecial = specialPrefab != null && specialSource != null;

        if (createSpecial)
        {
            specialSource.ReplaceWithMatchSpecial(specialPrefab);
        }

        foreach (Piece piece in allMatches)
        {
            if (piece != null && (!createSpecial || piece != specialSource))
            {
                MarkAsMatched(piece);
            }
        }
        gridManager?.UpdateGrid();
    }

    private GameObject GetSpecialPrefab(int horizontalCount, int verticalCount)
    {
        // 1. 6 or more in a straight line -> Bomb piece
        if (horizontalCount >= 6 || verticalCount >= 6) return BombPiece;

        // 2. 5 in a straight line -> Color piece (Clown / Rainbow Emoji)
        if (horizontalCount == 5 || verticalCount == 5) return ColorPiece;

        // 3. T-shape or L-shape (3+ in both directions) -> Bomb piece (3x3 Blast)
        if (horizontalCount >= 3 && verticalCount >= 3) return BombPiece;

        // 4. 4 in a horizontal row -> Row clear piece (Horizontal Stripes)
        if (horizontalCount == 4) return RowPiece;

        // 5. 4 in a vertical column -> Column clear piece (Vertical Stripes)
        if (verticalCount == 4) return ColoumnPiece;

        return null;
    }
    private void ReplaceWithMatchSpecial(GameObject specialPrefab)
    {
        if (specialPrefab == null) return;

        int spawnX = X;
        int spawnY = Y;

        // Capture current visual scale before destroying

        GameObject special = Instantiate(specialPrefab, new Vector2(spawnX, spawnY), Quaternion.identity, gridManager.transform);
        special.transform.localScale = Vector3.zero;
        Piece specialPiece = special.GetComponent<Piece>();
        if (specialPiece != null)
        {
            // --- Copy ALL essential references immediately (don't wait for Start()) ---
            specialPiece.gridManager = gridManager;
            specialPiece.levelData = levelData != null ? levelData : (gridManager != null ? gridManager.levelData : null);
            // Preserve the special prefab's own particles & prefabs (never overwrite with null)
            if (specialPiece.RowPiece == null) specialPiece.RowPiece = RowPiece;
            if (specialPiece.ColoumnPiece == null) specialPiece.ColoumnPiece = ColoumnPiece;
            if (specialPiece.BombPiece == null) specialPiece.BombPiece = BombPiece;
            if (specialPiece.ColorPiece == null) specialPiece.ColorPiece = ColorPiece;
            if (specialPiece.matchedParticle == null && matchedParticle != null) specialPiece.matchedParticle = matchedParticle;
            if (specialPiece.bombParticle1 == null && bombParticle1 != null) specialPiece.bombParticle1 = bombParticle1;
            if (specialPiece.bombParticle2 == null && bombParticle2 != null) specialPiece.bombParticle2 = bombParticle2;
            if (specialPiece.bombParticle3 == null && bombParticle3 != null) specialPiece.bombParticle3 = bombParticle3;
            if (specialPiece.pieceAnimator == null && pieceAnimator != null) specialPiece.pieceAnimator = pieceAnimator;
            specialPiece.pieceAnimator = pieceAnimator;

            specialPiece.pieceType = pieceType;
            specialPiece.isMatched = false;
            specialPiece.stickToGrid = true;
            specialPiece.preventSwipeBack = false;

            // Strict mutually exclusive special flags
            if (specialPrefab == ColorPiece || specialPiece.IsSpecialColorPiece)
            {
                specialPiece.IsSpecialColorPiece = true;
                specialPiece.IsSpecialRowPiece = false;
                specialPiece.IsSpecialColoumnPiece = false;
                specialPiece.IsSpecialBombPiece = false;
            }
            else if (specialPrefab == BombPiece || specialPiece.IsSpecialBombPiece)
            {
                specialPiece.IsSpecialBombPiece = true;
                specialPiece.IsSpecialRowPiece = false;
                specialPiece.IsSpecialColoumnPiece = false;
                specialPiece.IsSpecialColorPiece = false;
            }
            else if (specialPrefab == RowPiece)
            {
                specialPiece.IsSpecialRowPiece = true;
                specialPiece.IsSpecialColoumnPiece = false;
                specialPiece.IsSpecialBombPiece = false;
                specialPiece.IsSpecialColorPiece = false;
            }
            else if (specialPrefab == ColoumnPiece)
            {
                specialPiece.IsSpecialColoumnPiece = true;
                specialPiece.IsSpecialRowPiece = false;
                specialPiece.IsSpecialBombPiece = false;
                specialPiece.IsSpecialColorPiece = false;
            }
            specialPiece.IsUiBoosterPiece = false;
        }

        gridManager.RegisterNewPiece(special, spawnX, spawnY);

        Collider2D sourceCollider = GetComponent<Collider2D>();
        if (sourceCollider != null) sourceCollider.enabled = false;
        TriggerPieceMatchedEvent(pieceType);
        transform.DOKill();
        transform.DOScale(Vector3.zero, 0.24f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));

        // Animate the special piece in at the correct size
        // Always scale special pieces to standard unit cell size (Vector3.one)
        special.transform.DOScale(Vector3.one, 0.3f).SetDelay(0.2f).SetEase(Ease.OutBack);
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
            if (otherPiece != null)
            {
                Piece other = otherPiece.GetComponent<Piece>();
                if (other != null)
                {
                    other.CheckForMatchesWithoutAction();
                    if (other.isMatched)
                    {
                        return; // The other piece matched; it will clear and refill the board
                    }
                }
            }

            StartCoroutine(SwipeBackAfterDelay(0.1f));
        }
    }
    void MarkAsMatched(Piece piece)
    {
        if (piece == null) return;

        piece.preventSwipeBack = true; 
        
        if (otherPiece != null)
        {
            Piece other = otherPiece.GetComponent<Piece>();
            if (other != null)
            {
                other.preventSwipeBack = true;
            }
        }

        Collider2D collider = piece.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        

        gridManager.grid[piece.X, piece.Y] = null;
        TriggerPieceMatchedEvent(piece.pieceType);

        // Detonate special pieces when matched
        if (piece.IsSpecialRowPiece)
        {
            gridManager.SpawnHorizontalClear(piece.Y);
            piece.ClearRow(piece.Y);
        }
        else if (piece.IsSpecialColoumnPiece)
        {
            gridManager.SpawnVerticalClear(piece.X);
            piece.ClearColoumn(piece.X);
        }
        else if (piece.IsSpecialBombPiece)
        {
            piece.PlayBombEffect();
            piece.Bomb(piece.X, piece.Y);
        }
        else if (piece.IsSpecialColorPiece)
        {
            piece.ClearColour(piece.pieceType);
        }
        else
        {
            if (piece.matchedParticle != null)
            {
                GameObject mp = ObjectPoolManager.Spawn(piece.matchedParticle, piece.transform.position, Quaternion.identity); ObjectPoolManager.Despawn(mp, 1.5f);
            }
        }
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

    private IEnumerator SwipeBackAfterDelay(float delay = 0.1f)
    {
        yield return new WaitForSeconds(delay);

        if (isMatched)
        {
            yield break;
        }

        if (otherPiece == null)
        {
            if (gridManager != null) gridManager.canControl = true;
            yield break;
        }

        Piece other = otherPiece.GetComponent<Piece>();
        if (other == null || other.isMatched)
        {
            if (gridManager != null) gridManager.canControl = true;
            yield break;
        }

        if (preventSwipeBack || other.preventSwipeBack)
        {
            yield break;
        }

        // Prevent double execution from both pieces
        preventSwipeBack = true;
        other.preventSwipeBack = true;

        AudioManager.Instance?.PlaySFX("Swing_1");

        const float swipeTime = 0.25f;

        transform.DOMove(originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);
        other.transform.DOMove(other.originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);

        X = originalX;
        Y = originalY;
        other.X = other.originalX;
        other.Y = other.originalY;

        if (gridManager != null && gridManager.grid != null)
        {
            gridManager.grid[X, Y] = gameObject;
            gridManager.grid[other.X, other.Y] = other.gameObject;
        }

        yield return new WaitForSeconds(swipeTime);

        preventSwipeBack = false;
        if (other != null) other.preventSwipeBack = false;
        otherPiece = null;
        if (other != null) other.otherPiece = null;

        if (gridManager != null)
        {
            gridManager.canControl = true;
        }
    }
    void ClearColoumn(int coloumnIndex)
    {
        if (levelData == null || gridManager == null || gridManager.grid == null) return;
        for (int y = 0; y < levelData.gridHeight; y++)
        {
            Piece piece = gridManager.grid[coloumnIndex, y]?.GetComponent<Piece>();
            if (piece != null && !piece.isMatched)
            {
                piece.isMatched = true;
                MarkPieceDestroyed(piece);
            }
        }
        AudioManager.Instance?.PlaySFX("ColoumnClear");
        gridManager.UpdateGrid();
    }

    void ClearRow(int rowIndex)
    {
        if (levelData == null || gridManager == null || gridManager.grid == null) return;
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            Piece piece = gridManager.grid[x, rowIndex]?.GetComponent<Piece>();
            if (piece != null && !piece.isMatched)
            {
                piece.isMatched = true;
                MarkPieceDestroyed(piece);
            }
        }
        AudioManager.Instance?.PlaySFX("RowClear");
        gridManager.UpdateGrid();
    }

    private void MarkPieceDestroyed(Piece piece)
    {
        if (piece == null) return;

        gridManager.grid[piece.X, piece.Y] = null;
        TriggerPieceMatchedEvent(piece.pieceType);

        // Chain Reaction: Special power-ups trigger their abilities when caught in blasts/lasers
        if (piece.IsSpecialRowPiece)
        {
            gridManager.SpawnHorizontalClear(piece.Y);
            piece.ClearRow(piece.Y);
        }
        else if (piece.IsSpecialColoumnPiece)
        {
            gridManager.SpawnVerticalClear(piece.X);
            piece.ClearColoumn(piece.X);
        }
        else if (piece.IsSpecialBombPiece)
        {
            piece.PlayBombEffect();
            piece.Bomb(piece.X, piece.Y);
        }
        else if (piece.IsSpecialColorPiece)
        {
            piece.ClearColour(piece.pieceType);
        }
        else
        {
            if (piece.matchedParticle != null)
            {
                GameObject mp = ObjectPoolManager.Spawn(piece.matchedParticle, piece.transform.position, Quaternion.identity); ObjectPoolManager.Despawn(mp, 1.5f);
            }
        }
        piece.transform.DOKill();
        piece.transform.DOScale(Vector2.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gridManager.SpawnParticleEffect(piece.X, piece.Y);
            gridManager.GameOverLogic();
            Destroy(piece.gameObject);
        });
    }
    void ClearAllPieces()
    {
        if (levelData == null || gridManager == null) return;
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                Piece piece = gridManager.grid[x, y]?.GetComponent<Piece>();
                if (piece != null && !piece.isMatched)
                {
                    piece.isMatched = true;
                    MarkPieceDestroyed(piece);
                }
            }
        }
        gridManager.UpdateGrid();
    }

    void ClearColour(PieceType type)
    {
        if (levelData == null || gridManager == null) return;
        for (int x = 0; x < levelData.gridWidth; x++)
        {
            for (int y = 0; y < levelData.gridHeight; y++)
            {
                Piece piece = gridManager.grid[x, y]?.GetComponent<Piece>();
                if (piece != null && piece.pieceType == type && !piece.isMatched)
                {
                    piece.isMatched = true;
                    MarkPieceDestroyed(piece);
                }
            }
        }
        gridManager.UpdateGrid();
    }

    void Bomb(int x, int y)
    {
        vibrateDevice();
        if (levelData == null || gridManager == null) return;

        PlayBombEffect();

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
                        MarkPieceDestroyed(piece);
                    }
                }
            }
        }

        gridManager.UpdateGrid();
    }
    private void PlayBombEffect()
    {
        if (specialEffectPlayed) return;
        specialEffectPlayed = true;

        AudioManager.Instance?.PlaySFX("Bomb");
        int randomEffect = Random.Range(0, 3);
        GameObject effect = randomEffect == 0 ? bombParticle1 : randomEffect == 1 ? bombParticle2 : bombParticle3;
        if (effect != null)
        {
            GameObject p = ObjectPoolManager.Spawn(effect, transform.position, Quaternion.identity);
            ObjectPoolManager.Despawn(p, 2f);
        }
    }

    private void MarkAndDestroyColorPiece(Piece colorPiece)
    {
        colorPiece.isMatched = true;   
        gridManager.grid[colorPiece.X, colorPiece.Y] = null; 

        colorPiece.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
            .OnComplete(() => 
            {
                gridManager.UpdateGrid();
                gridManager.GameOverLogic();
                Destroy(colorPiece.gameObject);
            });
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
        if (!gridManager.canControl || gridManager.Ability_bombCurrentAmount <= 0) yield break;

        yield return null;

        if (stickToGrid)
        {
            GameObject bomb = Instantiate(BombPiece, transform.position, Quaternion.identity);
            bomb.transform.SetParent(gridManager.transform);
            bomb.transform.localScale = Vector3.zero;
            Piece bombPiece = bomb.GetComponent<Piece>();
            if (bombPiece != null)
            {
                  bombPiece.gridManager = gridManager;
                  bombPiece.levelData = levelData != null ? levelData : (gridManager != null ? gridManager.levelData : null);
                bombPiece.SetPosition(X, Y);
                bombPiece.IsSpecialBombPiece = true;
                bombPiece.IsUiBoosterPiece = true;
                gridManager.RegisterNewPiece(bomb, X, Y);
                gridManager.DeductAbility_Bomb(1);
            }
            AnimateBoosterPlacement(bomb);
        }
    }

    private IEnumerator ReplaceWithColor()
    {
        if (!gridManager.canControl || gridManager.Ability_colorBombCurrentAmount <= 0) yield break;

        yield return null;

        if (stickToGrid)
        {
            GameObject colorPiece = Instantiate(ColorPiece, transform.position, Quaternion.identity);
            colorPiece.transform.SetParent(gridManager.transform);
            colorPiece.transform.localScale = Vector3.zero;
            Piece colorPieceScript = colorPiece.GetComponent<Piece>();
            if (colorPieceScript != null)
            {
                colorPieceScript.SetPosition(X, Y);
                colorPieceScript.pieceType = pieceType;
                colorPieceScript.IsSpecialColorPiece = true;
                colorPieceScript.IsUiBoosterPiece = true;
                gridManager.RegisterNewPiece(colorPiece, X, Y);
                gridManager.DeductAbility_ColorBomb(1);
            }
            AnimateBoosterPlacement(colorPiece);
        }
    }

    private void AnimateBoosterPlacement(GameObject placedBooster)
    {
        Collider2D sourceCollider = GetComponent<Collider2D>();
        if (sourceCollider != null) sourceCollider.enabled = false;

        transform.DOKill();
        transform.DOScale(Vector3.zero, 0.22f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
        placedBooster.transform.DOScale(Vector3.one, 0.28f).SetDelay(0.14f).SetEase(Ease.OutBack);
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
        }
    }

    void vibrateDevice(float duration = 0.1f)
    {
        #if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
        #endif
    }
}
