using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for UnitArchetypeSO and UnitStats.
    /// </summary>
    public class UnitArchetypeTests
    {
        private UnitArchetypeSO _archetype;

        [SetUp]
        public void Setup()
        {
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_archetype != null)
            {
                Object.DestroyImmediate(_archetype);
            }
        }

        #region UnitArchetypeSO Tests

        [Test]
        public void CreateInstance_ReturnsValidScriptableObject()
        {
            Assert.IsNotNull(_archetype);
            Assert.IsInstanceOf<UnitArchetypeSO>(_archetype);
        }

        [Test]
        public void Validate_WithDefaults_ReturnsErrors()
        {
            // Default archetype has no ID, no display name, no prefab
            List<string> errors;
            bool isValid = _archetype.Validate(out errors);

            Assert.IsFalse(isValid, "Default archetype should not be valid");
            Assert.Greater(errors.Count, 0, "Should have validation errors");
            Assert.IsTrue(errors.Exists(e => e.Contains("ID")), "Should report missing ID");
            Assert.IsTrue(errors.Exists(e => e.Contains("display name")), "Should report missing display name");
            Assert.IsTrue(errors.Exists(e => e.Contains("prefab")), "Should report missing prefab");
        }

        [Test]
        public void CreateStats_ReturnsStatsWithArchetypeValues()
        {
            // We can't set private fields directly, so we test with defaults
            UnitStats stats = _archetype.CreateStats();

            Assert.AreEqual(_archetype.MaxHealth, stats.MaxHealth);
            Assert.AreEqual(_archetype.MaxHealth, stats.CurrentHealth);
            Assert.AreEqual(_archetype.MoveSpeed, stats.MoveSpeed);
            Assert.AreEqual(_archetype.DetectionRange, stats.DetectionRange);
            Assert.AreEqual(_archetype.Armor, stats.Armor);
        }

        [Test]
        public void DefaultValues_AreReasonable()
        {
            // Default values should be game-ready
            Assert.AreEqual(100, _archetype.MaxHealth);
            Assert.AreEqual(3f, _archetype.MoveSpeed);
            Assert.AreEqual(10f, _archetype.DetectionRange);
            Assert.AreEqual(0, _archetype.Armor);
            Assert.AreEqual(1f, _archetype.Scale);
        }

        #endregion

        #region UnitStats Tests

        [Test]
        public void UnitStats_HealthPercent_CalculatesCorrectly()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 50
            };

            Assert.AreEqual(0.5f, stats.HealthPercent, 0.001f);
        }

        [Test]
        public void UnitStats_HealthPercent_WithZeroMaxHealth_ReturnsZero()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 0,
                CurrentHealth = 0
            };

            Assert.AreEqual(0f, stats.HealthPercent);
        }

        [Test]
        public void UnitStats_IsAlive_TrueWhenHealthPositive()
        {
            UnitStats stats = new UnitStats { CurrentHealth = 1 };
            Assert.IsTrue(stats.IsAlive);
        }

        [Test]
        public void UnitStats_IsAlive_FalseWhenHealthZero()
        {
            UnitStats stats = new UnitStats { CurrentHealth = 0 };
            Assert.IsFalse(stats.IsAlive);
        }

        [Test]
        public void UnitStats_ApplyDamage_ReducesHealth()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                Armor = 0
            };

            int actualDamage = stats.ApplyDamage(30);

            Assert.AreEqual(30, actualDamage);
            Assert.AreEqual(70, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_ApplyDamage_WithArmor_ReducesDamage()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                Armor = 50 // 50% damage reduction
            };

            int actualDamage = stats.ApplyDamage(100);

            // With 50% armor, damage should be reduced to ~50
            Assert.AreEqual(50, actualDamage);
            Assert.AreEqual(50, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_ApplyDamage_AlwaysDoesAtLeastOneDamage()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                Armor = 99 // 99% damage reduction
            };

            int actualDamage = stats.ApplyDamage(1);

            Assert.GreaterOrEqual(actualDamage, 1, "Should do at least 1 damage");
        }

        [Test]
        public void UnitStats_ApplyDamage_CannotGoNegative()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 10,
                Armor = 0
            };

            stats.ApplyDamage(50);

            Assert.AreEqual(0, stats.CurrentHealth, "Health should not go below 0");
        }

        [Test]
        public void UnitStats_ApplyDamage_ZeroDamage_ReturnsZero()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                Armor = 0
            };

            int actualDamage = stats.ApplyDamage(0);

            Assert.AreEqual(0, actualDamage);
            Assert.AreEqual(100, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_ApplyDamage_NegativeDamage_ReturnsZero()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 100,
                Armor = 0
            };

            int actualDamage = stats.ApplyDamage(-10);

            Assert.AreEqual(0, actualDamage);
            Assert.AreEqual(100, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_Heal_RestoresHealth()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 50
            };

            int actualHeal = stats.Heal(30);

            Assert.AreEqual(30, actualHeal);
            Assert.AreEqual(80, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_Heal_CannotExceedMaxHealth()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 90
            };

            int actualHeal = stats.Heal(50);

            Assert.AreEqual(10, actualHeal, "Should only heal to max");
            Assert.AreEqual(100, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_Heal_AtFullHealth_ReturnsZero()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 100
            };

            int actualHeal = stats.Heal(10);

            Assert.AreEqual(0, actualHeal);
            Assert.AreEqual(100, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_Heal_ZeroAmount_ReturnsZero()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 50
            };

            int actualHeal = stats.Heal(0);

            Assert.AreEqual(0, actualHeal);
            Assert.AreEqual(50, stats.CurrentHealth);
        }

        [Test]
        public void UnitStats_Heal_NegativeAmount_ReturnsZero()
        {
            UnitStats stats = new UnitStats
            {
                MaxHealth = 100,
                CurrentHealth = 50
            };

            int actualHeal = stats.Heal(-10);

            Assert.AreEqual(0, actualHeal);
            Assert.AreEqual(50, stats.CurrentHealth);
        }

        #endregion
    }
}
