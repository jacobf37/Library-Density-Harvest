// This file is part of the Harvest Management library for LANDIS-II.

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// The parameters for computing the economic rank for a species.
    /// </summary>
    public struct BasalAreaParameters
    {
        private byte fireRisk;
        //private ushort minAge;

        //---------------------------------------------------------------------

        /// <summary>
        /// The species' economic rank.
        /// </summary>
        public byte Rank
        {
            get {
                return fireRisk;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The minimum age at which the species has economic value.
        /// </summary>
        //public ushort MinimumAge
        //{
        //    get {
        //        return minAge;
        //    }
        //}

        //---------------------------------------------------------------------

        public BasalAreaParameters(byte   rank)
        {
            this.fireRisk = rank;
        }
    }
}
