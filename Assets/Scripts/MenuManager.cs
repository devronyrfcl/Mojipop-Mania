using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void GoToProfile()
    {
        SceneManager.LoadScene("ProfileScene"); 
    }

    public void GoToSettings()
    {
        SceneManager.LoadScene("SettingScene"); 
    }

    public void GoToShop()
    {
        SceneManager.LoadScene("ShopScene");
    }

    public void GoToTuitorial()
    {
        SceneManager.LoadScene("TuitorialScene");
    }

    public void BackofProfile()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void BackofSettings()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void BackofShop()
    {
        SceneManager.LoadScene("MenuScene");
    }

    public void BackofTuitorial()
    {
        SceneManager.LoadScene("MenuScene");
    }

}
