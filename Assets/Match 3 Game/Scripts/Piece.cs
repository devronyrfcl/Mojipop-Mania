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

    //Input related variables
    private Vector2 firstTouchPosition; // Position of the first touch
    private Vector2 finalTouchPosition; // Position of the last touch
    //public float touchAngle; // Angle of the touch movement

    public GameObject otherPiece; // The Piece that will be swapped with the current Piece
    private Vector2 tempPosition; // Temporary position for moving the Piece

    private float swipeAngle; // Angle of the swipe gesture
    //public float swipeTime = 0.3f; // Duration for the tween animation



    public PieceType pieceType; // Type of the piece

    

    public bool IsSpecialBombPiece = false; // Special piece types for different match effects
    public bool IsSpecialRowPiece = false; // Special piece types for different match effects
    public bool IsSpecialColoumnPiece = false; // Special piece types for different match effects
    public bool IsSpecialColorPiece = false; // Special piece types for different match effects
    public bool preventSwipeBack = false; // Add this line

    private GridManager gridManager; // Reference to the PieceMatch script for matching logic

    public bool isMatched = false; // Flag to check if the piece is matched

    private Vector2 originalWorldPosition;
    private int originalX, originalY;

    private LevelData levelData;



    public bool stickToGrid = true; // Whether the piece should stick to the grid
    

    public GameObject ColoumnPiece;
    public GameObject RowPiece;
    public GameObject BombPiece;
    public GameObject ColorPiece;

    public Animator pieceAnimator; // Animator for the piece

    // Animation Hashes
    //private static readonly int IdleHash = Animator.StringToHash("2ndMotion");
    

    //Paricle Effect
    public GameObject matchedParticle;

    public GameObject bombParticle1;
    public GameObject bombParticle2;
    public GameObject bombParticle3;



    public void SetPosition(int x, int y)
    {
        X = x;
        Y = y;
        transform.position = new Vector2(x, y); // Set the position of the piece in the grid
    }


    // Start is called before the first frame update
    void Start()
    {
        gridManager = FindObjectOfType<GridManager>(); // Find the PieceMatch script in the scene
        //Invoke(nameof(TriggerGridUpdate), 0.2f); // Call the method to find matches at the start

        //FindMatches(); // Call the method to find matches at the start

        Invoke(nameof(FindMatches), 0.5f); // Call the method to find matches after a short delay

        //take levelData from the gridManager
        if (gridManager != null)
        {
            levelData = gridManager.levelData; // Get the LevelData from the GridManager
        }
        else
        {
            Debug.LogError("GridManager not found in the scene.");
        }
        
        StartCoroutine(AnimatePiece()); // Start the piece animation coroutine




        stickToGrid = true; // Enable sticking to grid by default



    }

    // Update is called once per frame
    //void Update()
    //{
        //CalculateAngle();
        /*Vector2Int snapped = Vector2Int.RoundToInt(transform.position);
        transform.position = new Vector2(snapped.x, snapped.y); // Snap the piece to the grid position*/

    //}

    private void FixedUpdate()
    {
        UpdateTargetPosition();
        //FindMatches(); // Call the method to find matches at the start
        SpecialPieceCall();
    }

    void SpecialPieceCall()
    {
        //if IsSpecialBombPiece is true and click on the piece using mouse raycast then bomb(int x, int y)
        if (IsSpecialBombPiece && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                Bomb(X, Y); // Call the Bomb method with the current piece's position

                AudioManager.Instance.PlaySFX("Bomb");
                //spawn random bomb particle effect at this position
                int randomEffect = Random.Range(1, 4);
                if (randomEffect == 1)
                {
                    Instantiate(bombParticle1, transform.position, Quaternion.identity);
                }
                else if (randomEffect == 2)
                {
                    Instantiate(bombParticle2, transform.position, Quaternion.identity);
                }
                else if (randomEffect == 3)
                {
                    Instantiate(bombParticle3, transform.position, Quaternion.identity);
                }

            }
        }

        //if IsSpecialRowPiece is true and click on the piece using mouse raycast then ClearRow(int y)
        if (IsSpecialRowPiece && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ClearRow(Y); // Call the ClearRow method with the current piece's Y position
            }
        }

        //if IsSpecialColoumnPiece is true and click on the piece using mouse raycast then ClearColoumn(int x)
        if (IsSpecialColoumnPiece && Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                ClearColoumn(X); // Call the ClearColoumn method with the current piece's X position
            }
        }
    }

    /*void UpdateTargetPosition() // Update the target position based on swipe
    {
        if (finalTouchPosition != Vector2.zero)
        {
            RaycastHit2D hit = Physics2D.Raycast(finalTouchPosition, Vector2.zero);
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                if(!gridManager.canControl) return; // If swiping is not allowed, exit early

                otherPiece = hit.collider.gameObject;
                Piece other = otherPiece.GetComponent<Piece>();

                Vector2Int currentGridPos = Vector2Int.RoundToInt(transform.position);
                Vector2Int otherGridPos = Vector2Int.RoundToInt(otherPiece.transform.position);
                Vector2Int difference = otherGridPos - currentGridPos;

                if ((Mathf.Abs(difference.x) == 1 && difference.y == 0) ||
                    (Mathf.Abs(difference.y) == 1 && difference.x == 0))
                {
                    //Debug.Log("Swiped to: " + otherPiece.name + " from: " + gameObject.name);

                    Vector2 myTarget = otherPiece.transform.position;
                    Vector2 otherTarget = transform.position;

                    float swipeTime = 0.3f;

                    originalWorldPosition = transform.position;
                    originalX = X;
                    originalY = Y;

                    

                    other.originalWorldPosition = otherPiece.transform.position;
                    other.originalX = other.X;
                    other.originalY = other.Y;

                    // Only update grid and logical positions if sticking to grid
                    if (stickToGrid)
                    {
                        // Animate movement
                        transform.DOMove(myTarget, swipeTime);
                        otherPiece.transform.DOMove(otherTarget, swipeTime);

                        // Swap in grid
                        gridManager.grid[X, Y] = otherPiece;
                        gridManager.grid[other.X, other.Y] = this.gameObject;

                        // Swap logical coordinates
                        int tempX = X;
                        int tempY = Y;
                        X = other.X;
                        Y = other.Y;
                        other.X = tempX;
                        other.Y = tempY;

                        // Trigger match check
                        Invoke(nameof(FindMatches), 0.5f);
                        other.Invoke(nameof(FindMatches), 0.5f);

                        // 🔹 NEW: Color piece swap interaction
                        if (this.IsSpecialColorPiece)
                        {
                            other.ClearColour(other.pieceType); // Clear all pieces of the type of the other piece
                            this.isMatched = true; // Mark this piece as matched
                            MarkAndDestroyColorPiece(this); // Destroy this piece after marking it as matched
                        }
                        else if (other.IsSpecialColorPiece)
                        {
                            this.ClearColour(this.pieceType); // Clear all pieces of this piece type
                            other.isMatched = true; // Mark the other piece as matched
                            MarkAndDestroyColorPiece(other); // Destroy the other piece after marking it as matched
                        }

                    }
                    else
                    {
                        // If not sticking to grid, just animate freely
                        transform.DOMove(myTarget, swipeTime);
                        otherPiece.transform.DOMove(otherTarget, swipeTime);
                    }

                    finalTouchPosition = Vector2.zero;
                }
            }
        }
    }*/


    void UpdateTargetPosition()
    {
        /*if (finalTouchPosition == Vector2.zero || !gridManager.canControl)
            return;*/

        if(!PlayerDataManager.Instance.isOnline)
        {
            gridManager.ActiveNoInternetConnectionPanel();
            return;
        }

        if (!gridManager.canControl)
        {
            finalTouchPosition = Vector2.zero; // 🔹 discard old swipe
            firstTouchPosition = Vector2.zero; // (optional, also reset start point)
            return;
        }

        if (finalTouchPosition == Vector2.zero)
            return;

        

        // Calculate swipe direction
        float dx = finalTouchPosition.x - firstTouchPosition.x;
        float dy = finalTouchPosition.y - firstTouchPosition.y;

        if (Mathf.Abs(dx) < 0.2f && Mathf.Abs(dy) < 0.2f)
            return; // Ignore tiny swipes

        int targetX = X;
        int targetY = Y;

        // Determine direction
        if (Mathf.Abs(dx) > Mathf.Abs(dy))
        {
            // Horizontal swipe
            if (dx > 0)
                targetX = X + 1; // Right
            else
                targetX = X - 1; // Left
        }
        else
        {
            // Vertical swipe
            if (dy > 0)
                targetY = Y + 1; // Up
            else
                targetY = Y - 1; // Down
        }

        // Check bounds
        if (targetX < 0 || targetX >= levelData.gridWidth || targetY < 0 || targetY >= levelData.gridHeight)
            return;

        GameObject targetPieceObj = gridManager.grid[targetX, targetY];
        if (targetPieceObj == null)
            return;

        Piece targetPiece = targetPieceObj.GetComponent<Piece>();
        if (targetPiece == null)
            return;

        // Set otherPiece for swipe back logic
        otherPiece = targetPieceObj;

        // Swap logic
        Vector2 myTarget = targetPiece.transform.position;
        Vector2 otherTarget = transform.position;
        float swipeTime = 0.3f;

        originalWorldPosition = transform.position;
        originalX = X;
        originalY = Y;

        targetPiece.originalWorldPosition = targetPiece.transform.position;
        targetPiece.originalX = targetPiece.X;
        targetPiece.originalY = targetPiece.Y;

        // Animate movement
        transform.DOMove(myTarget, swipeTime);
        targetPiece.transform.DOMove(otherTarget, swipeTime);

        // Swap in grid
        gridManager.grid[X, Y] = targetPieceObj;
        gridManager.grid[targetPiece.X, targetPiece.Y] = this.gameObject;

        // 🔹 NEW: Color piece swap interaction
        if (this.IsSpecialColorPiece)
        {
            targetPiece.ClearColour(targetPiece.pieceType); // Clear all pieces of the type of the target piece
            this.isMatched = true; // Mark this piece as matched
            MarkAndDestroyColorPiece(this); // Destroy this piece after marking it as matched
        }
        else if (targetPiece.IsSpecialColorPiece)
        {
            this.ClearColour(this.pieceType); // Clear all pieces of this piece type
            targetPiece.isMatched = true; // Mark the target piece as matched
            MarkAndDestroyColorPiece(targetPiece); // Destroy the target piece after marking it as matched
        }

        // Swap logical coordinates
        int tempX = X;
        int tempY = Y;
        X = targetPiece.X;
        Y = targetPiece.Y;
        targetPiece.X = tempX;
        targetPiece.Y = tempY;

        AudioManager.Instance.PlaySFX("Swing_1");

        gridManager.canControl = false; // Disable further input until the swap is resolved

        // Trigger match check
        Invoke(nameof(FindMatches), 0.5f);
        targetPiece.Invoke(nameof(FindMatches), 0.5f);

        finalTouchPosition = Vector2.zero;
    }

    void CalculateAngle()
    {
        swipeAngle = Mathf.Atan2(finalTouchPosition.y - firstTouchPosition.y, finalTouchPosition.x - firstTouchPosition.x) * 180 / Mathf.PI;
        //Debug.Log("Swipe Angle : " + swipeAngle);
        //gridManager.CheckForMatches();

        //Invoke(nameof(FindMatches), 0.2f); // Call the method to find matches after a short delay

        //FindMatches();

    }


    private void OnMouseDown()
    {
        
        
        
        firstTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log(firstTouchPosition);

        if (gridManager.isPlacingBomb)
        {
            //Debug.Log("Placing bomb on piece at (" + X + "," + Y + ")");
            //ReplaceWithBomb(); // Replace the piece with a bomb
            StartCoroutine(ReplaceWithBomb()); // Start the coroutine to replace with a bomb
            gridManager.isPlacingBomb = false; // Reset the bomb placement flag
            return; // Exit early if placing a bomb
        }

        if (gridManager.isPlacingColor)
        {
            //ReplaceWithColor(); // Replace the piece with a color piece
            StartCoroutine(ReplaceWithColor()); // Start the coroutine to replace with a color piece
            gridManager.isPlacingColor = false; // Reset the color placement flag
            return;
        }
    }

    private void OnMouseUp()
    {
        finalTouchPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //Debug.Log(finalTouchPosition);
        CalculateAngle();
    }


    public void CheckForMatchesWithoutAction()
    {
        if (gridManager == null || gridManager.grid == null || isMatched) return;

        List<Piece> horizontalMatches = new List<Piece>();
        List<Piece> verticalMatches = new List<Piece>();

        // Horizontal Match Check
        horizontalMatches.Add(this);

        // Check Left
        for (int i = 1; X - i >= 0; i++)
        {
            Piece next = gridManager.grid[X - i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                horizontalMatches.Add(next);
            else
                break;
        }

        // Check Right
        for (int i = 1; X + i < levelData.gridWidth; i++)
        {
            Piece next = gridManager.grid[X + i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                horizontalMatches.Add(next);
            else
                break;
        }

        // Vertical Match Check
        verticalMatches.Add(this);

        // Check Down
        for (int i = 1; Y - i >= 0; i++)
        {
            Piece next = gridManager.grid[X, Y - i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                verticalMatches.Add(next);
            else
                break;
        }

        // Check Up
        for (int i = 1; Y + i < levelData.gridHeight; i++)
        {
            Piece next = gridManager.grid[X, Y + i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && !next.isMatched)
                verticalMatches.Add(next);
            else
                break;
        }

        // Mark pieces as matched if 3+ found
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

    // Method 2: Actually execute the match and destroy pieces
    public void ExecuteMatch()
    {
        if (!isMatched || this == null) return;

        List<Piece> horizontalMatches = new List<Piece>();
        List<Piece> verticalMatches = new List<Piece>();

        // Re-check horizontal matches
        horizontalMatches.Add(this);
        for (int i = 1; X - i >= 0; i++)
        {
            Piece next = gridManager.grid[X - i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched)
                horizontalMatches.Add(next);
            else
                break;
        }
        for (int i = 1; X + i < levelData.gridWidth; i++)
        {
            Piece next = gridManager.grid[X + i, Y]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched)
                horizontalMatches.Add(next);
            else
                break;
        }

        // Re-check vertical matches
        verticalMatches.Add(this);
        for (int i = 1; Y - i >= 0; i++)
        {
            Piece next = gridManager.grid[X, Y - i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched)
                verticalMatches.Add(next);
            else
                break;
        }
        for (int i = 1; Y + i < levelData.gridHeight; i++)
        {
            Piece next = gridManager.grid[X, Y + i]?.GetComponent<Piece>();
            if (next != null && next.pieceType == pieceType && next.isMatched)
                verticalMatches.Add(next);
            else
                break;
        }

        // Determine if special piece should spawn
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

        // Destroy all matched pieces
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
                // Spawn special piece only once, at the first piece's position
                if (isFirstPiece && shouldSpawnSpecial)
                {
                    spawnPosition = piece.transform.position;
                    isFirstPiece = false;
                }

                MarkAsMatched(piece);
            }
        }

        // Spawn the special piece after destroying
        if (shouldSpawnSpecial && specialPieceToSpawn != null)
        {
            StartCoroutine(SpawnSpecialPieceDelayed(specialPieceToSpawn, spawnPosition));
        }

        // Deduct move only once per match action
        if (allMatches.Count >= 3)
        {
            gridManager.DeductMove();
        }
    }

    // Helper coroutine to spawn special piece after destruction
    private IEnumerator SpawnSpecialPieceDelayed(GameObject specialPrefab, Vector2 position)
    {
        yield return new WaitForSeconds(0.35f);

        GameObject spawnedPiece = Instantiate(specialPrefab, position, Quaternion.identity);
        spawnedPiece.transform.SetParent(gridManager.transform);

        int gridX = Mathf.RoundToInt(position.x);
        int gridY = Mathf.RoundToInt(position.y);

        gridManager.RegisterNewPiece(spawnedPiece, gridX, gridY);
    }

    // Modify your existing FindMatches() to use the new system:
    public void FindMatches()
    {
        if (gridManager == null || gridManager.grid == null) return;

        CheckForMatchesWithoutAction();

        // Wait a bit then execute if matches found
        if (isMatched)
        {
            Invoke(nameof(ExecuteMatch), 0.1f);
        }
        else
        {
            // No match found, allow swipe back if needed
            StartCoroutine(SwipeBackAfterDelay());
        }
    }


    void MarkAsMatched(Piece piece)
    {
        
        piece.preventSwipeBack = true; // Prevent swipe back for
        // Prevent swipe back for the other piece as well
        if (otherPiece != null)
        {
            Piece other = otherPiece.GetComponent<Piece>();
            if (other != null)
            {
                other.preventSwipeBack = true;
                //call otherPiece's ActiveSwapBack() coroutine
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

        // Clear grid reference immediately
        gridManager.grid[piece.X, piece.Y] = null;

        TriggerPieceMatchedEvent(piece.pieceType);



        // Effect handling with if–else logic
        if (piece.IsSpecialRowPiece)
        {
            // Row particle effect
            gridManager.SpawnHorizontalClear(piece.Y);
            Debug.Log("Row piece matched at Y: " + piece.Y);
        }
        else if (piece.IsSpecialColoumnPiece)
        {
            // Column particle effect
            gridManager.SpawnVerticalClear(piece.X);
            Debug.Log("Coloumn piece matched at X: " + piece.X);
        }
        else if (piece.IsSpecialColorPiece)
        {
            // TODO: Add your color bomb effect
            //SpawnColorBombEffect(piece.transform.position);
        }
        else if (piece.IsSpecialBombPiece)
        {
            // TODO: Add your bomb effect
            //SpawnBombEffect(piece.transform.position);
        }
        else
        {
            // Normal piece matched effect
            if (matchedParticle != null)
            {
                Instantiate(matchedParticle, piece.transform.position, Quaternion.identity);
                
            }
        }

        /*// Normal piece matched effect
        if (matchedParticle != null)
        {
            Instantiate(matchedParticle, piece.transform.position, Quaternion.identity);
        }*/

        // Animate scale down to zero before destroying the piece
        piece.transform.DOScale(Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
        {
            gridManager.SpawnParticleEffect(X, Y);

            gridManager.GameOverLogic();

            //FindMatches(); // Call the method to find matches at the start
            Destroy(piece.gameObject);
            gridManager.PlayRandomSFX();
        });

        
    }


    
    void TriggerGridUpdate()
    {
        //Invoke(nameof(gridManager.UpdateGrid), 0.1f); // Delay to allow destruction to complete

        //gridManager.UpdateGrid(); // Let GridManager handle collapsing and refilling
        //Debug.Log("Grid updated after match.");


        gridManager.UpdateGrid();
    }





    /*// Helper method to check if no matches were found after a swap. If no matches found, reverse the swap wait for 1 sec and reset the positions using dotween
    private IEnumerator SwipeBackAfterDelay(float delay = 0.3f)
    {
        //Debug.Log("No matches found, reversing swap...");

        yield return new WaitForSeconds(delay);

        if (otherPiece == null) yield break;

        Piece other = otherPiece.GetComponent<Piece>();
        if (other == null) yield break;

        AudioManager.Instance.PlaySFX("Swing_1");

        float swipeTime = 0.3f;

        // Move both pieces back to their original positions
        transform.DOMove(originalWorldPosition, swipeTime);
        otherPiece.transform.DOMove(other.originalWorldPosition, swipeTime);

        // Restore grid references
        X = originalX;
        Y = originalY;
        other.X = other.originalX;
        other.Y = other.originalY;

        gridManager.grid[X, Y] = this.gameObject;
        gridManager.grid[other.X, other.Y] = otherPiece;

        gridManager.canControl = true;


    }*/

    /*private IEnumerator SwipeBackAfterDelay(float delay = 0.3f)
    {
        // Small delay before reversing the swap
        yield return new WaitForSeconds(delay);

        // Validate references
        if (otherPiece == null) yield break;

        Piece other = otherPiece.GetComponent<Piece>();
        if (other == null) yield break;

        // Play feedback sound
        AudioManager.Instance?.PlaySFX("Swing_1");

        const float swipeTime = 0.3f;

        // Animate both pieces back to original positions
        transform.DOMove(originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);
        otherPiece.transform.DOMove(other.originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);

        // Update coordinates after swipe back
        X = originalX;
        Y = originalY;
        other.X = other.originalX;
        other.Y = other.originalY;

        // Restore grid references safely
        if (gridManager != null && gridManager.grid != null)
        {
            gridManager.grid[X, Y] = gameObject;
            gridManager.grid[other.X, other.Y] = otherPiece;
            gridManager.canControl = true;
        }
    }*/


    private IEnumerator SwipeBackAfterDelay(float delay = 0.3f)
    {
        // Small delay before reversing the swap
        yield return new WaitForSeconds(delay);

        // Prevent swipe back if this piece or the other is matched or destroyed
        if (isMatched || otherPiece == null || otherPiece.GetComponent<Piece>()?.isMatched == true || preventSwipeBack)
            yield break;

        Piece other = otherPiece.GetComponent<Piece>();
        if (other == null) yield break;

        // Play feedback sound
        AudioManager.Instance?.PlaySFX("Swing_1");

        const float swipeTime = 0.3f;

        // Animate both pieces back to original positions
        transform.DOMove(originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);
        otherPiece.transform.DOMove(other.originalWorldPosition, swipeTime).SetEase(Ease.OutQuad);

        // Update coordinates after swipe back
        X = originalX;
        Y = originalY;
        other.X = other.originalX;
        other.Y = other.originalY;

        // Restore grid references safely
        if (gridManager != null && gridManager.grid != null)
        {
            gridManager.grid[X, Y] = gameObject;
            gridManager.grid[other.X, other.Y] = otherPiece;
            
        }
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
                //Debug.Log("Coloumn cleared at index: " + coloumnIndex + " for piece type: " + piece.pieceType);
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
                //Debug.Log("Row cleared at index: " + rowIndex + " for piece type: " + piece.pieceType);
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
                    MarkAsMatched(piece);
                    //Distroy this piece using DOTween
                    piece.transform.DOScale(Vector2.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        gridManager.SpawnParticleEffect(x, y);
                        Destroy(piece.gameObject);
                    });
                }
            }
        }
    }

    void Bomb(int x, int y)
    {
        
        vibrateDevice(); // Vibrate the device when bomb is triggered
        
        // Clear surrounding pieces in a 3x3 area
        
        
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

                        

                        //Debug.Log("Bomb triggered at (" + targetX + ", " + targetY + ")");
                    }
                }
            }
        }
    }

    private void MarkAndDestroyColorPiece(Piece colorPiece)
    {
        colorPiece.isMatched = true;   // Mark as matched to avoid future interactions
        gridManager.grid[colorPiece.X, colorPiece.Y] = null; // Clear grid reference

        // Animate scale down and destroy
        colorPiece.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack)
            .OnComplete(() => Destroy(colorPiece.gameObject));
    }


    void StickToTheGrid()
    {
        if (stickToGrid)
        {
            // Snap the piece to the grid position
            Vector2 snappedPosition = new Vector2(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y));
            transform.position = snappedPosition;

            /*Vector2Int snapped = Vector2Int.RoundToInt(transform.position);
            transform.position = new Vector2(snapped.x, snapped.y); // Snap the piece to the grid position
            */
        }

    }


    //stickToGrid = false; // Disable sticking to grid for this operation

    //stickToGrid will be false for a certain float time. after that it will be true both for this piece and the other piece
    public void SetStickToGrid(float duration)
    {
        stickToGrid = false; // Disable sticking to grid for this operation
        Invoke(nameof(EnableStickToGrid), duration); // Re-enable after the specified duration
    }
    private void EnableStickToGrid()
    {
        stickToGrid = false; // Re-enable sticking to grid
        if (otherPiece != null)
        {
            Piece other = otherPiece.GetComponent<Piece>();
            if (other != null)
            {
                other.stickToGrid = false; // Also enable for the other piece
            }
        }
    }



    /*private void ReplaceWithBomb()
    {
        // Check if the piece is already a bomb
        if (IsSpecialBombPiece) return;
        // Destroy the current piece
        Destroy(gameObject);
        // Spawn a new bomb piece at the same position
        GameObject bomb = Instantiate(BombPiece, transform.position, Quaternion.identity);
        Piece bombPiece = bomb.GetComponent<Piece>();
        if (bombPiece != null)
        {
            bombPiece.SetPosition(X, Y); // Set the position in the grid
            bombPiece.IsSpecialBombPiece = true; // Mark it as a special bomb piece
            gridManager.RegisterNewPiece(bomb, X, Y); // Register the new piece in the grid manager

            
            bombPiece.IsSpecialBombPiece = true; // Mark it as a special bomb piece
        }
    }

    private void ReplaceWithColor()
    {
        if (IsSpecialColorPiece) return;
        // Destroy the current piece
        Destroy(gameObject);
        // Spawn a new color piece at the same position
        GameObject colorPiece = Instantiate(ColorPiece, transform.position, Quaternion.identity);
        Piece colorPieceScript = colorPiece.GetComponent<Piece>();
        if (colorPieceScript != null)
        {
            colorPieceScript.SetPosition(X, Y); // Set the position in the grid
            colorPieceScript.IsSpecialColorPiece = true; // Mark it as a special color piece
            gridManager.RegisterNewPiece(colorPiece, X, Y); // Register the new piece in the grid manager

            colorPieceScript.IsSpecialColorPiece = true; // Mark it as a special color piece
        }

    }*/

    private IEnumerator ReplaceWithBomb()
    {

        while (true)
        {
            if(!gridManager.canControl) yield break; // If swiping is not allowed, exit early

            if(gridManager.Ability_bombCurrentAmount <= 0) yield break; // If no bombs left, exit early

            yield return null;
            if (stickToGrid)
            {
                // Destroy the current piece
                Destroy(gameObject);
                // Spawn a new bomb piece at the same position
                GameObject bomb = Instantiate(BombPiece, transform.position, Quaternion.identity);
                Piece bombPiece = bomb.GetComponent<Piece>();
                if (bombPiece != null)
                {
                    bombPiece.SetPosition(X, Y); // Set the position in the grid
                    bombPiece.IsSpecialBombPiece = true; // Mark it as a special bomb piece
                    gridManager.RegisterNewPiece(bomb, X, Y); // Register the new piece in the grid manager
                    WaitForSeconds wait = new WaitForSeconds(0.1f);
                    bombPiece.IsSpecialBombPiece = true; // Mark it as a special bomb piece

                    gridManager.DeductAbility_Bomb(1); // Deduct the bomb amount from the UI manager
                }
                break; // Exit the loop after replacing with a bomb
            }
        }
    }
    private IEnumerator ReplaceWithColor()
    {
        while (true)
        {
            if (!gridManager.canControl) yield break; // If swiping is not allowed, exit early
            if(gridManager.Ability_colorBombCurrentAmount <= 0) yield break; // If no color bombs left, exit early
            yield return null;
            if (stickToGrid)
            {
                // Destroy the current piece
                Destroy(gameObject);
                // Spawn a new color piece at the same position
                GameObject colorPiece = Instantiate(ColorPiece, transform.position, Quaternion.identity);
                Piece colorPieceScript = colorPiece.GetComponent<Piece>();
                if (colorPieceScript != null)
                {
                    colorPieceScript.SetPosition(X, Y); // Set the position in the grid
                    colorPieceScript.IsSpecialColorPiece = true; // Mark it as a special color piece
                    gridManager.RegisterNewPiece(colorPiece, X, Y); // Register the new piece in the grid manager
                    WaitForSeconds wait = new WaitForSeconds(0.1f);
                    colorPieceScript.IsSpecialColorPiece = true; // Mark it as a special color piece

                    gridManager.DeductAbility_ColorBomb(1); // Deduct the color amount from the UI manager
                }
                break; // Exit the loop after replacing with a color piece
            }
        }
    }


    private void TriggerPieceMatchedEvent(PieceType type)
    {
        switch (type)
        {
            case PieceType.Smiling_Face:
                OnSmilingFaceMatched();
                break;
            case PieceType.Smiling_Face_with_Tear:
                OnSmilingFaceWithTearMatched();
                break;
            case PieceType.Angry_Face:
                OnAngryFaceMatched();
                break;
            case PieceType.Freeze_Face:
                OnLaughingFaceMatched();
                break;
            case PieceType.SunGlass_Face:
                OnSmilingFaceWithHeartEyesMatched();
                break;
            case PieceType.Jumbo_Angry:
                OnSleepingFaceMatched();
                break;
            case PieceType.Surprised_Face:
                OnSurprisedFaceMatched();
                break;
            case PieceType.Sad_Face:
                OnCryingFaceMatched();
                break;


        }
    }

    // Example custom behaviours:
    private void OnSmilingFaceMatched()
    {
        //Debug.Log("😊 Smiling Face matched! Play happy sound or animation.");
        gridManager.Smiling_Face();
    }

    private void OnSmilingFaceWithTearMatched()
    {
        //Debug.Log("😢 Smiling Face with Tear matched! Trigger sad effect.");
        gridManager.Smiling_Face_with_Tear();
    }

    private void OnAngryFaceMatched()
    {
        //Debug.Log("😠 Angry Face matched! Shake camera maybe.");
        gridManager.Angry_Face();
    }

    public void OnLaughingFaceMatched()
    {
        //Debug.Log("😂 Laughing Face matched! Play laughter sound.");
        gridManager.Laughing_Face();
    }
    public void OnSleepingFaceMatched()
    {
        //Debug.Log("😴 Sleeping Face matched! Maybe slow down time effect.");
        gridManager.Sleeping_Face();
    }
    public void OnSurprisedFaceMatched()
    {
        //Debug.Log("😲 Surprised Face matched! Trigger surprise effect.");
        gridManager.Surprised_Face();
    }
    public void OnCryingFaceMatched()
    {
        //Debug.Log("😭 Crying Face matched! Play sad sound or animation.");
        gridManager.Crying_Face();
    }

    private void OnSmilingFaceWithHeartEyesMatched()
    {
        //Debug.Log("😍 Smiling Face with Heart Eyes matched! Trigger love effect.");
        gridManager.Smiling_Face_With_Heart_Eyes();
    }

    // 2ndMotion animation trigger will called after and after few random seconds
    public IEnumerator AnimatePiece()
    {
        if (pieceAnimator != null)
        {
            //pieceAnimator.SetTrigger("2ndMotion"); // Trigger the 2ndMotion animation
        }
        else
        {
            //Debug.LogWarning("Piece Animator is not assigned!");
        }
        yield return new WaitForSeconds(Random.Range(1f, 5f)); // Wait for a random time before triggering again
        StartCoroutine(AnimatePiece()); // Repeat the animation
    }


    public void ActivateBomb()
    {
        if (IsSpecialBombPiece)
        {
            Bomb(X, Y);
            isMatched = true;
            MarkAndDestroyColorPiece(this); // Destroy this piece after marking it as matched
            gridManager.DeductAbility_Bomb(1); // Deduct the bomb amount from the UI manager
            AudioManager.Instance.PlaySFX("Bomb_1");
        }
    }

    
    public IEnumerator ActiveSwapBack()
    {
        yield return new WaitForSeconds(0.1f); // Small delay to ensure the swap has completed
        preventSwipeBack = false; // Allow swipe back again
    }


    void vibrateDevice(float duration = 0.1f)
    {
        #if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
        #endif
    }

}  