// This file is part of the Harvest Management library for LANDIS-II.

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// Helper methods for generating id numbers for events.
    /// </summary>
    public static class EventId
    {
        private static int mostRecentId = 0;

        /// <summary>
        /// Make the id number for a new disturbance event.
        /// </summary>
        public static int MakeNewId()
        {
            int newId = ++mostRecentId;
            return newId;
        }
    }
}
