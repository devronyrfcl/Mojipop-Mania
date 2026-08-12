using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

public class HomeButtonManager : MonoBehaviour
{
    [Header("References")]
    public RectTransform buttonRect;
    public RectTransform textRect;
    public GameObject panel;

    public GameObject[] DisbaleObjectArray;

    [Header("Animation Settings")]
    public float moveDuration = 0.5f;
    public float scaleDuration = 0.5f;
    public float textMoveOffset = -15f;
    public Ease easeType = Ease.OutBack;

    [Header("Events")]
    public UnityEvent onShow;
    public UnityEvent onHide;

    [Header("Banner Ad")]
    [Tooltip("ON = Banner visible when this button/panel is opened")]
    public bool showBanner = false;

    private Vector3 textOriginalPos;

    private HomeButtonManager[] homeButtonManagers;


    private void Awake()
    {
        if (textRect != null)
        {
            textOriginalPos = textRect.localPosition;
        }
    }


    private void Start()
    {
        // Find all menu buttons.
        homeButtonManagers = FindObjectsOfType<HomeButtonManager>();

        // Hide all other buttons/panels.
        foreach (HomeButtonManager manager in homeButtonManagers)
        {
            if (manager != this)
            {
                manager.HideButton();
            }
        }
    }


    public void ShowButton()
    {
        Debug.Log(
            "[MENU] ShowButton called on: " + gameObject.name +
            " | Show Banner = " + showBanner
        );


        // --------------------------------------------------------
        // BUTTON ANIMATION
        // --------------------------------------------------------

        if (buttonRect != null)
        {
            buttonRect
                .DOAnchorPosY(0f, moveDuration)
                .SetEase(Ease.OutQuad);
        }


        // --------------------------------------------------------
        // TEXT ANIMATION
        // --------------------------------------------------------

        if (textRect != null)
        {
            textRect.localScale = Vector3.zero;

            textRect
                .DOScale(Vector3.one, scaleDuration)
                .SetEase(easeType);

            textRect
                .DOLocalMoveY(
                    textOriginalPos.y + textMoveOffset,
                    scaleDuration
                )
                .SetEase(Ease.OutQuad);
        }


        // --------------------------------------------------------
        // HIDE OTHER BUTTONS
        // --------------------------------------------------------

        if (homeButtonManagers != null)
        {
            foreach (HomeButtonManager manager in homeButtonManagers)
            {
                if (manager != this)
                {
                    manager.HideButton();
                }
            }
        }


        // --------------------------------------------------------
        // DISABLE OBJECTS
        // --------------------------------------------------------

        if (DisbaleObjectArray != null)
        {
            foreach (GameObject obj in DisbaleObjectArray)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }


        // --------------------------------------------------------
        // SHOW PANEL
        // --------------------------------------------------------

        if (panel != null)
        {
            panel.SetActive(true);
        }


        // --------------------------------------------------------
        // EVENT
        // --------------------------------------------------------

        onShow?.Invoke();


        // --------------------------------------------------------
        // BANNER
        // --------------------------------------------------------

        if (AdsManager.Instance == null)
        {
            Debug.LogWarning(
                "[MENU] AdsManager.Instance is NULL!"
            );

            return;
        }


        if (showBanner)
        {
            Debug.Log(
                "[MENU] " + gameObject.name +
                " -> SHOW BANNER"
            );

            AdsManager.Instance.ShowBanner();
        }
        else
        {
            Debug.Log(
                "[MENU] " + gameObject.name +
                " -> HIDE BANNER"
            );

            AdsManager.Instance.HideBanner();
        }
    }


    public void HideButton()
    {
        // --------------------------------------------------------
        // BUTTON ANIMATION
        // --------------------------------------------------------

        if (buttonRect != null)
        {
            buttonRect
                .DOAnchorPosY(-45f, moveDuration)
                .SetEase(Ease.InQuad);
        }


        // --------------------------------------------------------
        // TEXT ANIMATION
        // --------------------------------------------------------

        if (textRect != null)
        {
            textRect
                .DOScale(Vector3.zero, scaleDuration)
                .SetEase(Ease.InBack);

            textRect
                .DOLocalMoveY(
                    textOriginalPos.y,
                    scaleDuration
                )
                .SetEase(Ease.InQuad);
        }


        // --------------------------------------------------------
        // EVENT
        // --------------------------------------------------------

        onHide?.Invoke();


        // --------------------------------------------------------
        // HIDE PANEL
        // --------------------------------------------------------

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}