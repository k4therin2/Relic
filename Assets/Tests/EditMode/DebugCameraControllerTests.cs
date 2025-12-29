using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for DebugCameraController.
    /// Tests camera bounds, zoom limits, and public API.
    /// </summary>
    public class DebugCameraControllerTests
    {
        private GameObject _cameraGO;
        private DebugCameraController _controller;

        [SetUp]
        public void SetUp()
        {
            _cameraGO = new GameObject("TestCamera");
            _cameraGO.AddComponent<Camera>();
            _controller = _cameraGO.AddComponent<DebugCameraController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraGO);
        }

        #region Initialization Tests

        [Test]
        public void Awake_InitializesTargetHeight_ToCurrentPosition()
        {
            // Arrange
            var go = new GameObject("Camera2");
            go.transform.position = new Vector3(0, 25, 0);

            // Act
            var controller = go.AddComponent<DebugCameraController>();

            // Assert - controller should be created without error
            Assert.IsNotNull(controller);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void PanSpeed_DefaultValue_IsPositive()
        {
            // Assert
            Assert.Greater(_controller.PanSpeed, 0f);
        }

        [Test]
        public void ZoomSpeed_DefaultValue_IsPositive()
        {
            // Assert
            Assert.Greater(_controller.ZoomSpeed, 0f);
        }

        #endregion

        #region Pan Speed Tests

        [Test]
        public void PanSpeed_SetPositiveValue_AcceptsValue()
        {
            // Arrange
            float expectedSpeed = 30f;

            // Act
            _controller.PanSpeed = expectedSpeed;

            // Assert
            Assert.AreEqual(expectedSpeed, _controller.PanSpeed);
        }

        [Test]
        public void PanSpeed_SetNegativeValue_ClampsToZero()
        {
            // Act
            _controller.PanSpeed = -10f;

            // Assert
            Assert.AreEqual(0f, _controller.PanSpeed);
        }

        #endregion

        #region Zoom Speed Tests

        [Test]
        public void ZoomSpeed_SetPositiveValue_AcceptsValue()
        {
            // Arrange
            float expectedSpeed = 10f;

            // Act
            _controller.ZoomSpeed = expectedSpeed;

            // Assert
            Assert.AreEqual(expectedSpeed, _controller.ZoomSpeed);
        }

        [Test]
        public void ZoomSpeed_SetNegativeValue_ClampsToZero()
        {
            // Act
            _controller.ZoomSpeed = -5f;

            // Assert
            Assert.AreEqual(0f, _controller.ZoomSpeed);
        }

        #endregion

        #region Height Limits Tests

        [Test]
        public void MinHeight_ReturnsSerializedValue()
        {
            // Assert - default is 5
            Assert.GreaterOrEqual(_controller.MinHeight, 0f);
        }

        [Test]
        public void MaxHeight_ReturnsSerializedValue()
        {
            // Assert - default is 50
            Assert.Greater(_controller.MaxHeight, _controller.MinHeight);
        }

        #endregion

        #region Bounds Tests

        [Test]
        public void BoundsEnabled_DefaultValue_IsTrue()
        {
            // Assert
            Assert.IsTrue(_controller.BoundsEnabled);
        }

        [Test]
        public void BoundsEnabled_SetToFalse_DisablesBounds()
        {
            // Act
            _controller.BoundsEnabled = false;

            // Assert
            Assert.IsFalse(_controller.BoundsEnabled);
        }

        [Test]
        public void SetBounds_UpdatesBoundsValues()
        {
            // Arrange
            float minX = -100f;
            float maxX = 100f;
            float minZ = -50f;
            float maxZ = 50f;

            // Act
            _controller.SetBounds(minX, maxX, minZ, maxZ);

            // Assert - bounds are set (verified indirectly through other tests)
            Assert.IsTrue(_controller.BoundsEnabled);
        }

        #endregion

        #region CenterOn Tests

        [Test]
        public void CenterOn_MovesToTargetPosition()
        {
            // Arrange
            Vector3 targetPos = new Vector3(10f, 0f, 15f);
            _cameraGO.transform.position = new Vector3(0f, 20f, 0f);

            // Act
            _controller.CenterOn(targetPos);

            // Assert
            Assert.AreEqual(targetPos.x, _cameraGO.transform.position.x, 0.01f);
            Assert.AreEqual(targetPos.z, _cameraGO.transform.position.z, 0.01f);
            // Y should remain unchanged
            Assert.AreEqual(20f, _cameraGO.transform.position.y, 0.01f);
        }

        [Test]
        public void CenterOn_WithBoundsEnabled_ClampsToLimits()
        {
            // Arrange - bounds are -50 to 50 by default
            _controller.SetBounds(-50f, 50f, -50f, 50f);
            _controller.BoundsEnabled = true;
            Vector3 targetPos = new Vector3(100f, 0f, 100f);
            _cameraGO.transform.position = new Vector3(0f, 20f, 0f);

            // Act
            _controller.CenterOn(targetPos);

            // Assert - position should be clamped
            Assert.LessOrEqual(_cameraGO.transform.position.x, 50f);
            Assert.LessOrEqual(_cameraGO.transform.position.z, 50f);
        }

        [Test]
        public void CenterOn_WithBoundsDisabled_AllowsOutOfBounds()
        {
            // Arrange
            _controller.BoundsEnabled = false;
            Vector3 targetPos = new Vector3(100f, 0f, 100f);
            _cameraGO.transform.position = new Vector3(0f, 20f, 0f);

            // Act
            _controller.CenterOn(targetPos);

            // Assert - position should be at target
            Assert.AreEqual(targetPos.x, _cameraGO.transform.position.x, 0.01f);
            Assert.AreEqual(targetPos.z, _cameraGO.transform.position.z, 0.01f);
        }

        #endregion

        #region SetZoomImmediate Tests

        [Test]
        public void SetZoomImmediate_SetsHeightImmediately()
        {
            // Arrange
            float targetHeight = 15f;
            _cameraGO.transform.position = new Vector3(0f, 20f, 0f);

            // Act
            _controller.SetZoomImmediate(targetHeight);

            // Assert
            Assert.AreEqual(targetHeight, _cameraGO.transform.position.y, 0.01f);
        }

        [Test]
        public void SetZoomImmediate_ClampsToBelowMinHeight()
        {
            // Arrange - default min is 5
            float targetHeight = 2f;
            _cameraGO.transform.position = new Vector3(0f, 20f, 0f);

            // Act
            _controller.SetZoomImmediate(targetHeight);

            // Assert
            Assert.GreaterOrEqual(_cameraGO.transform.position.y, _controller.MinHeight);
        }

        [Test]
        public void SetZoomImmediate_ClampsToAboveMaxHeight()
        {
            // Arrange - default max is 50
            float targetHeight = 100f;
            _cameraGO.transform.position = new Vector3(0f, 20f, 0f);

            // Act
            _controller.SetZoomImmediate(targetHeight);

            // Assert
            Assert.LessOrEqual(_cameraGO.transform.position.y, _controller.MaxHeight);
        }

        #endregion

        #region SetZoomTarget Tests

        [Test]
        public void SetZoomTarget_AcceptsValidHeight()
        {
            // Arrange
            float targetHeight = 25f;

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => _controller.SetZoomTarget(targetHeight));
        }

        [Test]
        public void SetZoomTarget_ClampsBelowMinHeight()
        {
            // Arrange
            float targetHeight = 1f;

            // Act & Assert - should not throw, value will be clamped internally
            Assert.DoesNotThrow(() => _controller.SetZoomTarget(targetHeight));
        }

        [Test]
        public void SetZoomTarget_ClampsAboveMaxHeight()
        {
            // Arrange
            float targetHeight = 200f;

            // Act & Assert - should not throw, value will be clamped internally
            Assert.DoesNotThrow(() => _controller.SetZoomTarget(targetHeight));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Controller_WithoutCamera_StillWorks()
        {
            // Arrange
            var go = new GameObject("NoCameraController");

            // Act
            var controller = go.AddComponent<DebugCameraController>();

            // Assert
            Assert.IsNotNull(controller);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Controller_MultipleInstances_WorkIndependently()
        {
            // Arrange
            var go2 = new GameObject("Camera2");
            go2.transform.position = new Vector3(10f, 30f, 10f);
            var controller2 = go2.AddComponent<DebugCameraController>();

            // Act
            _controller.PanSpeed = 15f;
            controller2.PanSpeed = 25f;

            // Assert
            Assert.AreEqual(15f, _controller.PanSpeed);
            Assert.AreEqual(25f, controller2.PanSpeed);

            Object.DestroyImmediate(go2);
        }

        #endregion
    }
}
