// This file is part of the Harvest Management library for LANDIS-II.

using Landis.SpatialModeling;

namespace Landis.Library.DensityHarvestManagement
{
    public class UIntPixel : Pixel
    {
        public Band<uint> MapCode  = "The numeric code for each raster cell";

        public UIntPixel()
        {
            SetBands(MapCode);
        }
    }
}
