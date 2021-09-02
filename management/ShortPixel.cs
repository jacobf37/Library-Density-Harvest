// This file is part of the Harvest Management library for LANDIS-II.

using Landis.SpatialModeling;

namespace Landis.Library.DensityHarvestManagement
{
    public class ShortPixel : Pixel
    {
        public Band<short> MapCode  = "The numeric code for each raster cell";

        public ShortPixel()
        {
            SetBands(MapCode);
        }
    }
}
