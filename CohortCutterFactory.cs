
using Landis.Core;
using Landis.Library.SiteHarvest;

namespace Landis.Library.DensityHarvest
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
            if (DensityThinning.CohortSelectors.Count == 0)
                cohortCutter = new WholeCohortCutter(cohortSelector, extensionType);
            else
            {
                cohortCutter = new PartialCohortCutter(cohortSelector,
                                                       DensityThinning.CohortSelectors,
                                                       extensionType);
                DensityThinning.ClearCohortSelectors();
            }
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
            if (DensityThinning.AdditionalCohortSelectors.Count == 0)
                cohortCutter = new WholeCohortCutter(cohortSelector, extensionType);
            else
            {
                cohortCutter = new PartialCohortCutter(cohortSelector,
                                                       DensityThinning.AdditionalCohortSelectors,
                                                       extensionType);
                DensityThinning.ClearCohortSelectors();
            }
            return cohortCutter;
        }
    }
}
