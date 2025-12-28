using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for UnitController MonoBehaviour.
    /// </summary>
    public class UnitControllerTests
    {
        private GameObject _unitGameObject;
        private UnitController _controller;
        private UnitArchetypeSO _archetype;

        [SetUp]
        public void Setup()
        {
            // Create unit GameObject with required components
            _unitGameObject = new GameObject("TestUnit");
            _unitGameObject.AddComponent<BoxCollider>();
            _controller = _unitGameObject.AddComponent<UnitController>();

            // Create test archetype
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_unitGameObject != null)
            {
                Object.DestroyImmediate(_unitGameObject);
            }
            if (_archetype != null)
            {
                Object.DestroyImmediate(_archetype);
            }
        }

        #region Initialization Tests

        [Test]
        public void UnitController_Created_HasRequiredComponents()
        {
            Assert.IsNotNull(_controller);
            Assert.IsNotNull(_unitGameObject.GetComponent<Collider>());
        }

        [Test]
        public void Initialize_SetsArchetypeAndTeam()
        {
            _controller.Initialize(_archetype, 0);

            Assert.AreEqual(_archetype, _controller.Archetype);
            Assert.AreEqual(0, _controller.TeamId);
        }

        [Test]
        public void Initialize_CreatesStatsFromArchetype()
        {
            _controller.Initialize(_archetype, 1);

            // Stats should match archetype
            Assert.AreEqual(_archetype.MaxHealth, _controller.Stats.MaxHealth);
            Assert.AreEqual(_archetype.MaxHealth, _controller.Stats.CurrentHealth);
            Assert.IsTrue(_controller.IsAlive);
        }

        [Test]
        public void Initialize_WithNullArchetype_LogsError()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Cannot initialize with null archetype"));

            _controller.Initialize(null, 0);

            Assert.IsNull(_controller.Archetype);
        }

        [Test]
        public void Initialize_SetsIdleState()
        {
            _controller.Initialize(_archetype, 0);

            Assert.AreEqual(UnitState.Idle, _controller.CurrentState);
        }

        #endregion

        #region Health Tests

        [Test]
        public void TakeDamage_ReducesHealth()
        {
            _controller.Initialize(_archetype, 0);
            int initialHealth = _controller.Stats.CurrentHealth;

            int damage = _controller.TakeDamage(30);

            Assert.Greater(damage, 0);
            Assert.Less(_controller.Stats.CurrentHealth, initialHealth);
        }

        [Test]
        public void TakeDamage_FiresHealthChangedEvent()
        {
            _controller.Initialize(_archetype, 0);
            bool eventFired = false;
            int eventDamage = 0;

            _controller.OnHealthChanged += (current, max, delta) => {
                eventFired = true;
                eventDamage = delta;
            };

            _controller.TakeDamage(25);

            Assert.IsTrue(eventFired);
            Assert.Less(eventDamage, 0, "Delta should be negative for damage");
        }

        [Test]
        public void TakeDamage_WhenDead_DoesNothing()
        {
            _controller.Initialize(_archetype, 0);

            // Kill the unit
            _controller.TakeDamage(1000);
            Assert.IsFalse(_controller.IsAlive);

            // Try to damage again
            int damage = _controller.TakeDamage(50);

            Assert.AreEqual(0, damage);
        }

        [Test]
        public void Heal_RestoresHealth()
        {
            _controller.Initialize(_archetype, 0);
            _controller.TakeDamage(50);
            int damagedHealth = _controller.Stats.CurrentHealth;

            int healed = _controller.Heal(20);

            Assert.Greater(healed, 0);
            Assert.Greater(_controller.Stats.CurrentHealth, damagedHealth);
        }

        [Test]
        public void Heal_FiresHealthChangedEvent()
        {
            _controller.Initialize(_archetype, 0);
            _controller.TakeDamage(50);

            bool eventFired = false;
            int eventHeal = 0;

            _controller.OnHealthChanged += (current, max, delta) => {
                eventFired = true;
                eventHeal = delta;
            };

            _controller.Heal(20);

            Assert.IsTrue(eventFired);
            Assert.Greater(eventHeal, 0, "Delta should be positive for healing");
        }

        [Test]
        public void HealthPercent_CalculatesCorrectly()
        {
            _controller.Initialize(_archetype, 0);
            _controller.TakeDamage(_archetype.MaxHealth / 2);

            Assert.AreEqual(0.5f, _controller.HealthPercent, 0.1f);
        }

        #endregion

        #region Death Tests

        [Test]
        public void TakeDamage_KillsUnit_FiresDeathEvent()
        {
            _controller.Initialize(_archetype, 0);
            bool deathEventFired = false;

            _controller.OnDeath += () => deathEventFired = true;

            _controller.TakeDamage(1000);

            Assert.IsTrue(deathEventFired);
            Assert.IsFalse(_controller.IsAlive);
        }

        [Test]
        public void TakeDamage_KillsUnit_SetsDeadState()
        {
            _controller.Initialize(_archetype, 0);

            _controller.TakeDamage(1000);

            Assert.AreEqual(UnitState.Dead, _controller.CurrentState);
        }

        [Test]
        public void TakeDamage_KillsUnit_DisablesCollider()
        {
            _controller.Initialize(_archetype, 0);
            var collider = _unitGameObject.GetComponent<Collider>();

            _controller.TakeDamage(1000);

            Assert.IsFalse(collider.enabled);
        }

        #endregion

        #region Selection Tests

        [Test]
        public void SetSelected_ChangesSelectionState()
        {
            _controller.Initialize(_archetype, 0);

            _controller.SetSelected(true);
            Assert.IsTrue(_controller.IsSelected);

            _controller.SetSelected(false);
            Assert.IsFalse(_controller.IsSelected);
        }

        [Test]
        public void SetSelected_FiresSelectionChangedEvent()
        {
            _controller.Initialize(_archetype, 0);
            bool eventFired = false;
            bool selectionState = false;

            _controller.OnSelectionChanged += (selected) => {
                eventFired = true;
                selectionState = selected;
            };

            _controller.SetSelected(true);

            Assert.IsTrue(eventFired);
            Assert.IsTrue(selectionState);
        }

        [Test]
        public void SetSelected_SameValue_DoesNotFireEvent()
        {
            _controller.Initialize(_archetype, 0);
            _controller.SetSelected(true);

            int eventCount = 0;
            _controller.OnSelectionChanged += (selected) => eventCount++;

            _controller.SetSelected(true); // Same value

            Assert.AreEqual(0, eventCount);
        }

        #endregion

        #region Squad Tests

        [Test]
        public void AssignToSquad_SetsSquadId()
        {
            _controller.Initialize(_archetype, 0);

            _controller.AssignToSquad(5);

            Assert.AreEqual(5, _controller.SquadId);
        }

        [Test]
        public void SquadId_DefaultsToNegativeOne()
        {
            _controller.Initialize(_archetype, 0);

            Assert.AreEqual(-1, _controller.SquadId);
        }

        #endregion

        #region State Tests

        [Test]
        public void CurrentState_DefaultsToIdle()
        {
            _controller.Initialize(_archetype, 0);

            Assert.AreEqual(UnitState.Idle, _controller.CurrentState);
        }

        [Test]
        public void Stop_WhenIdle_StaysIdle()
        {
            _controller.Initialize(_archetype, 0);

            _controller.Stop();

            Assert.AreEqual(UnitState.Idle, _controller.CurrentState);
        }

        #endregion

        #region Property Tests

        [Test]
        public void SelectionIndicatorPoint_DefaultsToTransform()
        {
            Assert.AreEqual(_controller.transform, _controller.SelectionIndicatorPoint);
        }

        [Test]
        public void HealthBarPoint_DefaultsToTransform()
        {
            Assert.AreEqual(_controller.transform, _controller.HealthBarPoint);
        }

        #endregion
    }
}
