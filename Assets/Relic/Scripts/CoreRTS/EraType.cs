namespace Relic.CoreRTS
{
    /// <summary>
    /// Enum defining the different historical/fictional eras in the game.
    /// Used for filtering upgrades and unit archetypes.
    /// </summary>
    /// <remarks>
    /// See Kyle's milestones.md for era requirements.
    /// Each era has distinct visual themes, units, and upgrades.
    /// </remarks>
    public enum EraType
    {
        /// <summary>Ancient era - bows, legions, shields</summary>
        Ancient = 0,

        /// <summary>Medieval era - crossbows, knights, castles</summary>
        Medieval = 1,

        /// <summary>World War II era - rifles, tanks, trenches</summary>
        WWII = 2,

        /// <summary>Future era - lasers, mechs, energy shields</summary>
        Future = 3,

        /// <summary>Universal upgrade that applies to all eras</summary>
        All = 99
    }
}
