using UnityEngine;
using TMPro;

public class LeaderboardCard : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI starText;

    [Header("Default Values (Optional)")]
    public string defaultRank = "1";
    public string defaultName = "Player";
    public string defaultStars = "0";

    void Start()
    {
        // Initialize with default values if assigned
        //ApplyValues(defaultRank, defaultName, defaultStars);
    }

    /// <summary>
    /// Public function to set leaderboard card values
    /// </summary>
    public void SetCardValues(string rank, string playerName, string stars)
    {
        if (rankText != null) rankText.text = rank;
        if (nameText != null) nameText.text = playerName;
        if (starText != null) starText.text = stars;
    }

    /// <summary>
    /// Helper to apply default or updated values
    /// </summary>
    private void ApplyValues(string rank, string playerName, string stars)
    {
        SetCardValues(rank, playerName, stars);
    }
}
