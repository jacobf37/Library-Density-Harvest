// This file is part of the Harvest Management library for LANDIS-II.

using Landis.SpatialModeling;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// Represents when a repeat harvest has occured on a stand
    /// </summary>
    public class RepeatHarvestStandHarvestedEvent
    {
        /// <summary>
        /// Information about an event that's passed to event handlers.
        /// </summary>
        public class Args : System.EventArgs
        {
            /// <summary>
            /// The stand that was just harvested.
            /// </summary>
            public Stand Stand { get; protected set; }

            /// <summary>
            /// The management area the stand belongs to
            /// </summary>
            public ManagementArea MgmtArea { get; protected set; }

            /// <summary>
            /// Which repeat the prescription is on
            /// </summary>
            public uint RepeatNumber { get; protected set; }

            /// <summary>
            /// Creates a new instance.
            /// </summary>
            /// <param name="site">The stand that was just harvested.</param>
            public Args(Stand stand, uint repeatNumber)
            {
                Stand = stand;
                MgmtArea = stand.ManagementArea;
                RepeatNumber = repeatNumber;
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