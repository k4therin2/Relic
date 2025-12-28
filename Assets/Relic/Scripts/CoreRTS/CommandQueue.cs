using UnityEngine;
using System;
using System.Collections.Generic;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Manages a queue of commands for a unit. Executes commands sequentially
    /// and supports both replace and queue modes.
    /// </summary>
    public class CommandQueue : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Configuration")]
        [Tooltip("Maximum number of commands that can be queued")]
        [Range(1, 20)]
        [SerializeField] private int _maxQueueSize = 10;

        #endregion

        #region Runtime State

        private UnitController _unit;
        private Queue<Command> _commands = new Queue<Command>();
        private Command _currentCommand;

        #endregion

        #region Events

        /// <summary>
        /// Fired when a new command starts executing.
        /// </summary>
        public event Action<Command> OnCommandStarted;

        /// <summary>
        /// Fired when a command completes.
        /// </summary>
        public event Action<Command> OnCommandCompleted;

        /// <summary>
        /// Fired when the command queue becomes empty.
        /// </summary>
        public event Action OnQueueEmpty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the currently executing command.
        /// </summary>
        public Command CurrentCommand => _currentCommand;

        /// <summary>
        /// Gets the number of commands waiting in queue (not including current).
        /// </summary>
        public int QueuedCount => _commands.Count;

        /// <summary>
        /// Gets whether a command is currently being executed.
        /// </summary>
        public bool IsExecuting => _currentCommand != null && !_currentCommand.IsComplete;

        /// <summary>
        /// Gets whether the queue is empty (no current or pending commands).
        /// </summary>
        public bool IsEmpty => _currentCommand == null && _commands.Count == 0;

        /// <summary>
        /// Maximum number of commands allowed in queue.
        /// </summary>
        public int MaxQueueSize => _maxQueueSize;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _unit = GetComponent<UnitController>();
            if (_unit == null)
            {
                Debug.LogError($"[CommandQueue] No UnitController found on {gameObject.name}");
            }
        }

        private void Update()
        {
            ProcessCommands();
        }

        #endregion

        #region Command Issuing

        /// <summary>
        /// Issues a command, replacing any current command and clearing the queue.
        /// This is the default behavior (like left-click in most RTS games).
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public void Issue(Command command)
        {
            if (command == null) return;

            ClearAllCommands();
            ExecuteCommand(command);
        }

        /// <summary>
        /// Queues a command to execute after all current commands complete.
        /// (Like shift+click in most RTS games).
        /// </summary>
        /// <param name="command">The command to queue.</param>
        /// <returns>True if command was queued, false if queue is full.</returns>
        public bool Queue(Command command)
        {
            if (command == null) return false;

            if (_commands.Count >= _maxQueueSize)
            {
                Debug.LogWarning($"[CommandQueue] Queue is full on {gameObject.name}");
                return false;
            }

            // If no current command, execute immediately
            if (_currentCommand == null)
            {
                ExecuteCommand(command);
            }
            else
            {
                _commands.Enqueue(command);
            }

            return true;
        }

        /// <summary>
        /// Stops the current command and clears all queued commands.
        /// </summary>
        public void ClearAllCommands()
        {
            // Cancel current command
            if (_currentCommand != null)
            {
                _currentCommand.Cancel(_unit);
                OnCommandCompleted?.Invoke(_currentCommand);
                _currentCommand = null;
            }

            // Clear queue
            while (_commands.Count > 0)
            {
                var cmd = _commands.Dequeue();
                cmd.Cancel(_unit);
            }

            OnQueueEmpty?.Invoke();
        }

        /// <summary>
        /// Stops the current command and starts the next queued command (if any).
        /// </summary>
        public void SkipCurrentCommand()
        {
            if (_currentCommand != null)
            {
                _currentCommand.Cancel(_unit);
                OnCommandCompleted?.Invoke(_currentCommand);
                _currentCommand = null;
            }

            StartNextCommand();
        }

        #endregion

        #region Command Processing

        private void ProcessCommands()
        {
            if (_unit == null || !_unit.IsAlive)
            {
                ClearAllCommands();
                return;
            }

            // Update current command
            if (_currentCommand != null)
            {
                _currentCommand.Update(_unit);

                if (_currentCommand.IsComplete)
                {
                    OnCommandCompleted?.Invoke(_currentCommand);
                    _currentCommand = null;
                    StartNextCommand();
                }
            }
        }

        private void StartNextCommand()
        {
            if (_commands.Count > 0)
            {
                var nextCommand = _commands.Dequeue();
                ExecuteCommand(nextCommand);
            }
            else
            {
                OnQueueEmpty?.Invoke();
            }
        }

        private void ExecuteCommand(Command command)
        {
            _currentCommand = command;
            _currentCommand.Execute(_unit);
            OnCommandStarted?.Invoke(_currentCommand);

            // Check if command completed immediately
            if (_currentCommand.IsComplete)
            {
                OnCommandCompleted?.Invoke(_currentCommand);
                _currentCommand = null;
                StartNextCommand();
            }
        }

        #endregion

        #region Debug

        /// <summary>
        /// Returns a list of all pending commands (for debugging).
        /// </summary>
        public List<Command> GetQueueSnapshot()
        {
            var result = new List<Command>();

            if (_currentCommand != null)
            {
                result.Add(_currentCommand);
            }

            foreach (var cmd in _commands)
            {
                result.Add(cmd);
            }

            return result;
        }

        private void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || _currentCommand == null) return;

            // Visualize current command
            if (_currentCommand is MoveCommand moveCmd)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, moveCmd.Destination);
                Gizmos.DrawWireSphere(moveCmd.Destination, 0.3f);
            }

            // Draw queued waypoints
            Vector3 lastPos = transform.position;
            if (_currentCommand is MoveCommand current)
            {
                lastPos = current.Destination;
            }

            Gizmos.color = Color.blue;
            foreach (var cmd in _commands)
            {
                if (cmd is MoveCommand queuedMove)
                {
                    Gizmos.DrawLine(lastPos, queuedMove.Destination);
                    Gizmos.DrawWireSphere(queuedMove.Destination, 0.2f);
                    lastPos = queuedMove.Destination;
                }
            }
        }

        #endregion
    }
}
