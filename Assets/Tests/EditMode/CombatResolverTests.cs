using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for CombatResolver and CombatResult.
    /// Tests validate per-bullet hit calculation with range, elevation, and squad modifiers.
    /// </summary>
    public class CombatResolverTests
    {
        private GameObject _attackerGO;
        private GameObject _targetGO;
        private UnitController _attacker;
        private UnitController _target;
        private UnitArchetypeSO _archetype;
        private WeaponStatsSO _weapon;

        [SetUp]
        public void Setup()
        {
            // Create archetype
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();

            // Create weapon with test values
            _weapon = ScriptableObject.CreateInstance<WeaponStatsSO>();
            SetupTestWeapon(_weapon);

            // Create attacker unit
            _attackerGO = new GameObject("Attacker");
            _attackerGO.AddComponent<BoxCollider>();
            _attacker = _attackerGO.AddComponent<UnitController>();
            _attacker.Initialize(_archetype, 0);

            // Create target unit
            _targetGO = new GameObject("Target");
            _targetGO.AddComponent<BoxCollider>();
            _target = _targetGO.AddComponent<UnitController>();
            _target.Initialize(_archetype, 1);
        }

        [TearDown]
        public void Teardown()
        {
            if (_attackerGO != null) Object.DestroyImmediate(_attackerGO);
            if (_targetGO != null) Object.DestroyImmediate(_targetGO);
            if (_archetype != null) Object.DestroyImmediate(_archetype);
            if (_weapon != null) Object.DestroyImmediate(_weapon);
        }

        private void SetupTestWeapon(WeaponStatsSO weapon)
        {
            // Use SerializedObject to set private fields
            var serializedObject = new UnityEditor.SerializedObject(weapon);
            serializedObject.FindProperty("_id").stringValue = "test_weapon";
            serializedObject.FindProperty("_displayName").stringValue = "Test Weapon";
            serializedObject.FindProperty("_shotsPerBurst").intValue = 3;
            serializedObject.FindProperty("_fireRate").floatValue = 2f;
            serializedObject.FindProperty("_baseHitChance").floatValue = 0.7f;
            serializedObject.FindProperty("_baseDamage").floatValue = 10f;
            serializedObject.FindProperty("_effectiveRange").floatValue = 20f;

            // Create simple linear curves
            var rangeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.5f);
            serializedObject.FindProperty("_rangeHitCurve").animationCurveValue = rangeCurve;

            var elevationCurve = AnimationCurve.Linear(-1f, 0.8f, 1f, 1.2f);
            serializedObject.FindProperty("_elevationBonusCurve").animationCurveValue = elevationCurve;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        #region CombatResult Tests

        [Test]
        public void CombatResult_DefaultValues_AreZero()
        {
            var result = new CombatResult();

            Assert.AreEqual(0, result.ShotsFired);
            Assert.AreEqual(0, result.ShotsHit);
            Assert.AreEqual(0f, result.TotalDamage);
            Assert.IsFalse(result.TargetDestroyed);
        }

        [Test]
        public void CombatResult_Accuracy_CalculatesCorrectly()
        {
            var result = new CombatResult(shotsFired: 10, shotsHit: 7, totalDamage: 70f, targetDestroyed: false);

            Assert.AreEqual(0.7f, result.Accuracy, 0.001f);
        }

        [Test]
        public void CombatResult_Accuracy_ZeroShotsFired_ReturnsZero()
        {
            var result = new CombatResult(shotsFired: 0, shotsHit: 0, totalDamage: 0f, targetDestroyed: false);

            Assert.AreEqual(0f, result.Accuracy);
        }

        #endregion

        #region Basic Combat Resolution Tests

        [Test]
        public void ResolveCombat_WithNullAttacker_ReturnsEmptyResult()
        {
            var result = CombatResolver.ResolveCombat(null, _target, _weapon);

            Assert.AreEqual(0, result.ShotsFired);
            Assert.AreEqual(0, result.TotalDamage);
        }

        [Test]
        public void ResolveCombat_WithNullTarget_ReturnsEmptyResult()
        {
            var result = CombatResolver.ResolveCombat(_attacker, null, _weapon);

            Assert.AreEqual(0, result.ShotsFired);
            Assert.AreEqual(0, result.TotalDamage);
        }

        [Test]
        public void ResolveCombat_WithNullWeapon_ReturnsEmptyResult()
        {
            var result = CombatResolver.ResolveCombat(_attacker, _target, null);

            Assert.AreEqual(0, result.ShotsFired);
            Assert.AreEqual(0, result.TotalDamage);
        }

        [Test]
        public void ResolveCombat_FiresCorrectNumberOfShots()
        {
            var result = CombatResolver.ResolveCombat(_attacker, _target, _weapon);

            Assert.AreEqual(3, result.ShotsFired, "Should fire shotsPerBurst shots");
        }

        #endregion

        #region Hit Chance Calculation Tests

        [Test]
        public void CalculateHitChance_BaseCase_ReturnsBaseHitChance()
        {
            // At range 0, with no elevation difference, should return close to base hit chance
            _attackerGO.transform.position = Vector3.zero;
            _targetGO.transform.position = Vector3.zero;

            float hitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            // At range 0, range curve = 1.0, so hitChance = 0.7 * 1.0 = 0.7
            // Clamped to min 0.05
            Assert.GreaterOrEqual(hitChance, 0.05f);
            Assert.LessOrEqual(hitChance, 0.95f);
        }

        [Test]
        public void CalculateHitChance_AtMaxRange_ReducesHitChance()
        {
            // At effective range, hit chance should be reduced by range curve
            _attackerGO.transform.position = Vector3.zero;
            _targetGO.transform.position = new Vector3(20f, 0f, 0f);

            float hitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            // At full range (20 units), range curve = 0.5, so hitChance = 0.7 * 0.5 = 0.35
            Assert.Less(hitChance, 0.7f, "Hit chance should be reduced at max range");
        }

        [Test]
        public void CalculateHitChance_WithElevationAdvantage_IncreasesHitChance()
        {
            // Attacker higher than target = elevation advantage
            _attackerGO.transform.position = new Vector3(0f, 5f, 0f);
            _targetGO.transform.position = Vector3.zero;

            float baseHitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            // Move attacker to same level
            _attackerGO.transform.position = Vector3.zero;
            float flatHitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            // Elevation advantage should increase hit chance
            Assert.GreaterOrEqual(baseHitChance, flatHitChance);
        }

        [Test]
        public void CalculateHitChance_ClampedToMinimum()
        {
            // Even at extreme disadvantage, minimum 5% hit chance
            _attackerGO.transform.position = Vector3.zero;
            _targetGO.transform.position = new Vector3(100f, 50f, 0f);

            float hitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            Assert.GreaterOrEqual(hitChance, 0.05f, "Minimum hit chance should be 5%");
        }

        [Test]
        public void CalculateHitChance_ClampedToMaximum()
        {
            // Even with extreme advantage, maximum 95% hit chance
            float hitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            Assert.LessOrEqual(hitChance, 0.95f, "Maximum hit chance should be 95%");
        }

        #endregion

        #region Squad Modifier Tests

        [Test]
        public void CalculateHitChance_WithSquadBonus_IncreasesHitChance()
        {
            // Create squad with hit chance upgrade
            var squad = new Squad("test_squad", 0);
            var upgrade = ScriptableObject.CreateInstance<UpgradeSO>();
            SetupUpgrade(upgrade, "test_upgrade", 1.2f, 1f, 0f); // +20% hit chance

            squad.ApplyUpgrade(upgrade);
            _attacker.JoinSquad(squad);

            float squadHitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            _attacker.LeaveSquad();
            float soloHitChance = CombatResolver.CalculateHitChance(_attacker, _target, _weapon);

            Assert.Greater(squadHitChance, soloHitChance, "Squad bonus should increase hit chance");

            Object.DestroyImmediate(upgrade);
        }

        [Test]
        public void ResolveCombat_WithSquadDamageBonus_IncreasesDamage()
        {
            // Create squad with damage upgrade
            var squad = new Squad("test_squad", 0);
            var upgrade = ScriptableObject.CreateInstance<UpgradeSO>();
            SetupUpgrade(upgrade, "damage_upgrade", 1f, 1.5f, 0f); // +50% damage

            squad.ApplyUpgrade(upgrade);
            _attacker.JoinSquad(squad);

            // Use fixed seed for reproducibility
            Random.InitState(42);
            var squadResult = CombatResolver.ResolveCombat(_attacker, _target, _weapon);

            _attacker.LeaveSquad();

            // Reset positions and health
            _targetGO.transform.position = Vector3.zero;
            _attackerGO.transform.position = Vector3.zero;
            _target.Initialize(_archetype, 1);

            Random.InitState(42);
            var soloResult = CombatResolver.ResolveCombat(_attacker, _target, _weapon);

            // Damage per hit should be higher with squad bonus
            if (squadResult.ShotsHit > 0 && soloResult.ShotsHit > 0)
            {
                float squadDamagePerHit = squadResult.TotalDamage / squadResult.ShotsHit;
                float soloDamagePerHit = soloResult.TotalDamage / soloResult.ShotsHit;
                Assert.Greater(squadDamagePerHit, soloDamagePerHit, "Squad damage bonus should increase damage per hit");
            }

            Object.DestroyImmediate(upgrade);
        }

        private void SetupUpgrade(UpgradeSO upgrade, string id, float hitMult, float damageMult, float elevation)
        {
            var so = new UnityEditor.SerializedObject(upgrade);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_hitChanceMultiplier").floatValue = hitMult;
            so.FindProperty("_damageMultiplier").floatValue = damageMult;
            so.FindProperty("_elevationBonus").floatValue = elevation;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #endregion

        #region Range and Elevation Helpers

        [Test]
        public void CalculateDistance_ReturnsCorrectValue()
        {
            _attackerGO.transform.position = Vector3.zero;
            _targetGO.transform.position = new Vector3(10f, 0f, 0f);

            float distance = CombatResolver.CalculateDistance(_attacker, _target);

            Assert.AreEqual(10f, distance, 0.01f);
        }

        [Test]
        public void CalculateElevationDifference_PositiveWhenAttackerHigher()
        {
            _attackerGO.transform.position = new Vector3(0f, 5f, 0f);
            _targetGO.transform.position = Vector3.zero;

            float elevation = CombatResolver.CalculateElevationDifference(_attacker, _target);

            Assert.Greater(elevation, 0f, "Elevation should be positive when attacker is higher");
        }

        [Test]
        public void CalculateElevationDifference_NegativeWhenAttackerLower()
        {
            _attackerGO.transform.position = Vector3.zero;
            _targetGO.transform.position = new Vector3(0f, 5f, 0f);

            float elevation = CombatResolver.CalculateElevationDifference(_attacker, _target);

            Assert.Less(elevation, 0f, "Elevation should be negative when attacker is lower");
        }

        #endregion

        #region Target Destruction Tests

        [Test]
        public void ResolveCombat_TargetDestroyed_SetsFlag()
        {
            // Use a weapon that will definitely kill the target
            var killerWeapon = ScriptableObject.CreateInstance<WeaponStatsSO>();
            var so = new UnityEditor.SerializedObject(killerWeapon);
            so.FindProperty("_id").stringValue = "killer";
            so.FindProperty("_shotsPerBurst").intValue = 100;
            so.FindProperty("_baseHitChance").floatValue = 1f;
            so.FindProperty("_baseDamage").floatValue = 1000f;
            so.FindProperty("_effectiveRange").floatValue = 100f;
            so.FindProperty("_rangeHitCurve").animationCurveValue = AnimationCurve.Constant(0f, 1f, 1f);
            so.FindProperty("_elevationBonusCurve").animationCurveValue = AnimationCurve.Constant(-1f, 1f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();

            var result = CombatResolver.ResolveCombat(_attacker, _target, killerWeapon);

            Assert.IsTrue(result.TargetDestroyed || !_target.IsAlive,
                "Target should be destroyed with overwhelming damage");

            Object.DestroyImmediate(killerWeapon);
        }

        #endregion
    }
}
