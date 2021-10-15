using Landis.Utilities;
using Landis.Core;
using Landis.Library.DensityCohorts;
using Landis.Library.DensityHarvestManagement;
using System.Collections.Generic;
using System.Linq;
using Landis.SpatialModeling;
using log4net;
using System;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// A disturbance where density cohorts are thinned or removed.
    /// </summary>
    /// <remarks>

    /// </remarks>
    public class DensityCohortCutter
        : ICohortCutter, DensityCohorts.IDisturbance
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DensityCohortCutter));
        private static readonly bool isDebugEnabled = log.IsDebugEnabled;

        private DensityCohortSelectors densityCohortSelectors;
        private CohortCounts cohortCounts;
        private CohortCounts partialCohortCounts;
        private (string removalOrder, float residualBasal) removalMethod;

        private Dictionary<ICohort, float> sortedCohorts;
        private Dictionary<ISpecies, Dictionary<ushort, float>> cohortRemovals;

        /// <summary>
        /// What type of disturbance is this.
        /// </summary>
        public ExtensionType Type { get; protected set; }

        /// <summary>
        /// The site currently that the disturbance is impacting.
        /// </summary>
        public ActiveSite CurrentSite { get; protected set; }

        public ICohortSelector CohortSelector { get; protected set; }

        public string RemovalOrder
        {
            get
            {
                return removalMethod.removalOrder;
            }
        }

        public float ResidualBasal
        {
            get
            {
                return removalMethod.residualBasal;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DensityCohortCutter(ICohortSelector cohortSelector,
                                   (string, float) removalInfo,                                   
                                   ExtensionType                              extensionType)
           
        {
            this.densityCohortSelectors = new DensityCohortSelectors((DensityCohortSelectors)cohortSelector);
            this.removalMethod = removalInfo;
            partialCohortCounts = new CohortCounts();
            Type = extensionType;
            
        }

        //---------------------------------------------------------------------

        int Landis.Library.DensityCohorts.IDisturbance.ReduceOrKillMarkedCohort(Landis.Library.DensityCohorts.ICohort cohort)
        {
            int reduction = 0;
            float removal;
            Dictionary<ushort, float> speciesCohorts;


            if (cohortRemovals.TryGetValue(cohort.Species, out speciesCohorts))
            {
                if (speciesCohorts.TryGetValue(cohort.Age, out removal))
                { reduction = (int)(removal); }
                
            }
            if (reduction > 0)
            {
                cohortCounts.IncrementCount(cohort.Species);
                if (reduction < cohort.Treenumber)
                    partialCohortCounts.IncrementCount(cohort.Species);
            }
            
            Record(reduction, cohort);
            return reduction;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Cuts the cohorts at a site.
        /// </summary>
        public void Cut(ActiveSite   site,
                                 CohortCounts cohortCounts)
        {
            if (isDebugEnabled)
            {
                log.DebugFormat("    {0} is cutting site {1}; cohorts are:",
                                GetType().Name,
                                site.Location);
                Debug.WriteSiteCohorts(log, site);
            }

            SiteVars.GetExternalVars();
            // Use age-only cohort selectors to harvest whole cohorts
            // Note: the base method sets the CurrentSite property, and resets
            // the counts to 0 before cutting.
            //base.Cut(site, cohortCounts);

            // Then do any partial harvesting with the partial cohort selectors.
            CurrentSite = site;
            this.cohortCounts = cohortCounts;

            sortedCohorts = new Dictionary<ICohort, float>();
            calculateRemovals(CurrentSite);

            double baCheck = 0;

            foreach (ISpeciesCohorts speciesCohorts in SiteVars.Cohorts[site])
            {
                foreach (ICohort cohort in speciesCohorts)
                {
                    baCheck += (cohort.ComputeCohortBasalArea(cohort) / Model.Core.CellArea);
                }
            }
                       

            SiteVars.Cohorts[site].ReduceOrKillDensityCohorts(this);

            if (isDebugEnabled)
            {
                log.DebugFormat("    Cohorts after cutting site {0}:",
                                site.Location);
                Debug.WriteSiteCohorts(log, site);
            }
            if (partialCohortCounts.AllSpecies > 0)
            {
                SiteVars.CohortsPartiallyDamaged[site] = partialCohortCounts.AllSpecies;
            }
            partialCohortCounts.Reset();

            baCheck = 0;

            foreach (ISpeciesCohorts speciesCohorts in SiteVars.Cohorts[site])
            {
                foreach (ICohort cohort in speciesCohorts)
                {
                    baCheck += (cohort.ComputeCohortBasalArea(cohort) / Model.Core.CellArea);
                }
            }
        }

        //---------------------------------------------------------------------

        private void calculateRemovals(ActiveSite site)
        {
            ISiteCohorts siteCohorts = SiteVars.Cohorts[site];

            Dictionary<ICohort, float> cohortDict = new Dictionary<ICohort, float>();
            foreach (ISpeciesCohorts speciesCohorts in siteCohorts)
            {
                foreach (ICohort cohort in speciesCohorts)
                {
                    DiameterCohortSelector specificDiameterCohortSelector;
                    if (densityCohortSelectors.TryGetValue(cohort.Species, out specificDiameterCohortSelector))
                    {
                        if (specificDiameterCohortSelector.Selects(cohort))
                        {
                            cohortDict.Add(cohort, cohort.Diameter);
                        }
                    }
                }
            }

            if (removalMethod.removalOrder == "A") {
                var sortedDict = from entry in cohortDict orderby entry.Value descending select entry;
                sortedCohorts = sortedDict.ToDictionary(pair => pair.Key, pair => pair.Value);

            } else if (removalMethod.removalOrder == "B")
            {
                var sortedDict = from entry in cohortDict orderby entry.Value ascending select entry;
                sortedCohorts = sortedDict.ToDictionary(pair => pair.Key, pair => pair.Value);
            }

            cohortRemovals = new Dictionary<ISpecies, Dictionary<ushort, float>>();

            Stand standForCurrentSite = SiteVars.Stand[site];
            double cutBA = standForCurrentSite.SiteRemovalShare(site);

            foreach (KeyValuePair<ICohort, float> entry in sortedCohorts)
            {
                var cohortBA = entry.Key.ComputeCohortBasalArea(entry.Key) / Model.Core.CellArea;

                float calcRemoval = 0;
                
                if ((cohortBA <= cutBA) && (cutBA > 0))
                {
                    calcRemoval = entry.Key.Treenumber;                    
                }
                else if ((cohortBA > cutBA) && (cutBA > 0))
                {
                    calcRemoval = (float)Math.Ceiling(cutBA / (cohortBA / entry.Key.Treenumber));
                }

                if (!cohortRemovals.ContainsKey(entry.Key.Species))
                {
                    Dictionary<ushort, float> removalEntry = new Dictionary<ushort, float>();
                    removalEntry.Add(entry.Key.Age, entry.Key.Treenumber);
                    cohortRemovals.Add(entry.Key.Species, removalEntry);
                } else
                {
                    cohortRemovals[entry.Key.Species].Add(entry.Key.Age, calcRemoval);
                }

                cutBA -= cohortBA;

                if (cutBA < 0)
                {
                    break;
                }
                
            }

        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Records the amount a cohort's biomass was cut (reduced).
        /// </summary>
        protected void Record(int     reduction,
                              Landis.Library.DensityCohorts.ICohort cohort)
        {
            SiteBiomass.RecordHarvest(cohort.Species, reduction);
            if (isDebugEnabled)
                log.DebugFormat("    {0}, age {1}, biomass {2} : reduction = {3}",
                                cohort.Species.Name,
                                cohort.Age,
                                cohort.Biomass,
                                reduction);
        }
    }
}
