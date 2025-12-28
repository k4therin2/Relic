using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for TeamColorApplier - applies team colors to unit mesh materials.
    /// TDD tests written first per WP-EXT-5.1.
    /// </summary>
    public class TeamColorApplierTests
    {
        private GameObject _unitGameObject;
        private UnitController _unitController;
        private TeamColorApplier _colorApplier;
        private UnitArchetypeSO _archetype;
        private MeshRenderer _renderer;

        [SetUp]
        public void Setup()
        {
            // Create a unit with mesh renderer
            _unitGameObject = new GameObject("TestUnit");
            _unitGameObject.AddComponent<BoxCollider>();

            // Add mesh renderer (using a primitive for testing)
            var meshFilter = _unitGameObject.AddComponent<MeshFilter>();
            _renderer = _unitGameObject.AddComponent<MeshRenderer>();
            _renderer.material = new Material(Shader.Find("Standard"));

            // Create archetype and unit controller
            _archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            _unitController = _unitGameObject.AddComponent<UnitController>();
            _unitController.Initialize(_archetype, 0);

            // Add the TeamColorApplier
            _colorApplier = _unitGameObject.AddComponent<TeamColorApplier>();
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

        #region Basic Functionality Tests

        [Test]
        public void TeamColorApplier_CanBeAddedToGameObject()
        {
            Assert.IsNotNull(_colorApplier);
        }

        [Test]
        public void TeamColorApplier_HasTeamColorsField()
        {
            // Verify default team colors exist
            var team0Color = _colorApplier.GetTeamColor(0);
            var team1Color = _colorApplier.GetTeamColor(1);

            Assert.AreNotEqual(Color.clear, team0Color);
            Assert.AreNotEqual(Color.clear, team1Color);
        }

        [Test]
        public void TeamColorApplier_DifferentTeamsHaveDifferentColors()
        {
            var team0Color = _colorApplier.GetTeamColor(0);
            var team1Color = _colorApplier.GetTeamColor(1);

            Assert.AreNotEqual(team0Color, team1Color);
        }

        #endregion

        #region Color Application Tests

        [Test]
        public void ApplyTeamColor_ChangesRendererColor()
        {
            var originalColor = _renderer.material.color;

            _colorApplier.ApplyTeamColor();

            // Should use MaterialPropertyBlock, check that it's been set
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);

            var appliedColor = block.GetColor("_BaseColor");
            // Color should be set (not black/clear)
            Assert.AreNotEqual(Color.clear, appliedColor);
        }

        [Test]
        public void ApplyTeamColor_UsesMaterialPropertyBlock()
        {
            // MaterialPropertyBlock should be used for GPU instancing efficiency
            _colorApplier.ApplyTeamColor();

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);

            // Verify property block is not empty
            Assert.IsFalse(block.isEmpty);
        }

        [Test]
        public void ApplyTeamColor_Team0_UsesTeam0Color()
        {
            // Unit is already team 0
            _colorApplier.ApplyTeamColor();

            var team0Color = _colorApplier.GetTeamColor(0);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);
            var appliedColor = block.GetColor("_BaseColor");

            Assert.AreEqual(team0Color, appliedColor);
        }

        [Test]
        public void ApplyTeamColor_Team1_UsesTeam1Color()
        {
            // Create new unit with team 1
            var team1GO = new GameObject("Team1Unit");
            team1GO.AddComponent<BoxCollider>();
            var meshRenderer = team1GO.AddComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Standard"));

            var archetype = ScriptableObject.CreateInstance<UnitArchetypeSO>();
            var controller = team1GO.AddComponent<UnitController>();
            controller.Initialize(archetype, 1);

            var applier = team1GO.AddComponent<TeamColorApplier>();
            applier.ApplyTeamColor();

            var team1Color = applier.GetTeamColor(1);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(block);
            var appliedColor = block.GetColor("_BaseColor");

            Assert.AreEqual(team1Color, appliedColor);

            // Cleanup
            Object.DestroyImmediate(team1GO);
            Object.DestroyImmediate(archetype);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void SetTeamColor_UpdatesColorForTeam()
        {
            var customColor = new Color(0.5f, 0.3f, 0.1f);

            _colorApplier.SetTeamColor(0, customColor);

            Assert.AreEqual(customColor, _colorApplier.GetTeamColor(0));
        }

        [Test]
        public void DefaultTeamColors_AreDistinguishable()
        {
            // Team 0 should be reddish (player 1)
            // Team 1 should be bluish (player 2)
            var team0Color = _colorApplier.GetTeamColor(0);
            var team1Color = _colorApplier.GetTeamColor(1);

            // Team 0 should be more red than blue
            Assert.Greater(team0Color.r, team0Color.b);

            // Team 1 should be more blue than red
            Assert.Greater(team1Color.b, team1Color.r);
        }

        #endregion

        #region Child Renderer Tests

        [Test]
        public void ApplyTeamColor_AppliesRecursively_ToChildRenderers()
        {
            // Add a child object with renderer
            var childGO = new GameObject("ChildMesh");
            childGO.transform.SetParent(_unitGameObject.transform);
            var childRenderer = childGO.AddComponent<MeshRenderer>();
            childRenderer.material = new Material(Shader.Find("Standard"));

            _colorApplier.ApplyTeamColor();

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            childRenderer.GetPropertyBlock(block);

            var appliedColor = block.GetColor("_BaseColor");
            Assert.AreEqual(_colorApplier.GetTeamColor(0), appliedColor);
        }

        [Test]
        public void ApplyTeamColor_CanExcludeTaggedRenderers()
        {
            // Some renderers (like UI elements) should not be team-colored
            var excludedGO = new GameObject("ExcludedMesh");
            excludedGO.tag = "IgnoreTeamColor";
            excludedGO.transform.SetParent(_unitGameObject.transform);
            var excludedRenderer = excludedGO.AddComponent<MeshRenderer>();
            excludedRenderer.material = new Material(Shader.Find("Standard"));

            _colorApplier.ApplyTeamColor();

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            excludedRenderer.GetPropertyBlock(block);

            // Should be empty (not colored)
            Assert.IsTrue(block.isEmpty);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void OnStart_AutomaticallyAppliesTeamColor()
        {
            // Simulate Start() being called (would happen automatically in play mode)
            _colorApplier.Initialize();

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(block);

            var appliedColor = block.GetColor("_BaseColor");
            Assert.AreEqual(_colorApplier.GetTeamColor(0), appliedColor);
        }

        #endregion
    }
}
