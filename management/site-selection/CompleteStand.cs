// This file is part of the Harvest Management library for LANDIS-II.

using Landis.SpatialModeling;
using System.Collections.Generic;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A site-selection method that harvests all the sites in a stand.
    /// </summary>
    public class CompleteStand
        : ISiteSelector
    {
        private double areaSelected;

        //---------------------------------------------------------------------

        public CompleteStand()
        {
        }

        //---------------------------------------------------------------------

        double ISiteSelector.AreaSelected
        {
            get {
                return areaSelected;
            }
        }
		
        //---------------------------------------------------------------------
        //mark the whole area selected as harvested
        IEnumerable<ActiveSite> ISiteSelector.SelectSites(Stand stand)
        {
            areaSelected = stand.ActiveArea;
            stand.MarkAsHarvested();
			//mark this stand's event id
			stand.EventId = EventId.MakeNewId();
			
            return stand;
        }
		
    }
}
