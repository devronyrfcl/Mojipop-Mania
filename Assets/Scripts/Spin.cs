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
    public int spinCount = 1;
    public TextMeshProUGUI spinLeftText;

    [Header("Bonus Count System")]
    public int bonusCount = 3; 
    public TextMeshProUGUI bonusCountText; 
    public Button watchAdButton; 

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

    private const string BONUS_COUNT_KEY = "BonusCount";
    private const string LAST_BONUS_DATE_KEY = "LastBonusDate";
    private const string SPIN_COUNT_KEY = "SpinCount";
    private const string LAST_SPIN_DATE_KEY = "LastSpinDate";

    void Start()
    {
        UpdateSpinText();
        ResetRewardUI();

        LoadSpinCount();
        LoadBonusCount(); 
        CheckAndResetDailyBonus(); 
        UpdateBonusCountUI(); 
    }

    #region "Ad Loading"
    public void ShowRewardedAd()
    {
        if (bonusCount <= 0)
        {
            Debug.LogWarning("No bonus ads left today!");
            return;
        }

        // 🔥 Uses your new AdsManager!
        AdsManager.Instance.ShowRewardedAd(() => 
        {
            UseBonusCount();
            AddBonusSpin();
        });
    }
    #endregion

    #region "Bonus Count System"
    void LoadBonusCount()
    {
        if (PlayerPrefs.HasKey(BONUS_COUNT_KEY)) bonusCount = PlayerPrefs.GetInt(BONUS_COUNT_KEY);
        else
        {
            bonusCount = 3; 
            SaveBonusCount();
        }
    }

    void SaveBonusCount()
    {
        PlayerPrefs.SetInt(BONUS_COUNT_KEY, bonusCount);
        PlayerPrefs.Save();
    }

    void CheckAndResetDailyBonus()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string lastDate = PlayerPrefs.GetString(LAST_BONUS_DATE_KEY, "");

        if (string.IsNullOrEmpty(lastDate) || lastDate != today)
        {
            if (bonusCount < 3) bonusCount++;
            PlayerPrefs.SetString(LAST_BONUS_DATE_KEY, today);
            SaveBonusCount();
        }
    }

    void UseBonusCount()
    {
        if (bonusCount > 0)
        {
            bonusCount--;
            SaveBonusCount();
            UpdateBonusCountUI();
        }
    }

    void UpdateBonusCountUI()
    {
        if (bonusCountText != null)
        {
            if (spinCount <= 0)
            {
                if (bonusCount <= 0)
                {
                    bonusCountText.text = "No ads bonus left! Come back tomorrow.";
                    spinNowBtn.SetActive(false);
                }
                else
                {
                    bonusCountText.text = "Bonus: " + $"{bonusCount}";
                }
                bonusCountText.gameObject.SetActive(true);
            }
            else
            {
                bonusCountText.text = "";
                bonusCountText.gameObject.SetActive(false);
            }
        }

        if (watchAdButton != null)
        {
            watchAdButton.interactable = (bonusCount > 0);
        }
    }
    #endregion

    #region "Spin System"
    void LoadSpinCount()
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        string lastDate = PlayerPrefs.GetString(LAST_SPIN_DATE_KEY, "");

        if (string.IsNullOrEmpty(lastDate) || lastDate != today)
        {
            spinCount = 1;
            PlayerPrefs.SetString(LAST_SPIN_DATE_KEY, today);
            SaveSpinCount();
        }
        else if (PlayerPrefs.HasKey(SPIN_COUNT_KEY))
        {
            spinCount = Mathf.Clamp(PlayerPrefs.GetInt(SPIN_COUNT_KEY), 0, 1);
        }
        else
        {
            spinCount = 1;
            SaveSpinCount();
        }
        UpdateSpinText();
        checkSpinCountForAds();
    }

    void SaveSpinCount()
    {
        spinCount = Mathf.Clamp(spinCount, 0, 1);
        PlayerPrefs.SetInt(SPIN_COUNT_KEY, spinCount);
        PlayerPrefs.Save();
    }

    public void StartSpin()
    {
        if (isSpinning) return; 

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
                    onSpinComplete.Invoke();
                });

            onWatchComplete.Invoke();
        }
        else
        {
            checkSpinCountForAds();
        }
    }

    private void UpdateSpinText()
    {
        if (spinLeftText != null) spinLeftText.text = "Spin left: " + spinCount;
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
        UpdateBonusCountUI();
    }

    public void AddBonusSpin()
    {
        spinCount = Mathf.Clamp(spinCount + 1, 0, 1);
        UpdateSpinText();
        SaveSpinCount(); 
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
        colorBombImage.SetActive(true);
        colorBombText.text = "1";
        PlayerDataManager.Instance.AddColorBombAbility(1);
    }

    void Result_2()
    {
        extraMoveImage.SetActive(true);
        extraMoveText.text = "1";
        PlayerDataManager.Instance.AddExtraMoveAbility(1);
    }

    void Result_3()
    {
        extraMoveImage.SetActive(true);
        extraMoveText.text = "2";
        PlayerDataManager.Instance.AddExtraMoveAbility(2);
    }

    void Result_4()
    {
        bombImage.SetActive(true);
        bombText.text = "1";
        PlayerDataManager.Instance.AddBombAbility(1);
    }

    void Result_5()
    {
        colorBombImage.SetActive(true);
        colorBombText.text = "1";
        PlayerDataManager.Instance.AddColorBombAbility(1);
    }

    void Result_6()
    {
        vibeImage.SetActive(true);
        vibeText.text = "1";
        PlayerDataManager.Instance.AddEnergy(1);
    }

    void Result_7()
    {
        extraMoveImage.SetActive(true);
        extraMoveText.text = "1";
        PlayerDataManager.Instance.AddExtraMoveAbility(1);
    }

    void Result_8()
    {
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
        if (vibeImage != null) vibeImage.SetActive(false); 
        if (ShuffleImage != null) ShuffleImage.SetActive(false); 

        checkSpinCountForAds(); 

        PlayerDataManager.Instance.SavePlayerData();
        SaveSpinCount();
        
        // 🔥 FIX: Tell StageManager to immediately redraw the text with the new item!
        StageManager stage = FindObjectOfType<StageManager>();
        if (stage != null)
        {
            stage.ShowTotalXPandTotalStars();
        }
    }
#endregion
}