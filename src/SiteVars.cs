using Landis.Core;
using Landis.Library.DensityCohorts;
using Landis.SpatialModeling;
using Landis.Library.DensityHarvestManagement;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// The library's site variables
    /// </summary>
    public static class SiteVars
    {
        private static ISiteVar<int> cohortsPartiallyDamaged;

        /// <summary>
        /// The stand that each site belongs to.
        /// </summary>
        public static ISiteVar<Stand> Stand { get; private set; }

        //---------------------------------------------------------------------
        /// <summary>
        /// Site variable with biomass cohorts
        /// </summary>
        public static ISiteVar<ISiteCohorts> Cohorts { get; private set; }
        //---------------------------------------------------------------------
        /// <summary>
        /// Site variable counting cohorts partially damaged (not removed)
        /// </summary>
        public static ISiteVar<int> CohortsPartiallyDamaged
        {
            get
            {
                return cohortsPartiallyDamaged;
            }
            set
            {
                cohortsPartiallyDamaged = value;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes the site variables.
        /// </summary>
        public static void Initialize()
        {
            Cohorts = Model.Core.GetSiteVar<ISiteCohorts>("Succession.DensityCohorts");
            Stand = Model.Core.GetSiteVar<Stand>("Harvest.Stand");
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Get references to external site variables defined by non-harvest
        /// extensions.
        /// </summary>
        /// <remarks>
        /// This needs to be called after all the extensions in a scenario
        /// have created and registered their site variables.  They should do
        /// that in the LoadParameters method of their PlugIn classes.  So
        /// this method needs to be called in the PlugIn.Initialize method of
        /// harvest extensions.
        /// </remarks>
        public static void GetExternalVars()
        {
            Stand = Model.Core.GetSiteVar<Stand>("Harvest.Stand");
        }
    }
}
