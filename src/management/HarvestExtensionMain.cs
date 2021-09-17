// This file is part of the Harvest Management library for LANDIS-II.

using Landis.Core;
using Landis.SpatialModeling;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// Base class for the main class of a harvest extension.
    /// </summary>
    public abstract class HarvestExtensionMain
        : ExtensionMain 
    {
        /// <summary>
        /// The extension type for harvest extensions.
        /// </summary>
        public static readonly ExtensionType ExtType = new ExtensionType("disturbance:harvest");

        public HarvestExtensionMain(string name)
            : base(name, ExtType)
        {
        }

        /// <summary>
        /// Raised when an individual site has been harvested.  Handlers
        /// can be added that do additional bookkeeping for the site.
        /// </summary>
        public static event SiteHarvestedEvent.Handler SiteHarvestedEvent;

        /// <summary>
        /// Raised when a stand has been harvested in a repeat harvest.
        /// This will be handled so logging repeats in eventlog is possible
        /// </summary>
        public static event RepeatHarvestStandHarvestedEvent.Handler RepeatStandHarvestedEvent;

        /// <summary>
        /// Raised when a prescription has finished a repeat harvest.
        /// Handled in Biomass-Harvest to make logging repeats in the
        /// summary log possible
        /// </summary>
        public static event RepeatHarvestPrescriptionFinishedEvent.Handler RepeatPrescriptionFinishedEvent;

        /// <summary>
        /// Signals that a site has just been harvested.
        /// </summary>
        public static void OnSiteHarvest(object sender,
                                         ActiveSite site)
        {
            if (SiteHarvestedEvent != null)
                SiteHarvestedEvent(sender, new SiteHarvestedEvent.Args(site));
        }

        /// <summary>
        /// Signals that a stand has been harvested in a repeat step
        /// </summary>
        public static void OnRepeatStandHarvest(object sender,
                                         Stand stand,
                                         uint repeatNumber)
        {
            if (RepeatStandHarvestedEvent != null)
                RepeatStandHarvestedEvent(sender, new RepeatHarvestStandHarvestedEvent.Args(stand, repeatNumber));
        }

        /// <summary>
        /// Signals that a prescription is finished with a repeat step
        /// </summary>
        public static void OnRepeatHarvestFinished(object sender,
                                         AppliedPrescription prescription,
                                         ManagementArea mgmtArea,
                                         uint repeatNumber,
                                         bool lastHarvest)
        {
            if (RepeatPrescriptionFinishedEvent != null)
                RepeatPrescriptionFinishedEvent(sender, new RepeatHarvestPrescriptionFinishedEvent.Args(prescription, mgmtArea, repeatNumber, lastHarvest));
        }
    }
}
