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
        
        // Ensure UI matches the loaded data instantly
        ButttonsConditions();
    }
    
    public void SetMusicVolume()
    {
        float volume = musicSlider.value;
        
        // 🔥 FIX: Clamp the minimum value so it never hits 0 and creates -Infinity
        if (volume <= 0.001f) volume = 0.001f;
        
        audioMixer.SetFloat("music", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(MusicVolumeKey, volume);
        
        ButttonsConditions();
    }

    public void SetsfxVolume()
    {
        float volume = sfxSlider.value;
        
        // 🔥 FIX: Clamp the minimum value
        if (volume <= 0.001f) volume = 0.001f;
        
        audioMixer.SetFloat("sfx", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat(SfxVolumeKey, volume);
        
        ButttonsConditions();
    }

    private void LoadVolume()
    {
        musicSlider.value = PlayerPrefs.GetFloat(MusicVolumeKey);
        sfxSlider.value = PlayerPrefs.GetFloat(SfxVolumeKey);
        
        SetMusicVolume();
        SetsfxVolume();
    }

    private void SetDefaultVolumes()
    {
        // 🔥 FIX: Explicitly set the sliders to max before saving defaults
        musicSlider.value = 1f;
        sfxSlider.value = 1f;
        
        SetMusicVolume();
        SetsfxVolume();
    }

    // --- BUTTON TOGGLES ---

    public void OnClickMusicOff() // The "Unmute" Button
    {
        musicSlider.value = 1f;
        SetMusicVolume();
    }
    
    public void OnClickMusicOn() // The "Mute" Button
    {
        // 🔥 FIX: Use 0.001f instead of 0f to prevent the AudioMixer crash
        musicSlider.value = 0.001f;
        SetMusicVolume();
    }
    
    public void OnClickSFXOff() // The "Unmute" Button
    {
        sfxSlider.value = 1f;
        SetsfxVolume();
    }

    public void OnClickSFXOn() // The "Mute" Button
    {
        // 🔥 FIX: Use 0.001f instead of 0f
        sfxSlider.value = 0.001f;
        SetsfxVolume();
    }

    // --- UI UPDATER ---

    void ButttonsConditions()
    {
        // 🔥 FIX: Use <= instead of == for floats. Floating point math is rarely exact!
        if (sfxSlider.value <= 0.001f)
        {
            SFXOnButton.SetActive(false);
            SFXOffButton.SetActive(true);
        }
        else
        {
            SFXOffButton.SetActive(false);
            SFXOnButton.SetActive(true);
        }

        if (musicSlider.value <= 0.001f)
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