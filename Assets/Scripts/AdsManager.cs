using System;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdsManager : MonoBehaviour
{
    // 🔥 The Singleton: This allows any script to call AdsManager.Instance from anywhere!
    public static AdsManager Instance { get; private set; }

    [Header("Ad Unit IDs")]
    private const string rewardedAdUnitId =
#if UNITY_ANDROID
        "ca-app-pub-5068573171198161/1757128667";
#elif UNITY_IOS
        "ca-app-pub-5068573171198161/3280588859";
#else
        "";
#endif

    private const string bannerAdUnitId =
#if UNITY_ANDROID
        "ca-app-pub-5068573171198161/2958529693";
#elif UNITY_IOS
        "ca-app-pub-5068573171198161/6281852798";
#else
        "";
#endif

    private RewardedAd rewardedAd;
    private BannerView bannerView;

    private void Awake()
    {
        // 🛡️ Singleton & Persistence Logic
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroys duplicates if you reload the Main Menu
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keeps this script alive across all scenes!
    }

    private void Start()
    {
        // Initialize the Google Mobile Ads SDK
        MobileAds.Initialize((InitializationStatus status) =>
        {
            LoadRewardedAd();
            LoadBannerAd();
            InvokeRepeating(nameof(AdWatchdog), 10f, 10f); // Keep checking if ads are ready
        });
    }

    private void AdWatchdog()
    {
        if (rewardedAd == null)
        {
            LoadRewardedAd();
        }
    }

    // ==========================================
    // BANNER AD LOGIC
    // ==========================================
    private void LoadBannerAd()
    {
        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        if (string.IsNullOrEmpty(bannerAdUnitId)) return;

        bannerView = new BannerView(bannerAdUnitId, AdSize.Banner, AdPosition.Top);
        var adRequest = new AdRequest();
        bannerView.LoadAd(adRequest);
    }

    // ==========================================
    // REWARDED AD LOGIC
    // ==========================================
    public void LoadRewardedAd()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

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
            Debug.Log("Rewarded ad loaded successfully in the background!");

            // Automatically load a new ad the moment the player closes the current one
            rewardedAd.OnAdFullScreenContentClosed += () => { LoadRewardedAd(); };
            rewardedAd.OnAdFullScreenContentFailed += (AdError err) => { LoadRewardedAd(); };
        });
    }

    // 🔥 THE MAGIC FUNCTION: Uses an "Action" so the caller decides what the reward is!
    public void ShowRewardedAd(Action onRewardEarned)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log("Video finished! Player gets the reward!");
                
                // This instantly executes whatever gameplay code you pass into it!
                onRewardEarned?.Invoke(); 
            });
        }
        else
        {
            Debug.LogWarning("Rewarded ad not ready. Attempting to reload...");
            LoadRewardedAd();
        }
    }
}