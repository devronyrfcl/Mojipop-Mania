using UnityEngine;

public class DynamicMapScroller : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag your Content object here")]
    public RectTransform contentBox;

    [Header("Scroll Settings")]
    [Tooltip("The minimum height of the map (just enough to show Level 1 and the bottom UI)")]
    public float baseHeight = 800f; 
    
    [Tooltip("How many pixels of height to add for each new level unlocked")]
    public float heightAddedPerLevel = 250f; 

    [Header("Player Data")]
    [Tooltip("Change this number in the Inspector to test different unlocked levels!")]
    public int highestUnlockedLevel = 1;

    void Start()
    {
        // Adjust the map height as soon as the map loads
        UpdateMapHeight();
    }

    public void UpdateMapHeight()
    {
        if (contentBox == null)
        {
            Debug.LogWarning("DynamicMapScroller: You forgot to drag the Content box into the script!");
            return;
        }

        // Calculate the total height needed. 
        // We subtract 1 because level 1 is already covered by the baseHeight.
        float totalHeight = baseHeight + (heightAddedPerLevel * (highestUnlockedLevel - 1));

        // Apply the new height to the Content box, keeping its current width
        contentBox.sizeDelta = new Vector2(contentBox.sizeDelta.x, totalHeight);
    }
}