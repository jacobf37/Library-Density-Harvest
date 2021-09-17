using Landis.Utilities;
using Landis.Core;
using Landis.Library.SiteHarvest;
using System.Collections.Generic;
using System.Text;
using System;
using Landis.Library.DensityCohorts;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// Static class for partial thinning.
    /// </summary>
    public static class ResidualThinning
    {
        /// <summary>
        /// The partial cohort selectors that have been read for each species.
        /// </summary>
        public static DensityCohortSelectors CohortSelectors { get; private set; }
        public static DensityCohortSelectors AdditionalCohortSelectors { get; private set; }
        private static IDictionary<float, Tuple<string, float>> residualBasal;


        //---------------------------------------------------------------------

        static ResidualThinning()
        {
            // Force the harvest library to register its read method for age
            // ranges.  Then replace it with this project's read method that
            // handles percentages for partial thinning.
            DiameterRangeParsing.InitializeClass();
            InputValues.Register<DiameterRange>(ResidualThinning.ReadDiameterRange);
            residualBasal = new Dictionary<float, Tuple<string, float>>();
            CohortSelectors = new DensityCohortSelectors();
            AdditionalCohortSelectors = new DensityCohortSelectors();
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
        /// Creates a new InputValueException for an invalid percentage input
        /// value.
        /// </summary>
        /// <returns></returns>
        public static InputValueException MakeInputValueException(string value,
                                                                  string message)
        {
            return new InputValueException(value,
                                           string.Format("\"{0}\" is not a valid percentage for partial thinning", value),
                                           new MultiLineText(message));
        }

        //---------------------------------------------------------------------
        private class ThinningDistributeSelection
        {
            //Names for each acceptable thinning distribution method
            public const string FromAbove = "A";    //Thinning from above (starting with largest cohorts first)
            public const string FromBelow = "B";    //Thinning from below (starting with smallest cohorts first)
            public const string Distributed = "D";  //Distributed across diameters
            
        }

        //---------------------------------------------------------------------



        //---------------------------------------------------------------------

        /// <summary>
        /// Reads number of trees for partial thinning of a cohort age.
        /// </summary>
        /// <remarks>
        /// The number is bracketed by parentheses.
        /// </remarks>
        public static InputValue<Tuple<string, float>> ReadResidual(StringReader reader)
        {

            TextReader.SkipWhitespace(reader);
            //index = reader.Index;

            //  Read left parenthesis
            int nextChar = reader.Peek();
            if (nextChar == -1)
                throw new InputValueException();  // Missing value
            if (nextChar != '(')
                throw MakeInputValueException(TextReader.ReadWord(reader),
                                              "Value does not start with \"(\"");

            StringBuilder valueAsStr = new StringBuilder();
            valueAsStr.Append((char)(reader.Read()));

            //  Read whitespace between '(' and percentage
            valueAsStr.Append(ReadWhitespace(reader));

            //  Read percentage
            string word = ReadWord(reader, ')');
            if (word == "")
                throw MakeInputValueException(valueAsStr.ToString(),
                                              "No value after \"(\"");
            valueAsStr.Append(word);

            int delimiterIndex = word.IndexOf('-');

            string distributorStr = word.Substring(0, delimiterIndex);
            string residualStr = word.Substring(delimiterIndex + 1);

            if (distributorStr != "A|B|D")
            {
                throw MakeInputValueException(valueAsStr.ToString(),
                                              string.Format("Unknown distributor value - \"{0}\" ", distributorStr));
            }

            float residual;
            try
            {
                residual = float.Parse(residualStr);
            }
            catch (System.FormatException exc)
            {
                throw MakeInputValueException(valueAsStr.ToString(),
                                              exc.Message);
            }

            if (residual < 0.0 || residual > 500000)
                throw MakeInputValueException(valueAsStr.ToString(),
                                              string.Format("{0} is not between 0 and 500,000", residual));

            //  Read whitespace and ')'
            valueAsStr.Append(ReadWhitespace(reader));
            char? ch = TextReader.ReadChar(reader);
            if (!ch.HasValue)
                throw MakeInputValueException(valueAsStr.ToString(),
                                              "Missing \")\"");
            valueAsStr.Append(ch.Value);
            if (ch != ')')
                throw MakeInputValueException(valueAsStr.ToString(),
                                              string.Format("Value ends with \"{0}\" instead of \")\"", ch));


            var outResidual = new Tuple<string, float>(ThinningDistributeSelection.FromAbove, residual);




            return new InputValue<Tuple<string, float>>(outResidual, "Number of Trees");
        }
        //---------------------------------------------------------------------

        //---------------------------------------------------------------------

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads whitespace from a string reader.
        /// </summary>
        public static string ReadWhitespace(StringReader reader)
        {
            StringBuilder whitespace = new StringBuilder();
            int i = reader.Peek();
            while (i != -1 && char.IsWhiteSpace((char) i)) {
                whitespace.Append((char) reader.Read());
                i = reader.Peek();
            }
            return whitespace.ToString();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads a word from a string reader.
        /// </summary>
        /// <remarks>
        /// The word is terminated by whitespace, the end of input, or a
        /// particular delimiter character.
        /// </remarks>
        public static string ReadWord(StringReader reader,
                                      char         delimiter)
        {
            StringBuilder word = new StringBuilder();
            int i = reader.Peek();
            while (i != -1 && ! char.IsWhiteSpace((char) i) && i != delimiter) {
                word.Append((char) reader.Read());
                i = reader.Peek();
            }
            return word.ToString();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads a cohort age or a range of ages (format: age-age) followed
        /// by an optional percentage for partial thinning.
        /// </summary>
        /// <remarks>
        /// The optional percentage is bracketed by parenthesis.
        /// </remarks>
        public static InputValue<DiameterRange> ReadDiameterRange(StringReader reader,
                                                          out int      index)
        {
            TextReader.SkipWhitespace(reader);
            index = reader.Index;

            string word = ReadWord(reader, '(');
            if (word == "")
                throw new InputValueException();  // Missing value

            DiameterRange diameterRange = DiameterRangeParsing.ParseDiameterRange(word);

            //  Does a residual follow?
            TextReader.SkipWhitespace(reader);
            if (reader.Peek() == '(') {

                InputValue<Tuple<string, float>> residual = ReadResidual(reader);

                if (residual.String != "(100%)")
                {
                    residualBasal[diameterRange.Start] = residual;
                }
            }

            return new InputValue<DiameterRange>(diameterRange, word);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates and stores a specific-ages cohort selector for a species
        /// if at least one percentage was found among the list of ages and
        /// ranges that were read.
        /// </summary>
        /// <returns>
        /// True if a selector was created and stored in the CohortSelectors
        /// property.  False is returned if no selector was created because
        /// there were no percentages read for any age or range.
        /// </returns>
        public static bool CreateCohortSelectorFor(ISpecies species,
                                                   IList<float> diameters,
                                                   IList<DiameterRange> diameterRanges)
        {
            if (residualBasal.Count == 0)
                return false;
            else
            {
                CohortSelectors[species] = new DiameterCohortSelector(diameters, diameterRanges, residualBasal);
                residualBasal.Clear();
                return true;
            }
        }

        /// <summary>
        /// Creates and stores a specific-ages cohort selector for a species
        /// if at least one percentage was found among the list of ages and
        /// ranges that were read.
        /// </summary>
        /// <returns>
        /// True if a selector was created and stored in the CohortSelectors
        /// property.  False is returned if no selector was created because
        /// there were no percentages read for any age or range.
        /// </returns>
        public static bool CreateAdditionalCohortSelectorFor(ISpecies species,
                                                   IList<float> diameters,
                                                   IList<DiameterRange> diameterRanges)
        {
            if (residualBasal.Count == 0)
                return false;
            else
            {
                AdditionalCohortSelectors[species] = new DiameterCohortSelector(diameters, diameterRanges, residualBasal);
                residualBasal.Clear();
                return true;
            }
        }

        /// <summary>
        /// Resets the cohort selectors for another prescription
        /// </summary>
        public static void ClearCohortSelectors()
        {
            if (CohortSelectors != null)
            {
                CohortSelectors.Clear();
            }
            if (AdditionalCohortSelectors != null)
            {
                AdditionalCohortSelectors.Clear();
            }
            if (residualBasal != null)
            {
                residualBasal.Clear();
            }
        }
    }
}
