using UnityEngine;

public class DynamicMapScroller : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag your Content object here")]
    public RectTransform contentBox;

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
}
