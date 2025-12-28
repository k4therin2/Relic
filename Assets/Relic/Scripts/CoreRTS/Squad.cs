using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Represents a group of units that share upgrades and move together.
    /// Manages member units and calculates aggregate stat modifiers from upgrades.
    /// </summary>
    /// <remarks>
    /// Squads are the primary organizational unit for applying upgrades.
    /// Upgrade effects are:
    /// - Hit chance: stacks multiplicatively
    /// - Damage: stacks multiplicatively
    /// - Elevation bonus: stacks additively
    /// See Kyle's milestones.md Milestone 3 for requirements.
    /// </remarks>
    public class Squad
    {
        #region Constants

        private const int MAX_SQUAD_SIZE = 20;
        private const int MAX_UPGRADES = 10;

        #endregion

        #region Private Fields

        private readonly string _id;
        private readonly int _teamId;
        private readonly List<UnitController> _members = new();
        private readonly List<UpgradeSO> _appliedUpgrades = new();
        private readonly Dictionary<string, int> _upgradeStacks = new();

        // Cached multipliers (recalculated when upgrades change)
        private float _cachedHitChanceMultiplier = 1f;
        private float _cachedDamageMultiplier = 1f;
        private float _cachedElevationBonusFlat = 0f;
        private bool _multipliersCacheValid = false;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a member is added to the squad.
        /// </summary>
        public event Action<UnitController> OnMemberAdded;

        /// <summary>
        /// Fired when a member is removed from the squad.
        /// </summary>
        public event Action<UnitController> OnMemberRemoved;

        /// <summary>
        /// Fired when an upgrade is applied to the squad.
        /// </summary>
        public event Action<UpgradeSO> OnUpgradeApplied;

        /// <summary>
        /// Fired when an upgrade is removed from the squad.
        /// </summary>
        public event Action<UpgradeSO> OnUpgradeRemoved;

        /// <summary>
        /// Fired when the squad's multipliers are recalculated.
        /// </summary>
        public event Action OnMultipliersChanged;

        #endregion

        #region Properties

        /// <summary>Unique identifier for this squad.</summary>
        public string Id => _id;

        /// <summary>Team ID this squad belongs to.</summary>
        public int TeamId => _teamId;

        /// <summary>Read-only list of squad members.</summary>
        public IReadOnlyList<UnitController> Members => _members;

        /// <summary>Number of units in the squad.</summary>
        public int MemberCount => _members.Count;

        /// <summary>Whether the squad is at maximum capacity.</summary>
        public bool IsFull => _members.Count >= MAX_SQUAD_SIZE;

        /// <summary>Read-only list of applied upgrades.</summary>
        public IReadOnlyList<UpgradeSO> AppliedUpgrades => _appliedUpgrades;

        /// <summary>Number of upgrades applied.</summary>
        public int UpgradeCount => _appliedUpgrades.Count;

        /// <summary>Combined hit chance multiplier from all upgrades (multiplicative).</summary>
        public float HitChanceMultiplier
        {
            get
            {
                RecalculateMultipliersIfNeeded();
                return _cachedHitChanceMultiplier;
            }
        }

        /// <summary>Combined damage multiplier from all upgrades (multiplicative).</summary>
        public float DamageMultiplier
        {
            get
            {
                RecalculateMultipliersIfNeeded();
                return _cachedDamageMultiplier;
            }
        }

        /// <summary>Combined elevation bonus from all upgrades (additive).</summary>
        public float ElevationBonusFlat
        {
            get
            {
                RecalculateMultipliersIfNeeded();
                return _cachedElevationBonusFlat;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new squad.
        /// </summary>
        /// <param name="id">Unique identifier for this squad.</param>
        /// <param name="teamId">Team this squad belongs to.</param>
        public Squad(string id, int teamId)
        {
            _id = id ?? throw new ArgumentNullException(nameof(id));
            _teamId = teamId;
        }

        #endregion

        #region Member Management

        /// <summary>
        /// Adds a unit to the squad.
        /// </summary>
        /// <param name="unit">The unit to add.</param>
        /// <returns>True if the unit was added successfully.</returns>
        public bool AddMember(UnitController unit)
        {
            if (unit == null)
                return false;

            if (IsFull)
            {
                Debug.LogWarning($"[Squad] Cannot add unit to squad {_id}: squad is full");
                return false;
            }

            if (_members.Contains(unit))
            {
                Debug.LogWarning($"[Squad] Unit {unit.name} is already in squad {_id}");
                return false;
            }

            _members.Add(unit);
            OnMemberAdded?.Invoke(unit);
            return true;
        }

        /// <summary>
        /// Removes a unit from the squad.
        /// </summary>
        /// <param name="unit">The unit to remove.</param>
        /// <returns>True if the unit was removed successfully.</returns>
        public bool RemoveMember(UnitController unit)
        {
            if (unit == null)
                return false;

            if (!_members.Remove(unit))
                return false;

            OnMemberRemoved?.Invoke(unit);
            return true;
        }

        /// <summary>
        /// Checks if the squad contains a unit.
        /// </summary>
        /// <param name="unit">The unit to check.</param>
        /// <returns>True if the unit is in this squad.</returns>
        public bool Contains(UnitController unit)
        {
            return unit != null && _members.Contains(unit);
        }

        /// <summary>
        /// Removes all members from the squad.
        /// </summary>
        public void ClearMembers()
        {
            var membersToRemove = _members.ToList();
            foreach (var member in membersToRemove)
            {
                RemoveMember(member);
            }
        }

        #endregion

        #region Upgrade Management

        /// <summary>
        /// Applies an upgrade to the squad.
        /// </summary>
        /// <param name="upgrade">The upgrade to apply.</param>
        /// <returns>True if the upgrade was applied successfully.</returns>
        public bool ApplyUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null)
                return false;

            if (_appliedUpgrades.Count >= MAX_UPGRADES)
            {
                Debug.LogWarning($"[Squad] Cannot apply upgrade to squad {_id}: max upgrades reached");
                return false;
            }

            // Check if we've exceeded max stacks for this upgrade
            string upgradeId = upgrade.Id ?? upgrade.name;
            int currentStacks = GetUpgradeStackCount(upgrade);

            if (currentStacks >= upgrade.MaxStacks)
            {
                Debug.LogWarning($"[Squad] Cannot apply upgrade {upgradeId}: max stacks ({upgrade.MaxStacks}) reached");
                return false;
            }

            // Check for mutually exclusive upgrades
            foreach (var existingUpgrade in _appliedUpgrades)
            {
                if (!existingUpgrade.CanStackWith(upgrade))
                {
                    Debug.LogWarning($"[Squad] Cannot apply upgrade {upgradeId}: conflicts with {existingUpgrade.Id}");
                    return false;
                }
            }

            _appliedUpgrades.Add(upgrade);

            // Update stack count
            if (!_upgradeStacks.ContainsKey(upgradeId))
                _upgradeStacks[upgradeId] = 0;
            _upgradeStacks[upgradeId]++;

            InvalidateMultipliersCache();
            OnUpgradeApplied?.Invoke(upgrade);
            return true;
        }

        /// <summary>
        /// Removes an upgrade from the squad.
        /// </summary>
        /// <param name="upgrade">The upgrade to remove.</param>
        /// <returns>True if the upgrade was removed successfully.</returns>
        public bool RemoveUpgrade(UpgradeSO upgrade)
        {
            if (upgrade == null)
                return false;

            if (!_appliedUpgrades.Remove(upgrade))
                return false;

            // Update stack count
            string upgradeId = upgrade.Id ?? upgrade.name;
            if (_upgradeStacks.ContainsKey(upgradeId))
            {
                _upgradeStacks[upgradeId]--;
                if (_upgradeStacks[upgradeId] <= 0)
                    _upgradeStacks.Remove(upgradeId);
            }

            InvalidateMultipliersCache();
            OnUpgradeRemoved?.Invoke(upgrade);
            return true;
        }

        /// <summary>
        /// Checks if the squad has a specific upgrade applied.
        /// </summary>
        /// <param name="upgrade">The upgrade to check.</param>
        /// <returns>True if the upgrade is applied to this squad.</returns>
        public bool HasUpgrade(UpgradeSO upgrade)
        {
            return upgrade != null && _appliedUpgrades.Contains(upgrade);
        }

        /// <summary>
        /// Gets the number of times a specific upgrade is stacked.
        /// </summary>
        /// <param name="upgrade">The upgrade to check.</param>
        /// <returns>The number of stacks of this upgrade.</returns>
        public int GetUpgradeStackCount(UpgradeSO upgrade)
        {
            if (upgrade == null)
                return 0;

            string upgradeId = upgrade.Id ?? upgrade.name;
            return _upgradeStacks.TryGetValue(upgradeId, out int count) ? count : 0;
        }

        /// <summary>
        /// Removes all upgrades from the squad.
        /// </summary>
        public void ClearUpgrades()
        {
            var upgradesToRemove = _appliedUpgrades.ToList();
            foreach (var upgrade in upgradesToRemove)
            {
                RemoveUpgrade(upgrade);
            }
        }

        #endregion

        #region Multiplier Calculation

        /// <summary>
        /// Invalidates the cached multipliers, forcing recalculation on next access.
        /// </summary>
        private void InvalidateMultipliersCache()
        {
            _multipliersCacheValid = false;
        }

        /// <summary>
        /// Recalculates multipliers if the cache is invalid.
        /// </summary>
        private void RecalculateMultipliersIfNeeded()
        {
            if (_multipliersCacheValid)
                return;

            RecalculateMultipliers();
        }

        /// <summary>
        /// Recalculates all multipliers from applied upgrades.
        /// Hit chance and damage stack multiplicatively.
        /// Elevation bonus stacks additively.
        /// </summary>
        private void RecalculateMultipliers()
        {
            _cachedHitChanceMultiplier = 1f;
            _cachedDamageMultiplier = 1f;
            _cachedElevationBonusFlat = 0f;

            foreach (var upgrade in _appliedUpgrades)
            {
                if (upgrade == null)
                    continue;

                // Multiplicative stacking for hit chance and damage
                _cachedHitChanceMultiplier *= upgrade.HitChanceMultiplier;
                _cachedDamageMultiplier *= upgrade.DamageMultiplier;

                // Additive stacking for elevation bonus
                _cachedElevationBonusFlat += upgrade.ElevationBonus;
            }

            _multipliersCacheValid = true;
            OnMultipliersChanged?.Invoke();
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Gets the center position of all squad members.
        /// </summary>
        /// <returns>The average position of all units, or Vector3.zero if empty.</returns>
        public Vector3 GetCenterPosition()
        {
            if (_members.Count == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int validCount = 0;

            foreach (var member in _members)
            {
                if (member != null)
                {
                    sum += member.transform.position;
                    validCount++;
                }
            }

            return validCount > 0 ? sum / validCount : Vector3.zero;
        }

        /// <summary>
        /// Gets all alive members of the squad.
        /// </summary>
        /// <returns>List of alive unit controllers.</returns>
        public List<UnitController> GetAliveMembers()
        {
            return _members.Where(m => m != null && m.IsAlive).ToList();
        }

        /// <summary>
        /// Gets the total health of all squad members.
        /// </summary>
        /// <returns>Sum of current health across all members.</returns>
        public int GetTotalHealth()
        {
            return _members.Where(m => m != null).Sum(m => m.Stats.CurrentHealth);
        }

        /// <summary>
        /// Gets the total max health of all squad members.
        /// </summary>
        /// <returns>Sum of max health across all members.</returns>
        public int GetTotalMaxHealth()
        {
            return _members.Where(m => m != null).Sum(m => m.Stats.MaxHealth);
        }

        /// <summary>
        /// Gets the health percentage of the squad.
        /// </summary>
        /// <returns>Average health percentage across all members.</returns>
        public float GetHealthPercent()
        {
            int totalMax = GetTotalMaxHealth();
            return totalMax > 0 ? (float)GetTotalHealth() / totalMax : 0f;
        }

        #endregion
    }
}
