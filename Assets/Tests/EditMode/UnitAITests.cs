using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for UnitAI component.
    /// Tests validate AI state machine transitions and behavior.
    /// </summary>
    public class UnitAITests
    {
        private GameObject _aiUnitGO;
        private GameObject _enemyGO;
        private UnitController _aiUnit;
        private UnitController _enemy;
        private UnitAI _unitAI;
        private UnitArchetypeSO _archetype;
        private WeaponStatsSO _weapon;

        [SetUp]
        public void Setup()
        {
            // Create archetype
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();

            // Create weapon
            _weapon = ScriptableObject.CreateInstance<WeaponStatsSO>();
            SetupTestWeapon(_weapon);

            // Create AI-controlled unit (team 0)
            _aiUnitGO = new GameObject("AIUnit");
            _aiUnitGO.AddComponent<BoxCollider>();
            _aiUnit = _aiUnitGO.AddComponent<UnitController>();
            // Force UnitController Awake (for NavMeshAgent)
            ForceAwake(_aiUnit);
            _aiUnit.Initialize(_archetype, 0);
            _unitAI = _aiUnitGO.AddComponent<UnitAI>();
            // Force UnitAI Awake to initialize _unitController reference
            ForceAwake(_unitAI);

            // Create enemy unit (team 1)
            _enemyGO = new GameObject("Enemy");
            _enemyGO.AddComponent<BoxCollider>();
            _enemy = _enemyGO.AddComponent<UnitController>();
            // Force UnitController Awake
            ForceAwake(_enemy);
            _enemy.Initialize(_archetype, 1);
        }

        /// <summary>
        /// Forces Awake to be called on a MonoBehaviour in EditMode tests.
        /// Unity doesn't always call lifecycle methods automatically in EditMode.
        /// </summary>
        private void ForceAwake(MonoBehaviour component)
        {
            var method = component.GetType().GetMethod("Awake",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public);
            method?.Invoke(component, null);
        }

        [TearDown]
        public void Teardown()
        {
            if (_aiUnitGO != null) Object.DestroyImmediate(_aiUnitGO);
            if (_enemyGO != null) Object.DestroyImmediate(_enemyGO);
            if (_archetype != null) Object.DestroyImmediate(_archetype);
            if (_weapon != null) Object.DestroyImmediate(_weapon);
        }

        private void SetupTestWeapon(WeaponStatsSO weapon)
        {
            var so = new UnityEditor.SerializedObject(weapon);
            so.FindProperty("_id").stringValue = "test_weapon";
            so.FindProperty("_shotsPerBurst").intValue = 3;
            so.FindProperty("_fireRate").floatValue = 2f;
            so.FindProperty("_baseHitChance").floatValue = 0.7f;
            so.FindProperty("_baseDamage").floatValue = 10f;
            so.FindProperty("_effectiveRange").floatValue = 20f;
            so.FindProperty("_rangeHitCurve").animationCurveValue = AnimationCurve.Constant(0f, 1f, 1f);
            so.FindProperty("_elevationBonusCurve").animationCurveValue = AnimationCurve.Constant(-1f, 1f, 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        #region State Machine Tests

        [Test]
        public void InitialState_IsIdle()
        {
            Assert.AreEqual(AIState.Idle, _unitAI.CurrentState);
        }

        [Test]
        public void SetState_ChangesCurrentState()
        {
            _unitAI.SetState(AIState.Moving);
            Assert.AreEqual(AIState.Moving, _unitAI.CurrentState);
        }

        [Test]
        public void SetState_SameState_DoesNotTriggerEvent()
        {
            int stateChanges = 0;
            _unitAI.OnStateChanged += (state) => stateChanges++;

            _unitAI.SetState(AIState.Idle); // Already idle

            Assert.AreEqual(0, stateChanges, "Should not fire event for same state");
        }

        [Test]
        public void SetState_DifferentState_TriggersEvent()
        {
            AIState newState = AIState.Idle;
            _unitAI.OnStateChanged += (state) => newState = state;

            _unitAI.SetState(AIState.Attacking);

            Assert.AreEqual(AIState.Attacking, newState);
        }

        #endregion

        #region Detection Tests

        [Test]
        public void DetectionRadius_DefaultsToPositive()
        {
            Assert.Greater(_unitAI.DetectionRadius, 0f);
        }

        [Test]
        public void DetectionRadius_CanBeSet()
        {
            _unitAI.DetectionRadius = 25f;
            Assert.AreEqual(25f, _unitAI.DetectionRadius);
        }

        [Test]
        public void IsEnemyInRange_WithEnemyInRange_ReturnsTrue()
        {
            _aiUnitGO.transform.position = Vector3.zero;
            _enemyGO.transform.position = new Vector3(5f, 0f, 0f);
            _unitAI.DetectionRadius = 10f;

            bool inRange = _unitAI.IsEnemyInRange(_enemy);

            Assert.IsTrue(inRange);
        }

        [Test]
        public void IsEnemyInRange_WithEnemyOutOfRange_ReturnsFalse()
        {
            _aiUnitGO.transform.position = Vector3.zero;
            _enemyGO.transform.position = new Vector3(50f, 0f, 0f);
            _unitAI.DetectionRadius = 10f;

            bool inRange = _unitAI.IsEnemyInRange(_enemy);

            Assert.IsFalse(inRange);
        }

        [Test]
        public void IsEnemyInRange_WithFriendlyUnit_ReturnsFalse()
        {
            // Create friendly unit (same team)
            var friendlyGO = new GameObject("Friendly");
            friendlyGO.AddComponent<BoxCollider>();
            var friendly = friendlyGO.AddComponent<UnitController>();
            friendly.Initialize(_archetype, 0); // Same team as AI unit

            _aiUnitGO.transform.position = Vector3.zero;
            friendlyGO.transform.position = new Vector3(5f, 0f, 0f);
            _unitAI.DetectionRadius = 10f;

            bool inRange = _unitAI.IsEnemyInRange(friendly);

            Assert.IsFalse(inRange, "Should not consider friendly as enemy");

            Object.DestroyImmediate(friendlyGO);
        }

        #endregion

        #region Target Management Tests

        [Test]
        public void CurrentTarget_InitiallyNull()
        {
            Assert.IsNull(_unitAI.CurrentTarget);
        }

        [Test]
        public void SetTarget_SetsCurrentTarget()
        {
            _unitAI.SetTarget(_enemy);

            Assert.AreEqual(_enemy, _unitAI.CurrentTarget);
        }

        [Test]
        public void SetTarget_WithEnemy_SwitchesToAttacking()
        {
            _unitAI.SetTarget(_enemy);

            Assert.AreEqual(AIState.Attacking, _unitAI.CurrentState);
        }

        [Test]
        public void ClearTarget_SetsTargetToNull()
        {
            _unitAI.SetTarget(_enemy);
            _unitAI.ClearTarget();

            Assert.IsNull(_unitAI.CurrentTarget);
        }

        [Test]
        public void ClearTarget_SwitchesToIdle()
        {
            _unitAI.SetTarget(_enemy);
            _unitAI.ClearTarget();

            Assert.AreEqual(AIState.Idle, _unitAI.CurrentState);
        }

        [Test]
        public void HasTarget_WithTarget_ReturnsTrue()
        {
            _unitAI.SetTarget(_enemy);

            Assert.IsTrue(_unitAI.HasTarget);
        }

        [Test]
        public void HasTarget_WithoutTarget_ReturnsFalse()
        {
            Assert.IsFalse(_unitAI.HasTarget);
        }

        #endregion

        #region Movement State Tests

        [Test]
        public void MoveTo_WithoutNavMesh_RemainsIdle()
        {
            // In EditMode tests, there's no NavMesh, so MoveTo should not change state
            // because UnitController.MoveTo fails and state should remain Idle
            _unitAI.MoveTo(new Vector3(10f, 0f, 10f));

            Assert.AreEqual(AIState.Idle, _unitAI.CurrentState, "State should remain Idle without NavMesh");
        }

        [Test]
        public void Stop_FromIdle_RemainsIdle()
        {
            // Stop from Idle state should keep the unit in Idle
            _unitAI.Stop();

            Assert.AreEqual(AIState.Idle, _unitAI.CurrentState);
        }

        [Test]
        public void Stop_ClearsTarget()
        {
            // Setting a target and then stopping should clear the target
            // (after Stop, AI goes to Idle and target is cleared)
            _unitAI.SetTarget(_enemy);
            Assert.AreEqual(AIState.Attacking, _unitAI.CurrentState);

            _unitAI.Stop();

            Assert.IsNull(_unitAI.CurrentTarget);
            Assert.AreEqual(AIState.Idle, _unitAI.CurrentState);
        }

        #endregion

        #region Enabled/Disabled Tests

        [Test]
        public void IsAIEnabled_DefaultsToTrue()
        {
            Assert.IsTrue(_unitAI.IsAIEnabled);
        }

        [Test]
        public void SetAIEnabled_False_DisablesAI()
        {
            _unitAI.SetAIEnabled(false);

            Assert.IsFalse(_unitAI.IsAIEnabled);
        }

        [Test]
        public void SetAIEnabled_True_EnablesAI()
        {
            _unitAI.SetAIEnabled(false);
            _unitAI.SetAIEnabled(true);

            Assert.IsTrue(_unitAI.IsAIEnabled);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void UnitController_HasAIReference()
        {
            // Check that UnitAI can be retrieved from UnitController
            var ai = _aiUnitGO.GetComponent<UnitAI>();

            Assert.IsNotNull(ai);
        }

        #endregion
    }
}
