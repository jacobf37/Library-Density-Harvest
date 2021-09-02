using Landis.Library.DensitySiteHarvest;
using Landis.Library.Succession;
using Landis.Library.DensityCohorts;
using Landis.SpatialModeling;
using System.Collections.Generic;

namespace Landis.Library.DensityHarvestManagement
{

    public class BasalAreaRemoval
    {
        private Stand currentStand;
        private double standResidual;

        private Dictionary<ActiveSite, double> siteRemovalShare;


        //---------------------------------------------------------------------

        public BasalAreaRemoval()
        {
            
        }

        //---------------------------------------------------------------------
        
        public static Dictionary<ActiveSite, double> DistributeBasalRemoval(Stand stand, double residualBA)
        {
            var dbr = new BasalAreaRemoval();
            dbr.siteRemovalShare = new Dictionary<ActiveSite, double>();
            double baRemoval = stand.BasalArea - residualBA;

            foreach (ActiveSite site in stand)
            {
                double siteBA = 0;
                foreach (ISpeciesCohorts speciesCohorts in SiteVars.Cohorts[site])
                {
                    foreach (ICohort cohort in speciesCohorts)
                    {
                        siteBA += cohort.ComputeCohortBasalArea(cohort);
                    }
                }
                double cutBA = siteBA / stand.BasalArea * baRemoval;
                dbr.siteRemovalShare[site] = cutBA;
            }

            return dbr.siteRemovalShare;
        }

    }
}