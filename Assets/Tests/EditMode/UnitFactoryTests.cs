using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for UnitFactory - responsible for spawning units from archetypes.
    /// </summary>
    public class UnitFactoryTests
    {
        private GameObject _factoryGameObject;
        private UnitFactory _factory;
        private UnitArchetypeSO _archetype;
        private GameObject _prefab;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void Setup()
        {
            _createdObjects = new List<GameObject>();

            // Create factory
            _factoryGameObject = new GameObject("Factory");
            _createdObjects.Add(_factoryGameObject);
            _factory = _factoryGameObject.AddComponent<UnitFactory>();

            // Create test prefab with required components
            _prefab = new GameObject("UnitPrefab");
            _prefab.AddComponent<BoxCollider>();
            _prefab.AddComponent<UnitController>();
            _createdObjects.Add(_prefab);

            // Create test archetype
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
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

            if (_archetype != null)
            {
                Object.DestroyImmediate(_archetype);
            }
        }

        #region SpawnUnit Tests

        [Test]
        public void SpawnUnit_CreatesGameObject()
        {
            SetArchetypePrefab(_archetype, _prefab);
            var position = new Vector3(5, 0, 5);

            var unit = _factory.SpawnUnit(_archetype, position, 0);
            if (unit != null) _createdObjects.Add(unit);

            Assert.IsNotNull(unit);
        }

        [Test]
        public void SpawnUnit_PositionsUnitCorrectly()
        {
            SetArchetypePrefab(_archetype, _prefab);
            var position = new Vector3(5, 0, 5);

            var unit = _factory.SpawnUnit(_archetype, position, 0);
            if (unit != null) _createdObjects.Add(unit);

            Assert.AreEqual(position, unit.transform.position);
        }

        [Test]
        public void SpawnUnit_InitializesUnitController()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var unit = _factory.SpawnUnit(_archetype, Vector3.zero, 1);
            if (unit != null) _createdObjects.Add(unit);

            var controller = unit.GetComponent<UnitController>();
            Assert.IsNotNull(controller);
            Assert.AreEqual(_archetype, controller.Archetype);
            Assert.AreEqual(1, controller.TeamId);
        }

        [Test]
        public void SpawnUnit_WithNullArchetype_ReturnsNull()
        {
            var unit = _factory.SpawnUnit(null, Vector3.zero, 0);

            Assert.IsNull(unit);
        }

        [Test]
        public void SpawnUnit_WithNullPrefab_ReturnsNull()
        {
            // Archetype without prefab
            var emptyArchetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();

            var unit = _factory.SpawnUnit(emptyArchetype, Vector3.zero, 0);

            Assert.IsNull(unit);
            Object.DestroyImmediate(emptyArchetype);
        }

        [Test]
        public void SpawnUnit_SetsCorrectTeamId()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var redUnit = _factory.SpawnUnit(_archetype, Vector3.zero, 0);
            var blueUnit = _factory.SpawnUnit(_archetype, Vector3.right * 2, 1);
            if (redUnit != null) _createdObjects.Add(redUnit);
            if (blueUnit != null) _createdObjects.Add(blueUnit);

            Assert.AreEqual(0, redUnit.GetComponent<UnitController>().TeamId);
            Assert.AreEqual(1, blueUnit.GetComponent<UnitController>().TeamId);
        }

        [Test]
        public void SpawnUnit_AppliesRotation()
        {
            SetArchetypePrefab(_archetype, _prefab);
            var rotation = Quaternion.Euler(0, 90, 0);

            var unit = _factory.SpawnUnit(_archetype, Vector3.zero, 0, rotation);
            if (unit != null) _createdObjects.Add(unit);

            Assert.AreEqual(rotation.eulerAngles.y, unit.transform.rotation.eulerAngles.y, 0.01f);
        }

        [Test]
        public void SpawnUnit_FiresOnUnitSpawnedEvent()
        {
            SetArchetypePrefab(_archetype, _prefab);
            UnitController spawnedController = null;
            _factory.OnUnitSpawned += (controller) => spawnedController = controller;

            var unit = _factory.SpawnUnit(_archetype, Vector3.zero, 0);
            if (unit != null) _createdObjects.Add(unit);

            Assert.IsNotNull(spawnedController);
            Assert.AreEqual(unit.GetComponent<UnitController>(), spawnedController);
        }

        #endregion

        #region SpawnAt (SpawnPoint) Tests

        [Test]
        public void SpawnAtPoint_SpawnsAtSpawnPointPosition()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var spawnPointGO = new GameObject("SpawnPoint");
            spawnPointGO.transform.position = new Vector3(10, 0, 10);
            var spawnPoint = spawnPointGO.AddComponent<SpawnPoint>();
            _createdObjects.Add(spawnPointGO);

            var unit = _factory.SpawnAtPoint(_archetype, spawnPoint);
            if (unit != null) _createdObjects.Add(unit);

            Assert.AreEqual(spawnPointGO.transform.position, unit.transform.position);
        }

        [Test]
        public void SpawnAtPoint_UsesSpawnPointTeam()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var spawnPointGO = new GameObject("SpawnPoint");
            var spawnPoint = spawnPointGO.AddComponent<SpawnPoint>();
            SetSpawnPointTeam(spawnPoint, 1);
            _createdObjects.Add(spawnPointGO);

            var unit = _factory.SpawnAtPoint(_archetype, spawnPoint);
            if (unit != null) _createdObjects.Add(unit);

            Assert.AreEqual(1, unit.GetComponent<UnitController>().TeamId);
        }

        [Test]
        public void SpawnAtPoint_WithNullSpawnPoint_ReturnsNull()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var unit = _factory.SpawnAtPoint(_archetype, null);

            Assert.IsNull(unit);
        }

        [Test]
        public void SpawnAtPoint_AppliesSpawnPointRotation()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var spawnPointGO = new GameObject("SpawnPoint");
            spawnPointGO.transform.rotation = Quaternion.Euler(0, 45, 0);
            var spawnPoint = spawnPointGO.AddComponent<SpawnPoint>();
            _createdObjects.Add(spawnPointGO);

            var unit = _factory.SpawnAtPoint(_archetype, spawnPoint);
            if (unit != null) _createdObjects.Add(unit);

            Assert.AreEqual(45f, unit.transform.rotation.eulerAngles.y, 0.1f);
        }

        #endregion

        #region Tracking Tests

        [Test]
        public void GetAllUnits_ReturnsSpawnedUnits()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var unit1 = _factory.SpawnUnit(_archetype, Vector3.zero, 0);
            var unit2 = _factory.SpawnUnit(_archetype, Vector3.right, 0);
            if (unit1 != null) _createdObjects.Add(unit1);
            if (unit2 != null) _createdObjects.Add(unit2);

            var allUnits = _factory.GetAllUnits();

            Assert.AreEqual(2, allUnits.Count);
        }

        [Test]
        public void GetUnitsByTeam_ReturnsCorrectTeamUnits()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var redUnit1 = _factory.SpawnUnit(_archetype, Vector3.zero, 0);
            var redUnit2 = _factory.SpawnUnit(_archetype, Vector3.right, 0);
            var blueUnit = _factory.SpawnUnit(_archetype, Vector3.forward, 1);
            if (redUnit1 != null) _createdObjects.Add(redUnit1);
            if (redUnit2 != null) _createdObjects.Add(redUnit2);
            if (blueUnit != null) _createdObjects.Add(blueUnit);

            var redUnits = _factory.GetUnitsByTeam(0);
            var blueUnits = _factory.GetUnitsByTeam(1);

            Assert.AreEqual(2, redUnits.Count);
            Assert.AreEqual(1, blueUnits.Count);
        }

        [Test]
        public void UnitCount_ReturnsCorrectCount()
        {
            SetArchetypePrefab(_archetype, _prefab);

            Assert.AreEqual(0, _factory.UnitCount);

            var unit = _factory.SpawnUnit(_archetype, Vector3.zero, 0);
            if (unit != null) _createdObjects.Add(unit);

            Assert.AreEqual(1, _factory.UnitCount);
        }

        #endregion

        #region Cleanup Tests

        [Test]
        public void DestroyUnit_RemovesFromTracking()
        {
            SetArchetypePrefab(_archetype, _prefab);

            var unit = _factory.SpawnUnit(_archetype, Vector3.zero, 0);
            var controller = unit.GetComponent<UnitController>();

            _factory.DestroyUnit(controller);

            Assert.AreEqual(0, _factory.UnitCount);
        }

        [Test]
        public void DestroyAllUnits_ClearsAllTrackedUnits()
        {
            SetArchetypePrefab(_archetype, _prefab);

            _factory.SpawnUnit(_archetype, Vector3.zero, 0);
            _factory.SpawnUnit(_archetype, Vector3.right, 1);

            _factory.DestroyAllUnits();

            Assert.AreEqual(0, _factory.UnitCount);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Sets the unit prefab on an archetype using serialized field access.
        /// </summary>
        private void SetArchetypePrefab(UnitArchetypeSO archetype, GameObject prefab)
        {
            // Use reflection to set the private field
            var field = typeof(UnitArchetypeSO).GetField("_unitPrefab",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(archetype, prefab);

            // Also set ID and display name for validation to pass
            var idField = typeof(UnitArchetypeSO).GetField("_id",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            idField?.SetValue(archetype, "test_unit");

            var nameField = typeof(UnitArchetypeSO).GetField("_displayName",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            nameField?.SetValue(archetype, "Test Unit");
        }

        /// <summary>
        /// Sets the team ID on a spawn point using serialized field access.
        /// </summary>
        private void SetSpawnPointTeam(SpawnPoint spawnPoint, int teamId)
        {
            var field = typeof(SpawnPoint).GetField("_teamId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(spawnPoint, teamId);
        }

        #endregion
    }
}
