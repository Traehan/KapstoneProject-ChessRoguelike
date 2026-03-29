using GameManager;
using UnityEngine;
using Chess;
public class StartScreen : MonoBehaviour
{
    void Start()
    {
        GameEvents.OnMusicRequested?.Invoke(SoundEventId.MusicMenu);
    }
    
    public void OnClickStart()
    {
        SceneController.instance.GoTo("ClanSelectScene");
    }
}
