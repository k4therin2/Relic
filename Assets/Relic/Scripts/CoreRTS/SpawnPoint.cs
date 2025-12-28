using UnityEngine;

namespace Relic.CoreRTS
{
    /// <summary>
    /// Defines a spawn point for units on the battlefield.
    /// Units spawned at this point will be assigned the specified team.
    /// </summary>
    /// <remarks>
    /// Spawn points are typically placed as children of BattlefieldRoot.
    /// See Kyle's milestones.md Milestone 2 for requirements.
    /// </remarks>
    public class SpawnPoint : MonoBehaviour
    {
        #region Constants

        /// <summary>Red team ID constant.</summary>
        public const int TEAM_RED = 0;

        /// <summary>Blue team ID constant.</summary>
        public const int TEAM_BLUE = 1;

        #endregion

        #region Serialized Fields

        [Header("Team Configuration")]
        [Tooltip("Team ID for units spawned at this point (0 = Red, 1 = Blue)")]
        [SerializeField] private int _teamId = 0;

        [Header("Spawn Settings")]
        [Tooltip("Radius within which units can spawn (randomized position)")]
        [Range(0f, 10f)]
        [SerializeField] private float _spawnRadius = 0.5f;

        [Tooltip("Direction units face when spawned (if Use Transform Direction is false)")]
        [SerializeField] private Vector3 _spawnDirection = Vector3.forward;

        [Tooltip("Use the transform's forward direction for spawn direction")]
        [SerializeField] private bool _useTransformDirection = true;

        [Header("Visual Settings")]
        [Tooltip("Color for gizmo visualization")]
        [SerializeField] private Color _gizmoColor = Color.red;

        #endregion

        #region Properties

        /// <summary>
        /// Team ID for units spawned at this point.
        /// </summary>
        public int TeamId => _teamId;

        /// <summary>
        /// Spawn radius for random position offset.
        /// </summary>
        public float SpawnRadius => _spawnRadius;

        /// <summary>
        /// World position of this spawn point.
        /// </summary>
        public Vector3 Position => transform.position;

        /// <summary>
        /// Rotation for spawned units.
        /// </summary>
        public Quaternion Rotation => _useTransformDirection
            ? transform.rotation
            : Quaternion.LookRotation(_spawnDirection, Vector3.up);

        /// <summary>
        /// Forward direction for spawned units.
        /// </summary>
        public Vector3 Forward => _useTransformDirection
            ? transform.forward
            : _spawnDirection.normalized;

        /// <summary>
        /// Returns true if this is a red team spawn point.
        /// </summary>
        public bool IsRedTeam => _teamId == TEAM_RED;

        /// <summary>
        /// Returns true if this is a blue team spawn point.
        /// </summary>
        public bool IsBlueTeam => _teamId == TEAM_BLUE;

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a spawn position within the spawn radius.
        /// </summary>
        /// <returns>A world position for spawning a unit.</returns>
        public Vector3 GetSpawnPosition()
        {
            if (_spawnRadius <= 0f)
            {
                return transform.position;
            }

            // Random position within circle on XZ plane
            Vector2 randomOffset = Random.insideUnitCircle * _spawnRadius;
            return new Vector3(
                transform.position.x + randomOffset.x,
                transform.position.y,
                transform.position.z + randomOffset.y
            );
        }

        /// <summary>
        /// Gets the spawn rotation for units.
        /// </summary>
        /// <returns>The rotation spawned units should have.</returns>
        public Quaternion GetSpawnRotation()
        {
            return Rotation;
        }

        /// <summary>
        /// Sets the team for this spawn point.
        /// </summary>
        /// <param name="teamId">The team ID to set.</param>
        public void SetTeam(int teamId)
        {
            _teamId = teamId;
            UpdateGizmoColor();
        }

        #endregion

        #region Unity Lifecycle

        private void OnValidate()
        {
            UpdateGizmoColor();
        }

        private void UpdateGizmoColor()
        {
            _gizmoColor = _teamId == TEAM_RED ? Color.red : Color.blue;
        }

        #endregion

        #region Debug Visualization

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;

            // Draw spawn point marker
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            // Draw spawn radius
            if (_spawnRadius > 0f)
            {
                Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.3f);
                DrawWireCircle(transform.position, _spawnRadius, 32);
            }

            // Draw spawn direction
            Gizmos.color = _gizmoColor;
            Gizmos.DrawRay(transform.position, Forward * 1f);
        }

        private void OnDrawGizmosSelected()
        {
            // Draw more detailed visualization when selected
            Gizmos.color = _gizmoColor;
            Gizmos.DrawSphere(transform.position, 0.1f);

            // Draw team label area
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.5f);
            DrawWireCircle(transform.position, _spawnRadius, 64);
        }

        /// <summary>
        /// Draws a wire circle in the XZ plane.
        /// </summary>
        private void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }

        #endregion
    }
}
