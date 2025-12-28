using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for Command, MoveCommand, StopCommand, and AttackCommand.
    /// </summary>
    public class CommandTests
    {
        private GameObject _unitGameObject;
        private UnitController _unit;
        private UnitArchetypeSO _archetype;

        [SetUp]
        public void Setup()
        {
            _unitGameObject = new GameObject("TestUnit");
            _unitGameObject.AddComponent<BoxCollider>();
            _unit = _unitGameObject.AddComponent<UnitController>();
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            _unit.Initialize(_archetype, 0);
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

        #region MoveCommand Tests

        [Test]
        public void MoveCommand_Type_IsMove()
        {
            var cmd = new MoveCommand(Vector3.zero);
            Assert.AreEqual(CommandType.Move, cmd.Type);
        }

        [Test]
        public void MoveCommand_Destination_MatchesConstructorArg()
        {
            var destination = new Vector3(5f, 0f, 10f);
            var cmd = new MoveCommand(destination);

            Assert.AreEqual(destination, cmd.Destination);
        }

        [Test]
        public void MoveCommand_InitialState_NotComplete()
        {
            var cmd = new MoveCommand(Vector3.zero);

            Assert.IsFalse(cmd.IsComplete);
            Assert.IsFalse(cmd.IsCancelled);
        }

        [Test]
        public void MoveCommand_Cancel_SetsIsCompleteAndIsCancelled()
        {
            var cmd = new MoveCommand(Vector3.zero);

            cmd.Cancel(_unit);

            Assert.IsTrue(cmd.IsComplete);
            Assert.IsTrue(cmd.IsCancelled);
        }

        [Test]
        public void MoveCommand_Execute_WithDeadUnit_CompletesImmediately()
        {
            var cmd = new MoveCommand(Vector3.forward * 10);
            _unit.TakeDamage(10000); // Kill unit

            cmd.Execute(_unit);

            Assert.IsTrue(cmd.IsComplete);
        }

        [Test]
        public void MoveCommand_Execute_WithNullUnit_CompletesImmediately()
        {
            var cmd = new MoveCommand(Vector3.forward * 10);

            cmd.Execute(null);

            Assert.IsTrue(cmd.IsComplete);
        }

        [Test]
        public void MoveCommand_Update_WithDeadUnit_CompletesImmediately()
        {
            var cmd = new MoveCommand(Vector3.forward * 10);
            _unit.TakeDamage(10000);

            cmd.Update(_unit);

            Assert.IsTrue(cmd.IsComplete);
        }

        [Test]
        public void MoveCommand_Update_WithNullUnit_CompletesImmediately()
        {
            var cmd = new MoveCommand(Vector3.forward * 10);

            cmd.Update(null);

            Assert.IsTrue(cmd.IsComplete);
        }

        [Test]
        public void MoveCommand_Update_WhenCancelled_CompletesImmediately()
        {
            var cmd = new MoveCommand(Vector3.forward * 10);
            cmd.Cancel(_unit);

            cmd.Update(_unit);

            Assert.IsTrue(cmd.IsComplete);
        }

        #endregion

        #region StopCommand Tests

        [Test]
        public void StopCommand_Type_IsStop()
        {
            var cmd = new StopCommand();
            Assert.AreEqual(CommandType.Stop, cmd.Type);
        }

        [Test]
        public void StopCommand_Execute_CompletesImmediately()
        {
            var cmd = new StopCommand();

            cmd.Execute(_unit);

            Assert.IsTrue(cmd.IsComplete);
        }

        [Test]
        public void StopCommand_Execute_WithNullUnit_DoesNotThrow()
        {
            var cmd = new StopCommand();

            Assert.DoesNotThrow(() => cmd.Execute(null));
            Assert.IsTrue(cmd.IsComplete);
        }

        [Test]
        public void StopCommand_Update_CompletesImmediately()
        {
            var cmd = new StopCommand();

            cmd.Update(_unit);

            Assert.IsTrue(cmd.IsComplete);
        }

        #endregion

        #region AttackCommand Tests

        [Test]
        public void AttackCommand_Type_IsAttack()
        {
            var targetGO = new GameObject("Target");
            targetGO.AddComponent<BoxCollider>();
            var target = targetGO.AddComponent<UnitController>();
            target.Initialize(_archetype, 1);

            var cmd = new AttackCommand(target);

            Assert.AreEqual(CommandType.Attack, cmd.Type);

            Object.DestroyImmediate(targetGO);
        }

        [Test]
        public void AttackCommand_Target_MatchesConstructorArg()
        {
            var targetGO = new GameObject("Target");
            targetGO.AddComponent<BoxCollider>();
            var target = targetGO.AddComponent<UnitController>();
            target.Initialize(_archetype, 1);

            var cmd = new AttackCommand(target);

            Assert.AreEqual(target, cmd.Target);

            Object.DestroyImmediate(targetGO);
        }

        [Test]
        public void AttackCommand_Execute_WithNullUnit_CompletesImmediately()
        {
            var targetGO = new GameObject("Target");
            targetGO.AddComponent<BoxCollider>();
            var target = targetGO.AddComponent<UnitController>();

            var cmd = new AttackCommand(target);

            cmd.Execute(null);

            Assert.IsTrue(cmd.IsComplete);

            Object.DestroyImmediate(targetGO);
        }

        [Test]
        public void AttackCommand_Execute_WithNullTarget_CompletesImmediately()
        {
            var cmd = new AttackCommand(null);

            cmd.Execute(_unit);

            Assert.IsTrue(cmd.IsComplete);
        }

        [Test]
        public void AttackCommand_Execute_WithDeadTarget_CompletesImmediately()
        {
            var targetGO = new GameObject("Target");
            targetGO.AddComponent<BoxCollider>();
            var target = targetGO.AddComponent<UnitController>();
            target.Initialize(_archetype, 1);
            target.TakeDamage(10000); // Kill target

            var cmd = new AttackCommand(target);

            cmd.Execute(_unit);

            Assert.IsTrue(cmd.IsComplete);

            Object.DestroyImmediate(targetGO);
        }

        [Test]
        public void AttackCommand_Update_WithDeadTarget_CompletesImmediately()
        {
            var targetGO = new GameObject("Target");
            targetGO.AddComponent<BoxCollider>();
            var target = targetGO.AddComponent<UnitController>();
            target.Initialize(_archetype, 1);

            var cmd = new AttackCommand(target);
            cmd.Execute(_unit);

            target.TakeDamage(10000); // Kill target during combat

            cmd.Update(_unit);

            Assert.IsTrue(cmd.IsComplete);

            Object.DestroyImmediate(targetGO);
        }

        [Test]
        public void AttackCommand_Cancel_StopsUnit()
        {
            var targetGO = new GameObject("Target");
            targetGO.AddComponent<BoxCollider>();
            var target = targetGO.AddComponent<UnitController>();
            target.Initialize(_archetype, 1);

            var cmd = new AttackCommand(target);

            cmd.Cancel(_unit);

            Assert.IsTrue(cmd.IsComplete);
            Assert.IsTrue(cmd.IsCancelled);

            Object.DestroyImmediate(targetGO);
        }

        #endregion
    }
}
