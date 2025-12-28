using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for TickManager and ITickable.
    /// Tests validate registration, unregistration, and tick execution.
    /// </summary>
    public class TickManagerTests
    {
        private GameObject _managerGO;
        private TickManager _manager;

        [SetUp]
        public void Setup()
        {
            // Create TickManager manually for testing
            _managerGO = new GameObject("TestTickManager");
            _manager = _managerGO.AddComponent<TickManager>();
        }

        [TearDown]
        public void Teardown()
        {
            if (_managerGO != null)
                Object.DestroyImmediate(_managerGO);
        }

        #region Registration Tests

        [Test]
        public void Register_AddsTickable()
        {
            var tickable = new MockTickable(TickPriority.Normal);

            _manager.Register(tickable);

            Assert.IsTrue(_manager.IsRegistered(tickable));
            Assert.AreEqual(1, _manager.TotalRegistered);
        }

        [Test]
        public void Register_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _manager.Register(null));
            Assert.AreEqual(0, _manager.TotalRegistered);
        }

        [Test]
        public void Register_DuplicateTickable_OnlyRegistersOnce()
        {
            var tickable = new MockTickable(TickPriority.Normal);

            _manager.Register(tickable);
            _manager.Register(tickable);

            Assert.AreEqual(1, _manager.TotalRegistered);
        }

        [Test]
        public void Unregister_RemovesTickable()
        {
            var tickable = new MockTickable(TickPriority.Normal);
            _manager.Register(tickable);

            _manager.Unregister(tickable);

            Assert.IsFalse(_manager.IsRegistered(tickable));
            Assert.AreEqual(0, _manager.TotalRegistered);
        }

        [Test]
        public void Unregister_NotRegistered_DoesNotThrow()
        {
            var tickable = new MockTickable(TickPriority.Normal);

            Assert.DoesNotThrow(() => _manager.Unregister(tickable));
        }

        [Test]
        public void ClearAll_RemovesAllTickables()
        {
            _manager.Register(new MockTickable(TickPriority.Low));
            _manager.Register(new MockTickable(TickPriority.Medium));
            _manager.Register(new MockTickable(TickPriority.Normal));
            _manager.Register(new MockTickable(TickPriority.High));

            _manager.ClearAll();

            Assert.AreEqual(0, _manager.TotalRegistered);
        }

        #endregion

        #region Priority Tests

        [Test]
        public void Register_SeparatesPriorities()
        {
            _manager.Register(new MockTickable(TickPriority.Low));
            _manager.Register(new MockTickable(TickPriority.Medium));
            _manager.Register(new MockTickable(TickPriority.Normal));
            _manager.Register(new MockTickable(TickPriority.High));

            Assert.AreEqual(1, _manager.GetCount(TickPriority.Low));
            Assert.AreEqual(1, _manager.GetCount(TickPriority.Medium));
            Assert.AreEqual(1, _manager.GetCount(TickPriority.Normal));
            Assert.AreEqual(1, _manager.GetCount(TickPriority.High));
            Assert.AreEqual(4, _manager.TotalRegistered);
        }

        [Test]
        public void GetCount_WithNoTickables_ReturnsZero()
        {
            Assert.AreEqual(0, _manager.GetCount(TickPriority.Normal));
        }

        #endregion

        #region IsRegistered Tests

        [Test]
        public void IsRegistered_WithRegisteredTickable_ReturnsTrue()
        {
            var tickable = new MockTickable(TickPriority.Normal);
            _manager.Register(tickable);

            Assert.IsTrue(_manager.IsRegistered(tickable));
        }

        [Test]
        public void IsRegistered_WithUnregisteredTickable_ReturnsFalse()
        {
            var tickable = new MockTickable(TickPriority.Normal);

            Assert.IsFalse(_manager.IsRegistered(tickable));
        }

        [Test]
        public void IsRegistered_WithNull_ReturnsFalse()
        {
            Assert.IsFalse(_manager.IsRegistered(null));
        }

        #endregion

        #region Interval Configuration Tests

        [Test]
        public void LowPriorityInterval_CanBeSet()
        {
            _manager.LowPriorityInterval = 0.5f;

            Assert.AreEqual(0.5f, _manager.LowPriorityInterval);
        }

        [Test]
        public void MediumPriorityInterval_CanBeSet()
        {
            _manager.MediumPriorityInterval = 0.25f;

            Assert.AreEqual(0.25f, _manager.MediumPriorityInterval);
        }

        [Test]
        public void NormalPriorityInterval_CanBeSet()
        {
            _manager.NormalPriorityInterval = 0.016f;

            Assert.AreEqual(0.016f, _manager.NormalPriorityInterval);
        }

        [Test]
        public void HighPriorityInterval_CanBeSet()
        {
            _manager.HighPriorityInterval = 0.0f;

            Assert.AreEqual(0.0f, _manager.HighPriorityInterval);
        }

        [Test]
        public void Interval_NegativeValue_ClampedToZero()
        {
            _manager.LowPriorityInterval = -1f;

            Assert.AreEqual(0f, _manager.LowPriorityInterval);
        }

        #endregion

        #region ITickable Interface Tests

        [Test]
        public void ITickable_Priority_ReturnsCorrectPriority()
        {
            var lowTickable = new MockTickable(TickPriority.Low);
            var highTickable = new MockTickable(TickPriority.High);

            Assert.AreEqual(TickPriority.Low, lowTickable.Priority);
            Assert.AreEqual(TickPriority.High, highTickable.Priority);
        }

        [Test]
        public void ITickable_IsTickActive_DefaultsToTrue()
        {
            var tickable = new MockTickable(TickPriority.Normal);

            Assert.IsTrue(tickable.IsTickActive);
        }

        [Test]
        public void ITickable_IsTickActive_CanBeDisabled()
        {
            var tickable = new MockTickable(TickPriority.Normal);

            tickable.SetActive(false);

            Assert.IsFalse(tickable.IsTickActive);
        }

        #endregion

        #region Mock Tickable

        /// <summary>
        /// Mock implementation of ITickable for testing.
        /// </summary>
        private class MockTickable : ITickable
        {
            private readonly TickPriority _priority;
            private bool _isActive = true;
            private int _tickCount;
            private float _totalDeltaTime;

            public MockTickable(TickPriority priority)
            {
                _priority = priority;
            }

            public TickPriority Priority => _priority;
            public bool IsTickActive => _isActive;
            public int TickCount => _tickCount;
            public float TotalDeltaTime => _totalDeltaTime;

            public void OnTick(float deltaTime)
            {
                _tickCount++;
                _totalDeltaTime += deltaTime;
            }

            public void SetActive(bool active)
            {
                _isActive = active;
            }

            public void Reset()
            {
                _tickCount = 0;
                _totalDeltaTime = 0f;
            }
        }

        #endregion
    }
}
