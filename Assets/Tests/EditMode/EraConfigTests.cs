using NUnit.Framework;
using UnityEngine;
using Relic.Data;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for EraConfigSO ScriptableObject.
    /// These tests verify validation logic, property access, and data integrity.
    /// </summary>
    [TestFixture]
    public class EraConfigTests
    {
        private EraConfigSO _eraConfig;

        [SetUp]
        public void SetUp()
        {
            // Create a test era config
            _eraConfig = ScriptableObject.CreateInstance<EraConfigSO>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_eraConfig != null)
            {
                Object.DestroyImmediate(_eraConfig);
            }
        }

        #region Validation Tests

        [Test]
        public void Validate_EmptyConfig_ReturnsErrors()
        {
            // Act
            bool isValid = _eraConfig.Validate(out var errors);

            // Assert
            Assert.IsFalse(isValid, "Empty config should be invalid");
            Assert.IsTrue(errors.Count > 0, "Should have validation errors");
            Assert.IsTrue(errors.Exists(e => e.Contains("ID")), "Should require ID");
        }

        [Test]
        public void Validate_MissingDisplayName_ReturnsError()
        {
            // Arrange - Set ID but not display name via reflection
            SetPrivateField(_eraConfig, "_id", "test_era");

            // Act
            bool isValid = _eraConfig.Validate(out var errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("Display name")));
        }

        [Test]
        public void Validate_NoArchetypes_ReturnsError()
        {
            // Arrange
            SetPrivateField(_eraConfig, "_id", "test_era");
            SetPrivateField(_eraConfig, "_displayName", "Test Era");

            // Act
            bool isValid = _eraConfig.Validate(out var errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("archetype")));
        }

        [Test]
        public void Validate_ValidConfig_ReturnsTrue()
        {
            // Arrange
            SetPrivateField(_eraConfig, "_id", "test_era");
            SetPrivateField(_eraConfig, "_displayName", "Test Era");

            var archetypes = new List<UnitArchetypeReference>
            {
                CreateArchetypeRef("unit1", "Test Unit", 100)
            };
            SetPrivateField(_eraConfig, "_unitArchetypes", archetypes);

            // Act
            bool isValid = _eraConfig.Validate(out var errors);

            // Assert
            Assert.IsTrue(isValid, $"Valid config should pass. Errors: {string.Join(", ", errors)}");
            Assert.AreEqual(0, errors.Count);
        }

        [Test]
        public void Validate_NegativeResources_ReturnsError()
        {
            // Arrange
            SetPrivateField(_eraConfig, "_id", "test_era");
            SetPrivateField(_eraConfig, "_displayName", "Test Era");
            SetPrivateField(_eraConfig, "_startingResources", -100);

            var archetypes = new List<UnitArchetypeReference>
            {
                CreateArchetypeRef("unit1", "Test Unit", 100)
            };
            SetPrivateField(_eraConfig, "_unitArchetypes", archetypes);

            // Act
            bool isValid = _eraConfig.Validate(out var errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("negative")));
        }

        [Test]
        public void Validate_MaxResourcesLessThanStarting_ReturnsError()
        {
            // Arrange
            SetPrivateField(_eraConfig, "_id", "test_era");
            SetPrivateField(_eraConfig, "_displayName", "Test Era");
            SetPrivateField(_eraConfig, "_startingResources", 2000);
            SetPrivateField(_eraConfig, "_maxResources", 1000);

            var archetypes = new List<UnitArchetypeReference>
            {
                CreateArchetypeRef("unit1", "Test Unit", 100)
            };
            SetPrivateField(_eraConfig, "_unitArchetypes", archetypes);

            // Act
            bool isValid = _eraConfig.Validate(out var errors);

            // Assert
            Assert.IsFalse(isValid);
            Assert.IsTrue(errors.Exists(e => e.Contains("Max resources")));
        }

        #endregion

        #region Property Access Tests

        [Test]
        public void Properties_ReturnCorrectValues()
        {
            // Arrange
            SetPrivateField(_eraConfig, "_id", "medieval");
            SetPrivateField(_eraConfig, "_displayName", "Medieval Era");
            SetPrivateField(_eraConfig, "_description", "Test description");
            SetPrivateField(_eraConfig, "_primaryColor", Color.red);
            SetPrivateField(_eraConfig, "_startingResources", 1500);

            // Assert
            Assert.AreEqual("medieval", _eraConfig.Id);
            Assert.AreEqual("Medieval Era", _eraConfig.DisplayName);
            Assert.AreEqual("Test description", _eraConfig.Description);
            Assert.AreEqual(Color.red, _eraConfig.PrimaryColor);
            Assert.AreEqual(1500, _eraConfig.StartingResources);
        }

        [Test]
        public void UnitArchetypes_ReturnsReadOnlyList()
        {
            // Arrange
            var archetypes = new List<UnitArchetypeReference>
            {
                CreateArchetypeRef("unit1", "Unit 1", 100),
                CreateArchetypeRef("unit2", "Unit 2", 200)
            };
            SetPrivateField(_eraConfig, "_unitArchetypes", archetypes);

            // Act
            var result = _eraConfig.UnitArchetypes;

            // Assert
            Assert.AreEqual(2, result.Count);
            Assert.IsInstanceOf<IReadOnlyList<UnitArchetypeReference>>(result);
        }

        #endregion

        #region Lookup Tests

        [Test]
        public void GetArchetype_ExistingId_ReturnsArchetype()
        {
            // Arrange
            var archetypes = new List<UnitArchetypeReference>
            {
                CreateArchetypeRef("spearman", "Spearman", 50),
                CreateArchetypeRef("archer", "Archer", 75)
            };
            SetPrivateField(_eraConfig, "_unitArchetypes", archetypes);

            // Act
            var result = _eraConfig.GetArchetype("spearman");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("spearman", result.Id);
            Assert.AreEqual("Spearman", result.DisplayName);
        }

        [Test]
        public void GetArchetype_NonExistingId_ReturnsNull()
        {
            // Arrange
            var archetypes = new List<UnitArchetypeReference>
            {
                CreateArchetypeRef("spearman", "Spearman", 50)
            };
            SetPrivateField(_eraConfig, "_unitArchetypes", archetypes);

            // Act
            var result = _eraConfig.GetArchetype("knight");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetArchetype_NullId_ReturnsNull()
        {
            // Act
            var result = _eraConfig.GetArchetype(null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetArchetype_EmptyId_ReturnsNull()
        {
            // Act
            var result = _eraConfig.GetArchetype("");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public void GetUpgrade_ExistingId_ReturnsUpgrade()
        {
            // Arrange
            var upgrades = new List<UpgradeReference>
            {
                CreateUpgradeRef("veteran", "Veteran Training", 150),
                CreateUpgradeRef("armor", "Heavy Armor", 200)
            };
            SetPrivateField(_eraConfig, "_availableUpgrades", upgrades);

            // Act
            var result = _eraConfig.GetUpgrade("veteran");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("veteran", result.Id);
        }

        [Test]
        public void GetUpgrade_NonExistingId_ReturnsNull()
        {
            // Arrange
            var upgrades = new List<UpgradeReference>
            {
                CreateUpgradeRef("veteran", "Veteran Training", 150)
            };
            SetPrivateField(_eraConfig, "_availableUpgrades", upgrades);

            // Act
            var result = _eraConfig.GetUpgrade("shield");

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region Helper Methods

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
            else
            {
                Assert.Fail($"Field {fieldName} not found");
            }
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
        /// Creates an UpgradeReference for testing.
        /// </summary>
        private UpgradeReference CreateUpgradeRef(string id, string displayName, int cost)
        {
            var upgrade = new UpgradeReference();
            SetPrivateField(upgrade, "_id", id);
            SetPrivateField(upgrade, "_displayName", displayName);
            SetPrivateField(upgrade, "_cost", cost);
            return upgrade;
        }

        #endregion
    }
}
