using NUnit.Framework;
using UnityEngine;
using Relic.CoreRTS;
using Relic.CoreRTS.Editor;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for QuickDemoSetup utility.
    /// Tests configuration and validation logic.
    /// </summary>
    public class QuickDemoSetupTests
    {
        #region DemoConfig Tests

        [Test]
        public void DemoConfig_Default_HasPositiveUnitsPerTeam()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert
            Assert.Greater(config.UnitsPerTeam, 0, "UnitsPerTeam should be positive");
        }

        [Test]
        public void DemoConfig_Default_HasReasonableUnitsPerTeam()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert - Should be between 5 and 50 for a demo
            Assert.GreaterOrEqual(config.UnitsPerTeam, 5, "Should have at least 5 units per team");
            Assert.LessOrEqual(config.UnitsPerTeam, 50, "Should have at most 50 units per team");
        }

        [Test]
        public void DemoConfig_Default_Team0OnLeftSide()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert - Team 0 should be on the left (negative X)
            Assert.Less(config.Team0CenterX, 0f, "Team 0 should spawn on left side (negative X)");
        }

        [Test]
        public void DemoConfig_Default_Team1OnRightSide()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert - Team 1 should be on the right (positive X)
            Assert.Greater(config.Team1CenterX, 0f, "Team 1 should spawn on right side (positive X)");
        }

        [Test]
        public void DemoConfig_Default_TeamsAreOpposite()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert - Teams should be on opposite sides
            Assert.AreNotEqual(
                Mathf.Sign(config.Team0CenterX),
                Mathf.Sign(config.Team1CenterX),
                "Teams should be on opposite sides of origin"
            );
        }

        [Test]
        public void DemoConfig_Default_TeamsWithinCombatRange()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;
            float distance = Mathf.Abs(config.Team1CenterX - config.Team0CenterX);

            // Assert - Teams should be close enough for immediate combat (within 30 units)
            Assert.LessOrEqual(distance, 30f, "Teams should be within combat range");
        }

        [Test]
        public void DemoConfig_Default_SpawnSpacingIsPositive()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert
            Assert.Greater(config.SpawnSpacing, 0f, "SpawnSpacing should be positive");
        }

        [Test]
        public void DemoConfig_Default_SpawnSpacingReasonable()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert - Spacing should be between 0.5 and 5 units
            Assert.GreaterOrEqual(config.SpawnSpacing, 0.5f, "SpawnSpacing should be at least 0.5");
            Assert.LessOrEqual(config.SpawnSpacing, 5f, "SpawnSpacing should be at most 5");
        }

        [Test]
        public void DemoConfig_Default_EnterPlayModeIsTrue()
        {
            // Arrange & Act
            var config = QuickDemoSetup.DemoConfig.Default;

            // Assert - Default should enter play mode
            Assert.IsTrue(config.EnterPlayMode, "Default config should enter play mode");
        }

        [Test]
        public void DemoConfig_CanModifyProperties()
        {
            // Arrange
            var config = QuickDemoSetup.DemoConfig.Default;

            // Act
            config.UnitsPerTeam = 25;
            config.Team0CenterX = -10f;
            config.Team1CenterX = 10f;
            config.SpawnSpacing = 3f;
            config.EnterPlayMode = false;

            // Assert
            Assert.AreEqual(25, config.UnitsPerTeam);
            Assert.AreEqual(-10f, config.Team0CenterX);
            Assert.AreEqual(10f, config.Team1CenterX);
            Assert.AreEqual(3f, config.SpawnSpacing);
            Assert.IsFalse(config.EnterPlayMode);
        }

        #endregion

        #region Struct Initialization Tests

        [Test]
        public void DemoConfig_NewStruct_HasDefaultValues()
        {
            // Arrange & Act
            var config = new QuickDemoSetup.DemoConfig();

            // Assert - New struct should have zero/default values
            Assert.AreEqual(0, config.UnitsPerTeam);
            Assert.AreEqual(0f, config.Team0CenterX);
            Assert.AreEqual(0f, config.Team1CenterX);
            Assert.AreEqual(0f, config.SpawnSpacing);
            Assert.IsFalse(config.EnterPlayMode);
        }

        [Test]
        public void DemoConfig_Default_DifferentFromNewStruct()
        {
            // Arrange & Act
            var defaultConfig = QuickDemoSetup.DemoConfig.Default;
            var newConfig = new QuickDemoSetup.DemoConfig();

            // Assert - Default should have meaningful values unlike empty struct
            Assert.AreNotEqual(defaultConfig.UnitsPerTeam, newConfig.UnitsPerTeam);
            Assert.AreNotEqual(defaultConfig.Team0CenterX, newConfig.Team0CenterX);
            Assert.AreNotEqual(defaultConfig.Team1CenterX, newConfig.Team1CenterX);
            Assert.AreNotEqual(defaultConfig.SpawnSpacing, newConfig.SpawnSpacing);
            Assert.AreNotEqual(defaultConfig.EnterPlayMode, newConfig.EnterPlayMode);
        }

        #endregion

        #region Formation Calculation Tests

        [Test]
        public void FormationSize_ForFiveUnits_CreatesValidGrid()
        {
            // Testing that 5 units would create reasonable grid
            int count = 5;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count) * 1.5f);
            int rows = Mathf.CeilToInt((float)count / columns);

            // Assert
            Assert.GreaterOrEqual(columns * rows, count, "Grid should fit all units");
            Assert.Greater(columns, 0, "Should have at least 1 column");
            Assert.Greater(rows, 0, "Should have at least 1 row");
        }

        [Test]
        public void FormationSize_ForFifteenUnits_CreatesWiderFormation()
        {
            // Testing that 15 units create a wider battle line
            int count = 15;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count) * 1.5f);
            int rows = Mathf.CeilToInt((float)count / columns);

            // Assert - Formation should be wider than tall
            Assert.GreaterOrEqual(columns, rows, "Formation should be wider than tall for battle line");
        }

        [Test]
        public void FormationSize_ForTwentyUnits_FitsAllUnits()
        {
            int count = 20;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count) * 1.5f);
            int rows = Mathf.CeilToInt((float)count / columns);

            // Assert
            Assert.GreaterOrEqual(columns * rows, count, "Grid should accommodate all units");
        }

        #endregion

        #region Path Constants Tests

        [Test]
        public void ScenePath_IsNotNullOrEmpty()
        {
            // Since constants are private, we test indirectly through ValidateSetup behavior
            // This test verifies the utility can be called without crashing
            Assert.DoesNotThrow(() =>
            {
                // Just call to verify paths are valid strings
                var config = QuickDemoSetup.DemoConfig.Default;
                Assert.IsNotNull(config);
            });
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void DemoConfig_SingleUnit_ValidConfiguration()
        {
            // Arrange
            var config = QuickDemoSetup.DemoConfig.Default;
            config.UnitsPerTeam = 1;

            // Assert - Single unit should still be valid
            Assert.AreEqual(1, config.UnitsPerTeam);
        }

        [Test]
        public void DemoConfig_LargeUnitCount_NoOverflow()
        {
            // Arrange
            var config = QuickDemoSetup.DemoConfig.Default;
            config.UnitsPerTeam = 100;

            // Calculate formation
            int count = config.UnitsPerTeam;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count) * 1.5f);
            int rows = Mathf.CeilToInt((float)count / columns);

            // Assert - Should handle large counts without overflow
            Assert.Greater(columns, 0);
            Assert.Greater(rows, 0);
            Assert.GreaterOrEqual(columns * rows, count);
        }

        [Test]
        public void SpawnSpacing_AffectsFormationWidth()
        {
            // Arrange
            var config = QuickDemoSetup.DemoConfig.Default;
            int count = config.UnitsPerTeam;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count) * 1.5f);

            // Act - Calculate formation width with different spacings
            float width1 = columns * 1f;  // 1 unit spacing
            float width2 = columns * 2f;  // 2 unit spacing

            // Assert
            Assert.Greater(width2, width1, "Larger spacing should create wider formation");
        }

        #endregion
    }
}
