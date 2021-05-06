using Landis.Utilities;
using Landis.Library.DensityCohorts;
using Landis.Library.SiteHarvest;
using System.Collections.Generic;

namespace Landis.Library.DensityHarvest
{
    /// <summary>
    /// Selects specific ages and ranges of ages among a species' cohorts
    /// for harvesting.
    /// </summary>
    public class SpecificAgesCohortSelector
    {
        private static uint defaultRemoval;

        private AgesAndRanges agesAndRanges;
        private IDictionary<ushort, uint> treeremovals;

        //---------------------------------------------------------------------

        static SpecificAgesCohortSelector()
        {
            defaultRemoval = 1;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="ages">List of individual ages that are selected.</param>
        /// <param name="ranges">List of age ranges that are selected.</param>
        /// <param name="treeremovals">The number of trees to remove for each age.
        /// </param>
        public SpecificAgesCohortSelector(IList<ushort>                   ages,
                                          IList<AgeRange>                 ranges,
                                          IDictionary<ushort, uint> treeremovals)
        {
            agesAndRanges = new AgesAndRanges(ages, ranges);
            this.treeremovals = new Dictionary<ushort, uint>(treeremovals);
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
        public bool Selects(ICohort cohort, out uint removal)
        {
                ushort ageToLookUp = 0;
                AgeRange? containingRange;
                if (agesAndRanges.Contains(cohort.Age, out containingRange))
                {
                    if (! containingRange.HasValue)
                        ageToLookUp = cohort.Age;
                    else {
                        ageToLookUp = containingRange.Value.Start;
                    }
                    if (!treeremovals.TryGetValue(ageToLookUp, out removal))
                        removal = defaultRemoval;
                    return true;
                }
                removal = 0;
                return false;
        }
    }
}
