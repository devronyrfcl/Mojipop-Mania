using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSettings : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    private const string MusicVolumeKey = "musicVolume";
    private const string SfxVolumeKey = "sfxVolume";

    public GameObject MusicOnButton;
    public GameObject MusicOffButton;
    public GameObject SFXOnButton;
    public GameObject SFXOffButton;

    private void Start()
    {


        if (PlayerPrefs.HasKey(MusicVolumeKey))
        {
            LoadVolume();
        }
        else
        {
            SetDefaultVolumes();
        }

        ButttonsConditions();


    }

    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        audioMixer.SetFloat("music", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        ButttonsConditions();
    }

    public void SetsfxVolume()
    {
        float volume = sfxSlider.value;
        audioMixer.SetFloat("sfx", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        ButttonsConditions();
    }

    private void LoadVolume()
    {
        musicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey);
        SetMusicVolume();
        sfxSlider.value = PlayerPrefs.GetFloat(SfxVolumeKey);
        SetsfxVolume();
    }

    private void SetDefaultVolumes()
    {
        // Set default volume values or any other initialization logic here
        SetMusicVolume();
        SetsfxVolume();
    }

    public void OnClickMusicOff()
    {
        MusicOffButton.SetActive(false);
        MusicOnButton.SetActive(true);
        musicSlider.value = 1f;
        SetMusicVolume();
    }
    public void OnClickMusicOn()
    {
        MusicOnButton.SetActive(false);
        MusicOffButton.SetActive(true);
        musicSlider.value = 0f;
        SetMusicVolume();
    }
    public void OnClickSFXOff()
    {
        SFXOffButton.SetActive(false);
        SFXOnButton.SetActive(true);
        sfxSlider.value = 1f;
        SetsfxVolume();
    }

    public void OnClickSFXOn()
    {
        SFXOnButton.SetActive(false);
        SFXOffButton.SetActive(true);
        sfxSlider.value = 0f;
        SetsfxVolume();

    }

    void ButttonsConditions()
    {
        if (sfxSlider.value == 0.001f)
        {
            SFXOnButton.SetActive(false);
            SFXOffButton.SetActive(true);
        }
        else
        {
            SFXOffButton.SetActive(false);
            SFXOnButton.SetActive(true);
        }

        if (musicSlider.value == 0.001f)
        {
            MusicOnButton.SetActive(false);
            MusicOffButton.SetActive(true);
        }
        else
        {
            MusicOffButton.SetActive(false);
            MusicOnButton.SetActive(true);
        }
    }

}