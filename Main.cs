using Landis.Core;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// Main entry point for initializing and configuring the library.
    /// </summary>
    public static class Main
    {
        /// <summary>
        /// Initialize the library for use by client code.
        /// </summary>
        public static void InitializeLib(ICore modelCore)
        {
            Landis.Library.SiteHarvest.Main.InitializeLib(modelCore);

            Model.Core = modelCore;
            SiteVars.Initialize();
            DensityThinning.InitializeClass();
        }
    }
}
