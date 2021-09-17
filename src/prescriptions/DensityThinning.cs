using Landis.Utilities;
using Landis.Core;
using System.Collections.Generic;
using System.Text;
using System;

namespace Landis.Library.DensitySiteHarvest
{
    /// <summary>
    /// Static class for partial thinning.
    /// </summary>
    public static class DensityThinning
    {
        /// <summary>
        /// The partial cohort selectors that have been read for each species.
        /// </summary>
        public static DensityCohortSelectors CohortSelectors { get; private set; }
        public static DensityCohortSelectors AdditionalCohortSelectors { get; private set; }
        private static IDictionary<ushort, uint> treeremovals;

        //---------------------------------------------------------------------

        static DensityThinning()
        {
            // Force the harvest library to register its read method for age
            // ranges.  Then replace it with this project's read method that
            // handles percentages for partial thinning.
            DiameterRangeParsing.InitializeClass();
            InputValues.Register<DiameterRange>(DensityThinning.ReadDiameterRange);
            treeremovals = new Dictionary<ushort, uint>();
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

        /// <summary>
        /// Reads number of trees for partial thinning of a cohort age.
        /// </summary>
        /// <remarks>
        /// The number is bracketed by parentheses.
        /// </remarks>
        public static InputValue<uint> ReadTreeRemoval(StringReader reader)
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
            uint removeTrees;
            try
            {
                removeTrees = (uint)Int32.Parse(word);
            }
            catch (System.FormatException exc)
            {
                throw MakeInputValueException(valueAsStr.ToString(),
                                              exc.Message);
            }

            if (removeTrees < 0.0 || removeTrees > 500000)
                throw MakeInputValueException(valueAsStr.ToString(),
                                              string.Format("{0} is not between 0 and 500,000", removeTrees));

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


            return new InputValue<uint>(removeTrees, "Number of Trees");
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

        public static InputValue<DiameterRange> ReadDiameterRange(StringReader reader,
                                                          out int index)
        {
            TextReader.SkipWhitespace(reader);
            index = reader.Index;

            string word = ReadWord(reader, '(');
            if (word == "")
                throw new InputValueException();  // Missing value

            DiameterRange diameterRange = DiameterRangeParsing.ParseDiameterRange(word);


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

            CohortSelectors[species] = new DiameterCohortSelector(diameters, diameterRanges);
            return true;
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
            AdditionalCohortSelectors[species] = new DiameterCohortSelector(diameters, diameterRanges);
            return true;
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
            if (treeremovals != null)
            {
                treeremovals.Clear();
            }
        }
    }
}