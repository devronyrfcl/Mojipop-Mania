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

        // ✅ Move EmojisImage into view (Y: 2150 → -1777)
        yield return emojiRect.DOAnchorPosY(-1250f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();

        // ✅ Wait 1 second
        yield return new WaitForSeconds(1f);

        // ✅ Deactivate loading object during this time
        LoadingObject.SetActive(false);

        // ✅ Move EmojisImage back up (Y: -1777 → 2150)
        yield return emojiRect.DOAnchorPosY(2150f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();

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
        //EmojisImage_2.SetActive(true); // Show EmojisImage_2
        
        RectTransform emojiRect = EmojisImage.GetComponent<RectTransform>();
        //set emojiRect position to Y -1777
        emojiRect.anchoredPosition = new Vector2(emojiRect.anchoredPosition.x, -1777f);

        // ✅ Move EmojisImage into view (Y: -1777 → 2150)
        yield return emojiRect.DOAnchorPosY(2150f, 1f).SetEase(Ease.InOutQuad).WaitForCompletion();


    }

}
