using UnityEngine;
using UnityEngine.UI;

public class DynamicMapScroller : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag your Content object here")]
    public RectTransform contentBox;
    [Tooltip("The map ScrollRect. If empty, it is found on this GameObject.")]
    public ScrollRect mapScrollRect;

    [Header("Scroll Settings")]
    [Tooltip("The minimum height of the map (just enough to show Level 1 and the bottom UI)")]
    public float baseHeight = 1200f; 
    
    [Tooltip("How many pixels of height to add for each new level unlocked")]
    public float heightAddedPerLevel = 280f; 

    [Header("Player Data")]
    [Tooltip("Change this number in the Inspector to test different unlocked levels!")]
    public int highestUnlockedLevel = 1;

    private void Awake()
    {
        if (mapScrollRect == null) mapScrollRect = GetComponent<ScrollRect>();
        UpdateMapHeight();
    }

    private void Start()
    {
        UpdateMapHeight();
    }

    private void OnEnable()
    {
        UpdateMapHeight();
    }

    public void SyncUnlockedLevel()
    {
        if (PlayerDataManager.Instance != null)
        {
            int maxLevel = PlayerDataManager.Instance.currentLevel;
            if (PlayerDataManager.Instance.playerData != null && PlayerDataManager.Instance.playerData.Levels != null)
            {
                foreach (var lvl in PlayerDataManager.Instance.playerData.Levels)
                {
                    if (lvl != null && lvl.LevelLocked == 0 && lvl.LevelID > maxLevel)
                    {
                        maxLevel = lvl.LevelID;
                    }
                }
            }
            highestUnlockedLevel = Mathf.Max(highestUnlockedLevel, maxLevel);
        }
    }

    public void UpdateMapHeight()
    {
        if (contentBox == null)
        {
            Debug.LogWarning("DynamicMapScroller: You forgot to drag the Content box into the script!");
            return;
        }

        SyncUnlockedLevel();

        // Ensure contentBox is large enough to cover all levels on the map (at least 24 levels) plus extra buffer
        int levelCountForHeight = Mathf.Max(highestUnlockedLevel + 3, 24);
        float totalHeight = baseHeight + (heightAddedPerLevel * (levelCountForHeight - 1));

        // Apply the new height to the Content box, keeping its current width
        contentBox.sizeDelta = new Vector2(contentBox.sizeDelta.x, totalHeight);
    }

    /// <summary>Opens the map at the level the player last chose, not at Level 1.</summary>
    public void FocusLastOpenedLevel(int levelIndex, int totalLevelCount)
    {
        if (mapScrollRect == null || totalLevelCount <= 1) return;
        StartCoroutine(FocusAfterLayout(levelIndex, totalLevelCount));
    }

    private System.Collections.IEnumerator FocusAfterLayout(int levelIndex, int totalLevelCount)
    {
        // Content height is assigned during menu setup; wait one frame so ScrollRect
        // has valid bounds before applying the normalized position.
        yield return null;
        Canvas.ForceUpdateCanvases();
        float normalized = Mathf.Clamp01(levelIndex / (float)(totalLevelCount - 1));
        mapScrollRect.verticalNormalizedPosition = normalized;
        mapScrollRect.velocity = Vector2.zero;
    }
}
