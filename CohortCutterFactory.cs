
using Landis.Core;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// A factory for making instances of cohort cutters (WholeCohortCutter
    /// and PartialCohortCutter).
    /// </summary>
    public static class CohortCutterFactory
    {
        /// <summary>
        /// Creates a cohort cutter instance.
        /// </summary>
        /// <returns>
        /// An instance of WholeCohortCutter if no species is partially thinned
        /// by the cohort selector.  If the selector has a percentage for at
        /// least one species, then an instance of PartialCohortCutter is
        /// returned.
        /// </returns>
        public static ICohortCutter CreateCutter(ICohortSelector cohortSelector,
                                                 ExtensionType   extensionType)
        {
            ICohortCutter cohortCutter;

            cohortCutter = new DensityCohortCutter(cohortSelector,
                                                    DensityThinning.CohortSelectors,
                                                    extensionType);
            DensityThinning.ClearCohortSelectors();

            return cohortCutter;
        }

        /// <summary>
        /// Creates a cohort cutter instance.
        /// </summary>
        /// <returns>
        /// An instance of WholeCohortCutter if no species is partially thinned
        /// by the cohort selector.  If the selector has a percentage for at
        /// least one species, then an instance of PartialCohortCutter is
        /// returned.
        /// </returns>
        public static ICohortCutter CreateAdditionalCutter(ICohortSelector cohortSelector,
                                                 ExtensionType extensionType)
        {
            ICohortCutter cohortCutter;

            cohortCutter = new DensityCohortCutter(cohortSelector,
                                                    DensityThinning.AdditionalCohortSelectors,
                                                    extensionType);
            DensityThinning.ClearCohortSelectors();

            return cohortCutter;
        }
    }
}
