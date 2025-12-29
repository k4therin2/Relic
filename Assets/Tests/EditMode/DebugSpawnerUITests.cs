using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using Relic.CoreRTS;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for DebugSpawnerUI spawner logic.
    /// Tests spawn position generation and property validation.
    /// </summary>
    public class DebugSpawnerUITests
    {
        private GameObject _spawnerGO;
        private DebugSpawnerUI _spawner;

        [SetUp]
        public void SetUp()
        {
            _spawnerGO = new GameObject("TestSpawner");
            _spawner = _spawnerGO.AddComponent<DebugSpawnerUI>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_spawnerGO != null)
            {
                Object.DestroyImmediate(_spawnerGO);
            }
        }

        #region Property Tests

        [Test]
        public void UnitsPerSpawn_DefaultValue_IsPositive()
        {
            // Assert
            Assert.Greater(_spawner.UnitsPerSpawn, 0);
        }

        [Test]
        public void UnitsPerSpawn_SetValidValue_AcceptsValue()
        {
            // Arrange
            int expected = 10;

            // Act
            _spawner.UnitsPerSpawn = expected;

            // Assert
            Assert.AreEqual(expected, _spawner.UnitsPerSpawn);
        }

        [Test]
        public void UnitsPerSpawn_SetNegativeValue_ClampsToOne()
        {
            // Act
            _spawner.UnitsPerSpawn = -5;

            // Assert
            Assert.AreEqual(1, _spawner.UnitsPerSpawn);
        }

        [Test]
        public void UnitsPerSpawn_SetZero_ClampsToOne()
        {
            // Act
            _spawner.UnitsPerSpawn = 0;

            // Assert
            Assert.AreEqual(1, _spawner.UnitsPerSpawn);
        }

        [Test]
        public void UnitsPerSpawn_SetAboveMax_ClampsToMax()
        {
            // Act
            _spawner.UnitsPerSpawn = 100;

            // Assert
            Assert.LessOrEqual(_spawner.UnitsPerSpawn, 20);
        }

        [Test]
        public void MaxUnitsPerTeam_DefaultValue_IsPositive()
        {
            // Assert
            Assert.Greater(_spawner.MaxUnitsPerTeam, 0);
        }

        [Test]
        public void MaxUnitsPerTeam_SetValidValue_AcceptsValue()
        {
            // Arrange
            int expected = 50;

            // Act
            _spawner.MaxUnitsPerTeam = expected;

            // Assert
            Assert.AreEqual(expected, _spawner.MaxUnitsPerTeam);
        }

        [Test]
        public void MaxUnitsPerTeam_SetNegativeValue_ClampsToOne()
        {
            // Act
            _spawner.MaxUnitsPerTeam = -10;

            // Assert
            Assert.AreEqual(1, _spawner.MaxUnitsPerTeam);
        }

        #endregion

        #region Count Tests

        [Test]
        public void Team0Count_InitialValue_IsZero()
        {
            // Assert
            Assert.AreEqual(0, _spawner.Team0Count);
        }

        [Test]
        public void Team1Count_InitialValue_IsZero()
        {
            // Assert
            Assert.AreEqual(0, _spawner.Team1Count);
        }

        [Test]
        public void TotalUnitCount_InitialValue_IsZero()
        {
            // Assert
            Assert.AreEqual(0, _spawner.TotalUnitCount);
        }

        #endregion

        #region Spawn Position Tests

        [Test]
        public void GetSpawnPositions_ReturnsCorrectCount()
        {
            // Arrange
            int count = 5;

            // Act
            List<Vector3> positions = _spawner.GetSpawnPositions(0, count);

            // Assert
            Assert.AreEqual(count, positions.Count);
        }

        [Test]
        public void GetSpawnPositions_Team0_PositionsOnLeftSide()
        {
            // Arrange
            int count = 5;

            // Act
            List<Vector3> positions = _spawner.GetSpawnPositions(0, count);

            // Assert - Team 0 spawns on left side (negative X)
            foreach (var pos in positions)
            {
                Assert.Less(pos.x, 0f);
            }
        }

        [Test]
        public void GetSpawnPositions_Team1_PositionsOnRightSide()
        {
            // Arrange
            int count = 5;

            // Act
            List<Vector3> positions = _spawner.GetSpawnPositions(1, count);

            // Assert - Team 1 spawns on right side (positive X)
            foreach (var pos in positions)
            {
                Assert.Greater(pos.x, 0f);
            }
        }

        [Test]
        public void GetSpawnPositions_PositionsAtGroundLevel()
        {
            // Arrange
            int count = 5;

            // Act
            List<Vector3> positions = _spawner.GetSpawnPositions(0, count);

            // Assert - Y should be at ground level (0)
            foreach (var pos in positions)
            {
                Assert.AreEqual(0f, pos.y, 0.01f);
            }
        }

        [Test]
        public void GetSpawnPositions_ZeroCount_ReturnsEmptyList()
        {
            // Act
            List<Vector3> positions = _spawner.GetSpawnPositions(0, 0);

            // Assert
            Assert.AreEqual(0, positions.Count);
        }

        [Test]
        public void GetSpawnPositions_LargeCount_ReturnsAllPositions()
        {
            // Arrange
            int count = 20;

            // Act
            List<Vector3> positions = _spawner.GetSpawnPositions(0, count);

            // Assert
            Assert.AreEqual(count, positions.Count);
        }

        [Test]
        public void GetSpawnPositions_PositionsAreUnique()
        {
            // Arrange
            int count = 10;

            // Act
            List<Vector3> positions = _spawner.GetSpawnPositions(0, count);

            // Assert - positions should not be exactly the same
            HashSet<string> uniquePositions = new HashSet<string>();
            foreach (var pos in positions)
            {
                // Round to 1 decimal to handle float precision
                string key = $"{Mathf.Round(pos.x * 10)},{Mathf.Round(pos.z * 10)}";
                uniquePositions.Add(key);
            }

            // Allow for some overlap due to randomization, but most should be unique
            Assert.Greater(uniquePositions.Count, count / 2);
        }

        #endregion

        #region Clear Tests

        [Test]
        public void ClearAll_WithNoUnits_DoesNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _spawner.ClearAll());
        }

        [Test]
        public void ClearAll_ResetsCountsToZero()
        {
            // Act
            _spawner.ClearAll();

            // Assert
            Assert.AreEqual(0, _spawner.Team0Count);
            Assert.AreEqual(0, _spawner.Team1Count);
            Assert.AreEqual(0, _spawner.TotalUnitCount);
        }

        #endregion
    }
}
