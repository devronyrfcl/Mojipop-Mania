using UnityEngine;

public class ShopNavigation : MonoBehaviour
{
    [Header("Drag your panels here")]
    public GameObject shopPanel;
    public GameObject homePanel;
    public GameObject menuPanel;

    private string lastPanel = "";

    // Set this on the Shop button in HOME
    public void OpenFromHome()
    {
        lastPanel = "Home";
        shopPanel.SetActive(true);
        if (homePanel != null) homePanel.SetActive(false);
    }

    // Set this on the Shop button in MAIN MENU
    public void OpenFromMenu()
    {
        lastPanel = "Menu";
        shopPanel.SetActive(true);
        if (menuPanel != null) menuPanel.SetActive(false);
    }

    // Set this on the pink Back button inside the SHOP
    public void ClickBack()
    {
        shopPanel.SetActive(false);

        if (lastPanel == "Home")
        {
            if (homePanel != null) homePanel.SetActive(true);
        }
        else if (lastPanel == "Menu")
        {
            if (menuPanel != null) menuPanel.SetActive(true);
        }
    }
}