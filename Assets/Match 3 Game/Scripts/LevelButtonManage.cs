using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LevelButtonManager : MonoBehaviour
{
    [Header("Level Data")]
    public int starCount; // 0-3
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
        Button btn = GetComponent<Button>();
        if (btn != null) btn.interactable = value;
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonImage != null) buttonImage.color = value ? Color.white : Color.gray;
    }

    private void Awake()
    {
        buttonImage = GetComponent<Image>();
    }

    private void Start()
    {
        UpdateButtonState();
        UpdateStarDisplay();
        UpdateLevelIdText();
    }

    public void UpdateButtonState()
    {
        if (buttonImage == null) buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.sprite = isLocked ? lockedSprite : unlockedSprite;
        }

        if (currentLevelGlow != null)
        {
            currentLevelGlow.SetActive(isCurrentLevel);
        }
    }

    public void UpdateStarDisplay()
    {
        // Case 1: Locked - No stars
        if (isLocked && !isCurrentLevel)
        {
            if (normalStars != null)
            {
                for (int i = 0; i < normalStars.Length; i++)
                {
                    if (normalStars[i] != null) normalStars[i].SetActive(false);
                }
            }
            if (glowStars != null)
            {
                for (int i = 0; i < glowStars.Length; i++)
                {
                    if (glowStars[i] != null) glowStars[i].SetActive(false);
                }
            }
            if (lockIcon != null) lockIcon.SetActive(true);
            return;
        }

        // Case 2: CurrentLevel or Unlocked - Show stars
        if (normalStars != null)
        {
            for (int i = 0; i < normalStars.Length; i++)
            {
                if (normalStars[i] != null) normalStars[i].SetActive(true);
            }
        }
        if (glowStars != null)
        {
            for (int i = 0; i < glowStars.Length; i++)
            {
                if (glowStars[i] != null) glowStars[i].SetActive(false);
            }
        }
        if (lockIcon != null) lockIcon.SetActive(false);

        // Show earned stars (glow effect)
        if (glowStars != null && normalStars != null)
        {
            for (int i = 0; i < starCount && i < glowStars.Length && i < normalStars.Length; i++)
            {
                if (normalStars[i] != null) normalStars[i].SetActive(false);
                if (glowStars[i] != null) glowStars[i].SetActive(true);
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
        UpdateStarDisplay();
    }

    public void SetCurrentLevel(bool current)
    {
        isCurrentLevel = current;
        UpdateButtonState();
        UpdateStarDisplay();
    }

    public void OnLevelButtonPressed()
    {
        if (stageManager != null)
        {
            stageManager.SelectLevel(this); // just pass the clicked button
        }
    }
}
