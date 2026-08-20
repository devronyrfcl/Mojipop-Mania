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
        LoadingBar.fillAmount = 0f;
        namePanel.SetActive(false);
        newButton.SetActive(false);
        

        StartCoroutine(FillBar());
        CheckForFirstLaunched();

        //UserID.text = "User ID: " + PlayFabManager.Instance.playerID;

    }

    public void SetUserName()
    {
        
        string userName = userNameInput.text.Trim();
        if (string.IsNullOrEmpty(userName))
        {
            Debug.LogWarning("Username cannot be empty.");
            return;
        }
        
        PlayerDataManager.Instance.SetName(userName);
        PlayerDataManager.Instance.SavePlayerData();

    }



    IEnumerator FillBar()
    {
        while (LoadingBar.fillAmount < 1f)
        {
            LoadingBar.fillAmount += fillSpeed * Time.deltaTime;
            yield return null;
        }
        
        //Debug.Log("? Loading bar filled completely!");
        namePanel.SetActive(true);

        //proceed to next step
        yield return null;
        LoadingFrame.gameObject.SetActive(false);
        newButton.SetActive(true);

        
        stageManager.RefreashData();


    }

    public void OnNextClicked()
    {

        namePanel.SetActive(false);
        SetUserName();

        SceneManager.LoadScene("MainMenu");

    }


    public void OnNewButtonClicked()
    {
        newButton.SetActive(false);
        StartCoroutine(SecondLoading());
    }
    IEnumerator SecondLoading()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        if (emojiRect != null)
        {
            emojiRect.anchoredPosition = new Vector2(0f, 3000f);
            yield return emojiRect.DOAnchorPosY(0f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }
        yield return new WaitForSeconds(0.3f);
        LoadingObject.SetActive(false);
        if (emojiRect != null)
        {
            yield return emojiRect.DOAnchorPosY(-3000f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }
        stageManager.ShowTotalXPandTotalStars();
        stageManager.RefreashData();
    }


    void CheckForFirstLaunched()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();

        if (PlayerDataManager.Instance.isLaunched)
        {
            LoadingObject.SetActive(false);
            StartCoroutine(EmojiLoading());
        }
        else
        {
            Debug.Log("StageManager: First launch detected, showing emojis.");
        }
    }

    IEnumerator EmojiLoading()
    {
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        if (emojiRect != null)
        {
            emojiRect.anchoredPosition = new Vector2(0f, 0f);
            yield return emojiRect.DOAnchorPosY(-3000f, 0.6f).SetEase(Ease.InOutQuad).WaitForCompletion();
        }
    }

}
