using GameManager;
using UnityEngine;

public class StartScreen : MonoBehaviour
{
    public void OnClickStart()
    {
        SceneController.instance.GoTo("SampleScene");
        SceneController.instance.LoadAdditive("UI_Battle");
    }
}
