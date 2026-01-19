using PlayFab.ClientModels;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;

public class Leaderboard : MonoBehaviour
{
    [Header("UI Prefabs")]
    public GameObject playerCardPrefab;    // Prefab for own player card
    public GameObject otherPlayerPrefab;   // Prefab for other players

    [Header("UI Parent")]
    public Transform cardParent;           // Vertical layout group parent

    [Header("Player Info")]
    public string PlayerID;           // Set from PlayerDataManager or PlayFabManager

    private void Start()
    {
        

        Invoke("GetPlayerID", 2f); // Delay to ensure PlayerDataManager is initialized

        
    }

    public void GetAndShowLeaderboard()
    {
        var request = new GetLeaderboardRequest
        {
            StatisticName = "XP", // Replace with your statistic name
            StartPosition = 0,
            MaxResultsCount = 10 // Top 10
        };
        PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardReceived, OnError);
    }

    private void OnLeaderboardReceived(GetLeaderboardResult result)
    {
        // Clear old entries
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in result.Leaderboard)
        {
            // Spawn correct prefab
            GameObject playerCard = entry.PlayFabId == PlayerID ?
                Instantiate(playerCardPrefab, cardParent) :
                Instantiate(otherPlayerPrefab, cardParent);

            // Get LeaderboardCard component
            LeaderboardCard card = playerCard.GetComponent<LeaderboardCard>();
            if (card != null)
            {
                string rank = (entry.Position + 1).ToString(); // Position is 0-based
                string name = string.IsNullOrEmpty(entry.DisplayName) ? entry.PlayFabId : entry.DisplayName;
                string stars = entry.StatValue.ToString();

                card.SetCardValues(rank, name, stars);
                
                Debug.Log($"Leaderboard Card Created: {rank} - {name} - {stars}");
            }
            else
            {
                Debug.LogWarning("Prefab is missing LeaderboardCard script!");
            }
        }
    }

    private void OnError(PlayFabError error)
    {
        Debug.LogError("Error retrieving leaderboard: " + error.GenerateErrorReport());
    }

    void GetPlayerID()
    {
        //take PlayerID from PlayerDataManager
        PlayerID = PlayerDataManager.Instance.PlayFabPlayerID; // Assuming PlayerDataManager has PlayerID
        if (string.IsNullOrEmpty(PlayerID))
        {
            Debug.LogError("PlayerID is not set. Please ensure PlayerDataManager is initialized.");
            return;
        }
    }
}
