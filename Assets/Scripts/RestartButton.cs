// Unity core features
using UnityEngine;

// Scene management for restarting
using UnityEngine.SceneManagement;

// A simple script that restarts the current scene.
// We attach this to a UI button.
public class RestartButton : MonoBehaviour
{
    // This function is called when the Restart button is clicked/tapped.
    // It's public so the Button's On Click event can call it.
    public void OnRestartButtonClicked()
    {
        // Reload the current scene, restarting everything.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}