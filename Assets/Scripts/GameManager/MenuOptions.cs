using UnityEngine;

public class MenuOptions : MonoBehaviour
{
    
    public GameObject Panel_Menu;
    public void OnMenuClick_Open()
    {
        Panel_Menu.SetActive(true);
    }
    
    public void OnMenuClick_Close()
    {
        Panel_Menu.SetActive(false);
    }

    public void OnMenuClick_Quit()
    {
        // If running in the Unity editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If running as a built app (Windows, Mac, Android, etc.)
        Application.Quit();
#endif

        Debug.Log("Quit command executed");
    }
}
