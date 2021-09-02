// This file is part of the Harvest Management library for LANDIS-II.

using System.Collections.Generic;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A collection of management areas.
    /// </summary>
    public interface IManagementAreaDataset
        : IEnumerable<ManagementArea>
    {
        /// <summary>
        /// Finds a management area by its map code.
        /// </summary>
        /// <returns>
        /// null if there is no management area with the specified map code.
        /// </returns>
        ManagementArea Find(uint mapCode);
    }
}
