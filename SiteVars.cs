using Landis.Core;
using Landis.Library.DensityCohorts;
using Landis.SpatialModeling;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// The library's site variables
    /// </summary>
    public static class SiteVars
    {
        private static ISiteVar<int> cohortsPartiallyDamaged;

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
        }
    }
}
