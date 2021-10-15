// This file is part of the Harvest Management library for LANDIS-II.

using Landis.Utilities;
using Landis.Core;
using Landis.Library.DensitySiteHarvest;
using Landis.Library.Succession;
using System.Collections.Generic;
using System.Text;
using System;

using FormatException = System.FormatException;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// A parser that reads harvest parameters from text input.
    /// </summary>
    public class InputParametersParser
        : BasicParameterParser<IInputParameters>
    {
        private string extensionName;
        private ISpeciesDataset speciesDataset;
        private IStandRankingMethod rankingMethod;  //made global because of re-use
        private InputVar<string> speciesName;
        private List<RoundedInterval> roundedIntervals;
        private List<List<string>> parserNotes;
        private static int scenarioStart = 0;
        private static int scenarioEnd = Model.Core.EndTime;

        private static class Names
        {
            public const string HarvestImplementations = "HarvestImplementations";
            public const string MaximumAge = "MaximumAge";
            public const string MinimumAge = "MinimumAge";
            public const string spatialArrangement = "SpatialArrangement";
            public const string minimumTimeSinceLastHarvest = "MinimumTimeSinceLastHarvest";
            public const string timeSinceLastFire = "TimeSinceLastFire";
            public const string timeSinceLastWind = "TimeSinceLastWind";

            public const string PreventEstablishment = ParameterNames.PreventEstablishment;
            public const string MultipleRepeat = "MultipleRepeat";
            public const string MinTimeSinceDamage = "MinTimeSinceDamage";
            public const string Plant = ParameterNames.Plant;
            public const string Prescription = "Prescription";
            public const string PrescriptionMaps = "PrescriptionMaps";
            public const string SingleRepeat = "SingleRepeat";
            public const string SiteSelection = "SiteSelection";
            public const string CohortRemoval = "CohortsRemoved";
            public const string RemovalOrder = "RemovalOrder";
            public const string ForestTypeTable = "ForestTypeTable";
            public const string StandAdjacency = "StandAdjacency";
            public const string PresalvageYears = "PresalvageYears";
            public const string TimesToRepeat = "TimesToRepeat";
        }

        //---------------------------------------------------------------------

        public override string LandisDataValue
        {
            get {
                return extensionName;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// A list of the repeat-harvest intervals that were rounded up during
        /// the most recent call to the Parse method.
        /// </summary>
        public List<RoundedInterval> RoundedRepeatIntervals
        {
            get {
                return roundedIntervals;
            }
        }

        /// <summary>
        /// A List of List(s) of strings
        /// Each list of strings is a note to be passed to the UI/Log
        /// The indents and formatting are included for each line so
        /// that the PlugIn only needs to loop through the Lists
        /// </summary>
        public List<List<string>> ParserNotes
        {
            get
            {
                return parserNotes;
            }
        }

        //---------------------------------------------------------------------

        static InputParametersParser()
        {
            // FIXME: Hack to ensure that Percentage is registered with InputValues
            Landis.Utilities.Percentage p = new Landis.Utilities.Percentage();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="speciesDataset">
        /// The dataset of species to look up species' names in.
        /// </param>
        public InputParametersParser(string          extensionName,
                                     ISpeciesDataset speciesDataset)
            : base(speciesDataset, true)
            // The "true" above --> keywords are enabled for cohort selectors
        {
            this.extensionName = extensionName;
            this.speciesDataset = speciesDataset;
            this.speciesName = new InputVar<string>("Species");
            this.roundedIntervals = new List<RoundedInterval>();
            this.parserNotes = new List<List<string>>();
        }

        //---------------------------------------------------------------------

        protected override IInputParameters Parse()
        {
            roundedIntervals.Clear();
            parserNotes.Clear();

            ReadLandisDataVar();

            InputParameters parameters = CreateEmptyParameters();

            InputVar<int> timestep = new InputVar<int>("Timestep");
            ReadVar(timestep);
            parameters.Timestep = timestep.Value;

            InputVar<string> mgmtAreaMap = new InputVar<string>("ManagementAreas");
            ReadVar(mgmtAreaMap);
            parameters.ManagementAreaMap = mgmtAreaMap.Value;

            InputVar<string> standMap = new InputVar<string>("Stands");
            ReadVar(standMap);
            parameters.StandMap = standMap.Value;
            //first read the prescription
            ReadPrescriptions(parameters.Prescriptions, timestep.Value.Actual);
            //then read the implementation
            ReadHarvestImplementations(parameters.ManagementAreas, parameters.Prescriptions);


            //  Output file parameters

            InputVar<string> prescriptionMapNames = new InputVar<string>(Names.PrescriptionMaps);
            ReadVar(prescriptionMapNames);
            parameters.PrescriptionMapNames = prescriptionMapNames.Value;

            // A hook for a parameter used by Biomass Harvest extension.
            ReadBiomassMaps(parameters);

            InputVar<string> eventLogFile = new InputVar<string>("EventLog");
            ReadVar(eventLogFile);
            parameters.EventLog = eventLogFile.Value;

            InputVar<string> summaryLogFile = new InputVar<string>("SummaryLog");
            ReadVar(summaryLogFile);
            parameters.SummaryLog = summaryLogFile.Value;

            CheckNoDataAfter("the " + summaryLogFile.Name + " parameter");
            return parameters; //.GetComplete();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new instance of a parameter object with no values in it.
        /// </summary>
        /// <remarks>
        /// This is a hook to allow a derived class (e.g., the parser in a
        /// harvest extension) to override it, and return an instance of a
        /// derived parameter class that extends the InputParameters class
        /// with additional parameters.
        /// </remarks>
        protected virtual InputParameters CreateEmptyParameters()
        {
            return new InputParameters();
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads the Biomass Maps parameter.
        /// </summary>
        /// <remarks>
        /// This is a hook to allow the derived parser class in the Biomass
        /// Harvest extension to read an additional parameter.  By default,
        /// this method does nothing (i.e., the behavior required by the Base
        /// Harvest extension).
        /// </remarks>
        protected virtual void ReadBiomassMaps(InputParameters parameters)
        {
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads 0 or more prescriptions from text input.
        /// </summary>
        protected void ReadPrescriptions(List<Prescription> prescriptions,
                                         int                harvestTimestep)
        {
            Dictionary<string, int> lineNumbers = new Dictionary<string, int>();

            InputVar<int> singleRepeat = new InputVar<int>(Names.SingleRepeat);
            InputVar<int> multipleRepeat = new InputVar<int>(Names.MultipleRepeat);
            InputVar<int> timesToRepeat = new InputVar<int>(Names.TimesToRepeat);

            int nameLineNumber = LineNumber;
            InputVar<string> prescriptionName = new InputVar<string>(Names.Prescription);
            while (ReadOptionalVar(prescriptionName)) {
                string name = prescriptionName.Value.Actual;
                int lineNumber;
                if (lineNumbers.TryGetValue(name, out lineNumber))
                    throw new InputValueException(prescriptionName.Value.String,
                                                  "The name {0} was previously used on line {1}",
                                                  prescriptionName.Value.String, lineNumber);
                else
                    lineNumbers[name] = nameLineNumber;

                //get ranking method
                rankingMethod = ReadRankingMethod();

                //get stand-harvesting requirements, will modify rankingMethod variable
                ReadForestTypeTable();

                //get site selection method
                ISiteSelector siteSelector = ReadSiteSelector();

                //get the minTimeSinceDamage
                int minTimeSinceDamage = 0;
                InputVar<int> minTimeSinceDamageVar = new InputVar<int>("MinTimeSinceDamage");
                if(ReadOptionalVar(minTimeSinceDamageVar))
                {
                    minTimeSinceDamage = minTimeSinceDamageVar.Value;
                }

                bool preventEstablishment  = ReadPreventEstablishment();

                InputVar<float> residualBasal = new InputVar<float>("ResidualBasal");

                ReadVar(residualBasal);

                InputVar<string> removalOrder = new InputVar<string>("RemovalOrder");

                ReadVar(removalOrder);

                //JSF - Error checking for removal order

                (string, float) removalInfo = (removalOrder.Value, residualBasal.Value);

                ICohortSelector cohortSelector = ReadCohortSelector(false);

                ICohortCutter cohortCutter = CreateCohortCutter(cohortSelector, removalInfo);

                Planting.SpeciesList speciesToPlant = ReadSpeciesToPlant();

                //  Repeat harvest?
                int repeatParamLineNumber = LineNumber;
                string currentLine = this.CurrentLine;
                if (ReadOptionalVar(singleRepeat)) {
                    int interval = ValidateRepeatInterval(singleRepeat.Value,
                                                          repeatParamLineNumber,
                                                          harvestTimestep);
                    ReadVar(residualBasal);
                    ReadVar(removalOrder);
                    (string, float) additionalRemovalInfo = (removalOrder.Value, residualBasal.Value);
                    ICohortSelector additionalCohortSelector = ReadCohortSelector(true);
                    ICohortCutter additionalCohortCutter = CreateAdditionalCohortCutter(additionalCohortSelector, additionalRemovalInfo);
                    Planting.SpeciesList additionalSpeciesToPlant = ReadSpeciesToPlant();
                    prescriptions.Add(new SingleRepeatHarvest(name,
                                                              rankingMethod,
                                                              siteSelector,
                                                              cohortCutter,
                                                              speciesToPlant,
                                                              additionalCohortCutter,
                                                              additionalSpeciesToPlant,
                                                              minTimeSinceDamage,
                                                              preventEstablishment,
                                                              interval));
                }
                else if (ReadOptionalVar(multipleRepeat)) {
                    int interval = ValidateRepeatInterval(multipleRepeat.Value,
                                                          repeatParamLineNumber,
                                                          harvestTimestep);

                    bool repeatSet = ReadOptionalVar(timesToRepeat);
                    if (repeatSet)
                    {
                        if (timesToRepeat.Value == 0)
                        {
                            throw new Exception("Multiple Repeat requires repeats.");
                        }
                        else if (timesToRepeat.Value == 1)
                        {
                            throw new Exception("Multiple Repeat requires more than one repeat, use Single Repeat instead.");
                        }
                        prescriptions.Add(new RepeatHarvest(name,
                                                        rankingMethod,
                                                        siteSelector,
                                                        cohortCutter,
                                                        speciesToPlant,
                                                        minTimeSinceDamage,
                                                        preventEstablishment,
                                                        interval,
                                                        timesToRepeat.Value));
                    }
                    else
                    {
                        prescriptions.Add(new RepeatHarvest(name,
                                                        rankingMethod,
                                                        siteSelector,
                                                        cohortCutter,
                                                        speciesToPlant,
                                                        minTimeSinceDamage,
                                                        preventEstablishment,
                                                        interval));
                    }
                }
                else {
                    prescriptions.Add(new Prescription(name,
                                                       rankingMethod,
                                                       siteSelector,
                                                       cohortCutter,
                                                       speciesToPlant,
                                                       minTimeSinceDamage,
                                                       preventEstablishment));
                }
            }
        }

        //---------------------------------------------------------------------

        protected IStandRankingMethod ReadRankingMethod()
        {
        //1. read which ranking method is chosen- eg. Random and MaxCohortAge
        //2. check for optional ranking requirements- eg. minimumAge and maximumAge
        //3. form rankingMethod and return it.

            InputVar<string> rankingName = new InputVar<string>("StandRanking");
            ReadVar(rankingName);
            bool check = false;
            bool reqs = false;

            IStandRankingMethod rankingMethod;
            
            if (rankingName.Value.Actual == "BasalArea")
                rankingMethod = new BasalAreaRank();
            else if (rankingName.Value.Actual == "Random")
                rankingMethod = new RandomRank();
            /*            if (rankingName.Value.Actual == "Economic")
                            rankingMethod = new EconomicRank(ReadEconomicRankTable());
                        else if (rankingName.Value.Actual == "MaxCohortAge")
                            rankingMethod = new MaxCohortAge();
                        else if (rankingName.Value.Actual == "Random")
                            rankingMethod = new RandomRank();
                        else if (rankingName.Value.Actual == "RegulateAges")
                            rankingMethod = new RegulateAgesRank();
                        else if (rankingName.Value.Actual == "FireHazard")
                            rankingMethod = new FireRiskRank(ReadFireRiskTable());
                        else if (rankingName.Value.Actual == "TimeSinceDisturbance")
                        {
                            rankingMethod = new TimeSinceDisturbanceRank();
                            check = true;
                        }*/

            ////list of ranking methods which have not been implemented yet
            //else if ((rankingName.Value.Actual == "SpeciesBiomass") ||
            //        (rankingName.Value.Actual == "TotalBiomass")) {
            //    throw new InputValueException(rankingName.Value.String,
            //                                  rankingName.Value.String + " is not implemented yet");
            //}

            else
            {
                string[] methodList = new string[]{"Stand ranking methods:",
                                                   "  Economic",
                                                   "  MaxCohortAge",
                                                   "  Random",
                                                   "  RegulateAges",
                                                   "  FireRisk",
                                                   "  TimeSinceDisturbance"};
                throw new InputValueException(rankingName.Value.String,
                                              rankingName.Value.String + " is not a valid stand ranking",
                                              new MultiLineText(methodList));
            }

            //  Read optional ranking requirements
/*            InputVar<ushort> presalvageYears = new InputVar<ushort>("PresalvageYears");
            if (ReadOptionalVar(presalvageYears))
            {
                //get minAge
                ushort presalvYears = presalvageYears.Value.Actual;
                //add the minimumAge ranking requirement to this ranking method.
                rankingMethod.AddRequirement(new Presalvage(presalvYears));
            }*/
            ushort? minAge = null;
            InputVar<ushort> minimumAge = new InputVar<ushort>("MinimumAge");
            if (ReadOptionalVar(minimumAge)) {
                //get minAge
                minAge = minimumAge.Value.Actual;
                //add the minimumAge ranking requirement to this ranking method.
                rankingMethod.AddRequirement(new MinimumAge(minAge.Value));
            }

            InputVar<ushort> maximumAge = new InputVar<ushort>("MaximumAge");
            if (ReadOptionalVar(maximumAge)) {
                //get maxAge
                ushort maxAge = maximumAge.Value.Actual;
                //throw exception if maxAge < minAge
                if (minAge.HasValue && maxAge < minAge)
                    throw new InputValueException(maximumAge.Value.String,
                                                  "{0} is < minimum age ({1})",
                                                  maximumAge.Value.String,
                                                  minimumAge.Value.String);
                //add the maximumAge ranking requirement to this ranking method.
                rankingMethod.AddRequirement(new MaximumAge(maxAge));
            }

/*            InputVar<ushort> firetime = new InputVar<ushort>("TimeSinceLastFire");
            if (ReadOptionalVar(firetime))
            {
                reqs = true;
                //get the firetime
                ushort fire = firetime.Value.Actual;
                //add the firetime ranking requirement to this ranking method.
                rankingMethod.AddRequirement(new TimeSinceLastFire(fire));
            }

            InputVar<ushort> windtime = new InputVar<ushort>("TimeSinceLastWind");
            if (ReadOptionalVar(windtime))
            {
                reqs = true;
                //get the windtime
                ushort wind = windtime.Value.Actual;
                //add the windtime ranking requirement to this ranking method.
                rankingMethod.AddRequirement(new TimeSinceLastWind(wind));
            }*/

            //stand adjacency variables and constraints
            InputVar<ushort> standAdjacency = new InputVar<ushort>("StandAdjacency");
            InputVar<string> adjacencyType = new InputVar<string>("AdjacencyType");
            InputVar<ushort> adjacencyNeighborSetAside = new InputVar<ushort>("AdjacencyNeighborSetAside");
            //if stand-adjacency is defined, check flags
            if (ReadOptionalVar(standAdjacency)) {
                //get adjacency-type
                ushort adjacency = standAdjacency.Value.Actual;
                ReadVar(adjacencyType);

                if (adjacencyType.Value.String != "StandAge" && adjacencyType.Value.String != "TimeSinceLastHarvested") {
                    string[] methodList = new string[]{"AdjacencyType methods:",
                                                       "    StandAge",
                                                       "    TimeSinceLastHarvested"};
                    throw new InputValueException(adjacencyType.Value.String,
                                                  adjacencyType.Value.String + " is not a valid site selection method",
                                                  new MultiLineText(methodList));
                }
                string ad_type = adjacencyType.Value.String;

                //get set-aside var if defined
                ushort set_aside = 0;
                if (ReadOptionalVar(adjacencyNeighborSetAside)) {
                    //Model.Core.UI.WriteLine("adjacencyNeighborSetAside = {0}", adjacencyNeighborSetAside.Value.Actual);
                    set_aside = adjacencyNeighborSetAside.Value.Actual;
                }
                //add stand-adjacency to list of ranking requirements
                rankingMethod.AddRequirement(new StandAdjacency(adjacency, ad_type, set_aside));
            }


            InputVar<ushort> spatialArrangement = new InputVar<ushort>("SpatialArrangement");
            if (ReadOptionalVar(spatialArrangement)) {
                //get minimum age requirement
                ushort s_minAge = spatialArrangement.Value.Actual;
                //add ranking requirement for neighbor stands to be at least of minimum age (defined by s_minAge)
                rankingMethod.AddRequirement(new SpatialArrangement(s_minAge));
            }

            InputVar<ushort> minimumTimeSinceLastHarvest = new InputVar<ushort>("MinimumTimeSinceLastHarvest");
            if (ReadOptionalVar(minimumTimeSinceLastHarvest)) {
                //get minimum time requirement
                ushort min_time = minimumTimeSinceLastHarvest.Value.Actual;
                //add requirement for this stand to have not been harvested within the minimum
                //time ranking requirement specified (defined by min_time)
                rankingMethod.AddRequirement(new MinTimeSinceLastHarvest(min_time));
            }

            if(check == true && reqs == false)
            {
                throw NewParseException("TimeSinceLastFire or TimeSinceLastWind is required when ranking with TimeSinceDisturbance.");
            }

            return rankingMethod;
        }

        //---------------------------------------------------------------------

        private static List<string> namesThatFollowRankingMethod = new List<string>(
            new string[]{
                Names.SiteSelection,
                //  Optional ranking requirements
                Names.MaximumAge,
                Names.MinimumAge,
                Names.ForestTypeTable,
                Names.minimumTimeSinceLastHarvest,
                Names.MinTimeSinceDamage,
                Names.StandAdjacency,
                Names.timeSinceLastFire,
                Names.timeSinceLastWind

            }
        );

        //---------------------------------------------------------------------

/*        protected EconomicRankTable ReadEconomicRankTable()
        {
            SpeciesLineNumbers.Clear();  // in case parser re-used

            InputVar<byte> rank = new InputVar<byte>("Economic Rank");
            InputVar<ushort> minAge = new InputVar<ushort>("Minimum Age");
            string lastColumn = "the " + minAge.Name + " column";

            EconomicRankTable table = new EconomicRankTable();
            while (! AtEndOfInput && ! namesThatFollowRankingMethod.Contains(CurrentName)) {
                StringReader currentLine = new StringReader(CurrentLine);

                //  Species name
                ISpecies species = ReadSpecies(currentLine);

                //  Economic rank
                ReadValue(rank, currentLine);
                const byte maxRank = 100;
                if (rank.Value.Actual > maxRank)
                    throw new InputValueException(rank.Value.String,
                                                  "Economic rank must be between 0 and {0}",
                                                  maxRank);

                //  Minimum age
                ReadValue(minAge, currentLine);
                CheckNoDataAfter(lastColumn, currentLine);

                table[species] = new EconomicRankParameters(rank.Value.Actual,
                                                            minAge.Value.Actual);
                GetNextLine();
            }

            if (SpeciesLineNumbers.Count == 0)
                throw NewParseException("Expected a line starting with a species name");

            return table;
        }

        //---------------------------------------------------------------------

        protected FireRiskTable ReadFireRiskTable()
        {
            Dictionary<int, int> indexLineNumbers = new Dictionary<int, int>(); ;
            //speciesLineNumbers.Clear();  // in case parser re-used

            InputVar<int> ftindex = new InputVar<int>("Fuel Type Index");
            InputVar<byte> rank = new InputVar<byte>("Fuel Type Rank");
            //InputVar<ushort> minAge = new InputVar<ushort>("Minimum Age");
            string lastColumn = "the " + rank.Name + " column";

            FireRiskTable table = new FireRiskTable();
            while (!AtEndOfInput && !namesThatFollowRankingMethod.Contains(CurrentName))
            {
                StringReader currentLine = new StringReader(CurrentLine);

                //  Fuel type index
                ReadValue(ftindex, currentLine);
                int lineNumber;
                if (indexLineNumbers.TryGetValue(ftindex.Value, out lineNumber))
                    throw new InputValueException(ftindex.Value.String,
                                                  "The fuel type {0} was previously used on line {1}",
                                                  ftindex.Value.String, lineNumber);
                else
                    indexLineNumbers[ftindex.Value] = lineNumber;

                //  Economic rank
                ReadValue(rank, currentLine);
                const byte maxRank = 100;
                if (rank.Value.Actual > maxRank)
                    throw new InputValueException(rank.Value.String,
                                                  "Economic rank must be between 0 and {0}",
                                                  maxRank);

                //  Minimum age
                //ReadValue(minAge, currentLine);
                CheckNoDataAfter(lastColumn, currentLine);

                table[ftindex.Value] = new FireRiskParameters(rank.Value.Actual);
                GetNextLine();
            }

            if (indexLineNumbers.Count == 0)
                throw NewParseException("Expected a line starting with a fuel type index");

            return table;
        }*/

        //---------------------------------------------------------------------


        private static List<string> namesThatFollowForestType = new List<string>(
            new string[]{
                Names.SiteSelection,
                Names.CohortRemoval
            }
        );

        //----------------------------------------------------------------------

        protected void ReadForestTypeTable() {

            SpeciesLineNumbers.Clear();  // in case parser re-used

            int optionalStatements = 0;

            //check if this is the ForestTypeTable
            if (CurrentName == Names.ForestTypeTable) {
                ReadName(Names.ForestTypeTable);

                //fresh input variables for table
                InputVar<string> inclusionRule = new InputVar<string>("Inclusion Rule");
                //InputVar<ushort> minAge = new InputVar<ushort>("Min Age");
                //InputVar<ushort> maxAge = new InputVar<ushort>("Max Age");
                InputVar<AgeRange> age_range = new InputVar<AgeRange>("Age Range");
                InputVar<string> percentOfCells = new InputVar<string>("PercentOfCells");  //as a string so it can include keyword 'highest'
                InputVar<string> speciesName = new InputVar<string>("Species");


                //list for each rule- each line is a separate rule
                List<InclusionRule> rule_list = new List<InclusionRule>();
                //keep reading until no longer in the ForestTypeTable
                while (! AtEndOfInput && !namesThatFollowForestType.Contains(CurrentName)) {
                    StringReader currentLine = new StringReader(CurrentLine);

                    //  inclusionRule column
                    ReadValue(inclusionRule, currentLine);

                    //verify inclusion rule = 'optional', 'required', or 'forbidden'
                    if (inclusionRule.Value.Actual != "Optional" && inclusionRule.Value.Actual != "Required"
                                    && inclusionRule.Value.Actual != "Forbidden") {
                        string[] ic_list = new string[]{"Valid Inclusion Rules:",
                                                                           "    Optional",
                                                                           "    Required",
                                                                           "    Forbidden"};
                        throw new InputValueException(CurrentName, CurrentName + " is not a valid inclusion rule.",
                                                  new MultiLineText(ic_list));
                    }

                    if (inclusionRule.Value.Actual == "Optional")
                        optionalStatements++;


                    TextReader.SkipWhitespace(currentLine);
                    ReadValue(age_range, currentLine);

                    //percentage column
                    TextReader.SkipWhitespace(currentLine);
                    ReadValue(percentOfCells, currentLine);
                    //Model.Core.UI.WriteLine("percentOfCells = {0}", percentOfCells.Value.String);
                    //cannot validate until parsing is done.  will do this in the inclusionRule constructor

                    //a list in case there are multiple species on this line
                    List<string> species_list = new List<string>();
                    //add each species to this rule's species list
                    TextReader.SkipWhitespace(currentLine);
                    while (currentLine.Peek() != -1) {
                        //species column (build list)

                        ReadValue(speciesName, currentLine);
                        string name = speciesName.Value.String;

                        ISpecies species = GetSpecies(new InputValue<string>(name, speciesName.Value.String));
                        if (species_list.Contains(species.Name))
                            throw NewParseException("The species {0} appears more than once.", species.Name);
                        species_list.Add(species.Name);

                        //species_list.Add(species.Value.String);
                        TextReader.SkipWhitespace(currentLine);
                    }

                    //add this new inclusion rule (by parameters)  to the requirement
                    rule_list.Add(new InclusionRule(inclusionRule.Value.String,
                                                    age_range.Value.Actual,
                                                    percentOfCells.Value.String,
                                                    species_list));

                    GetNextLine();
                }
                //create a new requirement with this list of rules
                IRequirement inclusionRequirement = new InclusionRequirement(rule_list);
                //add this requirement to the ranking method
                rankingMethod.AddRequirement(inclusionRequirement);
            }

            if(optionalStatements > 0 && optionalStatements < 2)
                throw new InputValueException(CurrentName, "If there are optional statements, there must be more than one",
                                                  "ForestTypeTable");

        }

        //---------------------------------------------------------------------

        protected ISiteSelector ReadSiteSelector()
        {
            InputVar<ISiteSelector> siteSelector = new InputVar<ISiteSelector>(Names.SiteSelection, ReadSiteSelector);
            ReadVar(siteSelector);

            return siteSelector.Value.Actual;
        }

        //---------------------------------------------------------------------

        private static class SiteSelection
        {
            //Names for each acceptable selection method
            public const string Complete                = "Complete";               //harvest whole stands
            public const string CompleteAndSpreading    = "CompleteStandSpread";    //spread by complete stand
            public const string TargetAndSpreading      = "PartialStandSpread";     //spread by site
            public const string Patch                   = "PatchCutting";           //make patches of specified size
        }

        //---------------------------------------------------------------------

        protected InputValue<ISiteSelector> ReadSiteSelector(StringReader reader,
                                                             out int      index)
        {
            TextReader.SkipWhitespace(reader);
            index = reader.Index;
            string name = TextReader.ReadWord(reader);
            if (name == "")
                throw new InputValueException();  // Missing value

            ISiteSelector selector;
            StringBuilder valueAsStr = new StringBuilder(name);
            //  Site selection -- Complete stand
            if (name == SiteSelection.Complete) {
                selector = new CompleteStand();
            }
            //  Site selection -- Target size with partial or complete spread

            else if (name == SiteSelection.CompleteAndSpreading || name == SiteSelection.TargetAndSpreading) {

                InputVar<double> minTargetSize = new InputVar<double>("the minimum target harvest size");
                ReadValue(minTargetSize, reader);

                InputVar<double> maxTargetSize = new InputVar<double>("the maximum target harvest size");
                ReadValue(maxTargetSize, reader);


                //validate the target size for spreading algorithms
                StandSpreading.ValidateTargetSizes(minTargetSize.Value,
                    maxTargetSize.Value);

                if (name == SiteSelection.TargetAndSpreading) {
                    // Site selection -- partial spread
                    selector = new PartialStandSpreading(minTargetSize.Value.Actual,
                        maxTargetSize.Value.Actual);
                }
                else {
                    //  Site selection -- complete stand
                    selector = new CompleteStandSpreading(minTargetSize.Value.Actual,
                    maxTargetSize.Value.Actual);

                }
                valueAsStr.AppendFormat(" {0}", minTargetSize.Value.String);
                valueAsStr.AppendFormat(" {0}", maxTargetSize.Value.String);

            }

            //  Site selection -- Patch cutting
            else if (name == SiteSelection.Patch) {
                InputVar<Percentage> percentage = new InputVar<Percentage>("the site percentage for patch cutting");
                ReadValue(percentage, reader);
                PatchCutting.ValidatePercentage(percentage.Value);

                InputVar<double> size = new InputVar<double>("the target patch size");
                ReadValue(size, reader);
                PatchCutting.ValidateSize(size.Value);

                string allowOverlap = string.Empty;
                TextReader.SkipWhitespace(reader);
                index = reader.Index;
                allowOverlap = TextReader.ReadWord(reader);
                selector = new PatchCutting(percentage.Value.Actual, size.Value.Actual, allowOverlap);


                valueAsStr.AppendFormat(" {0} {1} {2}", percentage.Value.String,
                                                    size.Value.String,
                                                    allowOverlap);
            }

            else {
                string[] methodList = new string[]{"Site selection methods:",
                                                   "  " + SiteSelection.Complete,
                                                   "  " + SiteSelection.CompleteAndSpreading,
                                                   "  " + SiteSelection.TargetAndSpreading,
                                                   "  " + SiteSelection.Patch};
                throw new InputValueException(name,
                                              name + " is not a valid site selection method",
                                              new MultiLineText(methodList));
            }
            return new InputValue<ISiteSelector>(selector, valueAsStr.ToString());
        }

        //---------------------------------------------------------------------

        protected ICohortSelector ReadCohortSelector(bool forSingleRepeat)
        {

            InputVar<string> cohortSelection = new InputVar<string>("CohortsRemoved");
            ReadVar(cohortSelection);

            if (cohortSelection.Value.Actual == "ClearCut")
                return new ClearCut();

            //if (cohortSelection.Value.Actual == "PlantOnly")
            //    return new MultiSpeciesCohortSelector();

            if (cohortSelection.Value.Actual == "DiameterBasalThinning")
            {
                InputValues.Register<DiameterRange>(ResidualThinning.ReadDiameterRange);
                return ReadSpeciesAndDiameters(Names.Plant,
                             Names.SingleRepeat,
                             Names.MultipleRepeat,
                             Names.Prescription,
                             Names.HarvestImplementations);

            }

/*            if (cohortSelection.Value.Actual == "SpeciesList") {
                if (forSingleRepeat)
                    return ReadSpeciesAndCohorts(Names.Plant,
                                                 Names.Prescription,
                                                 Names.HarvestImplementations);
                else
                    return ReadSpeciesAndCohorts(Names.Plant,
                                                 Names.SingleRepeat,
                                                 Names.MultipleRepeat,
                                                 Names.Prescription,
                                                 Names.HarvestImplementations);
            }*/

            throw new InputValueException(cohortSelection.Value.String,
                                          cohortSelection.Value.String + " is not a valid cohort selection",
                                          new MultiLineText("Valid values: ClearCut or SpeciesList"));
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Creates a new cohort cutter for a cohort selector.
        /// </summary>
        /// <remarks>
        /// By default, this method creates a WholeCohortCutter instance.  But
        /// the parser class for Biomass Harvest extension overrides this method
        /// so it can determine whether to create a PartialCohortCutter or a
        /// WholeCohortCutter.
        /// </remarks>
        protected virtual ICohortCutter CreateCohortCutter(ICohortSelector cohortSelector, (string, float) removalInfo)
        {
            return new DensityCohortCutter(cohortSelector, removalInfo, HarvestExtensionMain.ExtType);
        }

        /// <summary>
        /// Creates a new cohort cutter for a cohort selector.
        /// </summary>
        /// <remarks>
        /// By default, this method creates a WholeCohortCutter instance.  But
        /// the parser class for Biomass Harvest extension overrides this method
        /// so it can determine whether to create a PartialCohortCutter or a
        /// WholeCohortCutter.
        /// </remarks>
        protected virtual ICohortCutter CreateAdditionalCohortCutter(ICohortSelector cohortSelector, (string, float) removalInfo)
        {
            return new DensityCohortCutter(cohortSelector, removalInfo, HarvestExtensionMain.ExtType);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Validates the interval for a repeat harvest.
        /// </summary>
        /// <param name="interval">
        /// The interval to validate.
        /// </param>
        /// <param name="lineNumber">
        /// The line number where the interval was located in the text input.
        /// </param>
        /// <param name="harvestTimestep">
        /// The timestep of the harvest plug-in.
        /// </param>
        /// <returns>
        /// If the interval is not a multiple of the harvest timestep, then
        /// the method rounds the interval up to the next multiple and returns
        /// it.
        /// </returns>
        public int ValidateRepeatInterval(InputValue<int> interval,
                                          int             lineNumber,
                                          int             harvestTimestep)
        {
            if (interval.Actual <= 0)
                throw new InputValueException(interval.String,
                                              "Interval for repeat harvest must be > 0");

            if (interval.Actual % harvestTimestep == 0)
                return interval.Actual;
            else {
                int intervalRoundedUp = ((interval.Actual / harvestTimestep) + 1) * harvestTimestep;
                roundedIntervals.Add(new RoundedInterval(interval.Actual,
                                                         intervalRoundedUp,
                                                         lineNumber));
                return intervalRoundedUp;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Reads harvest implementations: which prescriptions are applied to
        /// which management areas.
        /// </summary>
        protected void ReadHarvestImplementations(ManagementAreaDataset mgmtAreas,
                                                  List<Prescription>    prescriptions)
        {
            ReadName(Names.HarvestImplementations);

            InputVar<ushort> mgmtAreaMapCode = new InputVar<ushort>("Mgmt Area");
            InputVar<string> prescriptionName = new InputVar<string>("Prescription");
            InputVar<int> beginTimeVar = new InputVar<int>("Begin Time");
            InputVar<int> endTimeVar = new InputVar<int>("End Time");
            InputVar<Percentage> areaToHarvest = new InputVar<Percentage>("Area To Harvest");
            InputVar<Percentage> standsToHarvest = new InputVar<Percentage>("Stands To Harvest");
            StringReader sr100 = new StringReader("100%");

            while (! AtEndOfInput && CurrentName != Names.PrescriptionMaps) {
                StringReader currentLine = new StringReader(CurrentLine);

                //  Mgmt Area column
                ReadValue(mgmtAreaMapCode, currentLine);
                ushort mapCode = mgmtAreaMapCode.Value.Actual;
                ManagementArea mgmtArea = mgmtAreas.Find(mapCode);
                if (mgmtArea == null) {
                    //add the management area, and add it to the collection of management areas
                    mgmtArea = new ManagementArea(mapCode);
                    mgmtAreas.Add(mgmtArea);
                }

                //  Prescription column
                ReadValue(prescriptionName, currentLine);
                string name = prescriptionName.Value.Actual;
                Prescription prescription = prescriptions.Find(new MatchName(name).Predicate);
                if (prescription == null)
                    throw new InputValueException(prescriptionName.Value.String,
                                                  prescriptionName.Value.String + " is an unknown prescription name");


                TextReader.SkipWhitespace(currentLine);
                //  Area to Harvest column
                string strVal = TextReader.ReadWord(currentLine);
                sr100 = new StringReader("100%");
                //If the keyword 'Stands' is attached to the area %, then applies to % of stands to harvest instead of area
                if (strVal.Contains("Stands"))
                {
                    char[] trimChar = { 'S', 't', 'a', 'n', 'd', 's' };
                    string clipText = strVal.TrimEnd(trimChar);
                    StringReader testRead = new StringReader(clipText);
                    ReadValue(standsToHarvest, testRead);
                    ReadValue(areaToHarvest, sr100);
                }
                else
                {
                    StringReader testRead = new StringReader(strVal);
                    //  Area to Harvest column
                    ReadValue(areaToHarvest, testRead);
                    ReadValue(standsToHarvest, sr100);
                }
                //get percentage to harvest (type Percentage ensures that it is in percent format)
                Percentage percentageToHarvest = areaToHarvest.Value.Actual;
                //check for valid percentage
                if (percentageToHarvest <= 0.0 || percentageToHarvest > 1.0)
                    throw new InputValueException(areaToHarvest.Value.String,
                                                  "Percentage must be between 0% and 100%");
                //get percentage of stands to harvest (type Percentage ensures that it is in percent format)
                Percentage percentStandsToHarvest = standsToHarvest.Value.Actual;
                //check for valid percentage
                if (percentStandsToHarvest <= 0.0 || percentStandsToHarvest > 1.0)
                    throw new InputValueException(standsToHarvest.Value.String,
                                                  "Percentage must be between 0% and 100%");

                //  Begin Time and End Time columns
                //  They are optional, so the possibilities are:
                //
                //          Begin Time   End Time
                //          ----------   --------
                //      1)   present     present
                //      2)   present     missing
                //      3)   missing     missing

                //  The default values for the starting and ending times for
                //  an applied prescription.
                int beginTime = scenarioStart;
                int endTime   = scenarioEnd;

                TextReader.SkipWhitespace(currentLine);
                if (currentLine.Peek() != -1) {
                    ReadValue(beginTimeVar, currentLine);
                    beginTime = beginTimeVar.Value.Actual;
                    if (beginTime < scenarioStart)
                        throw new InputValueException(beginTimeVar.Value.String,
                                                      string.Format("Year {0} is before the scenario start year ({1})",
                                                                    beginTimeVar.Value.String,
                                                                    scenarioStart));
                    if (beginTime > scenarioEnd)
                    {
                        //throw new InputValueException(beginTimeVar.Value.String,
                        //                              string.Format("Year {0} is after the scenario' end year ({1})",
                        //                                            beginTimeVar.Value.String,
                        //                                            scenarioEnd));
                        string line1 = string.Format("   NOTE: Begin year {0} is after the scenario' end year {1}", beginTimeVar.Value.String, scenarioEnd);
                        string line2 = string.Format("         on line {0} of the harvest input file...", LineNumber);
                        List<string> noteList = new List<string>();
                        noteList.Add(line1);
                        noteList.Add(line2);
                        parserNotes.Add(noteList);
                    }

                    TextReader.SkipWhitespace(currentLine);
                    if (currentLine.Peek() != -1) {
                        ReadValue(endTimeVar, currentLine);
                        endTime = endTimeVar.Value.Actual;
                        if (endTime < beginTime)
                            throw new InputValueException(endTimeVar.Value.String,
                                                          string.Format("Year {0} is before the Begin Time ({1})",
                                                                        endTimeVar.Value.String,
                                                                        beginTimeVar.Value.String));
                        if (endTime > scenarioEnd)
                        {
                            string line1 = string.Format("   NOTE: End Year {0} is after the scenario' end year {1}", endTimeVar.Value.String, scenarioEnd);
                            string line2 = string.Format("         on line {0} of the harvest input file...", LineNumber);
                            List<string> noteList = new List<string>();
                            noteList.Add(line1);
                            noteList.Add(line2);
                            parserNotes.Add(noteList);
                            throw new InputValueException(endTimeVar.Value.String,
                                                              string.Format("Year {0} is after the scenario' end year ({1})",
                                                                            endTimeVar.Value.String,
                                                                            scenarioEnd));  
                        }

                        CheckNoDataAfter("the " + endTimeVar.Name + " column",
                                         currentLine);
                    }
                }


                //if the perscription has already been applied to this management area
                //NOTE: .IsApplied has been modified, and this has been moved to AFTER the
                //begin and end times are founded.
                if (mgmtArea.IsApplied(prescriptionName.Value.String, beginTime, endTime))
                    throw new InputValueException(prescriptionName.Value.String,
                                                  "Prescription {0} has already been applied to management area {1} with begin time = {2} and end time = {3}",
                                                  prescriptionName.Value.String, mgmtArea.MapCode, beginTime, endTime);

                //begin applying prescription to this management area
                mgmtArea.ApplyPrescription(prescription,
                                           percentageToHarvest,
                                           percentStandsToHarvest,
                                           beginTime,
                                           endTime);



                CheckNoDataAfter("the " + prescriptionName.Name + " column",
                                 currentLine);
                GetNextLine();
            }
        }

        //---------------------------------------------------------------------

        protected ISpecies GetSpecies(InputValue<string> name)
        {
            ISpecies species = speciesDataset[name.Actual]; // Model.Core.Species[name.Actual];
            if (species == null)
                throw new InputValueException(name.String,
                                              "{0} is not a species name.",
                                              name.String);
            return species;
        }
        //---------------------------------------------------------------------

        public class MatchName
        {
            private string name;

            //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            public MatchName(string name)
            {
                this.name = name;
            }

            //- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

            public bool Predicate(Prescription prescription)
            {
                return prescription.Name == name;
            }
        }
    }
}