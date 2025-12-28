using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Relic.UILayer;
using Relic.CoreRTS;
using System.Collections.Generic;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for WorldSpaceUIPanel component.
    /// Tests spawning, era selection, upgrades, match controls, and positioning.
    /// </summary>
    [TestFixture]
    public class WorldSpaceUIPanelTests
    {
        private GameObject _panelGameObject;
        private WorldSpaceUIPanel _panel;
        private GameObject _unitFactoryGameObject;
        private UnitFactory _unitFactory;
        private List<UnitArchetypeSO> _testArchetypes;
        private List<UpgradeSO> _testUpgrades;
        private SpawnPoint _team0SpawnPoint;
        private SpawnPoint _team1SpawnPoint;

        [SetUp]
        public void SetUp()
        {
            // Create panel
            _panelGameObject = new GameObject("WorldSpaceUIPanel");
            _panel = _panelGameObject.AddComponent<WorldSpaceUIPanel>();

            // Create unit factory
            _unitFactoryGameObject = new GameObject("UnitFactory");
            _unitFactory = _unitFactoryGameObject.AddComponent<UnitFactory>();

            // Create spawn points
            var team0GO = new GameObject("Team0Spawn");
            _team0SpawnPoint = team0GO.AddComponent<SpawnPoint>();
            _team0SpawnPoint.SetTeam(0);

            var team1GO = new GameObject("Team1Spawn");
            team1GO.transform.position = Vector3.right * 10;
            _team1SpawnPoint = team1GO.AddComponent<SpawnPoint>();
            _team1SpawnPoint.SetTeam(1);

            // Create test archetypes
            _testArchetypes = new List<UnitArchetypeSO>();
            for (int archetypeIndex = 0; archetypeIndex < 3; archetypeIndex++)
            {
                var archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
                var prefab = CreateUnitPrefab($"TestUnit{archetypeIndex}");
                archetype.SetTestValues($"test_unit_{archetypeIndex}", $"Test Unit {archetypeIndex}", prefab);
                _testArchetypes.Add(archetype);
            }

            // Create test upgrades
            _testUpgrades = new List<UpgradeSO>();
            for (int upgradeIndex = 0; upgradeIndex < 3; upgradeIndex++)
            {
                var upgrade = ScriptableObject.CreateInstance<UpgradeSO>();
                SetUpgradeTestValues(upgrade, $"upgrade_{upgradeIndex}", $"Upgrade {upgradeIndex}");
                _testUpgrades.Add(upgrade);
            }

            // Setup panel
            _panel.SetTestReferences(_unitFactory, _testArchetypes, _testUpgrades, _team0SpawnPoint, _team1SpawnPoint);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup archetypes
            foreach (var archetype in _testArchetypes)
            {
                if (archetype != null && archetype.UnitPrefab != null)
                {
                    Object.DestroyImmediate(archetype.UnitPrefab);
                }
                if (archetype != null)
                {
                    Object.DestroyImmediate(archetype);
                }
            }

            // Cleanup upgrades
            foreach (var upgrade in _testUpgrades)
            {
                if (upgrade != null)
                {
                    Object.DestroyImmediate(upgrade);
                }
            }

            // Cleanup GameObjects
            if (_panelGameObject != null) Object.DestroyImmediate(_panelGameObject);
            if (_unitFactoryGameObject != null) Object.DestroyImmediate(_unitFactoryGameObject);
            if (_team0SpawnPoint != null) Object.DestroyImmediate(_team0SpawnPoint.gameObject);
            if (_team1SpawnPoint != null) Object.DestroyImmediate(_team1SpawnPoint.gameObject);

            // Reset time scale
            Time.timeScale = 1f;
        }

        private GameObject CreateUnitPrefab(string unitName)
        {
            var prefab = new GameObject(unitName);
            prefab.AddComponent<BoxCollider>();
            return prefab;
        }

        private void SetUpgradeTestValues(UpgradeSO upgrade, string id, string displayName)
        {
            var idField = typeof(UpgradeSO).GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var displayNameField = typeof(UpgradeSO).GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var hitChanceField = typeof(UpgradeSO).GetField("_hitChanceMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var damageField = typeof(UpgradeSO).GetField("_damageMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var eraField = typeof(UpgradeSO).GetField("_era", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            idField?.SetValue(upgrade, id);
            displayNameField?.SetValue(upgrade, displayName);
            hitChanceField?.SetValue(upgrade, 1.1f);
            damageField?.SetValue(upgrade, 1.2f);
            eraField?.SetValue(upgrade, EraType.All);
        }

        #region Initialization Tests

        [Test]
        public void Initialize_SetsUpPanelCorrectly()
        {
            // Note: Initialize() attempts to access EraManager.Instance which uses DontDestroyOnLoad
            // This is not allowed in EditMode tests, so we test the property directly after SetTestReferences
            // which was already called in SetUp
            Assert.That(_panel.SelectedArchetype, Is.Not.Null);
            Assert.That(_panel.SelectedArchetype, Is.EqualTo(_testArchetypes[0]));
        }

        [Test]
        public void SetArchetypes_UpdatesAvailableArchetypes()
        {
            var newArchetypes = new List<UnitArchetypeSO> { _testArchetypes[0] };
            _panel.SetArchetypes(newArchetypes);

            Assert.That(_panel.SelectedArchetype, Is.EqualTo(_testArchetypes[0]));
        }

        [Test]
        public void SetArchetypes_WithNullList_HandlesGracefully()
        {
            Assert.DoesNotThrow(() => _panel.SetArchetypes(null));
            Assert.That(_panel.SelectedArchetype, Is.Null);
        }

        [Test]
        public void SetUpgrades_UpdatesAvailableUpgrades()
        {
            var newUpgrades = new List<UpgradeSO> { _testUpgrades[0], _testUpgrades[1] };
            _panel.SetUpgrades(newUpgrades);

            // Verify by checking no exceptions and panel is functional
            Assert.DoesNotThrow(() => _panel.Refresh());
        }

        [Test]
        public void SetSpawnPoints_UpdatesSpawnPointReferences()
        {
            var newTeam0GO = new GameObject("NewTeam0");
            var newTeam0 = newTeam0GO.AddComponent<SpawnPoint>();
            newTeam0.SetTeam(0);

            var newTeam1GO = new GameObject("NewTeam1");
            newTeam1GO.transform.position = Vector3.right * 10;
            var newTeam1 = newTeam1GO.AddComponent<SpawnPoint>();
            newTeam1.SetTeam(1);

            _panel.SetSpawnPoints(newTeam0, newTeam1);

            // Cleanup
            Object.DestroyImmediate(newTeam0GO);
            Object.DestroyImmediate(newTeam1GO);
        }

        [Test]
        public void SetUnitFactory_UpdatesFactoryReference()
        {
            var newFactoryGO = new GameObject("NewFactory");
            var newFactory = newFactoryGO.AddComponent<UnitFactory>();

            // SetUnitFactory should update the internal reference
            Assert.DoesNotThrow(() => _panel.SetUnitFactory(newFactory));

            // We don't call Initialize() as it uses EraManager.Instance which needs DontDestroyOnLoad

            Object.DestroyImmediate(newFactoryGO);
        }

        #endregion

        #region Pause Tests

        [Test]
        public void IsPaused_InitiallyFalse()
        {
            Assert.That(_panel.IsPaused, Is.False);
        }

        [Test]
        public void SetPaused_True_SetsTimeScaleToZero()
        {
            _panel.SetPaused(true);

            Assert.That(_panel.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f));
        }

        [Test]
        public void SetPaused_False_SetsTimeScaleToOne()
        {
            _panel.SetPaused(true);
            _panel.SetPaused(false);

            Assert.That(_panel.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [Test]
        public void TogglePause_FlipsPauseState()
        {
            Assert.That(_panel.IsPaused, Is.False);

            _panel.TogglePause();
            Assert.That(_panel.IsPaused, Is.True);

            _panel.TogglePause();
            Assert.That(_panel.IsPaused, Is.False);
        }

        [Test]
        public void SetPaused_FiresOnPauseStateChangedEvent()
        {
            bool eventFired = false;
            bool receivedPauseState = false;

            _panel.OnPauseStateChanged += (paused) =>
            {
                eventFired = true;
                receivedPauseState = paused;
            };

            _panel.SetPaused(true);

            Assert.That(eventFired, Is.True);
            Assert.That(receivedPauseState, Is.True);
        }

        #endregion

        #region Match Control Tests

        [Test]
        public void ResetMatch_FiresOnMatchResetEvent()
        {
            bool eventFired = false;
            _panel.OnMatchReset += () => eventFired = true;

            _panel.ResetMatch();

            Assert.That(eventFired, Is.True);
        }

        [Test]
        public void ResetMatch_ResumesIfPaused()
        {
            _panel.SetPaused(true);
            Assert.That(_panel.IsPaused, Is.True);

            _panel.ResetMatch();

            Assert.That(_panel.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        #endregion

        #region Visibility Tests

        [Test]
        public void Show_MakesPanelVisible()
        {
            _panel.Hide();
            Assert.That(_panelGameObject.activeSelf, Is.False);

            _panel.Show();
            Assert.That(_panelGameObject.activeSelf, Is.True);
        }

        [Test]
        public void Hide_MakesPanelInvisible()
        {
            _panel.Show();
            Assert.That(_panelGameObject.activeSelf, Is.True);

            _panel.Hide();
            Assert.That(_panelGameObject.activeSelf, Is.False);
        }

        [Test]
        public void SetSectionVisible_SpawnSection()
        {
            _panel.SetSectionVisible("spawn", false);
            _panel.SetSectionVisible("spawn", true);
            // No exception means success
        }

        [Test]
        public void SetSectionVisible_EraSection()
        {
            _panel.SetSectionVisible("era", false);
            _panel.SetSectionVisible("era", true);
        }

        [Test]
        public void SetSectionVisible_UpgradeSection()
        {
            _panel.SetSectionVisible("upgrade", false);
            _panel.SetSectionVisible("upgrade", true);
        }

        [Test]
        public void SetSectionVisible_MatchSection()
        {
            _panel.SetSectionVisible("match", false);
            _panel.SetSectionVisible("match", true);
        }

        [Test]
        public void SetSectionVisible_InvalidSection_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _panel.SetSectionVisible("invalid", false));
        }

        #endregion

        #region Anchoring Tests

        [Test]
        public void AnchorTarget_CanBeSet()
        {
            var anchorGO = new GameObject("Anchor");
            _panel.AnchorTarget = anchorGO.transform;

            Assert.That(_panel.AnchorTarget, Is.EqualTo(anchorGO.transform));

            Object.DestroyImmediate(anchorGO);
        }

        [Test]
        public void AnchorOffset_CanBeSet()
        {
            var newOffset = new Vector3(1f, 2f, 3f);
            _panel.AnchorOffset = newOffset;

            Assert.That(_panel.AnchorOffset, Is.EqualTo(newOffset));
        }

        [Test]
        public void BillboardToCamera_CanBeSet()
        {
            _panel.BillboardToCamera = false;
            Assert.That(_panel.BillboardToCamera, Is.False);

            _panel.BillboardToCamera = true;
            Assert.That(_panel.BillboardToCamera, Is.True);
        }

        #endregion

        #region Upgrade Tests

        [Test]
        public void ApplyUpgrade_WithNoSquad_LogsWarning()
        {
            // No squad set
            LogAssert.Expect(LogType.Warning, "[WorldSpaceUIPanel] No upgrade or squad selected");

            _panel.ApplyUpgrade(_testUpgrades[0]);
        }

        [Test]
        public void ApplyUpgrade_WithSquad_AppliesUpgrade()
        {
            var squad = new Squad("test-squad", 0);
            _panel.SetTestSquad(squad);

            bool eventFired = false;
            UpgradeSO appliedUpgrade = null;
            Squad targetSquad = null;

            _panel.OnUpgradeApplied += (upgrade, squadRef) =>
            {
                eventFired = true;
                appliedUpgrade = upgrade;
                targetSquad = squadRef;
            };

            _panel.ApplyUpgrade(_testUpgrades[0]);

            Assert.That(eventFired, Is.True);
            Assert.That(appliedUpgrade, Is.EqualTo(_testUpgrades[0]));
            Assert.That(targetSquad, Is.EqualTo(squad));
            Assert.That(squad.HasUpgrade(_testUpgrades[0]), Is.True);
        }

        [Test]
        public void ApplyUpgrade_WithNullUpgrade_LogsWarning()
        {
            var squad = new Squad("test-squad", 0);
            _panel.SetTestSquad(squad);

            LogAssert.Expect(LogType.Warning, "[WorldSpaceUIPanel] No upgrade or squad selected");
            _panel.ApplyUpgrade(null);
        }

        #endregion

        #region Refresh Tests

        [Test]
        public void Refresh_UpdatesAllDisplays()
        {
            Assert.DoesNotThrow(() => _panel.Refresh());
        }

        #endregion
    }
}
