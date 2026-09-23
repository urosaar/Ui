using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(AudioSource))]
public class ButtonSoundEffects : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("MP3 Sound Files")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    private AudioSource audioSource;

    private void Awake()
    {
        // Grabs the AudioSource that is automatically added to the button
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Plays the hover sound when the mouse enters the button area
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Plays the click sound when the button is clicked
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}