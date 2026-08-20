using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using System.Collections;


public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [SerializeField] private bool useTestAds = true; // 💻 Turn on for LDPlayer, turn off for production release!

    // --- Production IDs ---
    private const string prodRewardedAndroid = "ca-app-pub-5068573171198161/1669388004";
    private const string prodRewardedIOS = "ca-app-pub-5068573171198161/3280588859";
    private const string prodBannerAndroid = "ca-app-pub-5068573171198161/2773364923";
    private const string prodBannerIOS = "ca-app-pub-5068573171198161/6281852798";

    // --- Google Universal Test IDs ---
    private const string testRewardedAndroid = "ca-app-pub-3940256099942544/5224354917";
    private const string testRewardedIOS = "ca-app-pub-3940256099942544/1712485313";
    private const string testBannerAndroid = "ca-app-pub-3940256099942544/6300978111";
    private const string testBannerIOS = "ca-app-pub-3940256099942544/2934735716";

    private RewardedAd rewardedAd;
    private BannerView bannerView;

    private bool isCurrentlyLoading = false;
    private bool mobileAdsInitialized = false;

    // Main Game scene name
    private const string MAIN_GAME_SCENE = "MainGame";

    // Controls whether the banner should currently be visible.
    //
    // MainGame:
    //      Automatically true
    //
    // MainMenu:
    //      Controlled by ShowBanner() / HideBanner()
    //
    // Default is false, meaning Home/Main panel starts with no banner.
    private bool bannerShouldBeVisible = false;


    // ============================================================
    // LOGGING
    // ============================================================

    private void LogLoadAdError(string format, string adUnitId, LoadAdError error)
    {
        if (error == null)
        {
            Debug.LogError(
                $"[AdMob][{format}] Load failed for {adUnitId}, " +
                $"but LoadAdError was null."
            );

            return;
        }

        Debug.LogError(
            $"[AdMob][{format}] Load failed\n" +
            $"Ad Unit: {adUnitId}\n" +
            $"Code: {error.GetCode()}\n" +
            $"Message: {error.GetMessage()}"
        );
    }


    private void LogAdError(string format, string adUnitId, AdError error)
    {
        if (error == null)
        {
            Debug.LogError(
                $"[AdMob][{format}] Full-screen failure for {adUnitId}, " +
                $"but AdError was null."
            );

            return;
        }

        Debug.LogError(
            $"[AdMob][{format}] Full-screen error\n" +
            $"Ad Unit: {adUnitId}\n" +
            $"Code: {error.GetCode()}\n" +
            $"Message: {error.GetMessage()}"
        );
    }


    // ============================================================
    // AD UNIT IDs
    // ============================================================

    private string GetRewardedId()
    {
        if (useTestAds) return
#if UNITY_ANDROID
            testRewardedAndroid;
#else
            testRewardedIOS;
#endif

        return
#if UNITY_ANDROID
            prodRewardedAndroid;
#else
            prodRewardedIOS;
#endif
    }


    private string GetBannerId()
    {
        if (useTestAds) return
#if UNITY_ANDROID
            testBannerAndroid;
#else
            testBannerIOS;
#endif

        return
#if UNITY_ANDROID
            prodBannerAndroid;
#else
            prodBannerIOS;
#endif
    }


    // ============================================================
    // UNITY LIFECYCLE
    // ============================================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {
        // Listen for scene changes.
        SceneManager.sceneLoaded += OnSceneLoaded;

        InitializeGoogleMobileAdsConsent();
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (!isPaused && mobileAdsInitialized)
        {
            Debug.Log("[AdMob] App resumed from pause/background.");
            if (bannerShouldBeVisible)
            {
                LoadBannerAd();
            }
            if (rewardedAd == null && !isCurrentlyLoading)
            {
                LoadRewardedAd();
            }
        }
    }


    // ============================================================
    // SCENE HANDLING
    // ============================================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[AdMob] Scene loaded: {scene.name}");

        if (!mobileAdsInitialized)
        {
            Debug.Log(
                "[AdMob] Mobile Ads not initialized yet. " +
                "Banner will be handled after initialization."
            );

            return;
        }

        // --------------------------------------------------------
        // MAIN GAME
        // --------------------------------------------------------
        if (scene.name == MAIN_GAME_SCENE)
        {
            Debug.Log(
                "[AdMob] MainGame detected - banner should be visible."
            );

            bannerShouldBeVisible = true;

            UpdateBannerVisibility();

            return;
        }


        // --------------------------------------------------------
        // MAIN MENU
        // --------------------------------------------------------
        if (scene.name == "MainMenu")
        {
            Debug.Log(
                "[AdMob] MainMenu detected - starting with banner hidden."
            );

            // Every time MainMenu is entered, start at Home/Main.
            bannerShouldBeVisible = false;

            UpdateBannerVisibility();

            return;
        }


        // --------------------------------------------------------
        // OTHER SCENES
        // --------------------------------------------------------

        Debug.Log(
            $"[AdMob] {scene.name} is not MainGame or MainMenu - hiding banner."
        );

        bannerShouldBeVisible = false;

        UpdateBannerVisibility();
    }


    // ============================================================
    // BANNER CONTROL
    // ============================================================

    /// <summary>
    /// Show the banner.
    ///
    /// Use this when opening:
    /// - Shop
    /// - Social
    /// - Settings
    /// - Any other panel where you want the banner
    /// </summary>
    public void ShowBanner()
    {
        Debug.Log("[AdMob][Banner] ShowBanner() called.");

        // Remember the desired state even if AdMob
        // hasn't finished initializing yet.
        bannerShouldBeVisible = true;

        if (!mobileAdsInitialized)
        {
            Debug.Log(
                "[AdMob][Banner] Mobile Ads not initialized yet. " +
                "Banner will show after initialization."
            );

            return;
        }

        UpdateBannerVisibility();
    }


    /// <summary>
    /// Hide the banner.
    ///
    /// Use this when opening:
    /// - Home/Main panel
    /// - Any panel where you don't want the banner
    /// </summary>
    public void HideBanner()
    {
        Debug.Log("[AdMob][Banner] HideBanner() called.");

        bannerShouldBeVisible = false;

        if (bannerView != null)
        {
            bannerView.Hide();

            Debug.Log("[AdMob][Banner] Banner hidden.");
        }
    }


    private void UpdateBannerVisibility()
    {
        if (!mobileAdsInitialized)
        {
            Debug.Log(
                "[AdMob][Banner] Cannot update visibility yet. " +
                "Mobile Ads is not initialized."
            );

            return;
        }

        Scene currentScene = SceneManager.GetActiveScene();

        Debug.Log(
            $"[AdMob][Banner] Updating visibility. " +
            $"Scene: {currentScene.name}, " +
            $"ShouldShow: {bannerShouldBeVisible}"
        );


        // --------------------------------------------------------
        // MAIN GAME
        // --------------------------------------------------------

        if (currentScene.name == MAIN_GAME_SCENE)
        {
            bannerShouldBeVisible = true;
        }


        // --------------------------------------------------------
        // OTHER SCENES
        // --------------------------------------------------------

        if (
            currentScene.name != MAIN_GAME_SCENE &&
            currentScene.name != "MainMenu"
        )
        {
            bannerShouldBeVisible = false;
        }


        // --------------------------------------------------------
        // SHOULD SHOW
        // --------------------------------------------------------

        if (bannerShouldBeVisible)
        {
            if (bannerView == null)
            {
                Debug.Log(
                    "[AdMob][Banner] Banner doesn't exist. Loading banner..."
                );

                LoadBannerAd();
            }
            else
            {
                bannerView.Show();

                Debug.Log(
                    "[AdMob][Banner] Existing banner shown."
                );
            }
        }

        // --------------------------------------------------------
        // SHOULD HIDE
        // --------------------------------------------------------

        else
        {
            if (bannerView != null)
            {
                bannerView.Hide();

                Debug.Log(
                    "[AdMob][Banner] Banner hidden."
                );
            }
        }
    }


    // ============================================================
    // GOOGLE MOBILE ADS INITIALIZATION
    // ============================================================

    private void InitializeGoogleMobileAdsConsent()
    {
        Debug.Log(
            "Google Mobile Ads gathering consent."
        );

        var requestParameters = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false
        };


        ConsentInformation.Update(
            requestParameters,
            (FormError updateError) =>
            {
                if (updateError != null)
                {
                    Debug.LogError(
                        $"[AdMob][UMP] Consent update failed: " +
                        $"{updateError.Message}"
                    );
                }


                ConsentForm.LoadAndShowConsentFormIfRequired(
                    (FormError formError) =>
                    {
                        if (formError != null)
                        {
                            Debug.LogError(
                                $"[AdMob][UMP] Consent form failed: " +
                                $"{formError.Message}"
                            );
                        }


                        // Strict consent flow:
                        // only initialize MobileAds if consent allows
                        // requesting ads.

                        bool canRequest =
                            ConsentInformation.CanRequestAds();

                        Debug.Log(
                            $"[AdMob][UMP] CanRequestAds after form: " +
                            $"{canRequest}"
                        );


                        if (canRequest)
                        {
                            InitializeGoogleMobileAds();
                        }
                        else
                        {
                            Debug.LogWarning(
                                "[AdMob][UMP] Consent not granted yet. " +
                                "Will poll briefly for consent state."
                            );

                            StartCoroutine(
                                WaitForConsentAndInit()
                            );
                        }
                    }
                );
            }
        );
    }


    private void InitializeGoogleMobileAds()
    {
        Debug.Log(
            "[AdMob] Initializing MobileAds now..."
        );


        MobileAds.Initialize(
            (InitializationStatus status) =>
            {
                if (status == null)
                {
                    Debug.LogError(
                        "[AdMob] Initialization failed."
                    );

                    return;
                }


                mobileAdsInitialized = true;


                Debug.Log(
                    $"[AdMob] Initialization complete: {status}"
                );


                // ------------------------------------------------
                // REWARDED
                // ------------------------------------------------

                LoadRewardedAd();


                // ------------------------------------------------
                // BANNER
                // ------------------------------------------------
                //
                // Do NOT blindly load the banner here.
                //
                // We first check:
                // - Current scene
                // - Current panel/banner state
                //

                Scene currentScene =
                    SceneManager.GetActiveScene();


                Debug.Log(
                    $"[AdMob] Current active scene after " +
                    $"initialization: {currentScene.name}"
                );


                if (currentScene.name == MAIN_GAME_SCENE)
                {
                    bannerShouldBeVisible = true;
                }
                else if (currentScene.name != "MainMenu")
                {
                    bannerShouldBeVisible = false;
                }

                // If MainMenu, keep the existing state.
                //
                // Default:
                // bannerShouldBeVisible = false
                //
                // If user somehow called ShowBanner()
                // before initialization, it remains true.

                UpdateBannerVisibility();


                // ------------------------------------------------
                // WATCHDOG
                // ------------------------------------------------

                InvokeRepeating(
                    nameof(AdWatchdog),
                    15f,
                    15f
                );
            }
        );
    }


    private IEnumerator WaitForConsentAndInit()
    {
        const int maxAttempts = 10;

        const float delay = 0.5f;

        int attempts = 0;


        while (attempts < maxAttempts)
        {
            attempts++;


            bool canRequest =
                ConsentInformation.CanRequestAds();


            Debug.Log(
                $"[AdMob][UMP] Polling consent " +
                $"(attempt {attempts}): " +
                $"CanRequestAds={canRequest}"
            );


            if (canRequest)
            {
                InitializeGoogleMobileAds();

                yield break;
            }


            yield return new WaitForSeconds(delay);
        }


        // If we get here, consent still isn't available.
        //
        // For development with test ads,
        // initialize anyway so testing can proceed.
        //
        // For production, do not initialize.

        if (useTestAds)
        {
            Debug.LogWarning(
                "[AdMob][UMP] Consent not available after polling; " +
                "initializing MobileAds because useTestAds is true " +
                "(development only)."
            );


            InitializeGoogleMobileAds();
        }
        else
        {
            Debug.LogWarning(
                "[AdMob][UMP] Consent not available after polling; " +
                "MobileAds will NOT initialize. Users in EEA may " +
                "require explicit consent."
            );
        }
    }


    // ============================================================
    // AD WATCHDOG
    // ============================================================

    private void AdWatchdog()
    {
        if (rewardedAd == null && !isCurrentlyLoading)
        {
            LoadRewardedAd();
        }

        Scene currentScene = SceneManager.GetActiveScene();
        if ((bannerShouldBeVisible || currentScene.name == MAIN_GAME_SCENE) && bannerView == null && mobileAdsInitialized)
        {
            LoadBannerAd();
        }
    }


    // ============================================================
    // BANNER AD LOADING
    // ============================================================

    private void LoadBannerAd()
    {
        if (!mobileAdsInitialized)
        {
            Debug.LogWarning("[AdMob] MobileAds not initialized; aborting LoadBannerAd().");
            return;
        }

        if (bannerView != null)
        {
            bannerView.Destroy();
            bannerView = null;
        }

        // Use Anchored Adaptive Banner for optimal fill rate and crisp mobile scaling
        AdSize bannerSize;
        try
        {
            bannerSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        }
        catch
        {
            bannerSize = AdSize.Banner;
        }

        bannerView = new BannerView(
            GetBannerId(),
            bannerSize,
            AdPosition.Top
        );

        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            LogLoadAdError("Banner", GetBannerId(), error);
            if (bannerView != null)
            {
                bannerView.Destroy();
                bannerView = null;
            }

            // Automatically retry loading after 5 seconds if the banner should be visible
            Scene currentScene = SceneManager.GetActiveScene();
            if (bannerShouldBeVisible || currentScene.name == MAIN_GAME_SCENE)
            {
                CancelInvoke(nameof(RetryLoadBanner));
                Invoke(nameof(RetryLoadBanner), 5f);
            }
        };

        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log($"[AdMob][Banner] Loaded: {GetBannerId()}");
            Scene currentScene = SceneManager.GetActiveScene();
            bool shouldShow = bannerShouldBeVisible || currentScene.name == MAIN_GAME_SCENE;

            if (shouldShow && bannerView != null)
            {
                bannerView.Show();
                Debug.Log("[AdMob][Banner] Banner shown.");
            }
            else if (bannerView != null)
            {
                bannerView.Hide();
                Debug.Log("[AdMob][Banner] Banner loaded but kept hidden.");
            }
        };

        bannerView.LoadAd(new AdRequest());
        Debug.Log("[AdMob][Banner] Loading banner...");
    }

    private void RetryLoadBanner()
    {
        if (bannerView == null && mobileAdsInitialized)
        {
            Debug.Log("[AdMob][Banner] Retrying banner load...");
            LoadBannerAd();
        }
    }


    // ============================================================
    // REWARDED AD
    // ============================================================

    public void LoadRewardedAd()
    {
        if (isCurrentlyLoading)
            return;


        // Prevent concurrent stacking requests.

        isCurrentlyLoading = true;


        if (rewardedAd != null)
        {
            rewardedAd.Destroy();

            rewardedAd = null;
        }


        if (!mobileAdsInitialized)
        {
            Debug.LogWarning(
                "[AdMob] MobileAds not initialized; " +
                "aborting LoadRewardedAd()."
            );


            isCurrentlyLoading = false;

            return;
        }


        RewardedAd.Load(
            GetRewardedId(),
            new AdRequest(),
            (RewardedAd ad, LoadAdError error) =>
            {
                isCurrentlyLoading = false;


                if (error != null || ad == null)
                {
                    if (error != null)
                    {
                        Debug.LogError(
                            $"[AdMob][Rewarded] Load failed. " +
                            $"Error Code: {error.GetCode()} - " +
                            $"Message: {error.GetMessage()}"
                        );
                    }
                    else
                    {
                        Debug.LogError(
                            "[AdMob][Rewarded] Load failed: " +
                            "returned ad is null and " +
                            "LoadAdError was null."
                        );
                    }


                    rewardedAd = null;

                    return;
                }


                rewardedAd = ad;


                Debug.Log(
                    $"[AdMob][Rewarded] Loaded: " +
                    $"{GetRewardedId()}"
                );


                // ------------------------------------------------
                // OPENED
                // ------------------------------------------------

                rewardedAd.OnAdFullScreenContentOpened += () =>
                {
                    Debug.Log(
                        $"[AdMob][Rewarded] Opened: " +
                        $"{GetRewardedId()}"
                    );
                };


                // ------------------------------------------------
                // CLOSED
                // ------------------------------------------------

                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    Debug.Log(
                        $"[AdMob][Rewarded] Closed: " +
                        $"{GetRewardedId()}"
                    );


                    rewardedAd = null;


                    LoadRewardedAd();
                };


                // ------------------------------------------------
                // FAILED
                // ------------------------------------------------

                rewardedAd.OnAdFullScreenContentFailed +=
                    (AdError err) =>
                    {
                        LogAdError(
                            "Rewarded",
                            GetRewardedId(),
                            err
                        );


                        rewardedAd = null;


                        LoadRewardedAd();
                    };
            }
        );
    }


    // ============================================================
    // SHOW REWARDED AD
    // ============================================================

    public void ShowRewardedAd(
        Action onRewardEarned
    )
    {
        if (!mobileAdsInitialized)
        {
            Debug.LogWarning(
                "[AdMob] Cannot show rewarded ad: " +
                "MobileAds not initialized yet."
            );

            return;
        }


        if (
            rewardedAd != null &&
            rewardedAd.CanShowAd()
        )
        {
            rewardedAd.Show(
                (Reward reward) =>
                {
                    // Run on main thread to avoid Unity
                    // physics/UI crashes.

                    MobileAdsEventExecutor.ExecuteInUpdate(
                        () =>
                        {
                            onRewardEarned?.Invoke();
                        }
                    );
                }
            );
        }
        else
        {
            Debug.LogWarning(
                "Ad not ready yet."
            );


            LoadRewardedAd();
        }
    }
}
