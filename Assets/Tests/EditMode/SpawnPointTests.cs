using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for SpawnPoint component.
    /// </summary>
    public class SpawnPointTests
    {
        private GameObject _spawnPointGameObject;
        private SpawnPoint _spawnPoint;

        [SetUp]
        public void Setup()
        {
            _spawnPointGameObject = new GameObject("TestSpawnPoint");
            _spawnPoint = _spawnPointGameObject.AddComponent<SpawnPoint>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_spawnPointGameObject != null)
            {
                Object.DestroyImmediate(_spawnPointGameObject);
            }
        }

        #region Properties Tests

        [Test]
        public void TeamId_DefaultsToZero()
        {
            Assert.AreEqual(0, _spawnPoint.TeamId);
        }

        [Test]
        public void SpawnRadius_DefaultsToPositive()
        {
            Assert.Greater(_spawnPoint.SpawnRadius, 0f);
        }

        [Test]
        public void Position_ReturnsTransformPosition()
        {
            _spawnPointGameObject.transform.position = new Vector3(5, 0, 10);

            Assert.AreEqual(new Vector3(5, 0, 10), _spawnPoint.Position);
        }

        [Test]
        public void Rotation_ReturnsTransformRotation()
        {
            _spawnPointGameObject.transform.rotation = Quaternion.Euler(0, 90, 0);

            Assert.AreEqual(90f, _spawnPoint.Rotation.eulerAngles.y, 0.1f);
        }

        [Test]
        public void Forward_ReturnsTransformForward()
        {
            _spawnPointGameObject.transform.rotation = Quaternion.Euler(0, 90, 0);

            Assert.AreEqual(_spawnPointGameObject.transform.forward, _spawnPoint.Forward);
        }

        #endregion

        #region Spawn Position Calculation Tests

        [Test]
        public void GetSpawnPosition_WithZeroRadius_ReturnsExactPosition()
        {
            SetSpawnRadius(_spawnPoint, 0f);
            _spawnPointGameObject.transform.position = new Vector3(5, 0, 5);

            var spawnPos = _spawnPoint.GetSpawnPosition();

            Assert.AreEqual(new Vector3(5, 0, 5), spawnPos);
        }

        [Test]
        public void GetSpawnPosition_WithRadius_ReturnsPositionWithinRadius()
        {
            SetSpawnRadius(_spawnPoint, 2f);
            _spawnPointGameObject.transform.position = Vector3.zero;

            // Test multiple times due to randomness
            for (int i = 0; i < 10; i++)
            {
                var spawnPos = _spawnPoint.GetSpawnPosition();
                float distance = Vector3.Distance(Vector3.zero, spawnPos);

                Assert.LessOrEqual(distance, 2f, "Spawn position should be within radius");
            }
        }

        [Test]
        public void GetSpawnPosition_PreservesYCoordinate()
        {
            SetSpawnRadius(_spawnPoint, 1f);
            _spawnPointGameObject.transform.position = new Vector3(0, 5, 0);

            var spawnPos = _spawnPoint.GetSpawnPosition();

            Assert.AreEqual(5f, spawnPos.y, "Y coordinate should be preserved");
        }

        #endregion

        #region Team Configuration Tests

        [Test]
        public void IsRedTeam_WhenTeamIdZero_ReturnsTrue()
        {
            SetTeamId(_spawnPoint, 0);

            Assert.IsTrue(_spawnPoint.IsRedTeam);
            Assert.IsFalse(_spawnPoint.IsBlueTeam);
        }

        [Test]
        public void IsBlueTeam_WhenTeamIdOne_ReturnsTrue()
        {
            SetTeamId(_spawnPoint, 1);

            Assert.IsFalse(_spawnPoint.IsRedTeam);
            Assert.IsTrue(_spawnPoint.IsBlueTeam);
        }

        #endregion

        #region Gizmos/Debug Tests

        [Test]
        public void SpawnPoint_ExistsAndHasTransform()
        {
            Assert.IsNotNull(_spawnPoint);
            Assert.IsNotNull(_spawnPoint.transform);
        }

        #endregion

        #region Helper Methods

        private void SetSpawnRadius(SpawnPoint spawnPoint, float radius)
        {
            var field = typeof(SpawnPoint).GetField("_spawnRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(spawnPoint, radius);
        }

        private void SetTeamId(SpawnPoint spawnPoint, int teamId)
        {
            var field = typeof(SpawnPoint).GetField("_teamId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(spawnPoint, teamId);
        }

        #endregion
    }
}
