// This file is part of the Harvest Management library for LANDIS-II.

using Landis.SpatialModeling;
using System.Collections.Generic;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A site-selection method.
    /// </summary>
    public interface ISiteSelector
    {
        /// <summary>
        /// Returns a collection of the sites selected from a stand and its
        /// neighbors.
        /// </summary>
        IEnumerable<ActiveSite> SelectSites(Stand stand);

        //---------------------------------------------------------------------

        /// <summary>
        /// The total area of the selected sites.
        /// </summary>
        double AreaSelected
        {
            get;
        }
    }
}
