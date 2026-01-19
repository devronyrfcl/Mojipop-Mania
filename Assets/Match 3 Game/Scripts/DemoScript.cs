using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoScript : MonoBehaviour
{


    public LeaderboardCard LeaderboardCard;


    public void UpdateCard()
    {
        // Example usage
        LeaderboardCard.SetCardValues("5", "PlayerFuck", "5");
    }


}
