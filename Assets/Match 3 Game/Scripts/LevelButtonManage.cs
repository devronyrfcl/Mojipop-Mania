using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LevelButtonManager : MonoBehaviour
{
    [Header("Level Data")]
    public int starCount; // 0–3
    public int levelId; // Example: 1, 2, 3...
    public bool isLocked;
    public bool isCurrentLevel;

    [Header("UI References")]
    public TMP_Text levelIdText; // TextMeshPro text for level number
    public GameObject[] normalStars; // Empty stars
    public GameObject[] glowStars;   // Filled stars

    [Header("Button States")]
    public Sprite lockedSprite;
    public Sprite unlockedSprite;
    private Image buttonImage;
    public GameObject lockIcon; // Optional lock icon

    [Header("Current Level Effect")]
    public GameObject currentLevelGlow; // Optional effect for current level

    public StageManager stageManager; // Assign in Inspector

    public void SetInteractable(bool value)
    {
        /*if (button != null)
            button.interactable = value;*/
        this.GetComponent<Button>().interactable = value;
        buttonImage.color = value ? Color.white : Color.gray; // Change color based on interactability
        /*if (currentLevelGlow != null)
        {
            currentLevelGlow.SetActive(value && isCurrentLevel);
        }
        if (levelIdText != null)
        {
            levelIdText.color = value ? Color.white : Color.gray; // Change text color based on interactability
        }*/

    }

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
    }

    private void Update()
    {
        UpdateButtonState();
        UpdateStarDisplay();
        UpdateLevelIdText();
    }

    

    public void UpdateButtonState()
    {
        buttonImage.sprite = isLocked ? lockedSprite : unlockedSprite;

        if (currentLevelGlow != null)
        {
            currentLevelGlow.SetActive(isCurrentLevel);
        }
    }

    public void UpdateStarDisplay()
    {
        // Case 1: Locked → No stars
        if (isLocked && !isCurrentLevel)
        {
            for (int i = 0; i < 3; i++)
            {
                normalStars[i].SetActive(false);
                glowStars[i].SetActive(false);
                lockIcon.SetActive(true);
            }
            return;
        }

        // Case 2: CurrentLevel or Unlocked → Show stars
        for (int i = 0; i < 3; i++)
        {
            normalStars[i].SetActive(true);
            glowStars[i].SetActive(false);
            lockIcon.SetActive(false);
        }

        // Show earned stars (glow effect)
        for (int i = 0; i < starCount; i++)
        {
            if (i < 3)
            {
                normalStars[i].SetActive(false);
                glowStars[i].SetActive(true);
            }
        }
    }

    public void UpdateLevelIdText()
    {
        if (levelIdText != null)
        {
            levelIdText.text = levelId.ToString();
        }
    }

    public void SetStar(int stars)
    {
        starCount = Mathf.Clamp(stars, 0, 3); // Ensure star count is between 0 and 3
        UpdateStarDisplay();
    }
    public void SetLevelId(int id)
    {
        levelId = id;
        UpdateLevelIdText();
    }
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        UpdateButtonState();
    }
    public void SetCurrentLevel(bool current)
    {
        isCurrentLevel = current;
        UpdateButtonState();
    }

    public void OnLevelButtonPressed()
    {
        if (stageManager != null)
        {
            
            stageManager.SelectLevel(this); // just pass the clicked button
        }
    }


}
