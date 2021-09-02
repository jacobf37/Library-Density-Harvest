// This file is part of the Harvest Management library for LANDIS-II.

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A requirement that a stand must meet in order to be eligible for
    /// ranking.
    /// </summary>
    public interface IRequirement
    {
        /// <summary>
        /// Does a stand meet the requirement?
        /// </summary>
        bool MetBy(Stand stand);
    }
}
