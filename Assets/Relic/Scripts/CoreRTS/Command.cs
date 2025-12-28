using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Base class for all unit commands. Commands are issued by the player
    /// or AI and executed by units through their CommandQueue.
    /// </summary>
    public abstract class Command
    {
        /// <summary>
        /// The type of this command for serialization/networking.
        /// </summary>
        public abstract CommandType Type { get; }

        /// <summary>
        /// Whether this command has completed execution.
        /// </summary>
        public bool IsComplete { get; protected set; }

        /// <summary>
        /// Whether this command was cancelled before completion.
        /// </summary>
        public bool IsCancelled { get; protected set; }

        /// <summary>
        /// Begins execution of this command on the given unit.
        /// Called once when the command starts.
        /// </summary>
        /// <param name="unit">The unit executing the command.</param>
        public abstract void Execute(UnitController unit);

        /// <summary>
        /// Updates the command. Called each frame while the command is active.
        /// </summary>
        /// <param name="unit">The unit executing the command.</param>
        public abstract void Update(UnitController unit);

        /// <summary>
        /// Cancels the command.
        /// </summary>
        /// <param name="unit">The unit executing the command.</param>
        public virtual void Cancel(UnitController unit)
        {
            IsCancelled = true;
            IsComplete = true;
        }
    }

    /// <summary>
    /// Types of commands for identification and networking.
    /// </summary>
    public enum CommandType
    {
        Move,
        Stop,
        Attack,
        // Future command types for M3+
        AttackMove,
        Patrol,
        Hold
    }

    /// <summary>
    /// Command to move a unit to a target position.
    /// </summary>
    public class MoveCommand : Command
    {
        private readonly Vector3 _destination;
        private readonly float _arrivalThreshold;

        public override CommandType Type => CommandType.Move;

        /// <summary>
        /// The destination position for this move command.
        /// </summary>
        public Vector3 Destination => _destination;

        /// <summary>
        /// Creates a new move command.
        /// </summary>
        /// <param name="destination">The target position to move to.</param>
        /// <param name="arrivalThreshold">How close the unit needs to be to consider arrival (default 0.5).</param>
        public MoveCommand(Vector3 destination, float arrivalThreshold = 0.5f)
        {
            _destination = destination;
            _arrivalThreshold = arrivalThreshold;
        }

        public override void Execute(UnitController unit)
        {
            if (unit == null || !unit.IsAlive)
            {
                IsComplete = true;
                return;
            }

            if (!unit.MoveTo(_destination))
            {
                // MoveTo failed (unit not on NavMesh, etc.)
                IsComplete = true;
            }
        }

        public override void Update(UnitController unit)
        {
            if (unit == null || !unit.IsAlive || IsCancelled)
            {
                IsComplete = true;
                return;
            }

            // Check if we've arrived
            if (!unit.IsMoving)
            {
                IsComplete = true;
            }
        }

        public override void Cancel(UnitController unit)
        {
            if (unit != null && unit.IsAlive)
            {
                unit.Stop();
            }
            base.Cancel(unit);
        }
    }

    /// <summary>
    /// Command to stop a unit immediately.
    /// </summary>
    public class StopCommand : Command
    {
        public override CommandType Type => CommandType.Stop;

        public override void Execute(UnitController unit)
        {
            if (unit != null && unit.IsAlive)
            {
                unit.Stop();
            }
            IsComplete = true;
        }

        public override void Update(UnitController unit)
        {
            // Stop command completes immediately
            IsComplete = true;
        }
    }

    /// <summary>
    /// Command to attack a target unit (stub for M3).
    /// </summary>
    public class AttackCommand : Command
    {
        private readonly UnitController _target;
        private readonly bool _attackMove;

        public override CommandType Type => CommandType.Attack;

        /// <summary>
        /// The target unit to attack.
        /// </summary>
        public UnitController Target => _target;

        /// <summary>
        /// Creates a new attack command.
        /// </summary>
        /// <param name="target">The unit to attack.</param>
        /// <param name="attackMove">If true, move toward target if out of range.</param>
        public AttackCommand(UnitController target, bool attackMove = true)
        {
            _target = target;
            _attackMove = attackMove;
        }

        public override void Execute(UnitController unit)
        {
            if (unit == null || !unit.IsAlive)
            {
                IsComplete = true;
                return;
            }

            if (_target == null || !_target.IsAlive)
            {
                IsComplete = true;
                return;
            }

            // TODO: Implement attack logic in M3
            // For now, just move toward target if attackMove is enabled
            if (_attackMove)
            {
                unit.MoveTo(_target.transform.position);
            }
        }

        public override void Update(UnitController unit)
        {
            if (unit == null || !unit.IsAlive || IsCancelled)
            {
                IsComplete = true;
                return;
            }

            if (_target == null || !_target.IsAlive)
            {
                IsComplete = true;
                return;
            }

            // TODO: Check if in range and attack in M3
            // For now, just follow target if attackMove enabled
            if (_attackMove && !unit.IsMoving)
            {
                unit.MoveTo(_target.transform.position);
            }
        }

        public override void Cancel(UnitController unit)
        {
            if (unit != null && unit.IsAlive)
            {
                unit.Stop();
            }
            base.Cancel(unit);
        }
    }
}
