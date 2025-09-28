using UnityEngine;
using UnityEngine.UI;

public enum SliderType
{
    musicVol,
    otherVol,
    crouch
}
public class SliderMethods : MonoBehaviour
{
    [SerializeField] private SoundManager SM;
    [SerializeField] private PlayerInterface player;
    [SerializeField] private SliderType sliderType;

    private void OnEnable()
    {
        if(sliderType == SliderType.crouch)
        {
            gameObject.GetComponent<Toggle>().isOn = player.GetCrouchToggle();
        }
        if (sliderType == SliderType.musicVol)
        {

            gameObject.GetComponent<Slider>().value = SM.getSoundMusicMod();
        }
        if (sliderType == SliderType.otherVol)
        {
            gameObject.GetComponent<Slider>().value = SM.GetSoundMod();
        }

    }

    private void Update()
    {
        if(gameObject.activeSelf)
        {
            if (sliderType == SliderType.crouch)
            {
                player.SetCrouchToggle(gameObject.GetComponent<Toggle>().isOn);
            }
            if (sliderType == SliderType.musicVol)
            {

                SM.SetSoundMusicMod(gameObject.GetComponent<Slider>().value);
            }
            if (sliderType == SliderType.otherVol)
            {
                SM.SetSoundMod(gameObject.GetComponent<Slider>().value);
            }
        }
    }

    public void setVolume()
    {
        SM.SetSoundMusicMod(gameObject.GetComponent<Slider>().value);
    }
    public void setOtherVolume()
    {
        SM.SetSoundMod(gameObject.GetComponent<Slider>().value);
    }
    public void setCrouch()
    {
        player.SetCrouchToggle(gameObject.GetComponent<Toggle>().isOn);
    }
}
