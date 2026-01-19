using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using GoogleMobileAds.Api;
using UnityEngine.UI;

public class Spin : MonoBehaviour
{
    [Header("Spin Settings")]
    public float spinDuration = 5f;
    public int spinRounds = 5;
    public GameObject spinObject;
    public UnityEvent onSpinComplete;
    public UnityEvent onWatchComplete;
    public GameObject spinNowBtn;
    public GameObject watchAdBtn;

    [Header("Spin Limit")]
    public int spinCount = 5;
    public TextMeshProUGUI spinLeftText;

    [Header("Bonus Count System")]
    public int bonusCount = 3; // Current available bonus ads
    public TextMeshProUGUI bonusCountText; // Text to show count or depleted message
    public Button watchAdButton; // Reference to watch ad button component

    private bool isSpinning = false;
    private float finalAngle;

    [Header("Reward UI")]
    public GameObject winPanel;
    public GameObject bombImage;
    public TextMeshProUGUI bombText;
    public GameObject colorBombImage;
    public TextMeshProUGUI colorBombText;
    public GameObject extraMoveImage;
    public TextMeshProUGUI extraMoveText;
    public GameObject vibeImage;
    public TextMeshProUGUI vibeText;
    public GameObject ShuffleImage;
    public TextMeshProUGUI ShuffleText;

    public string rewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917"; // test ad unit id

    private RewardedAd rewardedAd;

    // ✅ PlayerPrefs Keys
    private const string BONUS_COUNT_KEY = "BonusCount";
    private const string LAST_BONUS_DATE_KEY = "LastBonusDate";

    void Start()
    {
        UpdateSpinText();
        ResetRewardUI();

        LoadSpinCount();
        LoadBonusCount(); // ✅ Load bonus count
        CheckAndResetDailyBonus(); // ✅ Check for daily reset
        UpdateBonusCountUI(); // ✅ Update UI

        LoadRewardedAd();
    }

    #region "Ad Loading"
    void LoadRewardedAd()
    {
        var adRequest = new AdRequest();

        RewardedAd.Load(rewardedAdUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError("Failed to load rewarded ad: " + error);
                rewardedAd = null;
                return;
            }

            rewardedAd = ad;
            Debug.Log("Rewarded ad loaded successfully");

            rewardedAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Ad closed. Reloading new ad...");
                LoadRewardedAd();
            };

            rewardedAd.OnAdFullScreenContentFailed += (AdError err) =>
            {
                Debug.LogError("Ad failed to show: " + err);
            };

            rewardedAd.OnAdPaid += (AdValue value) =>
            {
                Debug.Log("Rewarded Ad revenue: " + value.Value);
            };
        });
    }

    public void ShowRewardedAd()
    {
        // ✅ Check if bonus count is available
        if (bonusCount <= 0)
        {
            Debug.LogWarning("No bonus ads left today!");
            return;
        }

        if (rewardedAd != null)
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Reward earned from ad: " + reward.Amount);

                // ✅ Deduct bonus count
                UseBonusCount();

                // ⭐ Give the player +1 spin
                AddBonusSpin();
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready. Reloading...");
            LoadRewardedAd();
        }
    }
    #endregion

    #region "Bonus Count System"

    /// <summary>
    /// Load bonus count from PlayerPrefs
    /// </summary>
    void LoadBonusCount()
    {
        if (PlayerPrefs.HasKey(BONUS_COUNT_KEY))
        {
            bonusCount = PlayerPrefs.GetInt(BONUS_COUNT_KEY);
        }
        else
        {
            bonusCount = 3; // Default value
            SaveBonusCount();
        }
        Debug.Log($"Loaded Bonus Count: {bonusCount}");
    }

    /// <summary>
    /// Save bonus count to PlayerPrefs
    /// </summary>
    void SaveBonusCount()
    {
        PlayerPrefs.SetInt(BONUS_COUNT_KEY, bonusCount);
        PlayerPrefs.Save();
        Debug.Log($"Saved Bonus Count: {bonusCount}");
    }

    /// <summary>
    /// Check if a new day has started and add 1 bonus
    /// </summary>
    void CheckAndResetDailyBonus()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string lastDate = PlayerPrefs.GetString(LAST_BONUS_DATE_KEY, "");

        if (string.IsNullOrEmpty(lastDate) || lastDate != today)
        {
            // ✅ New day detected - add 1 bonus (up to max 3)
            if (bonusCount < 3)
            {
                bonusCount++;
                Debug.Log($"Daily Bonus Added! New count: {bonusCount}");
            }
            else
            {
                Debug.Log("Bonus count already at max (3)");
            }

            // Update last bonus date
            PlayerPrefs.SetString(LAST_BONUS_DATE_KEY, today);
            SaveBonusCount();
        }
        else
        {
            Debug.Log($"Daily bonus already generated today. Current count: {bonusCount}");
        }
    }

    /// <summary>
    /// Use one bonus count when watching ad
    /// </summary>
    void UseBonusCount()
    {
        if (bonusCount > 0)
        {
            bonusCount--;
            SaveBonusCount();
            UpdateBonusCountUI();
            Debug.Log($"Bonus Count Used! Remaining: {bonusCount}");
        }
    }

    /// <summary>
    /// Update bonus count UI elements
    /// </summary>
    void UpdateBonusCountUI()
    {
        if (bonusCountText != null)
        {
            if (spinCount <= 0)
            {
                // ✅ Show bonus count or depleted message when spin count is 0
                if (bonusCount <= 0)
                {
                    bonusCountText.text = "No ads bonus left! Come back tomorrow.";
                    spinNowBtn.SetActive(false);
                }
                else
                {
                    //bonusCountText.text = $"{bonusCount}";
                    bonusCountText.text = "Bonus: " + $"{bonusCount}";
                }
                bonusCountText.gameObject.SetActive(true);
            }
            else
            {
                // ✅ Hide text when spin count is 1 or more
                bonusCountText.text = "";
                bonusCountText.gameObject.SetActive(false);
            }
        }

        // Make watch ad button non-interactable when no bonuses left
        if (watchAdButton != null)
        {
            watchAdButton.interactable = (bonusCount > 0);
            Debug.Log($"Watch Ad Button Interactable: {bonusCount > 0}");
        }
    }

    #endregion

    #region "Spin System"

    void LoadSpinCount()
    {
        if (PlayerPrefs.HasKey("SpinCount"))
        {
            spinCount = PlayerPrefs.GetInt("SpinCount");
        }
        else
        {
            spinCount = 3; // default value
            SaveSpinCount();
        }
        UpdateSpinText();
        checkSpinCountForAds();
    }

    void SaveSpinCount()
    {
        PlayerPrefs.SetInt("SpinCount", spinCount);
        PlayerPrefs.Save();
    }

    public void StartSpin()
    {
        if (isSpinning) return; // 🔒 block spam clicks

        if (spinCount > 0)
        {
            spinCount--;
            UpdateSpinText();

            isSpinning = true;
            spinNowBtn.SetActive(false);

            float randomAngle = UnityEngine.Random.Range(0f, 360f);
            float totalAngle = (360f * spinRounds) + randomAngle;

            spinObject.transform.DOKill();

            spinObject.transform
                .DORotate(new Vector3(0, 0, totalAngle), spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutCubic)
                .OnComplete(() =>
                {
                    isSpinning = false;
                    finalAngle = spinObject.transform.eulerAngles.z;

                    HandleResult(finalAngle);
                    checkSpinCountForAds();

                    onSpinComplete.Invoke();
                });

            onWatchComplete.Invoke();
        }
        else
        {
            Debug.Log("No spins left! Watch ad for more.");
            checkSpinCountForAds();
        }
    }

    private void UpdateSpinText()
    {
        if (spinLeftText != null)
        {
            spinLeftText.text = "Spin left: " + spinCount;
        }
    }

    void checkSpinCountForAds()
    {
        if (spinCount <= 0)
        {
            if (spinNowBtn != null) spinNowBtn.SetActive(false);
            if (watchAdBtn != null) watchAdBtn.SetActive(true);
        }
        else
        {
            if (spinNowBtn != null) spinNowBtn.SetActive(true);
            if (watchAdBtn != null) watchAdBtn.SetActive(false);
        }

        // ✅ Also update bonus count UI when checking spin state
        UpdateBonusCountUI();
    }

    public void AddBonusSpin()
    {
        spinCount += 1;
        UpdateSpinText();
        SaveSpinCount(); // ✅ Save immediately
        if (spinLeftText != null) spinLeftText.gameObject.SetActive(true);
        checkSpinCountForAds();
    }

    #endregion

    #region "Spin Results"

    private void HandleResult(float angle)
    {
        ResetRewardUI();
        winPanel.SetActive(true);

        angle = (angle + 22.5f) % 360f;

        if (angle >= 0 && angle < 45) Result_1();
        else if (angle >= 45 && angle < 90) Result_2();
        else if (angle >= 90 && angle < 135) Result_3();
        else if (angle >= 135 && angle < 180) Result_4();
        else if (angle >= 180 && angle < 225) Result_5();
        else if (angle >= 225 && angle < 270) Result_6();
        else if (angle >= 270 && angle < 315) Result_7();
        else if (angle >= 315 && angle < 360) Result_8();
    }

    void Result_1()
    {
        Debug.Log("Won Color Bomb x1!");
        colorBombImage.SetActive(true);
        colorBombText.text = "1";
        PlayerDataManager.Instance.AddColorBombAbility(1);
    }

    void Result_2()
    {
        Debug.Log("Won Moves x1!");
        extraMoveImage.SetActive(true);
        extraMoveText.text = "1";
        PlayerDataManager.Instance.AddExtraMoveAbility(1);
    }

    void Result_3()
    {
        Debug.Log("Won Moves x2!");
        extraMoveImage.SetActive(true);
        extraMoveText.text = "2";
        PlayerDataManager.Instance.AddExtraMoveAbility(2);
    }

    void Result_4()
    {
        /*Debug.Log("Won Bomb and Color Bomb");
        bombImage.SetActive(true);
        bombText.text = "2";
        colorBombImage.SetActive(true);
        colorBombText.text = "2";
        PlayerDataManager.Instance.AddBombAbility(2);
        PlayerDataManager.Instance.AddColorBombAbility(2);*/
        Debug.Log("Won Bomb x1");
        bombImage.SetActive(true);
        bombText.text = "1";
        PlayerDataManager.Instance.AddBombAbility(1);
    }

    void Result_5()
    {
        Debug.Log("Won Color Bomb x1!");
        colorBombImage.SetActive(true);
        colorBombText.text = "1";
        PlayerDataManager.Instance.AddColorBombAbility(1);
    }

    void Result_6()
    {
        /*Debug.Log("Won Color Bomb x1");
        colorBombImage.SetActive(true);
        colorBombText.text = "1";
        PlayerDataManager.Instance.AddColorBombAbility(1);*/
        Debug.Log("Won Vibe x1");
        vibeImage.SetActive(true);
        vibeText.text = "1";
        PlayerDataManager.Instance.AddEnergy(1);
    }

    void Result_7()
    {
        Debug.Log("Won Moves x1!");
        extraMoveImage.SetActive(true);
        extraMoveText.text = "1";
        PlayerDataManager.Instance.AddExtraMoveAbility(1);
    }

    void Result_8()
    {
        /*Debug.Log("Won Bomb x2!");
        bombImage.SetActive(true);
        bombText.text = "2";
        PlayerDataManager.Instance.AddBombAbility(2);*/
        Debug.Log("Won Shuffle x1");
        ShuffleImage.SetActive(true);
        ShuffleText.text = "1";
        PlayerDataManager.Instance.AddShuffleAbility(1);

    }

    void ResetRewardUI()
    {
        winPanel.SetActive(false);
        bombImage.SetActive(false);
        colorBombImage.SetActive(false);
        extraMoveImage.SetActive(false);
    }

    public void CloseWinPanel()
    {
        winPanel.SetActive(false);
        bombImage.SetActive(false);
        colorBombImage.SetActive(false);
        extraMoveImage.SetActive(false);
        spinNowBtn.SetActive(true);
        PlayerDataManager.Instance.SavePlayerData();
        SaveSpinCount();
    }

    #endregion
}