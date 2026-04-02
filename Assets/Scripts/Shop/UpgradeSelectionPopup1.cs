using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using Chess;

public class UpgradeSelectionPopup : MonoBehaviour
{
    private static UpgradeSelectionPopup instance;

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform pieceButtonContainer;
    [SerializeField] private Button pieceButtonPrefab;
    [SerializeField] private Button cancelButton;

    private Action<PieceDefinition> onPieceSelected;
    private PieceUpgradeSO currentUpgrade;
    private int upgradeCost;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancel);
        }
    }

    public static void ShowPopup(List<PieceDefinition> pieces, PieceUpgradeSO upgrade, int cost, Action<PieceDefinition> callback)
    {
        if (instance == null)
        {
            Debug.LogError("[UpgradeSelectionPopup] No instance found! Add this component to the Shop scene.");
            callback?.Invoke(null);
            return;
        }

        instance.DisplayPopup(pieces, upgrade, cost, callback);
    }

    void DisplayPopup(List<PieceDefinition> pieces, PieceUpgradeSO upgrade, int cost, Action<PieceDefinition> callback)
    {
        currentUpgrade = upgrade;
        upgradeCost = cost;
        onPieceSelected = callback;

        if (titleText != null)
        {
            titleText.text = $"Select piece to upgrade with: {upgrade.displayName}";
        }

        ClearPieceButtons();

        foreach (var piece in pieces)
        {
            if (piece == null) continue;

            Button btn = Instantiate(pieceButtonPrefab, pieceButtonContainer);
            
            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = $"{piece.displayName}\nHP: {piece.maxHP} ATK: {piece.attack}";
            }

            var icon = btn.GetComponentsInChildren<Image>();
            if (icon.Length > 1 && piece.icon != null)
            {
                icon[1].sprite = piece.icon;
            }

            PieceDefinition capturedPiece = piece;
            btn.onClick.AddListener(() => OnPieceButtonClicked(capturedPiece));
        }

        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
        }
    }

    void OnPieceButtonClicked(PieceDefinition piece)
    {
        onPieceSelected?.Invoke(piece);
        ClosePopup();
    }

    void OnCancel()
    {
        onPieceSelected?.Invoke(null);
        ClosePopup();
    }

    void ClosePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
        
        ClearPieceButtons();
        onPieceSelected = null;
        currentUpgrade = null;
    }

    void ClearPieceButtons()
    {
        if (pieceButtonContainer == null) return;

        foreach (Transform child in pieceButtonContainer)
        {
            Destroy(child.gameObject);
        }
    }
}
