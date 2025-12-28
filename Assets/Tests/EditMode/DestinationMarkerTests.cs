using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for DestinationMarker and DestinationMarkerManager.
    /// TDD tests written first per WP-EXT-5.1.
    /// </summary>
    public class DestinationMarkerTests
    {
        private GameObject _managerGameObject;
        private DestinationMarkerManager _markerManager;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void Setup()
        {
            _createdObjects = new List<GameObject>();

            // Create marker manager
            _managerGameObject = new GameObject("DestinationMarkerManager");
            _createdObjects.Add(_managerGameObject);
            _markerManager = _managerGameObject.AddComponent<DestinationMarkerManager>();
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
        }

        #region Manager Basic Tests

        [Test]
        public void DestinationMarkerManager_CanBeCreated()
        {
            Assert.IsNotNull(_markerManager);
        }

        [Test]
        public void DestinationMarkerManager_HasSingletonInstance()
        {
            Assert.AreEqual(_markerManager, DestinationMarkerManager.Instance);
        }

        #endregion

        #region Marker Creation Tests

        [Test]
        public void ShowMoveMarker_CreatesMarkerAtPosition()
        {
            var position = new Vector3(5f, 0f, 10f);

            var marker = _markerManager.ShowMoveMarker(position);

            Assert.IsNotNull(marker);
            Assert.AreEqual(position, marker.Position);
        }

        [Test]
        public void ShowMoveMarker_ReturnsMarkerOfTypeMoveType()
        {
            var marker = _markerManager.ShowMoveMarker(Vector3.zero);

            Assert.AreEqual(MarkerType.Move, marker.Type);
        }

        [Test]
        public void ShowAttackMarker_CreatesMarkerAtPosition()
        {
            var position = new Vector3(8f, 0f, 3f);

            var marker = _markerManager.ShowAttackMarker(position);

            Assert.IsNotNull(marker);
            Assert.AreEqual(position, marker.Position);
        }

        [Test]
        public void ShowAttackMarker_ReturnsMarkerOfTypeAttack()
        {
            var marker = _markerManager.ShowAttackMarker(Vector3.zero);

            Assert.AreEqual(MarkerType.Attack, marker.Type);
        }

        #endregion

        #region Marker Lifetime Tests

        [Test]
        public void Marker_HasDefaultLifetime()
        {
            var marker = _markerManager.ShowMoveMarker(Vector3.zero);

            // Default lifetime should be positive
            Assert.Greater(marker.Lifetime, 0f);
        }

        [Test]
        public void ShowMoveMarker_WithCustomLifetime_SetsLifetime()
        {
            var lifetime = 5f;

            var marker = _markerManager.ShowMoveMarker(Vector3.zero, lifetime);

            Assert.AreEqual(lifetime, marker.Lifetime);
        }

        [Test]
        public void ClearAllMarkers_RemovesAllActiveMarkers()
        {
            _markerManager.ShowMoveMarker(Vector3.zero);
            _markerManager.ShowMoveMarker(Vector3.one);
            _markerManager.ShowAttackMarker(Vector3.right);

            _markerManager.ClearAllMarkers();

            Assert.AreEqual(0, _markerManager.ActiveMarkerCount);
        }

        #endregion

        #region Marker Visuals Tests

        [Test]
        public void MoveMarker_HasGreenishColor()
        {
            var marker = _markerManager.ShowMoveMarker(Vector3.zero);

            // Move markers should be greenish
            Assert.Greater(marker.Color.g, marker.Color.r);
        }

        [Test]
        public void AttackMarker_HasReddishColor()
        {
            var marker = _markerManager.ShowAttackMarker(Vector3.zero);

            // Attack markers should be reddish
            Assert.Greater(marker.Color.r, marker.Color.b);
        }

        [Test]
        public void Marker_CreatesVisualGameObject()
        {
            var marker = _markerManager.ShowMoveMarker(Vector3.zero);

            Assert.IsNotNull(marker.Visual);
            Assert.IsTrue(marker.Visual.activeInHierarchy);
        }

        #endregion

        #region Pooling Tests

        [Test]
        public void ShowMoveMarker_ReusesPooledMarkers()
        {
            // Create and hide a marker
            var marker1 = _markerManager.ShowMoveMarker(Vector3.zero);
            marker1.Hide();

            // Create another marker - should reuse pooled one
            var marker2 = _markerManager.ShowMoveMarker(Vector3.one);

            // Visual should be same object (reused from pool)
            Assert.AreEqual(marker1.Visual, marker2.Visual);
        }

        [Test]
        public void HideMarker_ReturnsMarkerToPool()
        {
            var marker = _markerManager.ShowMoveMarker(Vector3.zero);
            int countBefore = _markerManager.ActiveMarkerCount;

            marker.Hide();

            Assert.AreEqual(countBefore - 1, _markerManager.ActiveMarkerCount);
        }

        #endregion

        #region Multiple Unit Markers Tests

        [Test]
        public void ShowMoveMarkerForUnits_CreatesMarkerForEachUnit()
        {
            var unit1GO = CreateTestUnit(0);
            var unit2GO = CreateTestUnit(0);
            var unit1 = unit1GO.GetComponent<UnitController>();
            var unit2 = unit2GO.GetComponent<UnitController>();

            var units = new List<UnitController> { unit1, unit2 };
            var destination = new Vector3(10f, 0f, 10f);

            var markers = _markerManager.ShowMoveMarkersForUnits(units, destination);

            Assert.AreEqual(2, markers.Count);
        }

        [Test]
        public void ShowMoveMarkerForUnits_MarkersAtSameDestination()
        {
            var unit1GO = CreateTestUnit(0);
            var unit2GO = CreateTestUnit(0);
            var unit1 = unit1GO.GetComponent<UnitController>();
            var unit2 = unit2GO.GetComponent<UnitController>();

            var units = new List<UnitController> { unit1, unit2 };
            var destination = new Vector3(10f, 0f, 10f);

            var markers = _markerManager.ShowMoveMarkersForUnits(units, destination);

            foreach (var marker in markers)
            {
                Assert.AreEqual(destination, marker.Position);
            }
        }

        #endregion

        #region SelectionManager Integration Tests

        [Test]
        public void ShowMarkerForSelectedUnits_ShowsAtDestination()
        {
            // Setup selection manager and units
            var selectionManagerGO = new GameObject("SelectionManager");
            _createdObjects.Add(selectionManagerGO);
            var selectionManager = selectionManagerGO.AddComponent<SelectionManager>();

            var unitGO = CreateTestUnit(0);
            var unit = unitGO.GetComponent<UnitController>();
            selectionManager.SelectUnit(unit);

            var destination = new Vector3(5f, 0f, 5f);

            // Show marker for selected units
            _markerManager.ShowMoveMarkerForSelection(destination);

            Assert.AreEqual(1, _markerManager.ActiveMarkerCount);
        }

        #endregion

        #region Helper Methods

        private GameObject CreateTestUnit(int teamId)
        {
            var unitGO = new GameObject($"TestUnit_{_createdObjects.Count}");
            _createdObjects.Add(unitGO);
            unitGO.AddComponent<BoxCollider>();

            var archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            var controller = unitGO.AddComponent<UnitController>();
            controller.Initialize(archetype, teamId);

            return unitGO;
        }

        #endregion
    }

    /// <summary>
    /// Unit tests for individual DestinationMarker component.
    /// </summary>
    public class DestinationMarkerComponentTests
    {
        private GameObject _markerGameObject;
        private DestinationMarker _marker;

        [SetUp]
        public void Setup()
        {
            _markerGameObject = new GameObject("TestMarker");
            _marker = _markerGameObject.AddComponent<DestinationMarker>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_markerGameObject != null)
            {
                Object.DestroyImmediate(_markerGameObject);
            }
        }

        [Test]
        public void DestinationMarker_CanBeCreated()
        {
            Assert.IsNotNull(_marker);
        }

        [Test]
        public void Initialize_SetsPositionAndType()
        {
            var position = new Vector3(1f, 2f, 3f);

            _marker.Initialize(position, MarkerType.Move, Color.green, 3f);

            Assert.AreEqual(position, _marker.Position);
            Assert.AreEqual(MarkerType.Move, _marker.Type);
        }

        [Test]
        public void Initialize_CreatesDefaultVisual()
        {
            _marker.Initialize(Vector3.zero, MarkerType.Move, Color.green, 3f);

            Assert.IsNotNull(_marker.Visual);
        }

        [Test]
        public void SetPosition_UpdatesMarkerPosition()
        {
            _marker.Initialize(Vector3.zero, MarkerType.Move, Color.green, 3f);
            var newPosition = new Vector3(5f, 0f, 5f);

            _marker.SetPosition(newPosition);

            Assert.AreEqual(newPosition, _marker.Position);
        }

        [Test]
        public void Hide_DeactivatesVisual()
        {
            _marker.Initialize(Vector3.zero, MarkerType.Move, Color.green, 3f);

            _marker.Hide();

            Assert.IsFalse(_marker.Visual.activeInHierarchy);
        }

        [Test]
        public void Show_ActivatesVisual()
        {
            _marker.Initialize(Vector3.zero, MarkerType.Move, Color.green, 3f);
            _marker.Hide();

            _marker.Show();

            Assert.IsTrue(_marker.Visual.activeInHierarchy);
        }

        [Test]
        public void IsActive_ReturnsVisualState()
        {
            _marker.Initialize(Vector3.zero, MarkerType.Move, Color.green, 3f);

            Assert.IsTrue(_marker.IsActive);

            _marker.Hide();

            Assert.IsFalse(_marker.IsActive);
        }
    }
}
