// This file is part of the Harvest Management library for LANDIS-II.

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A ranking requirement which requires a stand be no more than a certain
    /// maximum age to be eligible for ranking.
    /// </summary>
    public class MaximumAge
        : IRequirement
    {
        private ushort maxAge;

        //---------------------------------------------------------------------

        public MaximumAge(ushort age)
        {
            maxAge = age;
        }

        //---------------------------------------------------------------------

        bool IRequirement.MetBy(Stand stand)
        {
            return stand.Age <= maxAge;
        }
    }
}
