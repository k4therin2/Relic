using NUnit.Framework;
using UnityEngine;
using Relic.ARLayer;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for BattlefieldPlacer component.
    /// Note: Full AR functionality requires PlayMode tests on device.
    /// </summary>
    [TestFixture]
    public class BattlefieldPlacerTests
    {
        private GameObject placerGameObject;
        private BattlefieldPlacer placer;

        [SetUp]
        public void Setup()
        {
            placerGameObject = new GameObject("TestPlacer");
            placer = placerGameObject.AddComponent<BattlefieldPlacer>();
        }

        [TearDown]
        public void TearDown()
        {
            if (placerGameObject != null)
            {
                Object.DestroyImmediate(placerGameObject);
            }
        }

        [Test]
        public void InitialState_IsDetecting()
        {
            Assert.AreEqual(PlacementState.Detecting, placer.CurrentState);
        }

        [Test]
        public void IsPlaced_InitiallyFalse()
        {
            Assert.IsFalse(placer.IsPlaced);
        }

        [Test]
        public void PlacedBattlefield_InitiallyNull()
        {
            Assert.IsNull(placer.PlacedBattlefield);
        }

        [Test]
        public void BattlefieldScale_DefaultValue()
        {
            // Default scale should be reasonable for tabletop AR
            Assert.Greater(placer.BattlefieldScale, 0f);
            Assert.LessOrEqual(placer.BattlefieldScale, 2f);
        }

        [Test]
        public void BattlefieldScale_CanBeSet()
        {
            placer.BattlefieldScale = 0.75f;
            Assert.AreEqual(0.75f, placer.BattlefieldScale);
        }

        [Test]
        public void BattlefieldScale_ClampedToMinimum()
        {
            placer.BattlefieldScale = 0.001f;
            Assert.GreaterOrEqual(placer.BattlefieldScale, 0.1f);
        }

        [Test]
        public void BattlefieldSize_HasDefaultValue()
        {
            Assert.Greater(placer.BattlefieldSize.x, 0f);
            Assert.Greater(placer.BattlefieldSize.y, 0f);
        }

        [Test]
        public void BattlefieldSize_CanBeSet()
        {
            var newSize = new Vector2(1.5f, 1f);
            placer.BattlefieldSize = newSize;
            Assert.AreEqual(1.5f, placer.BattlefieldSize.x);
            Assert.AreEqual(1f, placer.BattlefieldSize.y);
        }

        [Test]
        public void BattlefieldSize_ClampedToMinimum()
        {
            placer.BattlefieldSize = new Vector2(-1f, 0f);
            Assert.GreaterOrEqual(placer.BattlefieldSize.x, 0.1f);
            Assert.GreaterOrEqual(placer.BattlefieldSize.y, 0.1f);
        }

        [Test]
        public void IsAreaSufficientForPlacement_ReturnsFalseForTinyArea()
        {
            bool sufficient = placer.IsAreaSufficientForPlacement(0.01f);
            Assert.IsFalse(sufficient);
        }

        [Test]
        public void IsAreaSufficientForPlacement_ReturnsTrueForLargeArea()
        {
            bool sufficient = placer.IsAreaSufficientForPlacement(10f);
            Assert.IsTrue(sufficient);
        }

        [Test]
        public void PlaceBattlefieldAtPosition_CreatesBattlefield()
        {
            var position = new Vector3(1f, 0f, 1f);
            var rotation = Quaternion.identity;

            placer.PlaceBattlefieldAtPosition(position, rotation);

            Assert.IsNotNull(placer.PlacedBattlefield);
            Assert.AreEqual(position, placer.PlacedBattlefield.transform.position);
        }

        [Test]
        public void PlaceBattlefieldAtPosition_SetsConfirmingState()
        {
            placer.PlaceBattlefieldAtPosition(Vector3.zero, Quaternion.identity);

            Assert.AreEqual(PlacementState.Confirming, placer.CurrentState);
        }

        [Test]
        public void ConfirmPlacement_SetsPlacedState()
        {
            // First place the battlefield
            placer.PlaceBattlefieldAtPosition(Vector3.zero, Quaternion.identity);

            // Then confirm
            placer.ConfirmPlacement();

            Assert.AreEqual(PlacementState.Placed, placer.CurrentState);
            Assert.IsTrue(placer.IsPlaced);
        }

        [Test]
        public void CancelPlacement_ResetsToDetecting()
        {
            // First place the battlefield
            placer.PlaceBattlefieldAtPosition(Vector3.zero, Quaternion.identity);

            // Then cancel
            placer.CancelPlacement();

            Assert.AreEqual(PlacementState.Detecting, placer.CurrentState);
            Assert.IsNull(placer.PlacedBattlefield);
        }

        [Test]
        public void Reset_ClearsPlacedBattlefield()
        {
            // Place and confirm
            placer.PlaceBattlefieldAtPosition(Vector3.zero, Quaternion.identity);
            placer.ConfirmPlacement();

            // Reset
            placer.Reset();

            Assert.AreEqual(PlacementState.Detecting, placer.CurrentState);
            Assert.IsNull(placer.PlacedBattlefield);
            Assert.IsFalse(placer.IsPlaced);
        }

        [Test]
        public void OnStateChanged_FiresWhenStateChanges()
        {
            PlacementState capturedState = PlacementState.Detecting;
            bool eventFired = false;

            placer.OnStateChanged += (state) =>
            {
                eventFired = true;
                capturedState = state;
            };

            placer.PlaceBattlefieldAtPosition(Vector3.zero, Quaternion.identity);

            Assert.IsTrue(eventFired);
            Assert.AreEqual(PlacementState.Confirming, capturedState);
        }

        [Test]
        public void OnPlacementConfirmed_FiresWithCorrectPosition()
        {
            var expectedPosition = new Vector3(1f, 0.5f, 2f);
            Vector3 capturedPosition = Vector3.zero;
            bool eventFired = false;

            placer.OnPlacementConfirmed += (pos, rot) =>
            {
                eventFired = true;
                capturedPosition = pos;
            };

            placer.PlaceBattlefieldAtPosition(expectedPosition, Quaternion.identity);
            placer.ConfirmPlacement();

            Assert.IsTrue(eventFired);
            Assert.AreEqual(expectedPosition, capturedPosition);
        }

        [Test]
        public void OnPlacementCancelled_FiresOnCancel()
        {
            bool eventFired = false;
            placer.OnPlacementCancelled += () => eventFired = true;

            placer.PlaceBattlefieldAtPosition(Vector3.zero, Quaternion.identity);
            placer.CancelPlacement();

            Assert.IsTrue(eventFired);
        }

        [Test]
        public void TryPlaceBattlefield_ReturnsFalseWhenAlreadyPlaced()
        {
            placer.PlaceBattlefieldAtPosition(Vector3.zero, Quaternion.identity);
            placer.ConfirmPlacement();

            bool result = placer.TryPlaceBattlefield();

            Assert.IsFalse(result);
        }
    }
}
