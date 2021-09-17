using Landis.Library.DensityCohorts;
using Landis.Core;
using System.Collections.Generic;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// A set of specific-ages cohort selectors with percentages.
    /// </summary>
    /// <remarks>
    /// A convenience class to improve code readability.
    /// </remarks>
    public class DensityCohortSelectors
        : Dictionary<ISpecies, DiameterCohortSelector>, ICohortSelector
    {
        /*        private Dictionary<ISpecies, DiameterCohortSelector> selectionMethods;

                //---------------------------------------------------------------------

                /// <summary>
                /// Gets or sets the selection method for a species.
                /// </summary>
                /// <remarks>
                /// When getting the selection method, if a species has none, null is
                /// returned.
                /// </remarks>
                public SelectCohorts.Method this[ISpecies species]
                {
                    get
                    {
                        SelectCohorts.Method method;
                        selectionMethods.TryGetValue(species, out method);
                        return method;
                    }

                    set
                    {
                        selectionMethods[species] = value;
                    }
                }

                //---------------------------------------------------------------------

                /// <summary>
                /// Creates a new instance.
                /// </summary>
                public DensityCohortSelectors()
                {
                    selectionMethods = new Dictionary<ISpecies, SelectCohorts.Method>();
                }

                //---------------------------------------------------------------------

                /// <summary>
                /// Selects which of a species' cohorts are harvested.
                /// </summary>
                public void Harvest(ISpeciesCohorts cohorts,
                                    ISpeciesCohortBoolArray isHarvested)
                {
                    SelectCohorts.Method selectionMethod;
                    if (selectionMethods.TryGetValue(cohorts.Species, out selectionMethod))
                        selectionMethod(cohorts, isHarvested);
                }*/


        /// <summary>
        /// Creates a new instance with no selectors initially.
        /// </summary>
        public DensityCohortSelectors()
            : base()
        {
        }

        //----------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance with a set of selectors.
        /// </summary>
        public DensityCohortSelectors(DensityCohortSelectors densityCohortSelectors)
            : base(densityCohortSelectors)
        {
        }
    }
}
