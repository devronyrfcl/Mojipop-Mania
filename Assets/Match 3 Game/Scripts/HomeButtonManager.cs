using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using UnityEditor;

public class HomeButtonManager : MonoBehaviour
{
    [Header("References")]
    public RectTransform buttonRect;   // Main Button RectTransform
    public RectTransform textRect;     // Text RectTransform
    public GameObject panel;

    public GameObject[] DisbaleObjectArray;

    [Header("Animation Settings")]
    public float moveDuration = 0.5f;
    public float scaleDuration = 0.5f;
    public float textMoveOffset = -15f; // how much text moves down
    public Ease easeType = Ease.OutBack;

    [Header("Events")]
    public UnityEvent onShow;
    public UnityEvent onHide;

    private Vector3 textOriginalPos;

    public HomeButtonManager[] homeButtonManagers; // Array of HomeButtonManager components

    void Awake()
    {
        if (textRect != null)
            textOriginalPos = textRect.localPosition;
    }

    private void Start()
    {
        //search for all HomeButtonManager components in the scene
        homeButtonManagers = FindObjectsOfType<HomeButtonManager>();
        // Ensure all buttons are initially hidden
        foreach (var manager in homeButtonManagers)
        {
            if (manager != this) // Avoid hiding itself
            {
                manager.HideButton();
            }
        }
    }

    public void ShowButton()
    {
        // Move button Y from -45 → 0
        buttonRect.DOAnchorPosY(0f, moveDuration).SetEase(Ease.OutQuad);

        // Animate text scale (0 → 1) and move down a little
        textRect.localScale = Vector3.zero;
        textRect.DOScale(Vector3.one, scaleDuration).SetEase(easeType);

        textRect.DOLocalMoveY(textOriginalPos.y + textMoveOffset, scaleDuration)
            .SetEase(Ease.OutQuad);

        onShow?.Invoke();

        // Notify other HomeButtonManagers to hide themselves
        foreach (var manager in homeButtonManagers)
        {
            if (manager != this) // Avoid hiding itself
            {
                manager.HideButton();
            }
        }

        //DisbaleObjectArray will be disabled when this button is shown
        foreach (var obj in DisbaleObjectArray)
        {
            if (obj != null)
            {
                obj.SetActive(false); // Disable objects when this button is shown
            }
        }

        panel.SetActive(true); // Show the panel when this button is shown
    }

    public void HideButton()
    {
        // Move button Y from 0 → -45
        buttonRect.DOAnchorPosY(-45f, moveDuration).SetEase(Ease.InQuad);

        // Animate text scale (1 → 0) and reset position
        textRect.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack);

        textRect.DOLocalMoveY(textOriginalPos.y, scaleDuration)
            .SetEase(Ease.InQuad);

        onHide?.Invoke();

        panel.SetActive(false); // Hide the panel when this button is hidden
    }
}
