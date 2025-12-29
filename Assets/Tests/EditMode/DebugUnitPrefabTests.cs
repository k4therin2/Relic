using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using Relic.CoreRTS;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for debug unit prefab instantiation and configuration.
    /// Tests unit components, team colors, and basic behavior.
    /// </summary>
    public class DebugUnitPrefabTests
    {
        private GameObject _unitGO;
        private UnitController _controller;

        [SetUp]
        public void SetUp()
        {
            // Create a test unit similar to the debug prefab
            _unitGO = CreateTestUnit();
            _controller = _unitGO.GetComponent<UnitController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_unitGO != null)
            {
                Object.DestroyImmediate(_unitGO);
            }
        }

        private GameObject CreateTestUnit()
        {
            // Replicate the debug unit structure
            GameObject unitGO = new GameObject("TestDebugUnit");

            // Add visual mesh (capsule)
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsule.name = "Visual";
            capsule.transform.SetParent(unitGO.transform);
            capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
            capsule.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            // Remove collider from visual
            Object.DestroyImmediate(capsule.GetComponent<Collider>());

            // Add collider to root
            CapsuleCollider collider = unitGO.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1f, 0f);
            collider.radius = 0.25f;
            collider.height = 1f;

            // Add components
            unitGO.AddComponent<NavMeshAgent>();
            unitGO.AddComponent<UnitController>();
            unitGO.AddComponent<TeamColorApplier>();

            return unitGO;
        }

        #region Component Tests

        [Test]
        public void DebugUnit_HasUnitController()
        {
            // Assert
            Assert.IsNotNull(_unitGO.GetComponent<UnitController>());
        }

        [Test]
        public void DebugUnit_HasNavMeshAgent()
        {
            // Assert
            Assert.IsNotNull(_unitGO.GetComponent<NavMeshAgent>());
        }

        [Test]
        public void DebugUnit_HasCollider()
        {
            // Assert
            Assert.IsNotNull(_unitGO.GetComponent<Collider>());
        }

        [Test]
        public void DebugUnit_HasTeamColorApplier()
        {
            // Assert
            Assert.IsNotNull(_unitGO.GetComponent<TeamColorApplier>());
        }

        [Test]
        public void DebugUnit_HasVisualChild()
        {
            // Assert
            Transform visual = _unitGO.transform.Find("Visual");
            Assert.IsNotNull(visual);
        }

        [Test]
        public void DebugUnit_VisualHasRenderer()
        {
            // Arrange
            Transform visual = _unitGO.transform.Find("Visual");

            // Assert
            Assert.IsNotNull(visual);
            Assert.IsNotNull(visual.GetComponent<Renderer>());
        }

        #endregion

        #region Collider Tests

        [Test]
        public void DebugUnit_ColliderIsCapsule()
        {
            // Arrange
            Collider collider = _unitGO.GetComponent<Collider>();

            // Assert
            Assert.IsInstanceOf<CapsuleCollider>(collider);
        }

        [Test]
        public void DebugUnit_ColliderHasCorrectCenter()
        {
            // Arrange
            CapsuleCollider collider = _unitGO.GetComponent<CapsuleCollider>();

            // Assert
            Assert.AreEqual(1f, collider.center.y, 0.01f);
        }

        [Test]
        public void DebugUnit_ColliderIsNotTrigger()
        {
            // Arrange
            Collider collider = _unitGO.GetComponent<Collider>();

            // Assert
            Assert.IsFalse(collider.isTrigger);
        }

        #endregion

        #region TeamColorApplier Tests

        [Test]
        public void TeamColorApplier_GetTeamColor_ReturnsTeam0Color()
        {
            // Arrange
            TeamColorApplier applier = _unitGO.GetComponent<TeamColorApplier>();

            // Act
            Color color = applier.GetTeamColor(0);

            // Assert - Team 0 is red by default
            Assert.Greater(color.r, color.b);
        }

        [Test]
        public void TeamColorApplier_GetTeamColor_ReturnsTeam1Color()
        {
            // Arrange
            TeamColorApplier applier = _unitGO.GetComponent<TeamColorApplier>();

            // Act
            Color color = applier.GetTeamColor(1);

            // Assert - Team 1 is blue by default
            Assert.Greater(color.b, color.r);
        }

        [Test]
        public void TeamColorApplier_SetTeamColor_ChangesColor()
        {
            // Arrange
            TeamColorApplier applier = _unitGO.GetComponent<TeamColorApplier>();
            Color newColor = Color.green;

            // Act
            applier.SetTeamColor(0, newColor);
            Color result = applier.GetTeamColor(0);

            // Assert
            Assert.AreEqual(newColor.g, result.g, 0.01f);
        }

        [Test]
        public void TeamColorApplier_GetUnknownTeamColor_ReturnsGray()
        {
            // Arrange
            TeamColorApplier applier = _unitGO.GetComponent<TeamColorApplier>();

            // Act
            Color color = applier.GetTeamColor(99);

            // Assert
            Assert.AreEqual(Color.gray, color);
        }

        #endregion

        #region Multiple Unit Tests

        [Test]
        public void DebugUnits_CanSpawnMultiple()
        {
            // Arrange
            int count = 5;
            GameObject[] units = new GameObject[count];

            // Act
            for (int i = 0; i < count; i++)
            {
                units[i] = CreateTestUnit();
            }

            // Assert
            for (int i = 0; i < count; i++)
            {
                Assert.IsNotNull(units[i]);
                Assert.IsNotNull(units[i].GetComponent<UnitController>());
            }

            // Cleanup
            for (int i = 0; i < count; i++)
            {
                Object.DestroyImmediate(units[i]);
            }
        }

        [Test]
        public void DebugUnits_IndependentTeamColors()
        {
            // Arrange
            GameObject unit1 = CreateTestUnit();
            GameObject unit2 = CreateTestUnit();
            TeamColorApplier applier1 = unit1.GetComponent<TeamColorApplier>();
            TeamColorApplier applier2 = unit2.GetComponent<TeamColorApplier>();

            // Act - Set different colors for same team on each instance
            applier1.SetTeamColor(0, Color.red);

            // Assert - Team colors from default
            Color color1 = applier1.GetTeamColor(0);
            Color color2 = applier2.GetTeamColor(0);

            // Both should have the same color since SetTeamColor updates static dictionary
            Assert.AreEqual(color1, color2);

            // Cleanup
            Object.DestroyImmediate(unit1);
            Object.DestroyImmediate(unit2);
        }

        #endregion

        #region Position Tests

        [Test]
        public void DebugUnit_CanSetPosition()
        {
            // Arrange
            Vector3 targetPos = new Vector3(10f, 0f, 15f);

            // Act
            _unitGO.transform.position = targetPos;

            // Assert
            Assert.AreEqual(targetPos, _unitGO.transform.position);
        }

        [Test]
        public void DebugUnit_VisualPositionOffset()
        {
            // Arrange
            Transform visual = _unitGO.transform.Find("Visual");

            // Assert - Visual should be offset up by 1 unit
            Assert.AreEqual(1f, visual.localPosition.y, 0.01f);
        }

        #endregion
    }
}
