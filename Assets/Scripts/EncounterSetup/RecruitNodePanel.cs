using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Card;

public class RecruitNodePanel : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] GameObject panelRoot;

    [Header("Option A")]
    [SerializeField] GameObject optionARoot;
    [SerializeField] CardView optionACardView;
    [SerializeField] Button chooseAButton;

    [Header("Option B")]
    [SerializeField] GameObject optionBRoot;
    [SerializeField] CardView optionBCardView;
    [SerializeField] Button chooseBButton;

    [Header("Leave / Skip")]
    [SerializeField] Button leaveButton;
    [SerializeField] TMP_Text leaveButtonText;

    List<UnitCardDefinitionSO> currentOptions = new();
    bool rewardChosen = false;

    void Awake()
    {
        if (chooseAButton != null)
        {
            chooseAButton.onClick.RemoveListener(OnChooseA);
            chooseAButton.onClick.AddListener(OnChooseA);
        }

        if (chooseBButton != null)
        {
            chooseBButton.onClick.RemoveListener(OnChooseB);
            chooseBButton.onClick.AddListener(OnChooseB);
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(OnLeave);
            leaveButton.onClick.AddListener(OnLeave);
        }

        CloseImmediate();
    }

    public void Open()
    {
        var gs = GameSession.I;
        if (gs == null)
        {
            Debug.LogWarning("[RecruitNodePanel] No GameSession found.");
            return;
        }

        currentOptions = gs.GetRecruitOptionsFromStartingTroopPool(2);
        rewardChosen = false;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        BindOption(optionARoot, optionACardView, currentOptions.Count > 0 ? currentOptions[0] : null);
        BindOption(optionBRoot, optionBCardView, currentOptions.Count > 1 ? currentOptions[1] : null);

        if (chooseAButton != null)
            chooseAButton.gameObject.SetActive(currentOptions.Count > 0);

        if (chooseBButton != null)
            chooseBButton.gameObject.SetActive(currentOptions.Count > 1);

        if (leaveButtonText != null)
            leaveButtonText.text = "Skip";
    }

    void BindOption(GameObject root, CardView cardView, UnitCardDefinitionSO def)
    {
        if (root != null)
            root.SetActive(def != null);

        if (cardView != null && def != null)
            cardView.BindDefinition(def);
    }

    void OnChooseA()
    {
        if (currentOptions.Count <= 0)
            return;

        Choose(currentOptions[0], keepA: true);
    }

    void OnChooseB()
    {
        if (currentOptions.Count <= 1)
            return;

        Choose(currentOptions[1], keepA: false);
    }

    void Choose(UnitCardDefinitionSO picked, bool keepA)
    {
        if (rewardChosen || picked == null)
            return;

        var gs = GameSession.I;
        if (gs == null)
            return;

        bool success = gs.RecruitUnit(picked);
        if (!success)
            return;

        rewardChosen = true;

        if (keepA)
        {
            if (optionBRoot != null)
                optionBRoot.SetActive(false);
        }
        else
        {
            if (optionARoot != null)
                optionARoot.SetActive(false);
        }

        if (chooseAButton != null)
            chooseAButton.gameObject.SetActive(false);

        if (chooseBButton != null)
            chooseBButton.gameObject.SetActive(false);

        if (leaveButtonText != null)
            leaveButtonText.text = "Leave";
    }

    void OnLeave()
    {
        CloseImmediate();
    }

    void CloseImmediate()
    {
        rewardChosen = false;
        currentOptions.Clear();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }
}