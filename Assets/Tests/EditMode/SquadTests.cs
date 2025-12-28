using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for Squad class.
    /// Tests validate squad membership management and upgrade stacking.
    /// </summary>
    public class SquadTests
    {
        private Squad _squad;
        private GameObject _unitGO1;
        private GameObject _unitGO2;
        private UnitController _unit1;
        private UnitController _unit2;
        private UnitArchetypeSO _archetype;
        private UpgradeSO _upgrade1;
        private UpgradeSO _upgrade2;

        [SetUp]
        public void Setup()
        {
            // Create squad
            _squad = new Squad("test_squad", 0);

            // Create test archetype
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();

            // Create test units
            _unitGO1 = new GameObject("Unit1");
            _unitGO1.AddComponent<BoxCollider>();
            _unit1 = _unitGO1.AddComponent<UnitController>();

            _unitGO2 = new GameObject("Unit2");
            _unitGO2.AddComponent<BoxCollider>();
            _unit2 = _unitGO2.AddComponent<UnitController>();

            // Create test upgrades
            _upgrade1 = ScriptableObject.CreateInstance<UpgradeSO>();
            _upgrade2 = ScriptableObject.CreateInstance<UpgradeSO>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_unitGO1 != null) Object.DestroyImmediate(_unitGO1);
            if (_unitGO2 != null) Object.DestroyImmediate(_unitGO2);
            if (_archetype != null) Object.DestroyImmediate(_archetype);
            if (_upgrade1 != null) Object.DestroyImmediate(_upgrade1);
            if (_upgrade2 != null) Object.DestroyImmediate(_upgrade2);
        }

        #region Creation Tests

        [Test]
        public void CreateSquad_WithIdAndTeam_IsValid()
        {
            Assert.IsNotNull(_squad);
            Assert.AreEqual("test_squad", _squad.Id);
            Assert.AreEqual(0, _squad.TeamId);
        }

        [Test]
        public void NewSquad_HasNoMembers()
        {
            Assert.AreEqual(0, _squad.MemberCount);
            Assert.IsEmpty(_squad.Members);
        }

        [Test]
        public void NewSquad_HasNoUpgrades()
        {
            Assert.AreEqual(0, _squad.UpgradeCount);
            Assert.IsEmpty(_squad.AppliedUpgrades);
        }

        [Test]
        public void NewSquad_HasNeutralMultipliers()
        {
            Assert.AreEqual(1f, _squad.HitChanceMultiplier, 0.001f);
            Assert.AreEqual(1f, _squad.DamageMultiplier, 0.001f);
            Assert.AreEqual(0f, _squad.ElevationBonusFlat, 0.001f);
        }

        #endregion

        #region Member Management Tests

        [Test]
        public void AddMember_WithValidUnit_IncreasesCount()
        {
            bool added = _squad.AddMember(_unit1);

            Assert.IsTrue(added, "Should successfully add unit");
            Assert.AreEqual(1, _squad.MemberCount);
            Assert.Contains(_unit1, (System.Collections.ICollection)_squad.Members);
        }

        [Test]
        public void AddMember_SameUnitTwice_ReturnsFalse()
        {
            _squad.AddMember(_unit1);
            bool addedAgain = _squad.AddMember(_unit1);

            Assert.IsFalse(addedAgain, "Should not add same unit twice");
            Assert.AreEqual(1, _squad.MemberCount);
        }

        [Test]
        public void AddMember_Null_ReturnsFalse()
        {
            bool added = _squad.AddMember(null);

            Assert.IsFalse(added, "Should not add null unit");
            Assert.AreEqual(0, _squad.MemberCount);
        }

        [Test]
        public void RemoveMember_ExistingUnit_DecreasesCount()
        {
            _squad.AddMember(_unit1);
            bool removed = _squad.RemoveMember(_unit1);

            Assert.IsTrue(removed, "Should successfully remove unit");
            Assert.AreEqual(0, _squad.MemberCount);
        }

        [Test]
        public void RemoveMember_NonExistingUnit_ReturnsFalse()
        {
            bool removed = _squad.RemoveMember(_unit1);

            Assert.IsFalse(removed, "Should not remove non-existing unit");
        }

        [Test]
        public void Contains_WithMember_ReturnsTrue()
        {
            _squad.AddMember(_unit1);

            Assert.IsTrue(_squad.Contains(_unit1));
        }

        [Test]
        public void Contains_WithoutMember_ReturnsFalse()
        {
            Assert.IsFalse(_squad.Contains(_unit1));
        }

        [Test]
        public void ClearMembers_RemovesAllUnits()
        {
            _squad.AddMember(_unit1);
            _squad.AddMember(_unit2);

            _squad.ClearMembers();

            Assert.AreEqual(0, _squad.MemberCount);
        }

        #endregion

        #region Upgrade Management Tests

        [Test]
        public void ApplyUpgrade_WithValidUpgrade_IncreasesCount()
        {
            bool applied = _squad.ApplyUpgrade(_upgrade1);

            Assert.IsTrue(applied, "Should successfully apply upgrade");
            Assert.AreEqual(1, _squad.UpgradeCount);
            Assert.Contains(_upgrade1, (System.Collections.ICollection)_squad.AppliedUpgrades);
        }

        [Test]
        public void ApplyUpgrade_SameUpgradeTwice_RespectMaxStacks()
        {
            // Default max stacks is 1, so second application should fail
            _squad.ApplyUpgrade(_upgrade1);
            bool appliedAgain = _squad.ApplyUpgrade(_upgrade1);

            Assert.IsFalse(appliedAgain, "Should not exceed max stacks");
            Assert.AreEqual(1, _squad.UpgradeCount);
        }

        [Test]
        public void ApplyUpgrade_Null_ReturnsFalse()
        {
            bool applied = _squad.ApplyUpgrade(null);

            Assert.IsFalse(applied, "Should not apply null upgrade");
            Assert.AreEqual(0, _squad.UpgradeCount);
        }

        [Test]
        public void RemoveUpgrade_ExistingUpgrade_DecreasesCount()
        {
            _squad.ApplyUpgrade(_upgrade1);
            bool removed = _squad.RemoveUpgrade(_upgrade1);

            Assert.IsTrue(removed, "Should successfully remove upgrade");
            Assert.AreEqual(0, _squad.UpgradeCount);
        }

        [Test]
        public void RemoveUpgrade_NonExistingUpgrade_ReturnsFalse()
        {
            bool removed = _squad.RemoveUpgrade(_upgrade1);

            Assert.IsFalse(removed, "Should not remove non-existing upgrade");
        }

        [Test]
        public void HasUpgrade_WithAppliedUpgrade_ReturnsTrue()
        {
            _squad.ApplyUpgrade(_upgrade1);

            Assert.IsTrue(_squad.HasUpgrade(_upgrade1));
        }

        [Test]
        public void HasUpgrade_WithoutUpgrade_ReturnsFalse()
        {
            Assert.IsFalse(_squad.HasUpgrade(_upgrade1));
        }

        [Test]
        public void ClearUpgrades_RemovesAllUpgrades()
        {
            _squad.ApplyUpgrade(_upgrade1);
            _squad.ApplyUpgrade(_upgrade2);

            _squad.ClearUpgrades();

            Assert.AreEqual(0, _squad.UpgradeCount);
        }

        #endregion

        #region Multiplier Calculation Tests

        [Test]
        public void Multipliers_WithNoUpgrades_AreNeutral()
        {
            Assert.AreEqual(1f, _squad.HitChanceMultiplier, 0.001f);
            Assert.AreEqual(1f, _squad.DamageMultiplier, 0.001f);
            Assert.AreEqual(0f, _squad.ElevationBonusFlat, 0.001f);
        }

        [Test]
        public void GetUpgradeStackCount_WithNoStacks_ReturnsZero()
        {
            int count = _squad.GetUpgradeStackCount(_upgrade1);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void GetUpgradeStackCount_WithOneStack_ReturnsOne()
        {
            _squad.ApplyUpgrade(_upgrade1);
            int count = _squad.GetUpgradeStackCount(_upgrade1);
            Assert.AreEqual(1, count);
        }

        #endregion

        #region Event Tests

        [Test]
        public void OnMemberAdded_IsInvokedWhenMemberAdded()
        {
            UnitController addedUnit = null;
            _squad.OnMemberAdded += (unit) => addedUnit = unit;

            _squad.AddMember(_unit1);

            Assert.AreEqual(_unit1, addedUnit);
        }

        [Test]
        public void OnMemberRemoved_IsInvokedWhenMemberRemoved()
        {
            UnitController removedUnit = null;
            _squad.OnMemberRemoved += (unit) => removedUnit = unit;

            _squad.AddMember(_unit1);
            _squad.RemoveMember(_unit1);

            Assert.AreEqual(_unit1, removedUnit);
        }

        [Test]
        public void OnUpgradeApplied_IsInvokedWhenUpgradeApplied()
        {
            UpgradeSO appliedUpgrade = null;
            _squad.OnUpgradeApplied += (upgrade) => appliedUpgrade = upgrade;

            _squad.ApplyUpgrade(_upgrade1);

            Assert.AreEqual(_upgrade1, appliedUpgrade);
        }

        [Test]
        public void OnUpgradeRemoved_IsInvokedWhenUpgradeRemoved()
        {
            UpgradeSO removedUpgrade = null;
            _squad.OnUpgradeRemoved += (upgrade) => removedUpgrade = upgrade;

            _squad.ApplyUpgrade(_upgrade1);
            _squad.RemoveUpgrade(_upgrade1);

            Assert.AreEqual(_upgrade1, removedUpgrade);
        }

        #endregion
    }
}
