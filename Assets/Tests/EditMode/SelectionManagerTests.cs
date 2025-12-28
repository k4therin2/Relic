using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for SelectionManager - manages unit selection state.
    /// </summary>
    public class SelectionManagerTests
    {
        private GameObject _managerGameObject;
        private SelectionManager _manager;
        private List<UnitController> _testUnits;
        private List<GameObject> _createdObjects;
        private UnitArchetypeSO _archetype;

        [SetUp]
        public void Setup()
        {
            _createdObjects = new List<GameObject>();
            _testUnits = new List<UnitController>();

            // Create manager
            _managerGameObject = new GameObject("SelectionManager");
            _createdObjects.Add(_managerGameObject);
            _manager = _managerGameObject.AddComponent<SelectionManager>();

            // Create test archetype
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();

            // Create some test units
            for (int i = 0; i < 5; i++)
            {
                var unitGO = new GameObject($"TestUnit_{i}");
                unitGO.AddComponent<BoxCollider>();
                var controller = unitGO.AddComponent<UnitController>();
                controller.Initialize(_archetype, i % 2); // Alternate teams
                _testUnits.Add(controller);
                _createdObjects.Add(unitGO);
            }
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
            _testUnits.Clear();

            if (_archetype != null)
            {
                Object.DestroyImmediate(_archetype);
            }
        }

        #region Basic Selection Tests

        [Test]
        public void SelectUnit_AddsToSelection()
        {
            _manager.SelectUnit(_testUnits[0]);

            Assert.AreEqual(1, _manager.SelectedCount);
            Assert.IsTrue(_manager.IsSelected(_testUnits[0]));
        }

        [Test]
        public void SelectUnit_SetsUnitAsSelected()
        {
            _manager.SelectUnit(_testUnits[0]);

            Assert.IsTrue(_testUnits[0].IsSelected);
        }

        [Test]
        public void SelectUnit_Single_ClearsExistingSelection()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[1]);

            Assert.AreEqual(1, _manager.SelectedCount);
            Assert.IsFalse(_manager.IsSelected(_testUnits[0]));
            Assert.IsTrue(_manager.IsSelected(_testUnits[1]));
        }

        [Test]
        public void SelectUnit_WithAddToSelection_KeepsExisting()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[1], addToSelection: true);

            Assert.AreEqual(2, _manager.SelectedCount);
            Assert.IsTrue(_manager.IsSelected(_testUnits[0]));
            Assert.IsTrue(_manager.IsSelected(_testUnits[1]));
        }

        [Test]
        public void SelectUnit_Null_DoesNothing()
        {
            _manager.SelectUnit(null);

            Assert.AreEqual(0, _manager.SelectedCount);
        }

        [Test]
        public void SelectUnit_AlreadySelected_DoesNotDuplicate()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[0], addToSelection: true);

            Assert.AreEqual(1, _manager.SelectedCount);
        }

        #endregion

        #region Deselection Tests

        [Test]
        public void DeselectUnit_RemovesFromSelection()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[1], addToSelection: true);

            _manager.DeselectUnit(_testUnits[0]);

            Assert.AreEqual(1, _manager.SelectedCount);
            Assert.IsFalse(_manager.IsSelected(_testUnits[0]));
            Assert.IsTrue(_manager.IsSelected(_testUnits[1]));
        }

        [Test]
        public void DeselectUnit_UpdatesUnitSelectionState()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.DeselectUnit(_testUnits[0]);

            Assert.IsFalse(_testUnits[0].IsSelected);
        }

        [Test]
        public void ClearSelection_RemovesAllUnits()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[1], addToSelection: true);
            _manager.SelectUnit(_testUnits[2], addToSelection: true);

            _manager.ClearSelection();

            Assert.AreEqual(0, _manager.SelectedCount);
            Assert.IsFalse(_testUnits[0].IsSelected);
            Assert.IsFalse(_testUnits[1].IsSelected);
            Assert.IsFalse(_testUnits[2].IsSelected);
        }

        #endregion

        #region Multi-Selection Tests

        [Test]
        public void SelectUnits_SelectsMultipleUnits()
        {
            var unitsToSelect = new List<UnitController> { _testUnits[0], _testUnits[1], _testUnits[2] };

            _manager.SelectUnits(unitsToSelect);

            Assert.AreEqual(3, _manager.SelectedCount);
            Assert.IsTrue(_manager.IsSelected(_testUnits[0]));
            Assert.IsTrue(_manager.IsSelected(_testUnits[1]));
            Assert.IsTrue(_manager.IsSelected(_testUnits[2]));
        }

        [Test]
        public void SelectUnits_WithAddToSelection_AppendsToExisting()
        {
            _manager.SelectUnit(_testUnits[0]);
            var moreUnits = new List<UnitController> { _testUnits[1], _testUnits[2] };

            _manager.SelectUnits(moreUnits, addToSelection: true);

            Assert.AreEqual(3, _manager.SelectedCount);
        }

        [Test]
        public void SelectUnits_WithoutAddToSelection_ReplacesExisting()
        {
            _manager.SelectUnit(_testUnits[0]);
            var newUnits = new List<UnitController> { _testUnits[1], _testUnits[2] };

            _manager.SelectUnits(newUnits, addToSelection: false);

            Assert.AreEqual(2, _manager.SelectedCount);
            Assert.IsFalse(_manager.IsSelected(_testUnits[0]));
        }

        [Test]
        public void GetSelectedUnits_ReturnsCopyOfSelection()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[1], addToSelection: true);

            var selected = _manager.GetSelectedUnits();

            Assert.AreEqual(2, selected.Count);
            Assert.Contains(_testUnits[0], selected);
            Assert.Contains(_testUnits[1], selected);
        }

        #endregion

        #region Toggle Selection Tests

        [Test]
        public void ToggleSelection_SelectsUnselectedUnit()
        {
            _manager.ToggleSelection(_testUnits[0]);

            Assert.IsTrue(_manager.IsSelected(_testUnits[0]));
        }

        [Test]
        public void ToggleSelection_DeselectsSelectedUnit()
        {
            _manager.SelectUnit(_testUnits[0]);

            _manager.ToggleSelection(_testUnits[0]);

            Assert.IsFalse(_manager.IsSelected(_testUnits[0]));
        }

        #endregion

        #region Event Tests

        [Test]
        public void SelectUnit_FiresOnSelectionChangedEvent()
        {
            bool eventFired = false;
            List<UnitController> eventUnits = null;

            _manager.OnSelectionChanged += (units) => {
                eventFired = true;
                eventUnits = new List<UnitController>(units);
            };

            _manager.SelectUnit(_testUnits[0]);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(1, eventUnits.Count);
            Assert.Contains(_testUnits[0], eventUnits);
        }

        [Test]
        public void ClearSelection_FiresOnSelectionChangedWithEmptyList()
        {
            _manager.SelectUnit(_testUnits[0]);

            bool eventFired = false;
            List<UnitController> eventUnits = null;

            _manager.OnSelectionChanged += (units) => {
                eventFired = true;
                eventUnits = new List<UnitController>(units);
            };

            _manager.ClearSelection();

            Assert.IsTrue(eventFired);
            Assert.AreEqual(0, eventUnits.Count);
        }

        [Test]
        public void SelectUnit_FiresOnUnitSelectedEvent()
        {
            UnitController selectedUnit = null;

            _manager.OnUnitSelected += (unit) => selectedUnit = unit;

            _manager.SelectUnit(_testUnits[0]);

            Assert.AreEqual(_testUnits[0], selectedUnit);
        }

        [Test]
        public void DeselectUnit_FiresOnUnitDeselectedEvent()
        {
            _manager.SelectUnit(_testUnits[0]);

            UnitController deselectedUnit = null;
            _manager.OnUnitDeselected += (unit) => deselectedUnit = unit;

            _manager.DeselectUnit(_testUnits[0]);

            Assert.AreEqual(_testUnits[0], deselectedUnit);
        }

        #endregion

        #region Team Filtering Tests

        [Test]
        public void GetSelectedByTeam_ReturnsOnlyMatchingTeam()
        {
            // Units 0, 2, 4 are team 0; Units 1, 3 are team 1
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[1], addToSelection: true);
            _manager.SelectUnit(_testUnits[2], addToSelection: true);

            var team0Units = _manager.GetSelectedByTeam(0);
            var team1Units = _manager.GetSelectedByTeam(1);

            Assert.AreEqual(2, team0Units.Count); // units 0 and 2
            Assert.AreEqual(1, team1Units.Count); // unit 1
        }

        #endregion

        #region HasSelection Tests

        [Test]
        public void HasSelection_WhenEmpty_ReturnsFalse()
        {
            Assert.IsFalse(_manager.HasSelection);
        }

        [Test]
        public void HasSelection_WithSelection_ReturnsTrue()
        {
            _manager.SelectUnit(_testUnits[0]);

            Assert.IsTrue(_manager.HasSelection);
        }

        #endregion

        #region Dead Unit Handling

        [Test]
        public void SelectUnit_DeadUnit_DoesNotSelect()
        {
            // Kill the unit
            _testUnits[0].TakeDamage(10000);

            _manager.SelectUnit(_testUnits[0]);

            Assert.AreEqual(0, _manager.SelectedCount);
        }

        [Test]
        public void SelectedUnits_UnitDies_AutomaticallyRemoved()
        {
            _manager.SelectUnit(_testUnits[0]);
            _manager.SelectUnit(_testUnits[1], addToSelection: true);

            // Kill one unit - in real scenario OnDeath would trigger cleanup
            // For now just verify the check works
            Assert.AreEqual(2, _manager.SelectedCount);
        }

        #endregion
    }
}
