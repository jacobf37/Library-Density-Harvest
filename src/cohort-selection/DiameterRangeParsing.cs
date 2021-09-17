// Copyright 2005 University of Wisconsin

using Landis.Utilities;
using System;
using FormatException = System.FormatException;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// Methods for parsing cohort age ranges.
    /// </summary>
    public static class DiameterRangeParsing
    {
        private static ParseMethod<float> floatParse;

        //---------------------------------------------------------------------

        static DiameterRangeParsing()
        {
            //  Register the local method for parsing a cohort age or age range.
            InputValues.Register<DiameterRange>(ParseDiameterRange);
            Landis.Utilities.Type.SetDescription<DiameterRange>("cohort diameter range");
            floatParse = InputValues.GetParseMethod<float>();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Initialize the class.
        /// </summary>
        /// <remarks>
        /// Client code can use this method to explicitly control when the
        /// class' static constructor is invoked.
        /// </remarks>
        public static void InitializeClass()
        {
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Parses a word for a cohort age or an age range (format: age-age).
        /// </summary>
        public static DiameterRange ParseDiameterRange(string word)
        {
            int delimiterIndex = word.IndexOf('-');
            if (delimiterIndex == -1) {
                float diameter = (float)Math.Round(ParseDiameter(word), 1);

                return new DiameterRange(diameter, diameter);
            }

            string startDiameter = word.Substring(0, delimiterIndex);
            string endDiameter = word.Substring(delimiterIndex + 1);
            if (endDiameter.Contains("-"))
                throw new FormatException("Valid format for diameter range: #-#");
            if (startDiameter == "") {
                if (endDiameter == "")
                    throw new FormatException("The range has no start and end diameters");
                else
                    throw new FormatException("The range has no start diameters");
            }
            float start = (float)Math.Round(ParseDiameter(startDiameter), 1);
            if (start < 0)
                throw new FormatException("The start diameter in the range must be >= 0");
            if (endDiameter == "")
                    throw new FormatException("The range has no end diameter");
            float end = (float)Math.Round(ParseDiameter(endDiameter), 1);
            if (start > end)
                throw new FormatException("The start diameter in the range must be <= the end diameter");
            return new DiameterRange(start, end);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Parses a cohort age from a text value.
        /// </summary>
        public static float ParseDiameter(string text)
        {
            try {
                return floatParse(text);
            }
            catch (System.OverflowException) {
                throw new FormatException(text + " is too large for an age; max = 65,535");
            }
            catch (System.Exception) {
                throw new FormatException(text + " is not a valid integer");
            }
        }
    }
}