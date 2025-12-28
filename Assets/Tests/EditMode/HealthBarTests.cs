using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for HealthBar component - unit health visualization.
    /// TDD tests written first per WP-EXT-5.1.
    /// </summary>
    public class HealthBarTests
    {
        private GameObject _unitGameObject;
        private UnitController _unitController;
        private HealthBar _healthBar;
        private UnitArchetypeSO _archetype;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void Setup()
        {
            _createdObjects = new List<GameObject>();

            // Create unit
            _unitGameObject = new GameObject("TestUnit");
            _createdObjects.Add(_unitGameObject);
            _unitGameObject.AddComponent<BoxCollider>();

            // Create archetype with specific health for testing
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            _archetype.name = "TestArchetype";

            _unitController = _unitGameObject.AddComponent<UnitController>();
            _unitController.Initialize(_archetype, 0);

            // Add health bar
            _healthBar = _unitGameObject.AddComponent<HealthBar>();
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

        #region Basic Tests

        [Test]
        public void HealthBar_CanBeCreated()
        {
            Assert.IsNotNull(_healthBar);
        }

        [Test]
        public void HealthBar_HasVisualGameObject()
        {
            _healthBar.Initialize();

            Assert.IsNotNull(_healthBar.BarVisual);
        }

        #endregion

        #region Health Display Tests

        [Test]
        public void HealthBar_ReflectsUnitHealthPercent()
        {
            _healthBar.Initialize();

            float healthPercent = _unitController.HealthPercent;
            float barFillAmount = _healthBar.FillAmount;

            Assert.AreEqual(healthPercent, barFillAmount, 0.01f);
        }

        [Test]
        public void HealthBar_UpdatesWhenUnitTakesDamage()
        {
            _healthBar.Initialize();
            float initialFill = _healthBar.FillAmount;

            _unitController.TakeDamage(10);
            _healthBar.UpdateHealthDisplay();

            Assert.Less(_healthBar.FillAmount, initialFill);
        }

        [Test]
        public void HealthBar_AtFullHealth_IsFullyFilled()
        {
            _healthBar.Initialize();

            // At full health, bar should be full
            Assert.AreEqual(1f, _healthBar.FillAmount, 0.01f);
        }

        [Test]
        public void HealthBar_WhenDead_IsEmpty()
        {
            _healthBar.Initialize();

            // Kill the unit
            _unitController.TakeDamage(10000);
            _healthBar.UpdateHealthDisplay();

            Assert.AreEqual(0f, _healthBar.FillAmount, 0.01f);
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void HealthBar_WhenFullHealth_CanBeHidden()
        {
            _healthBar.Initialize();
            _healthBar.SetHideWhenFull(true);

            // At full health, should be hidden
            Assert.IsFalse(_healthBar.IsVisible);
        }

        [Test]
        public void HealthBar_WhenDamaged_IsVisible()
        {
            _healthBar.Initialize();
            _healthBar.SetHideWhenFull(true);

            _unitController.TakeDamage(10);
            _healthBar.UpdateHealthDisplay();

            Assert.IsTrue(_healthBar.IsVisible);
        }

        [Test]
        public void HealthBar_AlwaysShow_ShowsAtFullHealth()
        {
            _healthBar.Initialize();
            _healthBar.SetHideWhenFull(false);

            Assert.IsTrue(_healthBar.IsVisible);
        }

        [Test]
        public void Show_MakesHealthBarVisible()
        {
            _healthBar.Initialize();
            _healthBar.Hide();

            _healthBar.Show();

            Assert.IsTrue(_healthBar.IsVisible);
        }

        [Test]
        public void Hide_MakesHealthBarInvisible()
        {
            _healthBar.Initialize();

            _healthBar.Hide();

            Assert.IsFalse(_healthBar.IsVisible);
        }

        #endregion

        #region Color Tests

        [Test]
        public void HealthBar_AtHighHealth_IsGreenish()
        {
            _healthBar.Initialize();

            Color barColor = _healthBar.BarColor;

            // High health = green
            Assert.Greater(barColor.g, barColor.r);
        }

        [Test]
        public void HealthBar_AtLowHealth_IsReddish()
        {
            _healthBar.Initialize();

            // Damage to get to low health (< 30%)
            _unitController.TakeDamage(_unitController.Stats.MaxHealth * 8 / 10);
            _healthBar.UpdateHealthDisplay();

            Color barColor = _healthBar.BarColor;

            // Low health = red
            Assert.Greater(barColor.r, barColor.g);
        }

        [Test]
        public void HealthBar_AtMidHealth_IsYellowish()
        {
            _healthBar.Initialize();

            // Damage to get to mid health (~50%)
            _unitController.TakeDamage(_unitController.Stats.MaxHealth / 2);
            _healthBar.UpdateHealthDisplay();

            Color barColor = _healthBar.BarColor;

            // Mid health = yellow (high red and green, low blue)
            Assert.Greater(barColor.r, barColor.b);
            Assert.Greater(barColor.g, barColor.b);
        }

        #endregion

        #region Positioning Tests

        [Test]
        public void HealthBar_PositionedAboveUnit()
        {
            _healthBar.Initialize();

            Vector3 unitPosition = _unitController.transform.position;
            Vector3 barPosition = _healthBar.BarVisual.transform.position;

            Assert.Greater(barPosition.y, unitPosition.y);
        }

        [Test]
        public void HealthBar_FollowsUnit()
        {
            _healthBar.Initialize();

            // Move unit
            var newPosition = new Vector3(5f, 0f, 5f);
            _unitController.transform.position = newPosition;
            _healthBar.LateUpdate(); // Simulate LateUpdate

            Vector3 barPosition = _healthBar.BarVisual.transform.position;

            Assert.AreEqual(newPosition.x, barPosition.x, 0.01f);
            Assert.AreEqual(newPosition.z, barPosition.z, 0.01f);
        }

        [Test]
        public void HealthBar_UsesHealthBarPointIfSet()
        {
            // Create a specific health bar point
            var pointGO = new GameObject("HealthBarPoint");
            _createdObjects.Add(pointGO);
            pointGO.transform.SetParent(_unitController.transform);
            pointGO.transform.localPosition = new Vector3(0, 2f, 0);

            // Re-initialize with point set
            _healthBar.Initialize();

            Vector3 expectedY = _unitController.HealthBarPoint.position.y;
            Vector3 barPosition = _healthBar.BarVisual.transform.position;

            // Bar should be near the health bar point
            Assert.AreEqual(expectedY, barPosition.y, 0.5f);
        }

        #endregion

        #region Billboard Tests

        [Test]
        public void HealthBar_FacesCamera()
        {
            _healthBar.Initialize();
            _healthBar.SetBillboard(true);

            // Create a camera
            var cameraGO = new GameObject("TestCamera");
            _createdObjects.Add(cameraGO);
            var camera = cameraGO.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 5f, -10f);
            camera.transform.LookAt(Vector3.zero);

            // Billboard update should face camera
            _healthBar.UpdateBillboard(camera);

            // Bar forward should point roughly toward camera
            Vector3 toCamera = (camera.transform.position - _healthBar.BarVisual.transform.position).normalized;
            float dot = Vector3.Dot(_healthBar.BarVisual.transform.forward, toCamera);

            // Should be facing camera (dot product close to -1 or 1 depending on orientation)
            Assert.Greater(Mathf.Abs(dot), 0.7f);
        }

        #endregion

        #region Event Subscription Tests

        [Test]
        public void HealthBar_AutoUpdatesOnHealthChange()
        {
            _healthBar.Initialize();
            _healthBar.SetAutoUpdate(true);

            float initialFill = _healthBar.FillAmount;

            // Damage triggers OnHealthChanged event
            _unitController.TakeDamage(20);

            // Should auto-update (simulated via event)
            Assert.Less(_healthBar.FillAmount, initialFill);
        }

        #endregion
    }
}
