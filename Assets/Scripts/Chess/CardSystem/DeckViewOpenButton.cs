using UnityEngine;

public class DeckViewOpenButton : MonoBehaviour
{
    [SerializeField] string panelTitle = "Deck";
    public GameObject MovementPanel;

    public void OpenDeckView()
    {
        if (DeckViewController.Instance == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController in scene.");
            return;
        }

        DeckViewController.Instance.OpenRunDeckView(panelTitle);
        MovementPanel.SetActive(false);
    }

    public void ToggleDeckView()
    {
        if (DeckViewController.Instance == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController in scene.");
            return;
        }

        DeckViewController.Instance.ToggleRunDeckView(panelTitle);
    }

    public void CloseDeckView()
    {
        if (DeckViewController.Instance == null)
        {
            Debug.LogWarning("[DeckViewOpenButton] No DeckViewController in scene.");
            return;
        }

        DeckViewController.Instance.Close();
        MovementPanel.SetActive(true);
    }
}