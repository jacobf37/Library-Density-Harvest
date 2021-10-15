using Landis.Library.DensityCohorts;
using System.Collections.Generic;
using System;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// Selects specific ages and ranges of ages among a species' cohorts
    /// for harvesting.
    /// </summary>
    public class DiameterCohortSelector
        
    {
        private DiametersAndRanges diametersAndRanges;
        //private IDictionary<ushort, uint> treeremovals;



        //---------------------------------------------------------------------

        /// <summary>
        /// Selects which cohorts are harvested.
        /// </summary>
        /// <returns>
        /// true if the given cohort is to be harvested.  The cohort's biomass
        /// should be reduced by the percentage returned in the second
        /// parameter.
        /// </returns>
        public bool Selects(ICohort cohort)
        {
            DiameterRange? notUsed;
            if (diametersAndRanges.Contains(cohort.Diameter, out notUsed))
            {

                return true;
            }
                
            return false;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DiameterCohortSelector(IList<float> diameters,
                                          IList<DiameterRange> ranges)
        {
            diametersAndRanges = new DiametersAndRanges(diameters, ranges);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Selects which of a species' cohorts are harvested.
        /// </summary>
        public void SelectCohorts(ISpeciesCohorts cohorts,
                                  ISpeciesCohortBoolArray isHarvested)
        {
            int i = 0;
            foreach (ICohort cohort in cohorts)
            {
                DiameterRange? notUsed;
                if (diametersAndRanges.Contains(cohort.Diameter, out notUsed))
                    isHarvested[i] = true;
                i++;
            }
        }


    }
}
