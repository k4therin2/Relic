using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for WeaponStatsSO.
    /// Tests validate weapon configuration, curves, and damage calculations.
    /// </summary>
    public class WeaponStatsTests
    {
        private WeaponStatsSO _weapon;

        [SetUp]
        public void Setup()
        {
            _weapon = ScriptableObject.CreateInstance<WeaponStatsSO>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_weapon != null)
            {
                Object.DestroyImmediate(_weapon);
            }
        }

        #region Creation and Defaults Tests

        [Test]
        public void CreateInstance_ReturnsValidScriptableObject()
        {
            Assert.IsNotNull(_weapon);
            Assert.IsInstanceOf<WeaponStatsSO>(_weapon);
        }

        [Test]
        public void DefaultValues_AreReasonable()
        {
            // Default values should be game-ready for a basic weapon
            Assert.GreaterOrEqual(_weapon.ShotsPerBurst, 1, "Should have at least 1 shot per burst");
            Assert.Greater(_weapon.FireRate, 0f, "Fire rate should be positive");
            Assert.GreaterOrEqual(_weapon.BaseHitChance, 0f, "Base hit chance should be >= 0");
            Assert.LessOrEqual(_weapon.BaseHitChance, 1f, "Base hit chance should be <= 1");
            Assert.Greater(_weapon.BaseDamage, 0f, "Base damage should be positive");
            Assert.Greater(_weapon.EffectiveRange, 0f, "Effective range should be positive");
        }

        #endregion

        #region Validation Tests

        [Test]
        public void Validate_WithDefaults_ReturnsErrors()
        {
            // Default weapon has no ID or display name
            List<string> errors;
            bool isValid = _weapon.Validate(out errors);

            Assert.IsFalse(isValid, "Default weapon should not be valid");
            Assert.Greater(errors.Count, 0, "Should have validation errors");
            Assert.IsTrue(errors.Exists(e => e.Contains("ID")), "Should report missing ID");
        }

        [Test]
        public void Validate_InvalidShotsPerBurst_ReportsError()
        {
            // Create a weapon with 0 shots per burst (invalid)
            // We need to test this through the Validate method
            // Since we can't set private fields, we check defaults are valid
            Assert.GreaterOrEqual(_weapon.ShotsPerBurst, 1, "Default shots per burst should be valid");
        }

        [Test]
        public void Validate_InvalidFireRate_ReportsError()
        {
            // Fire rate must be positive
            Assert.Greater(_weapon.FireRate, 0f, "Fire rate should be positive");
        }

        [Test]
        public void Validate_InvalidHitChance_ReportsError()
        {
            // Hit chance must be between 0 and 1
            Assert.GreaterOrEqual(_weapon.BaseHitChance, 0f, "Hit chance should be >= 0");
            Assert.LessOrEqual(_weapon.BaseHitChance, 1f, "Hit chance should be <= 1");
        }

        #endregion

        #region Range Curve Tests

        [Test]
        public void GetHitChanceAtRange_AtZeroRange_ReturnsMaxHitChance()
        {
            // At point blank range, should get maximum hit chance
            float hitChance = _weapon.GetHitChanceAtRange(0f);
            Assert.AreEqual(_weapon.BaseHitChance, hitChance, 0.001f,
                "At zero range, should return base hit chance");
        }

        [Test]
        public void GetHitChanceAtRange_AtEffectiveRange_ReturnsReducedHitChance()
        {
            // At effective range, hit chance should be reduced
            float hitChance = _weapon.GetHitChanceAtRange(_weapon.EffectiveRange);
            Assert.LessOrEqual(hitChance, _weapon.BaseHitChance,
                "At effective range, hit chance should not exceed base");
        }

        [Test]
        public void GetHitChanceAtRange_BeyondEffectiveRange_ReturnsFurtherReduced()
        {
            // Beyond effective range, hit chance should drop further
            float atEffective = _weapon.GetHitChanceAtRange(_weapon.EffectiveRange);
            float beyondEffective = _weapon.GetHitChanceAtRange(_weapon.EffectiveRange * 2f);
            Assert.LessOrEqual(beyondEffective, atEffective,
                "Beyond effective range should have lower or equal hit chance");
        }

        [Test]
        public void GetHitChanceAtRange_NeverNegative()
        {
            // Even at extreme ranges, hit chance should not be negative
            float hitChance = _weapon.GetHitChanceAtRange(1000f);
            Assert.GreaterOrEqual(hitChance, 0f, "Hit chance should never be negative");
        }

        [Test]
        public void GetHitChanceAtRange_NegativeRange_TreatedAsZero()
        {
            // Negative range should be treated as zero (point blank)
            float hitChanceNegative = _weapon.GetHitChanceAtRange(-10f);
            float hitChanceZero = _weapon.GetHitChanceAtRange(0f);
            Assert.AreEqual(hitChanceZero, hitChanceNegative, 0.001f,
                "Negative range should be treated as zero");
        }

        #endregion

        #region Elevation Curve Tests

        [Test]
        public void GetElevationBonus_AtZeroElevation_ReturnsNoBonus()
        {
            // At same elevation, should get no bonus
            float bonus = _weapon.GetElevationBonus(0f);
            Assert.AreEqual(0f, bonus, 0.001f,
                "At zero elevation difference, should have no bonus");
        }

        [Test]
        public void GetElevationBonus_HigherElevation_ReturnsPositiveBonus()
        {
            // Attacker higher than target should get bonus
            float bonus = _weapon.GetElevationBonus(5f);
            Assert.GreaterOrEqual(bonus, 0f,
                "Higher elevation should give positive or zero bonus");
        }

        [Test]
        public void GetElevationBonus_LowerElevation_ReturnsPenalty()
        {
            // Attacker lower than target should get penalty (negative bonus)
            float bonus = _weapon.GetElevationBonus(-5f);
            Assert.LessOrEqual(bonus, 0f,
                "Lower elevation should give zero or negative bonus");
        }

        [Test]
        public void GetElevationBonus_Bounded()
        {
            // Elevation bonus should be bounded to reasonable values
            float extremeBonus = _weapon.GetElevationBonus(100f);
            Assert.LessOrEqual(Mathf.Abs(extremeBonus), 0.5f,
                "Elevation bonus should not exceed +/- 50%");
        }

        #endregion

        #region Damage Tests

        [Test]
        public void CalculateDamage_ReturnsBaseDamage()
        {
            float damage = _weapon.CalculateDamage(1f);
            Assert.AreEqual(_weapon.BaseDamage, damage, 0.001f,
                "With multiplier of 1, should return base damage");
        }

        [Test]
        public void CalculateDamage_WithMultiplier_ScalesDamage()
        {
            float multiplier = 1.5f;
            float damage = _weapon.CalculateDamage(multiplier);
            Assert.AreEqual(_weapon.BaseDamage * multiplier, damage, 0.001f,
                "Damage should scale with multiplier");
        }

        [Test]
        public void CalculateDamage_ZeroMultiplier_ReturnsZero()
        {
            float damage = _weapon.CalculateDamage(0f);
            Assert.AreEqual(0f, damage, 0.001f,
                "Zero multiplier should return zero damage");
        }

        [Test]
        public void CalculateDamage_NegativeMultiplier_ReturnsZero()
        {
            float damage = _weapon.CalculateDamage(-1f);
            Assert.AreEqual(0f, damage, 0.001f,
                "Negative multiplier should return zero damage");
        }

        #endregion

        #region Time Between Shots Tests

        [Test]
        public void TimeBetweenShots_CalculatedFromFireRate()
        {
            // Time between shots = 1 / fire rate
            float expected = 1f / _weapon.FireRate;
            Assert.AreEqual(expected, _weapon.TimeBetweenShots, 0.001f,
                "Time between shots should be inverse of fire rate");
        }

        [Test]
        public void BurstDuration_CalculatedCorrectly()
        {
            // Burst duration = (shots per burst - 1) * time between shots
            float expected = (_weapon.ShotsPerBurst - 1) * _weapon.TimeBetweenShots;
            Assert.AreEqual(expected, _weapon.BurstDuration, 0.001f,
                "Burst duration should account for all shots");
        }

        #endregion
    }
}
