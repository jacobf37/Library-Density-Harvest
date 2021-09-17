// Copyright 2005 University of Wisconsin

using System.Collections.Generic;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// A set of specific ages and ranges of ages for one species' cohorts.
    /// </summary>
    public class DiametersAndRanges
    {
        private List<float> diameters;
        private List<DiameterRange> ranges;

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DiametersAndRanges(IList<float> diameters,
                             IList<DiameterRange> ranges)
        {
            this.diameters = new List<float>(diameters);
            this.ranges = new List<DiameterRange>(ranges);
        }

        //---------------------------------------------------------------------

    	/// <summary>
        /// Is a particular age included among the set of specific ages and ranges?
        /// </summary>
        public bool Contains(float diameter, out DiameterRange? containingRange)
        {
            containingRange = null;
            if (diameters.Contains(diameter))
                return true;
            foreach (DiameterRange range in ranges)
            {
                if (range.Contains(diameter))
                {
                    containingRange = range;
                    return true;
                }
            }
            return false;
        }
    }
}
