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
        private static Tuple<string, float> defaultResidual;

        private DiametersAndRanges diametersAndRanges;
        //private IDictionary<ushort, uint> treeremovals;
        private IDictionary<float, Tuple<string, float>> residualBasal;
        //---------------------------------------------------------------------

        public DiameterCohortSelector()
        {
            defaultResidual = Tuple.Create("A", (float)0.0);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="ages">List of individual ages that are selected.</param>
        /// <param name="ranges">List of age ranges that are selected.</param>
        /// <param name="treeremovals">The number of trees to remove for each age.
        /// </param>
        public DiameterCohortSelector(IList<float>             diameters,
                                          IList<DiameterRange>   ranges,
                                          IDictionary<float, Tuple<string, float>> residuals)
        {
            diametersAndRanges = new DiametersAndRanges(diameters, ranges);
            this.residualBasal = new Dictionary<float, Tuple<string, float>>(residuals);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Selects which cohorts are harvested.
        /// </summary>
        /// <returns>
        /// true if the given cohort is to be harvested.  The cohort's biomass
        /// should be reduced by the percentage returned in the second
        /// parameter.
        /// </returns>
        public bool Selects(ICohort cohort, out Tuple<string, float> residual)
        {
                float diameterToLookUp = 0;
                DiameterRange? containingRange;
                if (diametersAndRanges.Contains(cohort.Diameter, out containingRange))
                {
                    if (! containingRange.HasValue)
                        diameterToLookUp = cohort.Diameter;
                    else {
                        diameterToLookUp = containingRange.Value.Start;
                    }
                    if (!residualBasal.TryGetValue(diameterToLookUp, out residual))
                        residual = defaultResidual;
                    return true;
                }
                residual = null;
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
