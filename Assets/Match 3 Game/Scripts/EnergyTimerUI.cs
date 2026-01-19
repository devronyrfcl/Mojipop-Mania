using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class EnergyTimerUI : MonoBehaviour
{
    public TMP_Text energyTimerText; // Shows "Next energy in: 04:32"
    public TMP_Text currentEnergyText; // Shows "5/10"
    public TMP_Text energyTitleText; // Shows Title
    public Button AdsButton;

    private void Start()
    {
        StartCoroutine(UpdateEnergyTimerUI());
    }

    public void UpdateUI()
    {
        StartCoroutine(UpdateEnergyTimerUI());
    }

    private IEnumerator UpdateEnergyTimerUI()
    {
        while (true)
        {
            if (PlayerDataManager.Instance != null)
            {
                int currentEnergy = PlayerDataManager.Instance.GetEnergyCount();
                int maxEnergy = PlayerDataManager.Instance.playerData.MaxEnergy;

                // Update energy count display
                if (currentEnergyText != null)
                {
                    currentEnergyText.text = $"{currentEnergy}/{maxEnergy}";
                }

                // Update timer display
                if (energyTimerText != null)
                {
                    if (currentEnergy >= maxEnergy)
                    {
                        energyTimerText.text = "Energy Full!";
                        energyTitleText.text = "You have enough energy to play..!";
                        AdsButton.interactable = false;
                    }
                    else
                    {
                        string timeRemaining = PlayerDataManager.Instance.GetFormattedTimeUntilNextEnergy();
                        energyTimerText.text = $"Next in: {timeRemaining}";
                        energyTitleText.text = "Not enough energy to play this level!";
                        AdsButton.interactable = true;
                    }
                }
            }

            yield return new WaitForSeconds(1f); // Update every second
        }
    }


    public void SkipEnergyGenerateTime()
    {
        // Call the method to watch ad and reward energy
        PlayerDataManager.Instance.SkipEnergyGenerateTime();


    }



}