using NUnit.Framework;
using UnityEngine;
using Relic.ARLayer;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for BattlefieldScaleController.
    /// </summary>
    [TestFixture]
    public class BattlefieldScaleControllerTests
    {
        private GameObject controllerGameObject;
        private BattlefieldScaleController controller;

        [SetUp]
        public void Setup()
        {
            controllerGameObject = new GameObject("TestScaleController");
            controller = controllerGameObject.AddComponent<BattlefieldScaleController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (controllerGameObject != null)
            {
                Object.DestroyImmediate(controllerGameObject);
            }
        }

        [Test]
        public void CurrentScale_HasDefaultValue()
        {
            Assert.Greater(controller.CurrentScale, 0f);
        }

        [Test]
        public void CurrentScale_CanBeSet()
        {
            controller.CurrentScale = 0.75f;
            Assert.AreEqual(0.75f, controller.CurrentScale);
        }

        [Test]
        public void CurrentScale_ClampedToMinimum()
        {
            controller.CurrentScale = 0.001f;
            Assert.GreaterOrEqual(controller.CurrentScale, controller.MinScale);
        }

        [Test]
        public void CurrentScale_ClampedToMaximum()
        {
            controller.CurrentScale = 100f;
            Assert.LessOrEqual(controller.CurrentScale, controller.MaxScale);
        }

        [Test]
        public void CurrentWorldSize_ScalesWithCurrentScale()
        {
            float initialScale = controller.CurrentScale;
            var initialSize = controller.CurrentWorldSize;

            controller.CurrentScale = initialScale * 2f;
            var doubledSize = controller.CurrentWorldSize;

            Assert.AreEqual(initialSize.x * 2f, doubledSize.x, 0.001f);
            Assert.AreEqual(initialSize.y * 2f, doubledSize.y, 0.001f);
        }

        [Test]
        public void SetSmall_AppliesSmallPreset()
        {
            controller.SetSmall();
            Assert.AreEqual(BattlefieldScaleController.ScalePresets.Small, controller.CurrentScale);
        }

        [Test]
        public void SetMedium_AppliesMediumPreset()
        {
            controller.SetMedium();
            Assert.AreEqual(BattlefieldScaleController.ScalePresets.Medium, controller.CurrentScale);
        }

        [Test]
        public void SetLarge_AppliesLargePreset()
        {
            controller.SetLarge();
            Assert.AreEqual(BattlefieldScaleController.ScalePresets.Large, controller.CurrentScale);
        }

        [Test]
        public void SetExtraLarge_AppliesExtraLargePreset()
        {
            controller.SetExtraLarge();
            Assert.AreEqual(BattlefieldScaleController.ScalePresets.ExtraLarge, controller.CurrentScale);
        }

        [Test]
        public void ScaleUp_IncreasesScale()
        {
            float initialScale = controller.CurrentScale;
            controller.ScaleUp();
            Assert.Greater(controller.CurrentScale, initialScale);
        }

        [Test]
        public void ScaleDown_DecreasesScale()
        {
            controller.CurrentScale = 1.0f; // Start at a known value
            float initialScale = controller.CurrentScale;
            controller.ScaleDown();
            Assert.Less(controller.CurrentScale, initialScale);
        }

        [Test]
        public void ScaleBy_MultipliesScale()
        {
            controller.CurrentScale = 0.5f;
            controller.ScaleBy(2f);
            Assert.AreEqual(1.0f, controller.CurrentScale, 0.001f);
        }

        [Test]
        public void ScaleBy_RespectsBounds()
        {
            controller.CurrentScale = controller.MaxScale;
            controller.ScaleBy(2f);
            Assert.AreEqual(controller.MaxScale, controller.CurrentScale);
        }

        [Test]
        public void ResetScale_RestoresToDefault()
        {
            float defaultScale = controller.CurrentScale;
            controller.CurrentScale = 1.5f;
            controller.ResetScale();
            Assert.AreEqual(defaultScale, controller.CurrentScale);
        }

        [Test]
        public void GetWorldSizeForScale_ReturnsCorrectSize()
        {
            var sizeAtOne = controller.GetWorldSizeForScale(1.0f);
            var sizeAtHalf = controller.GetWorldSizeForScale(0.5f);

            Assert.AreEqual(sizeAtOne.x * 0.5f, sizeAtHalf.x, 0.001f);
            Assert.AreEqual(sizeAtOne.y * 0.5f, sizeAtHalf.y, 0.001f);
        }

        [Test]
        public void GetScaleForWorldSize_CalculatesCorrectScale()
        {
            var targetSize = new Vector2(1f, 0.6f);
            float scale = controller.GetScaleForWorldSize(targetSize);

            // Scale should produce approximately the target size
            var resultSize = controller.GetWorldSizeForScale(scale);
            Assert.LessOrEqual(resultSize.x, targetSize.x + 0.01f);
            Assert.LessOrEqual(resultSize.y, targetSize.y + 0.01f);
        }

        [Test]
        public void GetScaleToFitPlane_FitsWithPadding()
        {
            float planeWidth = 1.0f;
            float planeDepth = 0.8f;
            float padding = 0.1f;

            float scale = controller.GetScaleToFitPlane(planeWidth, planeDepth, padding);
            var battlefieldSize = controller.GetWorldSizeForScale(scale);

            // Battlefield should fit within plane minus padding
            Assert.LessOrEqual(battlefieldSize.x, planeWidth - (padding * 2));
            Assert.LessOrEqual(battlefieldSize.y, planeDepth - (padding * 2));
        }

        [Test]
        public void OnScaleChanged_FiresWhenScaleChanges()
        {
            float capturedScale = 0f;
            Vector2 capturedSize = Vector2.zero;
            bool eventFired = false;

            controller.OnScaleChanged += (scale, size) =>
            {
                eventFired = true;
                capturedScale = scale;
                capturedSize = size;
            };

            controller.CurrentScale = 0.75f;

            Assert.IsTrue(eventFired);
            Assert.AreEqual(0.75f, capturedScale);
            Assert.Greater(capturedSize.x, 0f);
        }

        [Test]
        public void OnScaleChanged_DoesNotFireWhenScaleUnchanged()
        {
            controller.CurrentScale = 0.5f;
            int eventCount = 0;

            controller.OnScaleChanged += (scale, size) => eventCount++;

            controller.CurrentScale = 0.5f; // Same value

            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void ScalePresets_AreOrderedBySize()
        {
            Assert.Less(BattlefieldScaleController.ScalePresets.Small,
                       BattlefieldScaleController.ScalePresets.Medium);
            Assert.Less(BattlefieldScaleController.ScalePresets.Medium,
                       BattlefieldScaleController.ScalePresets.Large);
            Assert.Less(BattlefieldScaleController.ScalePresets.Large,
                       BattlefieldScaleController.ScalePresets.ExtraLarge);
        }

        [Test]
        public void ScalePresets_ArePositive()
        {
            Assert.Greater(BattlefieldScaleController.ScalePresets.Small, 0f);
            Assert.Greater(BattlefieldScaleController.ScalePresets.Medium, 0f);
            Assert.Greater(BattlefieldScaleController.ScalePresets.Large, 0f);
            Assert.Greater(BattlefieldScaleController.ScalePresets.ExtraLarge, 0f);
        }
    }
}
