using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Singleton manager for unit selection state.
    /// Handles single and multi-selection, events, and team filtering.
    /// </summary>
    /// <remarks>
    /// Works in both AR and debug scenes. Input handlers call into this manager.
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    public class SelectionManager : MonoBehaviour
    {
        #region Singleton

        private static SelectionManager _instance;

        /// <summary>
        /// The singleton instance of SelectionManager.
        /// </summary>
        public static SelectionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SelectionManager>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("[SelectionManager] No instance found in scene.");
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Serialized Fields

        [Header("Configuration")]
        [Tooltip("Maximum number of units that can be selected at once")]
        [SerializeField] private int _maxSelectionSize = 100;

        [Tooltip("Player's team ID for filtering selectable units")]
        [SerializeField] private int _playerTeamId = 0;

        [Tooltip("Only allow selection of player's team units")]
        [SerializeField] private bool _restrictToPlayerTeam = true;

        #endregion

        #region Runtime State

        private readonly List<UnitController> _selectedUnits = new List<UnitController>();

        #endregion

        #region Events

        /// <summary>
        /// Fired when the selection changes. Provides the current selection list.
        /// </summary>
        public event Action<IReadOnlyList<UnitController>> OnSelectionChanged;

        /// <summary>
        /// Fired when a unit is selected.
        /// </summary>
        public event Action<UnitController> OnUnitSelected;

        /// <summary>
        /// Fired when a unit is deselected.
        /// </summary>
        public event Action<UnitController> OnUnitDeselected;

        #endregion

        #region Properties

        /// <summary>
        /// Number of currently selected units.
        /// </summary>
        public int SelectedCount => _selectedUnits.Count;

        /// <summary>
        /// Returns true if any units are selected.
        /// </summary>
        public bool HasSelection => _selectedUnits.Count > 0;

        /// <summary>
        /// Maximum selection size.
        /// </summary>
        public int MaxSelectionSize => _maxSelectionSize;

        /// <summary>
        /// The player's team ID.
        /// </summary>
        public int PlayerTeamId
        {
            get => _playerTeamId;
            set => _playerTeamId = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("[SelectionManager] Duplicate instance found, destroying.");
                Destroy(this);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #endregion

        #region Selection Methods

        /// <summary>
        /// Selects a single unit.
        /// </summary>
        /// <param name="unit">The unit to select.</param>
        /// <param name="addToSelection">If true, adds to existing selection. If false, clears selection first.</param>
        public void SelectUnit(UnitController unit, bool addToSelection = false)
        {
            if (unit == null) return;
            if (!CanSelectUnit(unit)) return;
            if (_selectedUnits.Contains(unit)) return; // Already selected

            if (!addToSelection)
            {
                ClearSelectionInternal(fireEvent: false);
            }

            if (_selectedUnits.Count >= _maxSelectionSize)
            {
                Debug.LogWarning($"[SelectionManager] Selection limit ({_maxSelectionSize}) reached.");
                return;
            }

            _selectedUnits.Add(unit);
            unit.SetSelected(true);
            unit.OnDeath += () => OnUnitDied(unit);

            OnUnitSelected?.Invoke(unit);
            OnSelectionChanged?.Invoke(_selectedUnits.AsReadOnly());
        }

        /// <summary>
        /// Selects multiple units.
        /// </summary>
        /// <param name="units">The units to select.</param>
        /// <param name="addToSelection">If true, adds to existing selection. If false, clears selection first.</param>
        public void SelectUnits(IEnumerable<UnitController> units, bool addToSelection = false)
        {
            if (units == null) return;

            if (!addToSelection)
            {
                ClearSelectionInternal(fireEvent: false);
            }

            foreach (var unit in units)
            {
                if (unit == null) continue;
                if (!CanSelectUnit(unit)) continue;
                if (_selectedUnits.Contains(unit)) continue;
                if (_selectedUnits.Count >= _maxSelectionSize) break;

                _selectedUnits.Add(unit);
                unit.SetSelected(true);
                unit.OnDeath += () => OnUnitDied(unit);
                OnUnitSelected?.Invoke(unit);
            }

            OnSelectionChanged?.Invoke(_selectedUnits.AsReadOnly());
        }

        /// <summary>
        /// Deselects a specific unit.
        /// </summary>
        /// <param name="unit">The unit to deselect.</param>
        public void DeselectUnit(UnitController unit)
        {
            if (unit == null) return;
            if (!_selectedUnits.Contains(unit)) return;

            _selectedUnits.Remove(unit);
            unit.SetSelected(false);

            OnUnitDeselected?.Invoke(unit);
            OnSelectionChanged?.Invoke(_selectedUnits.AsReadOnly());
        }

        /// <summary>
        /// Toggles the selection state of a unit.
        /// </summary>
        /// <param name="unit">The unit to toggle.</param>
        public void ToggleSelection(UnitController unit)
        {
            if (unit == null) return;

            if (_selectedUnits.Contains(unit))
            {
                DeselectUnit(unit);
            }
            else
            {
                SelectUnit(unit, addToSelection: true);
            }
        }

        /// <summary>
        /// Clears all selected units.
        /// </summary>
        public void ClearSelection()
        {
            ClearSelectionInternal(fireEvent: true);
        }

        /// <summary>
        /// Checks if a unit is currently selected.
        /// </summary>
        /// <param name="unit">The unit to check.</param>
        /// <returns>True if the unit is selected.</returns>
        public bool IsSelected(UnitController unit)
        {
            return unit != null && _selectedUnits.Contains(unit);
        }

        /// <summary>
        /// Gets a copy of the current selection.
        /// </summary>
        /// <returns>List of selected units.</returns>
        public List<UnitController> GetSelectedUnits()
        {
            CleanupDeadUnits();
            return new List<UnitController>(_selectedUnits);
        }

        /// <summary>
        /// Gets selected units of a specific team.
        /// </summary>
        /// <param name="teamId">The team ID to filter by.</param>
        /// <returns>List of selected units on that team.</returns>
        public List<UnitController> GetSelectedByTeam(int teamId)
        {
            CleanupDeadUnits();
            return _selectedUnits.Where(u => u != null && u.TeamId == teamId).ToList();
        }

        #endregion

        #region Command Helpers

        /// <summary>
        /// Issues a move command to all selected units.
        /// </summary>
        /// <param name="destination">The destination position.</param>
        public void CommandSelectedToMove(Vector3 destination)
        {
            foreach (var unit in _selectedUnits)
            {
                if (unit != null && unit.IsAlive)
                {
                    unit.MoveTo(destination);
                }
            }
        }

        /// <summary>
        /// Issues a stop command to all selected units.
        /// </summary>
        public void CommandSelectedToStop()
        {
            foreach (var unit in _selectedUnits)
            {
                if (unit != null && unit.IsAlive)
                {
                    unit.Stop();
                }
            }
        }

        #endregion

        #region Private Methods

        private bool CanSelectUnit(UnitController unit)
        {
            if (!unit.IsAlive) return false;

            if (_restrictToPlayerTeam && unit.TeamId != _playerTeamId)
            {
                return false;
            }

            return true;
        }

        private void ClearSelectionInternal(bool fireEvent)
        {
            foreach (var unit in _selectedUnits)
            {
                if (unit != null)
                {
                    unit.SetSelected(false);
                    OnUnitDeselected?.Invoke(unit);
                }
            }
            _selectedUnits.Clear();

            if (fireEvent)
            {
                OnSelectionChanged?.Invoke(_selectedUnits.AsReadOnly());
            }
        }

        private void OnUnitDied(UnitController unit)
        {
            if (_selectedUnits.Contains(unit))
            {
                _selectedUnits.Remove(unit);
                OnUnitDeselected?.Invoke(unit);
                OnSelectionChanged?.Invoke(_selectedUnits.AsReadOnly());
            }
        }

        private void CleanupDeadUnits()
        {
            _selectedUnits.RemoveAll(u => u == null || !u.IsAlive);
        }

        #endregion
    }
}
