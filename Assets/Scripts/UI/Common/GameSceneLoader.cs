using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneLoader : MonoBehaviour
{
    [SerializeField] private string menuSceneName = "MainMenu";

    public void BackToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }
}
