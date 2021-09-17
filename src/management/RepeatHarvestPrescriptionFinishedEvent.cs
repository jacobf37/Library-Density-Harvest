// This file is part of the Harvest Management library for LANDIS-II.

using Landis.SpatialModeling;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// Represents when a prescription has finished a repeat harvest step
    /// </summary>
    public class RepeatHarvestPrescriptionFinishedEvent
    {
        /// <summary>
        /// Information about an event that's passed to event handlers.
        /// </summary>
        public class Args : System.EventArgs
        {
            /// <summary>
            /// The prescription that just finished its repeat harvest
            /// </summary>
            public AppliedPrescription Prescription { get; protected set; }

            /// <summary>
            /// The management area the prescription belongs to
            /// </summary>
            public ManagementArea MgmtArea { get; protected set; }

            /// <summary>
            /// Tracks what repeat the stands were on
            /// </summary>
            public uint RepeatNumber { get; protected set; }

            /// <summary>
            /// Indicates if this is the last repeat harvest for the time step for logging purposes
            /// </summary>
            public bool LastHarvest { get; protected set; }

            /// <summary>
            /// Creates a new instance.
            /// </summary>
            /// <param name="site">The prescription that just finished its repeat harvest</param>
            public Args(AppliedPrescription prescription, ManagementArea mgmtArea, uint repeatNumber, bool lastHarvest)
            {
                Prescription = prescription;
                MgmtArea = mgmtArea;
                RepeatNumber = repeatNumber;
                LastHarvest = lastHarvest;
            }
        }

        /// <summary>
        /// Handler for this type of event.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="eventArgs">Details about the event.</param>
        public delegate void Handler(object sender,
                                     Args eventArgs);
    }
}