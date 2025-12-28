using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Relic.ARLayer
{
    /// <summary>
    /// Handles battlefield placement on AR detected planes.
    /// Supports tap-to-place with preview and confirmation flow.
    /// </summary>
    public class BattlefieldPlacer : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;

        [Header("Battlefield Settings")]
        [SerializeField] private GameObject battlefieldPrefab;
        [SerializeField] private GameObject previewPrefab;
        [SerializeField] private float battlefieldScale = 0.5f;
        [SerializeField] private Vector2 battlefieldSize = new Vector2(1f, 0.6f);

        [Header("Detection Settings")]
        [SerializeField] private PlaneDetectionMode detectionMode = PlaneDetectionMode.Horizontal;
        [SerializeField] private float minPlaneArea = 0.25f;

        // State
        private PlacementState currentState = PlacementState.Detecting;
        private GameObject previewInstance;
        private GameObject placedBattlefield;
        private Pose currentPlacementPose;
        private bool isValidPlacement;

        // Events
        public event Action OnPlacementStarted;
        public event Action<Vector3, Quaternion> OnPlacementConfirmed;
        public event Action OnPlacementCancelled;
        public event Action<PlacementState> OnStateChanged;

        // Raycast results cache
        private static readonly List<ARRaycastHit> raycastHits = new List<ARRaycastHit>();

        /// <summary>
        /// Current placement state.
        /// </summary>
        public PlacementState CurrentState => currentState;

        /// <summary>
        /// Whether the battlefield has been placed.
        /// </summary>
        public bool IsPlaced => currentState == PlacementState.Placed;

        /// <summary>
        /// The placed battlefield GameObject, or null if not placed.
        /// </summary>
        public GameObject PlacedBattlefield => placedBattlefield;

        /// <summary>
        /// Current battlefield scale (world units).
        /// </summary>
        public float BattlefieldScale
        {
            get => battlefieldScale;
            set => battlefieldScale = Mathf.Max(0.1f, value);
        }

        /// <summary>
        /// Battlefield dimensions in world units (width, depth).
        /// </summary>
        public Vector2 BattlefieldSize
        {
            get => battlefieldSize;
            set => battlefieldSize = new Vector2(Mathf.Max(0.1f, value.x), Mathf.Max(0.1f, value.y));
        }

        private void Awake()
        {
            ValidateComponents();
        }

        private void OnEnable()
        {
            if (planeManager != null)
            {
                ConfigurePlaneDetection();
            }
        }

        private void Update()
        {
            if (currentState == PlacementState.Detecting || currentState == PlacementState.Previewing)
            {
                UpdatePlacement();
            }
        }

        /// <summary>
        /// Initialize the placer with AR components.
        /// </summary>
        public void Initialize(ARRaycastManager raycast, ARPlaneManager planes)
        {
            raycastManager = raycast;
            planeManager = planes;
            ValidateComponents();
            ConfigurePlaneDetection();
        }

        /// <summary>
        /// Attempt to place the battlefield at the current preview position.
        /// </summary>
        public bool TryPlaceBattlefield()
        {
            if (!isValidPlacement || currentState == PlacementState.Placed)
            {
                return false;
            }

            PlaceBattlefieldAtPose(currentPlacementPose);
            return true;
        }

        /// <summary>
        /// Place the battlefield at a specific world position and rotation.
        /// </summary>
        public void PlaceBattlefieldAtPosition(Vector3 position, Quaternion rotation)
        {
            var pose = new Pose(position, rotation);
            PlaceBattlefieldAtPose(pose);
        }

        /// <summary>
        /// Confirm the current placement and transition to Placed state.
        /// </summary>
        public void ConfirmPlacement()
        {
            if (currentState != PlacementState.Confirming && currentState != PlacementState.Previewing)
            {
                Debug.LogWarning("BattlefieldPlacer: Cannot confirm - not in confirming state");
                return;
            }

            SetState(PlacementState.Placed);

            // Hide planes after placement for cleaner AR experience
            if (planeManager != null)
            {
                planeManager.enabled = false;
                SetPlanesActive(false);
            }

            OnPlacementConfirmed?.Invoke(placedBattlefield.transform.position,
                                          placedBattlefield.transform.rotation);
        }

        /// <summary>
        /// Cancel the current placement and return to detecting state.
        /// </summary>
        public void CancelPlacement()
        {
            if (placedBattlefield != null)
            {
                SafeDestroy(placedBattlefield);
                placedBattlefield = null;
            }

            SetState(PlacementState.Detecting);

            if (planeManager != null)
            {
                planeManager.enabled = true;
                SetPlanesActive(true);
            }

            OnPlacementCancelled?.Invoke();
        }

        /// <summary>
        /// Reset to initial detection state.
        /// </summary>
        public void Reset()
        {
            if (previewInstance != null)
            {
                SafeDestroy(previewInstance);
                previewInstance = null;
            }

            if (placedBattlefield != null)
            {
                SafeDestroy(placedBattlefield);
                placedBattlefield = null;
            }

            isValidPlacement = false;
            SetState(PlacementState.Detecting);

            if (planeManager != null)
            {
                planeManager.enabled = true;
                SetPlanesActive(true);
            }
        }

        /// <summary>
        /// Perform a raycast from screen center to find valid placement surface.
        /// </summary>
        public bool RaycastFromScreenCenter(out Pose hitPose)
        {
            hitPose = default;

            if (raycastManager == null)
            {
                return false;
            }

            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return RaycastFromScreenPoint(screenCenter, out hitPose);
        }

        /// <summary>
        /// Perform a raycast from a screen point to find valid placement surface.
        /// </summary>
        public bool RaycastFromScreenPoint(Vector2 screenPoint, out Pose hitPose)
        {
            hitPose = default;

            if (raycastManager == null)
            {
                return false;
            }

            raycastHits.Clear();
            var trackableTypes = GetTrackableTypesForDetectionMode();

            if (raycastManager.Raycast(screenPoint, raycastHits, trackableTypes))
            {
                // Get the closest hit
                hitPose = raycastHits[0].pose;

                // Validate the plane if plane detection is enabled
                if (planeManager != null)
                {
                    var trackableId = raycastHits[0].trackableId;
                    var plane = planeManager.GetPlane(trackableId);

                    if (plane != null && !IsPlaneValid(plane))
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if the specified area is large enough for battlefield placement.
        /// </summary>
        public bool IsAreaSufficientForPlacement(float areaInSquareMeters)
        {
            float requiredArea = battlefieldSize.x * battlefieldSize.y * battlefieldScale * battlefieldScale;
            return areaInSquareMeters >= Mathf.Max(minPlaneArea, requiredArea * 0.5f);
        }

        private void UpdatePlacement()
        {
            if (RaycastFromScreenCenter(out var hitPose))
            {
                isValidPlacement = true;
                currentPlacementPose = hitPose;

                UpdatePreview(hitPose);

                if (currentState == PlacementState.Detecting)
                {
                    SetState(PlacementState.Previewing);
                    OnPlacementStarted?.Invoke();
                }
            }
            else
            {
                isValidPlacement = false;
                HidePreview();

                if (currentState == PlacementState.Previewing)
                {
                    SetState(PlacementState.Detecting);
                }
            }
        }

        private void UpdatePreview(Pose pose)
        {
            if (previewPrefab == null)
            {
                return;
            }

            if (previewInstance == null)
            {
                previewInstance = Instantiate(previewPrefab);
            }

            previewInstance.SetActive(true);
            previewInstance.transform.position = pose.position;
            previewInstance.transform.rotation = pose.rotation;
            previewInstance.transform.localScale = new Vector3(
                battlefieldSize.x * battlefieldScale,
                1f,
                battlefieldSize.y * battlefieldScale
            );
        }

        private void HidePreview()
        {
            if (previewInstance != null)
            {
                previewInstance.SetActive(false);
            }
        }

        private void PlaceBattlefieldAtPose(Pose pose)
        {
            HidePreview();

            if (battlefieldPrefab != null)
            {
                placedBattlefield = Instantiate(battlefieldPrefab);
            }
            else
            {
                // Create a basic battlefield if no prefab is assigned
                placedBattlefield = CreateDefaultBattlefield();
            }

            placedBattlefield.transform.position = pose.position;
            placedBattlefield.transform.rotation = pose.rotation;
            ApplyBattlefieldScale(placedBattlefield);

            SetState(PlacementState.Confirming);
        }

        private GameObject CreateDefaultBattlefield()
        {
            var battlefield = new GameObject("Battlefield");

            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(battlefield.transform);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localScale = new Vector3(
                battlefieldSize.x * 0.1f,
                1f,
                battlefieldSize.y * 0.1f
            );

            // Create spawn points container
            var spawnPoints = new GameObject("SpawnPoints");
            spawnPoints.transform.SetParent(battlefield.transform);

            // Red team spawn
            var redSpawn = new GameObject("RedSpawn");
            redSpawn.transform.SetParent(spawnPoints.transform);
            redSpawn.transform.localPosition = new Vector3(-battlefieldSize.x * 0.4f, 0f, 0f);

            // Blue team spawn
            var blueSpawn = new GameObject("BlueSpawn");
            blueSpawn.transform.SetParent(spawnPoints.transform);
            blueSpawn.transform.localPosition = new Vector3(battlefieldSize.x * 0.4f, 0f, 0f);

            return battlefield;
        }

        private void ApplyBattlefieldScale(GameObject battlefield)
        {
            // Scale is already incorporated in the prefab/default creation
            // This method allows for runtime scale adjustments
            var currentScale = battlefield.transform.localScale;
            battlefield.transform.localScale = currentScale * battlefieldScale;
        }

        private void ConfigurePlaneDetection()
        {
            if (planeManager == null) return;

            planeManager.requestedDetectionMode = detectionMode;
        }

        private bool IsPlaneValid(ARPlane plane)
        {
            if (plane == null) return false;

            // Check if plane is large enough
            float planeArea = plane.size.x * plane.size.y;
            return IsAreaSufficientForPlacement(planeArea);
        }

        private TrackableType GetTrackableTypesForDetectionMode()
        {
            return detectionMode switch
            {
                PlaneDetectionMode.Horizontal => TrackableType.PlaneWithinPolygon,
                PlaneDetectionMode.Vertical => TrackableType.PlaneWithinPolygon,
                _ => TrackableType.PlaneWithinPolygon
            };
        }

        private void SetPlanesActive(bool active)
        {
            if (planeManager == null) return;

            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(active);
            }
        }

        private void SetState(PlacementState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        private void ValidateComponents()
        {
            if (raycastManager == null)
            {
                raycastManager = FindFirstObjectByType<ARRaycastManager>();
            }

            if (planeManager == null)
            {
                planeManager = FindFirstObjectByType<ARPlaneManager>();
            }
        }

        /// <summary>
        /// Safely destroys a GameObject in both Editor and Play mode.
        /// </summary>
        private void SafeDestroy(GameObject gameObjectToDestroy)
        {
            if (gameObjectToDestroy == null) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObjectToDestroy);
                return;
            }
#endif
            Destroy(gameObjectToDestroy);
        }
    }

    /// <summary>
    /// States for battlefield placement workflow.
    /// </summary>
    public enum PlacementState
    {
        /// <summary>Waiting for valid surface detection.</summary>
        Detecting,
        /// <summary>Valid surface found, showing preview.</summary>
        Previewing,
        /// <summary>Battlefield placed, awaiting confirmation.</summary>
        Confirming,
        /// <summary>Placement confirmed and finalized.</summary>
        Placed
    }
}
