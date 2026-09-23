using UnityEngine;

public class QuitManager : MonoBehaviour
{
    public void QuitGame()
    {
        // This closes the built application
        Application.Quit();

        // This stops Play Mode while testing in the Unity Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}