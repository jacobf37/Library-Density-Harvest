// This file is part of the Harvest Management library for LANDIS-II.

using Landis.Library.DensitySiteHarvest;
using Landis.Library.Succession;
using Landis.SpatialModeling;
using log4net;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A prescription describes how stands are ranked, how sites are selected,
    /// and which cohorts are harvested.
    /// </summary>
    public class Prescription
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Prescription));
        private static readonly bool isDebugEnabled = log.IsDebugEnabled;

        private static int nextNumber = 1;
        private int number;
        private string name;
        private IStandRankingMethod rankingMethod;
        private ISiteSelector siteSelector;
        private ICohortCutter cohortCutter;
        private Planting.SpeciesList speciesToPlant;
        private Stand currentStand;
        private int minTimeSinceDamage;
        private bool preventEstablishment;
        private CohortCounts cohortCounts;
        private bool isSingleRepeatStep;
        protected bool isSingleRepeatPrescription;
        private int repeatNumber = 0;
        
        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's number.
        /// </summary>
        /// <remarks>
        /// Each prescription's number is unique, and is generated and assigned
        /// when the prescription is initialized.
        /// </remarks>
        public int Number
        {
            get {
                return number;
            }
        }

        /// <summary>
        /// Indicates if the prescription is performing the final harvest for single repeat prescriptions
        /// </summary>
        public bool IsSingleRepeatStep
        {
            set
            {
                isSingleRepeatStep = value;
            }
            get
            {
                return isSingleRepeatStep;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The total number of prescription.
        /// </summary>
        public static int Count
        {
            get {
                return nextNumber;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's name.
        /// </summary>
        public string Name
        {
            get {
                return name;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's method for ranking stands.
        /// </summary>
        public IStandRankingMethod StandRankingMethod
        {
            get {
                return rankingMethod;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's method for selecting sites in stands.
        /// </summary>
        public ISiteSelector SiteSelectionMethod
        {
            set {
                siteSelector = value;
            }
            get {
                return siteSelector;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Sets the cohorts that will be removed by the prescription.
        /// </summary>
        /// <remarks>
        /// The purpose of this property is to allow derived classes to change
        /// the cohort selector; for example, a single repeat-harvest switching
        /// between its two cohort selectors.
        /// </remarks>
        protected ICohortCutter CohortCutter
        {
            set {
                cohortCutter = value;
            }
        }

        protected ISiteSelector SiteSelector
        {
            set
            {
                siteSelector = value;
            }
            get
            {
                return siteSelector;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Sets the optional list of species that are planted at each site
        /// harvested by the prescription.
        /// </summary>
        /// <remarks>
        /// The purpose of this property is to allow derived classes to change
        /// the species list; for example, a single repeat-harvest switching
        /// between the lists for initial harvests and repeat harvests.
        /// </remarks>
        public Planting.SpeciesList SpeciesToPlant
        {
            set {
                speciesToPlant = value;
            }
            get {
                return speciesToPlant;
            }
        }
        
        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's time limit for harvesting damaged sites. If
        /// a site has been damaged less than this, it should not be harvested.
        /// </summary>
        public int MinTimeSinceDamage {
            get {
                return minTimeSinceDamage;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription's boolean variable for preventing establishment
        /// </summary>
        public bool PreventEstablishment {
            get {
                return preventEstablishment;
            }
        }

        //---------------------------------------------------------------------

        public Prescription(string               name,
                            IStandRankingMethod  rankingMethod,
                            ISiteSelector        siteSelector,
                            ICohortCutter        cohortCutter,
                            Planting.SpeciesList speciesToPlant,
                            int                  minTimeSinceDamage,
                            bool                 preventEstablishment)
        {
            this.number = nextNumber;
            nextNumber++;

            this.name = name;
            this.rankingMethod = rankingMethod;
            this.siteSelector = siteSelector;
            this.cohortCutter = cohortCutter;
             
            this.speciesToPlant = speciesToPlant;
            this.minTimeSinceDamage = minTimeSinceDamage;
            this.preventEstablishment = preventEstablishment;
            this.isSingleRepeatStep = false;
            this.isSingleRepeatPrescription = false;

            cohortCounts = new CohortCounts();
        }

        //---------------------------------------------------------------------
        
        /// <summary>
        /// Harvests a stand (and possibly its neighbors) according to the
        /// prescription's site-selection method.
        /// </summary>
        /// <returns>
        /// The area that was harvested (units: hectares).
        /// </returns>
        // This is called by AppliedPrescription
        public virtual void Harvest(Stand stand)
        {
            if (isDebugEnabled)
                log.DebugFormat("  Harvesting stand {0} by {1} ...", stand.MapCode, Name);

            //set prescription name for stand
            stand.PrescriptionName = this.Name;
            stand.HarvestedRank = AppliedPrescription.CurrentRank;
            stand.LastPrescription = this;
            
            stand.MinTimeSinceDamage = this.minTimeSinceDamage;
            
            //set current stand
            currentStand = stand;
            currentStand.ClearDamageTable();

            DensityCohortCutter currentCutter = (DensityCohortCutter)this.cohortCutter;

            stand.DistributeBasalRemoval(currentCutter);

            // SelectSites(stand) is where either complete, complete stand spreading, or partial stand
            // spreading are activated.
            // tjs - This is what gets the sites that will be harvested
            
            if (isSingleRepeatStep)
            {
                foreach (ActiveSite site in stand)
                {
                    // Check if this was previously harvested in the last step
                    if (stand.IsSiteSetAside(site, this.Name))
                    {
                        HarvestSite(site, stand);
                    }
                }
            }
            else
            {
                foreach (ActiveSite site in SiteSelector.SelectSites(stand))
                {
                    HarvestSite(site, stand);

                    // Only queue up for a repeat harvest if cohorts were cut
                    if (this.isSingleRepeatPrescription && cohortCounts.AllSpecies > 0)
                    {
                        stand.SetSiteAside(site, this.Name);
                    }
                }
            }
            return; 
        }

        /// <summary>
        /// Does the actual work of harvesting individual sites
        /// </summary>
        public void HarvestSite(ActiveSite site, Stand stand)
        {
            // Site selection may have spread to other stands beyond the
            // original stand.
            Stand standForCurrentSite = SiteVars.Stand[site];

            // Always record the prescription even if nothing harvested; Supports plantOnly prescriptions
            SiteVars.Prescription[site] = this;
            SiteVars.PrescriptionName[site] = this.Name;

            if (isDebugEnabled)
                log.DebugFormat("  Cutting cohorts at {0} in stand {1}{2}", site,
                                SiteVars.Stand[site].MapCode,
                                (standForCurrentSite == stand)
                                    ? ""
                                    : string.Format(" (initial stand {0})",
                                                    stand.MapCode));
            cohortCutter.Cut(site, cohortCounts);

            if (cohortCounts.AllSpecies > 0)
            {
                SiteVars.CohortsDamaged[site] = cohortCounts.AllSpecies;
                standForCurrentSite.DamageTable.IncrementCounts(cohortCounts);
                stand.LastAreaHarvested += Model.Core.CellArea;
                if (isDebugEnabled)
                    log.DebugFormat("    # of cohorts damaged = {0}; stand.LastAreaHarvested = {1}",
                                    SiteVars.CohortsDamaged[site],
                                    stand.LastAreaHarvested);
                HarvestExtensionMain.OnSiteHarvest(this, site);
            }

            if (speciesToPlant != null)
            {
                if (isDebugEnabled)
                {
                    Model.Core.UI.WriteLine("  {0} {1}", speciesToPlant.ToString(), site.ToString());
                }
                Reproduction.ScheduleForPlanting(speciesToPlant, site);
            }
        }
    }
}