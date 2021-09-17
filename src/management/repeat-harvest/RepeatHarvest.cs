// This file is part of the Harvest Management library for LANDIS-II.

using Landis.Library.DensitySiteHarvest;
using Landis.Library.Succession;
using System.Collections.Generic;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A repeat harvest is a variation of a prescription that harvests stands
    /// and then sets them aside for additional harvests.
    /// </summary>
    public class RepeatHarvest
        : Prescription
    {
        private int interval;
        private StandSpreading spreadingSiteSelector;
        private List<Stand> harvestedStands;
        private int timesToRepeat;

        //---------------------------------------------------------------------

        /// <summary>
        /// The time interval between a stand's initial harvest and each of
        /// its additional harvests.
        /// </summary>
        /// <remarks>
        /// Units: years.
        /// </remarks>
        public int Interval
        {
            get {
                return interval;
            }
        }

        /// <summary>
        /// How many times the prescription should repeat, supplied by the user
        /// </summary>
        public int TimesToRepeat
        {
            get
            {
                return timesToRepeat;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The list of stands that were harvested during the most recent call
        /// to the Harvest method.
        /// </summary>
        public List<Stand> HarvestedStands
        {
            get {
                return harvestedStands;
            }
        }

        //---------------------------------------------------------------------

        public RepeatHarvest(string               name,
                             IStandRankingMethod  rankingMethod,
                             ISiteSelector        siteSelector,
                             ICohortCutter        cohortCutter,
                             Planting.SpeciesList speciesToPlant,
                             int                  minTimeSinceDamage,
                             bool                 preventEstablishment,
                             int                  interval,
                             int                  timesToRepeat = 0)
            : base(name, rankingMethod, siteSelector, cohortCutter, speciesToPlant, minTimeSinceDamage, preventEstablishment)
        {
            this.interval = interval;
            this.spreadingSiteSelector = siteSelector as StandSpreading;
            this.harvestedStands = new List<Stand>();

            if (timesToRepeat > 1)
            {
                this.timesToRepeat = timesToRepeat;
            }
            else
            {
                this.timesToRepeat = int.MaxValue;
            }
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
            base.Harvest(stand);
            
            harvestedStands.Clear();

            //  Add stand to list only if actually harvested
            if(stand.LastAreaHarvested > 0)
                harvestedStands.Add(stand); 
            
            if (spreadingSiteSelector != null)
                harvestedStands.AddRange(spreadingSiteSelector.HarvestedNeighbors);
            
            return; // areaHarvested;
        }
    }
}
