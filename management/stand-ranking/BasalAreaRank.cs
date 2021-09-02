using Landis.Library.DensityCohorts;
using System.Collections.Generic;
using Landis.SpatialModeling;

namespace Landis.Library.DensityHarvestManagement
{
    public class BasalAreaRank
        : Landis.Library.DensityHarvestManagement.StandRankingMethod
    {
        private List<ISpeciesCohorts> cohorts;

        private BasalAreaTable rankTable;

        //---------------------------------------------------------------------

        public BasalAreaRank()
        {

        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Ranks stand using basal area (m-2 / hectare)
        /// </summary>
        protected override double ComputeRank(Stand stand, int i)
        {

            SiteVars.GetExternalVars();

            double standBasalArea = 0.0;

            foreach (ActiveSite site in stand)
            {
                foreach (ISpeciesCohorts speciesCohorts in SiteVars.Cohorts[site])
                {
                    foreach (ICohort cohort in speciesCohorts)
                    {
                        standBasalArea += cohort.ComputeCohortBasalArea(cohort);
                    }
                }
            }

            return (standBasalArea / stand.ActiveArea);
        }

    }
}
