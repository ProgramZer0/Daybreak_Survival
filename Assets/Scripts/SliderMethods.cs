using UnityEngine;
using UnityEngine.UI;

public class SliderMethods : MonoBehaviour
{
    [SerializeField] private SoundManager SM;
    [SerializeField] private PlayerInterface player;

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
