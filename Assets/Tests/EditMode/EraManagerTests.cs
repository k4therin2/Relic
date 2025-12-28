using NUnit.Framework;
using UnityEngine;
using Relic.Core;
using Relic.Data;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for EraManager singleton.
    /// Tests era selection, cycling, and event handling.
    /// </summary>
    [TestFixture]
    public class EraManagerTests
    {
        private GameObject _managerObject;
        private EraManager _eraManager;
        private List<EraConfigSO> _testEras;

        [SetUp]
        public void SetUp()
        {
            // Create test eras
            _testEras = CreateTestEras();

            // Create EraManager instance
            _managerObject = new GameObject("TestEraManager");
            _eraManager = _managerObject.AddComponent<EraManager>();

            // Register test eras
            foreach (var era in _testEras)
            {
                _eraManager.RegisterEra(era);
            }
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up
            _eraManager.ClearEras();

            if (_managerObject != null)
            {
                Object.DestroyImmediate(_managerObject);
            }

            foreach (var era in _testEras)
            {
                if (era != null)
                {
                    Object.DestroyImmediate(era);
                }
            }
            _testEras.Clear();
        }

        #region Basic Functionality Tests

        [Test]
        public void SetEra_ValidEra_SetsCurrentEra()
        {
            // Act
            bool result = _eraManager.SetEra(_testEras[0]);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(_testEras[0], _eraManager.CurrentEra);
        }

        [Test]
        public void SetEra_ById_SetsCurrentEra()
        {
            // Act
            bool result = _eraManager.SetEra("ancient");

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual("ancient", _eraManager.CurrentEra.Id);
        }

        [Test]
        public void SetEra_NullEra_ReturnsFalse()
        {
            // Act
            bool result = _eraManager.SetEra((EraConfigSO)null);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void SetEra_UnregisteredEra_ReturnsFalse()
        {
            // Arrange
            var unregisteredEra = CreateEra("unregistered", "Unregistered");

            // Act
            bool result = _eraManager.SetEra(unregisteredEra);

            // Assert
            Assert.IsFalse(result);

            // Cleanup
            Object.DestroyImmediate(unregisteredEra);
        }

        [Test]
        public void SetEra_InvalidId_ReturnsFalse()
        {
            // Act
            bool result = _eraManager.SetEra("nonexistent");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void SetEra_SameEra_ReturnsTrue()
        {
            // Arrange
            _eraManager.SetEra(_testEras[0]);

            // Act
            bool result = _eraManager.SetEra(_testEras[0]);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(_testEras[0], _eraManager.CurrentEra);
        }

        #endregion

        #region Era Cycling Tests

        [Test]
        public void CycleNext_CyclesToNextEra()
        {
            // Arrange
            _eraManager.SetEra(_testEras[0]);

            // Act
            var result = _eraManager.CycleNext();

            // Assert
            Assert.AreEqual(_testEras[1], result);
            Assert.AreEqual(_testEras[1], _eraManager.CurrentEra);
        }

        [Test]
        public void CycleNext_AtEnd_WrapsToFirst()
        {
            // Arrange
            _eraManager.SetEra(_testEras[_testEras.Count - 1]);

            // Act
            var result = _eraManager.CycleNext();

            // Assert
            Assert.AreEqual(_testEras[0], result);
        }

        [Test]
        public void CyclePrevious_CyclesToPreviousEra()
        {
            // Arrange
            _eraManager.SetEra(_testEras[1]);

            // Act
            var result = _eraManager.CyclePrevious();

            // Assert
            Assert.AreEqual(_testEras[0], result);
            Assert.AreEqual(_testEras[0], _eraManager.CurrentEra);
        }

        [Test]
        public void CyclePrevious_AtStart_WrapsToLast()
        {
            // Arrange
            _eraManager.SetEra(_testEras[0]);

            // Act
            var result = _eraManager.CyclePrevious();

            // Assert
            Assert.AreEqual(_testEras[_testEras.Count - 1], result);
        }

        #endregion

        #region Lookup Tests

        [Test]
        public void GetEra_ExistingId_ReturnsEra()
        {
            // Act
            var result = _eraManager.GetEra("medieval");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("medieval", result.Id);
        }

        [Test]
        public void GetEra_NonExistingId_ReturnsNull()
        {
            // Act
            var result = _eraManager.GetEra("future_plus");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void HasEra_ExistingId_ReturnsTrue()
        {
            // Act & Assert
            Assert.IsTrue(_eraManager.HasEra("ancient"));
            Assert.IsTrue(_eraManager.HasEra("medieval"));
        }

        [Test]
        public void HasEra_NonExistingId_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_eraManager.HasEra("steampunk"));
        }

        [Test]
        public void HasEra_NullOrEmpty_ReturnsFalse()
        {
            // Act & Assert
            Assert.IsFalse(_eraManager.HasEra(null));
            Assert.IsFalse(_eraManager.HasEra(""));
        }

        #endregion

        #region Properties Tests

        [Test]
        public void AvailableEras_ReturnsAllRegisteredEras()
        {
            // Act
            var result = _eraManager.AvailableEras;

            // Assert
            Assert.AreEqual(_testEras.Count, result.Count);
        }

        [Test]
        public void EraCount_ReturnsCorrectCount()
        {
            // Assert
            Assert.AreEqual(_testEras.Count, _eraManager.EraCount);
        }

        #endregion

        #region Event Tests

        [Test]
        public void SetEra_FiresOnEraChangedEvent()
        {
            // Arrange
            EraConfigSO capturedOldEra = null;
            EraConfigSO capturedNewEra = null;
            _eraManager.OnEraChanged += (oldEra, newEra) =>
            {
                capturedOldEra = oldEra;
                capturedNewEra = newEra;
            };

            _eraManager.SetEra(_testEras[0]);

            // Act
            _eraManager.SetEra(_testEras[1]);

            // Assert
            Assert.AreEqual(_testEras[0], capturedOldEra);
            Assert.AreEqual(_testEras[1], capturedNewEra);
        }

        [Test]
        public void SetEra_SameEra_DoesNotFireEvent()
        {
            // Arrange
            _eraManager.SetEra(_testEras[0]);
            int eventCount = 0;
            _eraManager.OnEraChanged += (_, _) => eventCount++;

            // Act
            _eraManager.SetEra(_testEras[0]);

            // Assert
            Assert.AreEqual(0, eventCount);
        }

        [Test]
        public void SetEra_FiresOnEraAppliedEvent()
        {
            // Arrange
            EraConfigSO capturedEra = null;
            _eraManager.OnEraApplied += era => capturedEra = era;

            // Act
            _eraManager.SetEra(_testEras[0]);

            // Assert
            Assert.AreEqual(_testEras[0], capturedEra);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates test era configurations.
        /// </summary>
        private List<EraConfigSO> CreateTestEras()
        {
            return new List<EraConfigSO>
            {
                CreateEra("ancient", "Ancient Era"),
                CreateEra("medieval", "Medieval Era"),
                CreateEra("wwii", "World War II"),
                CreateEra("future", "Future Era")
            };
        }

        /// <summary>
        /// Creates a single test era.
        /// </summary>
        private EraConfigSO CreateEra(string id, string displayName)
        {
            var era = ScriptableObject.CreateInstance<EraConfigSO>();
            SetPrivateField(era, "_id", id);
            SetPrivateField(era, "_displayName", displayName);

            // Add a minimal archetype to pass validation
            var archetypes = new List<UnitArchetypeReference>
            {
                CreateArchetypeRef("unit1", "Test Unit", 100)
            };
            SetPrivateField(era, "_unitArchetypes", archetypes);

            return era;
        }

        /// <summary>
        /// Creates a UnitArchetypeReference for testing.
        /// </summary>
        private UnitArchetypeReference CreateArchetypeRef(string id, string displayName, int cost)
        {
            var archetype = new UnitArchetypeReference();
            SetPrivateField(archetype, "_id", id);
            SetPrivateField(archetype, "_displayName", displayName);
            SetPrivateField(archetype, "_cost", cost);
            return archetype;
        }

        /// <summary>
        /// Sets a private field value using reflection.
        /// </summary>
        private void SetPrivateField<T>(object obj, string fieldName, T value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(obj, value);
            }
        }

        #endregion
    }
}
