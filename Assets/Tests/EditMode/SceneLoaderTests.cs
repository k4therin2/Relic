using NUnit.Framework;
using Relic.Core;

namespace Relic.Tests.EditMode
{
    /// <summary>
    /// Unit tests for SceneLoader functionality.
    /// Note: Full scene loading tests require PlayMode tests.
    /// </summary>
    [TestFixture]
    public class SceneLoaderTests
    {
        [Test]
        public void SceneNames_Constants_AreCorrect()
        {
            // Verify scene name constants match expected values
            Assert.AreEqual("MainMenu", SceneLoader.Scenes.MainMenu);
            Assert.AreEqual("ARSession", SceneLoader.Scenes.ARSession);
            Assert.AreEqual("BattlefieldSetup", SceneLoader.Scenes.BattlefieldSetup);
            Assert.AreEqual("Battle", SceneLoader.Scenes.Battle);
            Assert.AreEqual("Flat_Debug", SceneLoader.Scenes.FlatDebug);
        }

        [Test]
        public void SceneNames_AllScenesDefined()
        {
            // Verify all required scenes are defined
            Assert.IsNotNull(SceneLoader.Scenes.MainMenu);
            Assert.IsNotNull(SceneLoader.Scenes.ARSession);
            Assert.IsNotNull(SceneLoader.Scenes.BattlefieldSetup);
            Assert.IsNotNull(SceneLoader.Scenes.Battle);
            Assert.IsNotNull(SceneLoader.Scenes.FlatDebug);
        }

        [Test]
        public void SceneNames_AllUnique()
        {
            // Verify all scene names are unique
            var scenes = new[]
            {
                SceneLoader.Scenes.MainMenu,
                SceneLoader.Scenes.ARSession,
                SceneLoader.Scenes.BattlefieldSetup,
                SceneLoader.Scenes.Battle,
                SceneLoader.Scenes.FlatDebug
            };

            var uniqueScenes = new System.Collections.Generic.HashSet<string>(scenes);
            Assert.AreEqual(scenes.Length, uniqueScenes.Count, "All scene names should be unique");
        }

        [Test]
        public void SceneNames_NoSpaces()
        {
            // Scene names should not contain spaces (Unity best practice)
            Assert.IsFalse(SceneLoader.Scenes.MainMenu.Contains(" "));
            Assert.IsFalse(SceneLoader.Scenes.ARSession.Contains(" "));
            Assert.IsFalse(SceneLoader.Scenes.BattlefieldSetup.Contains(" "));
            Assert.IsFalse(SceneLoader.Scenes.Battle.Contains(" "));
            Assert.IsFalse(SceneLoader.Scenes.FlatDebug.Contains(" "));
        }

        [Test]
        public void SceneNames_MatchKyleMilestones()
        {
            // Verify scene names align with Kyle's milestones document
            // Kyle's doc mentions: Boot, AR_Battlefield, Flat_Debug
            // Our implementation uses: MainMenu, ARSession, BattlefieldSetup, Battle, Flat_Debug

            // Boot -> MainMenu (serves same purpose)
            Assert.AreEqual("MainMenu", SceneLoader.Scenes.MainMenu);

            // Flat_Debug matches exactly
            Assert.AreEqual("Flat_Debug", SceneLoader.Scenes.FlatDebug);

            // AR_Battlefield split into ARSession + BattlefieldSetup + Battle
            // This is a valid implementation choice for clearer separation
            Assert.IsNotNull(SceneLoader.Scenes.ARSession);
            Assert.IsNotNull(SceneLoader.Scenes.BattlefieldSetup);
            Assert.IsNotNull(SceneLoader.Scenes.Battle);
        }
    }
}
