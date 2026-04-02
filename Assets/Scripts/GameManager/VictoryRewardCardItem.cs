using UnityEngine;
using UnityEngine.EventSystems;
using Card;
using Chess;

[DisallowMultipleComponent]
public class VictoryRewardCardItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] StatusDatabase statusDatabase;

    Card.Card _runtimeCard;
    VictoryRewardPanel _rewardPanel;
    int _rewardIndex = -1;

    public void Bind(Card.Card runtimeCard, VictoryRewardPanel rewardPanel, int rewardIndex)
    {
        _runtimeCard = runtimeCard;
        _rewardPanel = rewardPanel;
        _rewardIndex = rewardIndex;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_runtimeCard == null)
        {
            Debug.LogWarning("[VictoryRewardCardItem] No runtime card bound.");
            return;
        }

        // LEFT CLICK = claim reward
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_rewardPanel == null)
            {
                Debug.LogWarning("[VictoryRewardCardItem] No VictoryRewardPanel assigned.");
                return;
            }

            _rewardPanel.ClaimRewardAtIndex(_rewardIndex);
            return;
        }

        // RIGHT CLICK = inspect
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (CardInspectModal.Instance == null)
            {
                Debug.LogWarning("[VictoryRewardCardItem] No CardInspectModal in scene.");
                return;
            }

            CardInspectModal.Instance.Show(_runtimeCard, statusDatabase);
        }
    }
}