// Copyright 2005 University of Wisconsin

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// A range of diameters for cohort selection.
    /// </summary>
    public struct DiameterRange
    {
        private float start;
        private float end;

        //---------------------------------------------------------------------

        /// <summary>
        /// The starting diameter of the range.
        /// </summary>
        public float Start
        {
            get {
                return start;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The ending diameter of the range.
        /// </summary>
        public float End
        {
            get {
                return end;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public DiameterRange(float start,
                        float end)
    	{
            this.start = start;
            this.end   = end;
    	}

        //---------------------------------------------------------------------

        /// <summary>
        /// Does the range contain a particular age?
        /// </summary>
        public bool Contains(float diameter)
        {
            return (start <= diameter) && (diameter <= end);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Does the range overlap another range?
        /// </summary>
        public bool Overlaps(DiameterRange other)
        {
            return Contains(other.Start) || other.Contains(start);
        }
    }
}
