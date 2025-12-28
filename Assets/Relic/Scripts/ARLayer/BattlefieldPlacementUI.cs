using UnityEngine;
using UnityEngine.UI;

namespace Relic.ARLayer
{
    /// <summary>
    /// UI controller for the battlefield placement workflow.
    /// Shows instructions, confirm/cancel buttons based on placement state.
    /// </summary>
    public class BattlefieldPlacementUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text instructionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private GameObject placementPanel;

        [Header("Messages")]
        [SerializeField] private string detectingMessage = "Point at a flat surface to place battlefield";
        [SerializeField] private string previewingMessage = "Tap to place battlefield here";
        [SerializeField] private string confirmingMessage = "Confirm or cancel placement";
        [SerializeField] private string placedMessage = "Battlefield placed!";

        // Components
        private BattlefieldPlacer placer;

        private void Awake()
        {
            placer = FindFirstObjectByType<BattlefieldPlacer>();
            SetupButtonListeners();
        }

        private void OnEnable()
        {
            if (placer != null)
            {
                placer.OnStateChanged += HandleStateChanged;
                UpdateUI(placer.CurrentState);
            }
        }

        private void OnDisable()
        {
            if (placer != null)
            {
                placer.OnStateChanged -= HandleStateChanged;
            }
        }

        /// <summary>
        /// Initialize the UI with a placer reference.
        /// </summary>
        public void Initialize(BattlefieldPlacer battlefieldPlacer)
        {
            if (placer != null)
            {
                placer.OnStateChanged -= HandleStateChanged;
            }

            placer = battlefieldPlacer;

            if (placer != null)
            {
                placer.OnStateChanged += HandleStateChanged;
                UpdateUI(placer.CurrentState);
            }
        }

        private void SetupButtonListeners()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(OnCancelClicked);
            }

            if (resetButton != null)
            {
                resetButton.onClick.AddListener(OnResetClicked);
            }
        }

        private void HandleStateChanged(PlacementState newState)
        {
            UpdateUI(newState);
        }

        private void UpdateUI(PlacementState state)
        {
            // Update instruction text
            if (instructionText != null)
            {
                instructionText.text = GetMessageForState(state);
            }

            // Update button visibility
            switch (state)
            {
                case PlacementState.Detecting:
                    SetButtonsState(confirmVisible: false, cancelVisible: false, resetVisible: false);
                    break;

                case PlacementState.Previewing:
                    SetButtonsState(confirmVisible: true, cancelVisible: false, resetVisible: false);
                    if (confirmButton != null)
                    {
                        var buttonText = confirmButton.GetComponentInChildren<Text>();
                        if (buttonText != null) buttonText.text = "Place Here";
                    }
                    break;

                case PlacementState.Confirming:
                    SetButtonsState(confirmVisible: true, cancelVisible: true, resetVisible: false);
                    if (confirmButton != null)
                    {
                        var buttonText = confirmButton.GetComponentInChildren<Text>();
                        if (buttonText != null) buttonText.text = "Confirm";
                    }
                    break;

                case PlacementState.Placed:
                    SetButtonsState(confirmVisible: false, cancelVisible: false, resetVisible: true);
                    break;
            }
        }

        private string GetMessageForState(PlacementState state)
        {
            return state switch
            {
                PlacementState.Detecting => detectingMessage,
                PlacementState.Previewing => previewingMessage,
                PlacementState.Confirming => confirmingMessage,
                PlacementState.Placed => placedMessage,
                _ => detectingMessage
            };
        }

        private void SetButtonsState(bool confirmVisible, bool cancelVisible, bool resetVisible)
        {
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(confirmVisible);

            if (cancelButton != null)
                cancelButton.gameObject.SetActive(cancelVisible);

            if (resetButton != null)
                resetButton.gameObject.SetActive(resetVisible);
        }

        private void OnConfirmClicked()
        {
            if (placer == null) return;

            if (placer.CurrentState == PlacementState.Previewing)
            {
                placer.TryPlaceBattlefield();
            }
            else if (placer.CurrentState == PlacementState.Confirming)
            {
                placer.ConfirmPlacement();
            }
        }

        private void OnCancelClicked()
        {
            if (placer != null)
            {
                placer.CancelPlacement();
            }
        }

        private void OnResetClicked()
        {
            if (placer != null)
            {
                placer.Reset();
            }
        }
    }
}
