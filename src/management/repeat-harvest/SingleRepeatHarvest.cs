// This file is part of the Harvest Management library for LANDIS-II.

using Landis.Library.DensitySiteHarvest;
using Landis.Library.Succession;
using Landis.SpatialModeling;
using System;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A single repeat-harvest harvests stands and then sets them aside for
    /// just one additional harvest.  The additional harvest can remove a
    /// different set of cohorts than the initial harvest.
    /// </summary>
    public class SingleRepeatHarvest
        : RepeatHarvest
    {
        private ICohortCutter initialCohortCutter;
        private Planting.SpeciesList initialSpeciesToPlant;
        private ISiteSelector initialSiteSelector;

        private ICohortCutter additionalCohortCutter;
        private Planting.SpeciesList additionalSpeciesToPlant;

        //---------------------------------------------------------------------

        public SingleRepeatHarvest(string               name,
                                   IStandRankingMethod  rankingMethod,
                                   ISiteSelector        siteSelector,
                                   ICohortCutter        cohortCutter,
                                   Planting.SpeciesList speciesToPlant,
                                   ICohortCutter        additionalCohortCutter,
                                   Planting.SpeciesList additionalSpeciesToPlant,
                                   int                  minTimeSinceDamage,
                                   bool                 preventEstablishment,
                                   int                  interval)
            : base(name, rankingMethod, siteSelector, cohortCutter, speciesToPlant,
                   minTimeSinceDamage, preventEstablishment, interval)
        {
            this.initialCohortCutter = cohortCutter;
            this.initialSiteSelector = SiteSelector;
            this.initialSpeciesToPlant = speciesToPlant;

            this.additionalCohortCutter = additionalCohortCutter;
            this.additionalSpeciesToPlant = additionalSpeciesToPlant;
            this.isSingleRepeatPrescription = true;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Harvests a stand (and possibly its neighbors) according to the
        /// repeat-harvest's site-selection method.
        /// </summary>
        /// <returns>
        /// The area that was harvested (units: hectares).
        /// </returns>
        public override void Harvest(Stand stand)
        {
            if (stand.IsSetAside) {
                CohortCutter = additionalCohortCutter;
                SpeciesToPlant = additionalSpeciesToPlant;
                //
                //if(this.SiteSelectionMethod.GetType() == Landis.Extension.BiomassHarvest.PartialStandSpreading)
                //  SiteSelector = BiomassHarvest.WrapSiteSelector(SiteSelector);
                this.IsSingleRepeatStep = true;
            }
            else {
                CohortCutter = initialCohortCutter;
                SpeciesToPlant = initialSpeciesToPlant;
            }
            base.Harvest(stand);

            // Unmark specific cells that were set aside
            if (stand.IsSetAside)
            {
                stand.ClearSetAsideSites(this.Name);
                this.IsSingleRepeatStep = false;
            }

            return; 
        }
    }
}
