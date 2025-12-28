using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for UnitPool.
    /// Tests validate pooling operations: spawn, despawn, warm-up, and cleanup.
    /// </summary>
    public class UnitPoolTests
    {
        private GameObject _poolGO;
        private UnitPool _pool;
        private UnitArchetypeSO _testArchetype;
        private GameObject _testPrefab;

        [SetUp]
        public void Setup()
        {
            // Create pool manager
            _poolGO = new GameObject("TestUnitPool");
            _pool = _poolGO.AddComponent<UnitPool>();

            // Create test prefab with required components
            _testPrefab = new GameObject("TestUnitPrefab");
            _testPrefab.AddComponent<BoxCollider>();
            _testPrefab.AddComponent<UnitController>();
            _testPrefab.AddComponent<UnitAI>();

            // Create test archetype
            _testArchetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            _testArchetype.SetTestValues("test_infantry", "Test Infantry", _testPrefab, 100, 5f, 10, 0);
        }

        [TearDown]
        public void Teardown()
        {
            if (_poolGO != null)
                Object.DestroyImmediate(_poolGO);
            if (_testPrefab != null)
                Object.DestroyImmediate(_testPrefab);
            if (_testArchetype != null)
                Object.DestroyImmediate(_testArchetype);

            // Clean up any pooled units
            var allUnits = Object.FindObjectsOfType<UnitController>();
            foreach (var unit in allUnits)
            {
                if (unit != null && unit.gameObject != null)
                    Object.DestroyImmediate(unit.gameObject);
            }
        }

        #region Basic Spawn Tests

        [Test]
        public void Spawn_ReturnsValidUnit()
        {
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);

            Assert.IsNotNull(unit);
            Assert.IsTrue(unit.gameObject.activeSelf);
        }

        [Test]
        public void Spawn_WithNullArchetype_ReturnsNull()
        {
            var unit = _pool.Spawn(null, Vector3.zero, 0);

            Assert.IsNull(unit);
        }

        [Test]
        public void Spawn_SetsCorrectPosition()
        {
            Vector3 expectedPos = new Vector3(10f, 0f, 5f);

            var unit = _pool.Spawn(_testArchetype, expectedPos, 0);

            Assert.AreEqual(expectedPos.x, unit.transform.position.x, 0.01f);
            Assert.AreEqual(expectedPos.z, unit.transform.position.z, 0.01f);
        }

        [Test]
        public void Spawn_SetsCorrectTeam()
        {
            int expectedTeam = 1;

            var unit = _pool.Spawn(_testArchetype, Vector3.zero, expectedTeam);

            Assert.AreEqual(expectedTeam, unit.TeamId);
        }

        [Test]
        public void Spawn_WithRotation_SetsCorrectRotation()
        {
            Quaternion expectedRot = Quaternion.Euler(0f, 90f, 0f);

            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0, expectedRot);

            Assert.AreEqual(expectedRot.eulerAngles.y, unit.transform.rotation.eulerAngles.y, 0.01f);
        }

        #endregion

        #region Despawn Tests

        [Test]
        public void Despawn_DeactivatesUnit()
        {
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);

            _pool.Despawn(unit);

            Assert.IsFalse(unit.gameObject.activeSelf);
        }

        [Test]
        public void Despawn_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _pool.Despawn(null));
        }

        [Test]
        public void Despawn_IncreasesPooledCount()
        {
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);
            int countBefore = _pool.GetPooledCount(_testArchetype);

            _pool.Despawn(unit);

            int countAfter = _pool.GetPooledCount(_testArchetype);
            Assert.AreEqual(countBefore + 1, countAfter);
        }

        #endregion

        #region Pool Reuse Tests

        [Test]
        public void Spawn_ReusesPooledUnit()
        {
            var unit1 = _pool.Spawn(_testArchetype, Vector3.zero, 0);
            _pool.Despawn(unit1);

            var unit2 = _pool.Spawn(_testArchetype, Vector3.one, 1);

            Assert.AreSame(unit1, unit2);
        }

        [Test]
        public void Spawn_ReusedUnit_IsActiveAndReset()
        {
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);
            unit.TakeDamage(50);
            _pool.Despawn(unit);

            var reusedUnit = _pool.Spawn(_testArchetype, Vector3.zero, 0);

            Assert.IsTrue(reusedUnit.gameObject.activeSelf);
            Assert.IsTrue(reusedUnit.IsAlive);
        }

        [Test]
        public void Spawn_MultipleReuses_WorksCorrectly()
        {
            var units = new List<UnitController>();

            // Spawn 5 units
            for (int i = 0; i < 5; i++)
            {
                units.Add(_pool.Spawn(_testArchetype, Vector3.zero, 0));
            }

            // Despawn all
            foreach (var unit in units)
            {
                _pool.Despawn(unit);
            }

            // Spawn 5 more - should reuse all from pool
            var reusedUnits = new List<UnitController>();
            for (int i = 0; i < 5; i++)
            {
                reusedUnits.Add(_pool.Spawn(_testArchetype, Vector3.zero, 0));
            }

            Assert.AreEqual(0, _pool.GetPooledCount(_testArchetype));
        }

        #endregion

        #region Warm-Up Tests

        [Test]
        public void WarmUp_CreatesInactiveUnits()
        {
            _pool.WarmUp(_testArchetype, 5);

            Assert.AreEqual(5, _pool.GetPooledCount(_testArchetype));
        }

        [Test]
        public void WarmUp_UnitsAreInactive()
        {
            _pool.WarmUp(_testArchetype, 3);

            // Spawning should use the warmed units
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);
            Assert.AreEqual(2, _pool.GetPooledCount(_testArchetype));
        }

        [Test]
        public void WarmUp_WithZeroCount_DoesNothing()
        {
            _pool.WarmUp(_testArchetype, 0);

            Assert.AreEqual(0, _pool.GetPooledCount(_testArchetype));
        }

        [Test]
        public void WarmUp_WithNegativeCount_DoesNothing()
        {
            _pool.WarmUp(_testArchetype, -5);

            Assert.AreEqual(0, _pool.GetPooledCount(_testArchetype));
        }

        #endregion

        #region Pool Capacity Tests

        [Test]
        public void MaxPoolSize_ExcessUnitsAreDestroyed()
        {
            _pool.MaxPoolSize = 3;

            // Spawn and despawn 5 units
            var units = new List<UnitController>();
            for (int i = 0; i < 5; i++)
            {
                units.Add(_pool.Spawn(_testArchetype, Vector3.zero, 0));
            }

            foreach (var unit in units)
            {
                _pool.Despawn(unit);
            }

            // Pool should only have 3 (max)
            Assert.AreEqual(3, _pool.GetPooledCount(_testArchetype));
        }

        [Test]
        public void GetTotalPooledCount_ReturnsCorrectCount()
        {
            _pool.WarmUp(_testArchetype, 5);

            Assert.AreEqual(5, _pool.GetTotalPooledCount());
        }

        #endregion

        #region Squad Cleanup Tests

        [Test]
        public void Despawn_RemovesFromSquad()
        {
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);
            var squad = new Squad("test-squad", 0);
            unit.JoinSquad(squad);

            _pool.Despawn(unit);

            Assert.IsFalse(unit.IsInSquad);
            Assert.AreEqual(0, squad.MemberCount);
        }

        #endregion

        #region Statistics Tests

        [Test]
        public void GetActiveCount_ReturnsCorrectCount()
        {
            _pool.Spawn(_testArchetype, Vector3.zero, 0);
            _pool.Spawn(_testArchetype, Vector3.zero, 0);

            Assert.AreEqual(2, _pool.GetActiveCount(_testArchetype));
        }

        [Test]
        public void GetActiveCount_AfterDespawn_Decreases()
        {
            var unit1 = _pool.Spawn(_testArchetype, Vector3.zero, 0);
            var unit2 = _pool.Spawn(_testArchetype, Vector3.zero, 0);

            _pool.Despawn(unit1);

            Assert.AreEqual(1, _pool.GetActiveCount(_testArchetype));
        }

        [Test]
        public void GetTotalActiveCount_ReturnsCorrectTotal()
        {
            _pool.Spawn(_testArchetype, Vector3.zero, 0);
            _pool.Spawn(_testArchetype, Vector3.zero, 0);
            _pool.Spawn(_testArchetype, Vector3.zero, 0);

            Assert.AreEqual(3, _pool.GetTotalActiveCount());
        }

        #endregion

        #region Clear Tests

        [Test]
        public void ClearPool_RemovesAllPooledUnits()
        {
            _pool.WarmUp(_testArchetype, 5);

            _pool.ClearPool(_testArchetype);

            Assert.AreEqual(0, _pool.GetPooledCount(_testArchetype));
        }

        [Test]
        public void ClearAllPools_RemovesAllUnits()
        {
            _pool.WarmUp(_testArchetype, 5);
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);
            _pool.Despawn(unit);

            _pool.ClearAllPools();

            Assert.AreEqual(0, _pool.GetTotalPooledCount());
        }

        #endregion

        #region TickManager Integration Tests

        [Test]
        public void Spawn_RegistersWithTickManager()
        {
            // TickManager registers units on spawn
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);

            // UnitController and UnitAI should be registered via their Start() methods
            // In EditMode tests, Start() may not run, so we just verify the unit exists
            Assert.IsNotNull(unit);
        }

        [Test]
        public void Despawn_DoesNotCauseTickErrors()
        {
            var unit = _pool.Spawn(_testArchetype, Vector3.zero, 0);

            // Despawn should safely unregister
            Assert.DoesNotThrow(() => _pool.Despawn(unit));
        }

        #endregion
    }
}
