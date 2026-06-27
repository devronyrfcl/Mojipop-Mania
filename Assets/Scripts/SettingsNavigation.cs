using UnityEngine;

public class SettingsNavigation : MonoBehaviour
{
    [Header("Drag your panels here")]
    public GameObject settingsPanel;
    public GameObject mapPanel;
    public GameObject menuPanel;

    // This string will remember where we came from
    private string lastPanel = "";

    // Set this on the Gear button located on the MAP
    public void OpenFromMap()
    {
        lastPanel = "Map";
        settingsPanel.SetActive(true);
        mapPanel.SetActive(false); 
    }

    // Set this on the Gear button located on the MENU
    public void OpenFromMenu()
    {
        lastPanel = "Menu";
        settingsPanel.SetActive(true);
        menuPanel.SetActive(false); 
    }

    // Set this on the pink Back button inside SETTINGS
    public void ClickBack()
    {
        settingsPanel.SetActive(false);

        if (lastPanel == "Map")
        {
            mapPanel.SetActive(true);
        }
        else if (lastPanel == "Menu")
        {
            menuPanel.SetActive(true);
        }
    }
}