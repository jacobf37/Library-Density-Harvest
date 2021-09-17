// This file is part of the Harvest Management library for LANDIS-II.

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A collection of parameters for computing the economic ranks of species.
    /// </summary>
    public class BasalAreaTable
    {
        private BasalAreaParameters[] parameters;

        //---------------------------------------------------------------------

        public BasalAreaParameters this[int fuelTypeIndex]
        {
            get {
                return parameters[fuelTypeIndex];
            }

            set {
                parameters[fuelTypeIndex] = value;
            }
        }

        //---------------------------------------------------------------------

        public BasalAreaTable()
        {
            parameters = new BasalAreaParameters[150];  //up to 150 fuel types
            //foreach (FireRiskParameters fireRiskParm in parameters)
            //{
            //    fireRiskParm 
            //}
        }
    }
}
