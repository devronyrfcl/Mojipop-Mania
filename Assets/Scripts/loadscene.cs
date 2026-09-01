using DG.Tweening; // Make sure you have DOTween installed
using System.Collections;
using TMPro; // For TMP_InputField
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // For scene management

public class loadscene : MonoBehaviour
{
    public GameObject LoadingObject;

    public Image LoadingBar;
    public GameObject LoadingFrame;
    public float fillSpeed = 0.5f; 
    public GameObject namePanel;     
    public GameObject newButton;
    public GameObject EmojisImage;

    public TMP_InputField userNameInput;
    public TMP_Text UserID;

    public StageManager stageManager; // Reference to StageManager




    void Start()
    {
        if (LoadingBar != null) LoadingBar.fillAmount = 0f;
        if (namePanel != null) namePanel.SetActive(false);
        if (newButton != null) newButton.SetActive(false);
        
        StartCoroutine(FillBar());
    }

    public void SetUserName()
    {
        if (userNameInput == null) return;

        string userName = userNameInput.text.Trim();
        if (string.IsNullOrEmpty(userName))
        {
            Debug.LogWarning("Username cannot be empty.");
            return;
        }
        
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.SetName(userName);
            PlayerDataManager.Instance.SavePlayerData();
        }
    }

    IEnumerator FillBar()
    {
        while (LoadingBar != null && LoadingBar.fillAmount < 1f)
        {
            LoadingBar.fillAmount += fillSpeed * Time.deltaTime;
            yield return null;
        }
        
        yield return null;
        if (LoadingFrame != null) LoadingFrame.gameObject.SetActive(false);

        bool hasSavedName = PlayerDataManager.Instance != null && 
                            PlayerDataManager.Instance.playerData != null && 
                            !string.IsNullOrEmpty(PlayerDataManager.Instance.playerData.Name) &&
                            PlayerDataManager.Instance.playerData.Name != "Temp";

        if (hasSavedName)
        {
            // Existing player: skip name input panel and transition straight to menu
            if (namePanel != null) namePanel.SetActive(false);
            if (newButton != null) newButton.SetActive(false);
            StartCoroutine(SecondLoading());
        }
        else
        {
            // First launch without a name: prompt player to enter their name
            if (namePanel != null) namePanel.SetActive(true);
            if (newButton != null) newButton.SetActive(true);
        }
        
        if (stageManager != null)
        {
            stageManager.RefreashData();
        }
    }

    public void OnNextClicked()
    {
        if (userNameInput != null)
        {
            string userName = userNameInput.text.Trim();
            if (string.IsNullOrEmpty(userName))
            {
                Debug.LogWarning("Username cannot be empty.");
                return;
            }
        }

        SetUserName();

        if (namePanel != null) namePanel.SetActive(false);
        if (newButton != null) newButton.SetActive(false);

        // Smooth transition to menu
        StartCoroutine(SecondLoading());
    }

    public void OnNewButtonClicked()
    {
        if (newButton != null) newButton.SetActive(false);
        StartCoroutine(SecondLoading());
    }

    IEnumerator SecondLoading()
    {
        if (EmojisImage != null)
        {
            RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
            if (emojiRect != null)
            {
                emojiRect.anchoredPosition = new Vector2(0f, 3000f);
                yield return emojiRect.DOAnchorPosY(0f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
            }
        }
        yield return new WaitForSeconds(0.3f);
        if (LoadingObject != null) LoadingObject.SetActive(false);
        if (EmojisImage != null)
        {
            RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
            if (emojiRect != null)
            {
                yield return emojiRect.DOAnchorPosY(-3000f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
            }
        }
        if (stageManager != null)
        {
            stageManager.ShowTotalXPandTotalStars();
            stageManager.RefreashData();
        }
    }

}
