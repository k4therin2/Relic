using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for ARBoxSelection - drag/area multi-unit selection in AR.
    /// TDD tests written first per WP-EXT-5.1.
    /// </summary>
    public class ARBoxSelectionTests
    {
        private GameObject _selectorGameObject;
        private ARBoxSelection _boxSelection;
        private SelectionManager _selectionManager;
        private List<UnitController> _testUnits;
        private List<GameObject> _createdObjects;
        private UnitArchetypeSO _archetype;

        [SetUp]
        public void Setup()
        {
            _createdObjects = new List<GameObject>();
            _testUnits = new List<UnitController>();

            // Create selection manager
            var managerGO = new GameObject("SelectionManager");
            _createdObjects.Add(managerGO);
            _selectionManager = managerGO.AddComponent<SelectionManager>();

            // Create box selection
            _selectorGameObject = new GameObject("BoxSelector");
            _createdObjects.Add(_selectorGameObject);
            _boxSelection = _selectorGameObject.AddComponent<ARBoxSelection>();

            // Create test archetype
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();

            // Create test units in a grid pattern
            for (int i = 0; i < 9; i++)
            {
                int x = i % 3;
                int z = i / 3;
                var unitGO = CreateTestUnit(0, new Vector3(x * 2f, 0, z * 2f));
                _testUnits.Add(unitGO.GetComponent<UnitController>());
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

        #region Basic Tests

        [Test]
        public void ARBoxSelection_CanBeCreated()
        {
            Assert.IsNotNull(_boxSelection);
        }

        [Test]
        public void ARBoxSelection_InitiallyNotSelecting()
        {
            Assert.IsFalse(_boxSelection.IsSelecting);
        }

        #endregion

        #region Selection Start/End Tests

        [Test]
        public void StartSelection_SetsIsSelectingTrue()
        {
            var startPoint = new Vector3(0, 0, 0);

            _boxSelection.StartSelection(startPoint);

            Assert.IsTrue(_boxSelection.IsSelecting);
        }

        [Test]
        public void EndSelection_SetsIsSelectingFalse()
        {
            _boxSelection.StartSelection(Vector3.zero);

            _boxSelection.EndSelection();

            Assert.IsFalse(_boxSelection.IsSelecting);
        }

        [Test]
        public void StartSelection_StoresStartPoint()
        {
            var startPoint = new Vector3(1f, 0, 2f);

            _boxSelection.StartSelection(startPoint);

            Assert.AreEqual(startPoint, _boxSelection.StartPoint);
        }

        [Test]
        public void UpdateSelection_UpdatesCurrentPoint()
        {
            _boxSelection.StartSelection(Vector3.zero);
            var currentPoint = new Vector3(5f, 0, 5f);

            _boxSelection.UpdateSelection(currentPoint);

            Assert.AreEqual(currentPoint, _boxSelection.CurrentPoint);
        }

        #endregion

        #region Box Calculation Tests

        [Test]
        public void GetSelectionBounds_ReturnsCorrectBounds()
        {
            _boxSelection.StartSelection(new Vector3(0, 0, 0));
            _boxSelection.UpdateSelection(new Vector3(4, 0, 4));

            Bounds bounds = _boxSelection.GetSelectionBounds();

            Assert.AreEqual(new Vector3(2, 0, 2), bounds.center);
            Assert.AreEqual(new Vector3(4, 1, 4), bounds.size); // Height is fixed
        }

        [Test]
        public void GetSelectionBounds_WorksWithNegativeDirection()
        {
            // Drag from top-right to bottom-left
            _boxSelection.StartSelection(new Vector3(4, 0, 4));
            _boxSelection.UpdateSelection(new Vector3(0, 0, 0));

            Bounds bounds = _boxSelection.GetSelectionBounds();

            Assert.AreEqual(new Vector3(2, 0, 2), bounds.center);
        }

        #endregion

        #region Unit Detection Tests

        [Test]
        public void GetUnitsInSelection_ReturnsUnitsInBounds()
        {
            // Select box covering first 4 units (0,0 to 2,2)
            _boxSelection.StartSelection(new Vector3(-1, 0, -1));
            _boxSelection.UpdateSelection(new Vector3(3, 0, 3));

            var units = _boxSelection.GetUnitsInSelection();

            // Should contain units at (0,0), (2,0), (0,2), (2,2)
            Assert.AreEqual(4, units.Count);
        }

        [Test]
        public void GetUnitsInSelection_ReturnsEmptyWhenNoUnitsInBounds()
        {
            // Select box in empty area
            _boxSelection.StartSelection(new Vector3(100, 0, 100));
            _boxSelection.UpdateSelection(new Vector3(110, 0, 110));

            var units = _boxSelection.GetUnitsInSelection();

            Assert.AreEqual(0, units.Count);
        }

        [Test]
        public void GetUnitsInSelection_RespectsTeamFilter()
        {
            // Create an enemy unit in the selection area
            var enemyGO = CreateTestUnit(1, new Vector3(1, 0, 1));

            _boxSelection.SetTeamFilter(0); // Only select team 0
            _boxSelection.StartSelection(new Vector3(-1, 0, -1));
            _boxSelection.UpdateSelection(new Vector3(3, 0, 3));

            var units = _boxSelection.GetUnitsInSelection();

            // Should not include the team 1 unit
            foreach (var unit in units)
            {
                Assert.AreEqual(0, unit.TeamId);
            }
        }

        #endregion

        #region Selection Application Tests

        [Test]
        public void ApplySelection_SelectsUnitsInBounds()
        {
            _boxSelection.StartSelection(new Vector3(-1, 0, -1));
            _boxSelection.UpdateSelection(new Vector3(3, 0, 3));

            _boxSelection.ApplySelection();

            // Selection manager should have the units
            Assert.Greater(_selectionManager.SelectedCount, 0);
        }

        [Test]
        public void ApplySelection_WithAddMode_AddsToExisting()
        {
            // Pre-select a unit outside the box
            _selectionManager.SelectUnit(_testUnits[8]); // Unit at (4, 0, 4)
            int initialCount = _selectionManager.SelectedCount;

            _boxSelection.SetAddToSelection(true);
            _boxSelection.StartSelection(new Vector3(-1, 0, -1));
            _boxSelection.UpdateSelection(new Vector3(3, 0, 3));
            _boxSelection.ApplySelection();

            // Should have more than initial
            Assert.Greater(_selectionManager.SelectedCount, initialCount);
            // Original should still be selected
            Assert.IsTrue(_selectionManager.IsSelected(_testUnits[8]));
        }

        [Test]
        public void ApplySelection_WithoutAddMode_ReplacesExisting()
        {
            // Pre-select a unit outside the box
            _selectionManager.SelectUnit(_testUnits[8]); // Unit at (4, 0, 4)

            _boxSelection.SetAddToSelection(false);
            _boxSelection.StartSelection(new Vector3(-1, 0, -1));
            _boxSelection.UpdateSelection(new Vector3(3, 0, 3));
            _boxSelection.ApplySelection();

            // Original should NOT be selected anymore
            Assert.IsFalse(_selectionManager.IsSelected(_testUnits[8]));
        }

        #endregion

        #region Visual Feedback Tests

        [Test]
        public void StartSelection_ShowsSelectionVisual()
        {
            _boxSelection.StartSelection(Vector3.zero);

            Assert.IsTrue(_boxSelection.IsVisualActive);
        }

        [Test]
        public void EndSelection_HidesSelectionVisual()
        {
            _boxSelection.StartSelection(Vector3.zero);
            _boxSelection.EndSelection();

            Assert.IsFalse(_boxSelection.IsVisualActive);
        }

        [Test]
        public void UpdateSelection_UpdatesVisualBounds()
        {
            _boxSelection.StartSelection(new Vector3(0, 0, 0));
            _boxSelection.UpdateSelection(new Vector3(5, 0, 5));

            // Visual should cover the selection area
            Bounds visualBounds = _boxSelection.GetVisualBounds();

            Assert.Greater(visualBounds.size.x, 0);
            Assert.Greater(visualBounds.size.z, 0);
        }

        #endregion

        #region Minimum Size Tests

        [Test]
        public void GetUnitsInSelection_IgnoresTooSmallSelection()
        {
            // Very small drag (accidental click)
            _boxSelection.StartSelection(new Vector3(0, 0, 0));
            _boxSelection.UpdateSelection(new Vector3(0.05f, 0, 0.05f));

            bool isValidSelection = _boxSelection.IsValidSelection();

            Assert.IsFalse(isValidSelection);
        }

        [Test]
        public void IsValidSelection_TrueForLargeEnoughBox()
        {
            _boxSelection.StartSelection(new Vector3(0, 0, 0));
            _boxSelection.UpdateSelection(new Vector3(1, 0, 1));

            Assert.IsTrue(_boxSelection.IsValidSelection());
        }

        #endregion

        #region Helper Methods

        private GameObject CreateTestUnit(int teamId, Vector3 position)
        {
            var unitGO = new GameObject($"TestUnit_{_createdObjects.Count}");
            _createdObjects.Add(unitGO);
            unitGO.transform.position = position;

            // Add collider for physics-based selection
            var collider = unitGO.AddComponent<BoxCollider>();
            collider.size = Vector3.one;

            var controller = unitGO.AddComponent<UnitController>();
            controller.Initialize(_archetype, teamId);

            return unitGO;
        }

        #endregion
    }
}
