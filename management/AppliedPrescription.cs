// This file is part of the Harvest Management library for LANDIS-II.

using Landis.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;

namespace Landis.Library.DensityHarvestManagement
{
    /// <summary>
    /// The application of a prescription to a management area.
    /// </summary>
    public class AppliedPrescription
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly bool isDebugEnabled = log.IsDebugEnabled;

        private Prescription prescription;
        private StandSpreading standSpreadSiteSelector;
        private Percentage percentageToHarvest;  //TODO: change to Percentage0to100 once it's moved to Util lib
        private Percentage percentStandsToHarvest;
        private int beginTime;
        private int endTime;
        private double areaToHarvest;
        private double areaRemainingToHarvest;
        private double areaRemainingRatio;
        private double areaHarvested;
        private int standsHarvested;
        private int standsToHarvest;
        private int standsRemainingToHarvest;
        private double standsRemainingRatio;
        private Stand highest_ranked_stand;

        private StandRanking[] rankings;
        //  Index of the highest-ranked stand that's not yet harvested.
        private int highestUnharvestedStand;

        private static double currentRank;

        private bool applyPrescription; //Track whether the prescription was applied during the current timestep

        //---------------------------------------------------------------------

        /// <summary>
        /// The rank of the stand currently being harvested.
        /// </summary>
        public static double CurrentRank
        {
            get
            {
                return currentRank;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The prescription to apply.
        /// </summary>
        public Prescription Prescription
        {
            get {
                return prescription;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Return the highest ranked stand when needed.
        ///</summary>
        public Stand HighestRankedStand {
            get {
                return this.highest_ranked_stand;
            }
            set {
                this.highest_ranked_stand = value;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The percentage of the management area to be harvested.
        /// </summary>
        public Percentage PercentageToHarvest
        {
            get {
                return percentageToHarvest;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The percentage of the stands in management area to be harvested.
        /// </summary>
        public Percentage PercentStandsToHarvest
        {
            get
            {
                return percentStandsToHarvest;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// When the prescription should begin being applied to the management
        /// area.
        /// </summary>
        /// <remarks>
        /// Units: calendar year
        /// </remarks>
        public int BeginTime
        {
            get {
                return beginTime;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// When the prescription should stop being applied to the management
        /// area.
        /// </summary>
        /// <remarks>
        /// Units: calendar year
        /// </remarks>
        public int EndTime
        {
            get {
                return endTime;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The area (hectares) to be harvested by this prescription during
        /// each harvest timestep.
        /// </summary>
        public double AreaToHarvest
        {
            get {
                return areaToHarvest;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The area (hectares) remaining to be harvested by this prescription
        /// during the current timestep.
        /// </summary>
        public double AreaRemainingToHarvest
        {
            get {
                return areaRemainingToHarvest;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The ratio of AreaRemainingToHarvest to AreaToHarvest.
        /// </summary>
        public double AreaRemainingRatio
        {
            get {
                return areaRemainingRatio;
            }
        }
        //---------------------------------------------------------------------
        /// <summary>
        /// The ratio of StandsRemainingToHarvest to StandsToHarvest.
        /// </summary>
        public double StandsRemainingRatio
        {
            get
            {
                return standsRemainingRatio;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// The number of stands to be harvested by this prescription during
        /// each harvest timestep.
        /// </summary>
        public double StandsToHarvest
        {
            get
            {
                return standsToHarvest;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// The area (hectares) harvested by this prescription during the
        /// current timestep.
        /// </summary>
        public double AreaHarvested
        {
            get {
                return areaHarvested;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// The number of stands harvested by this prescription during the
        /// current timestep.
        /// </summary>
        public double StandsHarvested
        {
            get
            {
                return standsHarvested;
            }
        }

        //---------------------------------------------------------------------
        /// <summary>
        /// Any stands that haven't been harvested yet during the current
        /// timestep ranked > 0?
        /// </summary>
        public bool AnyUnharvestedStandsRankedAbove0
        {
            
            get {
                
                //  Search for the highest unharvested stand with rank > 0
                while (highestUnharvestedStand < rankings.Length) {

                    Stand temp = rankings[highestUnharvestedStand].Stand;

                    if (rankings[highestUnharvestedStand].Rank <= 0) {
                        return false;
                    }
                    // tjs 2009.02.22 - Adding check for rejected prescription
                    if (temp.Harvested || temp.IsSetAside
                        || temp.IsRejectedPrescriptionName(this.prescription.Name)) {
                        highestUnharvestedStand++;
                    } else {
                        if (rankings[highestUnharvestedStand].Rank > 0) {
                            //  Found an unharvested stand whose rank > 0
                            return true;
                        } else {
                            return false;
                        }
                    }
                } // while (highestUnharvestedStand < rankings.Length)

                //  End of rankings array so no more unharvested stands
                return false;
            }
        }

        //---------------------------------------------------------------------

        public AppliedPrescription(Prescription prescription,
                                   Percentage percentageToHarvest,
                                   Percentage percentStandsToHarvest,
                                   int          beginTime,
                                   int          endTime)
        {
            //set prescription
            this.prescription = prescription;
            
            //set stand ranking method
            this.standSpreadSiteSelector = prescription.SiteSelectionMethod as StandSpreading;
            
            //set harvest percentage
            this.percentageToHarvest = percentageToHarvest;

            //set harvest stands percentage
            this.percentStandsToHarvest = percentStandsToHarvest;
            
            //set begin time and end time
            this.beginTime = beginTime;
            this.endTime = endTime;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Finish initializing the applied prescription after reading the
        /// stand map.  Determine the area size to harvest.
        /// </summary>
        /// <param name="standCount">
        /// The number of stands in the management area that the prescription
        /// is applied to.
        /// </param>
        /// <param name="area">
        /// The area count for which this prescription is to be applied.
        /// </param>
        public void FinishInitialization(int    standCount,
                                         double area)
        {
            rankings = new StandRanking[standCount];
            //actually determine harvesting area here
            this.areaToHarvest = area * percentageToHarvest;
            this.standsToHarvest = (int)(standCount * percentStandsToHarvest);
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes the prescription for harvesting, which includes
        /// computing stand rankings.
        /// </summary>
        /// <remarks>
        /// AreaRemainingToHarvest is set to AreaToHarvest, AreaRemainingRatio
        /// is set to 1.0, and AreaHarvested is set to 0.0.
        /// </remarks>
        public void InitializeForHarvest(List<Stand> stands)
        {
            areaRemainingToHarvest = areaToHarvest;
            
            areaRemainingRatio = 1.0;
            areaHarvested = 0.0;

            prescription.StandRankingMethod.RankStands(stands, rankings);
            // Sort rankings highest to lowest
            Array.Sort<StandRanking>(rankings, CompareRankings);

            StandRanking[] eligibleStands = Array.FindAll<StandRanking>(rankings, RankOfZero);

            standsToHarvest = (int)Math.Round(eligibleStands.Length * percentStandsToHarvest);
            standsRemainingToHarvest = standsToHarvest;
            standsRemainingRatio = 1.0;
            standsHarvested = 0;

            highestUnharvestedStand = 0;

            //if (isDebugEnabled) {
            //Model.Core.UI.WriteLine("prescription {0}:", prescription.Name);
            //Model.Core.UI.WriteLine("  _Ranking_  Stand");
                //foreach (StandRanking ranking in rankings)
                    //Model.Core.UI.WriteLine("  {0,9}  {1,5}", ranking.Rank, ranking.Stand.MapCode);
            //}

            if (standSpreadSiteSelector != null)
                standSpreadSiteSelector.StandRankings = rankings;
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Compares two stand rankings such that the higher ranking comes
        /// before the lower ranking.
        /// </summary>
        public static int CompareRankings(StandRanking x,
                                          StandRanking y)
        {
            if (x.Rank > y.Rank)
                return -1;
            else if (x.Rank < y.Rank)
                return 1;
            else
                return 0;
        }


        //---------------------------------------------------------------------
        /// <summary>
        /// Determines if the stand has a rank not equal to 0
        /// Returns false if rank is 0, else returns true
        /// </summary>
        private static bool RankOfZero(StandRanking rank)
        {
            if (rank.Rank == 0)
                return false;
            else
                return true;

        }
        //---------------------------------------------------------------------
        /// <summary>
        /// Harvests the highest-ranked stand which hasn't been harvested yet
        /// during the current timestep.
        /// </summary>
        public virtual void HarvestHighestRankedStand()
        {
            //Model.Core.UI.WriteLine("  Standard Harvest Highest Ranked Stand.");

            //get the highest ranked unharvested stand
            Stand stand = rankings[highestUnharvestedStand].Stand;
            if (rankings[highestUnharvestedStand].Rank > 0) {
                if (!stand.IsSetAside) {
                    // set the global current rank so it can be taken by 
                    // Prescription.Harvest and applied 
                    // to this harvest event
                    currentRank = rankings[highestUnharvestedStand].Rank;
                    
                    prescription.Harvest(stand);
                    this.HighestRankedStand = stand;
                    
                    double harvestedArea = stand.LastAreaHarvested;
                    
                    areaHarvested += harvestedArea;
                    int harvestedStands = stand.LastStandsHarvested;
                    //if all (or more ??) is harvested, set areaRemainingToHarvest to 0
                    if (harvestedArea >= areaRemainingToHarvest) {
                        areaRemainingToHarvest = 0;
                    }
                    else {
                        areaRemainingToHarvest -= harvestedArea;
                    }
                    areaRemainingRatio = areaRemainingToHarvest / areaToHarvest;

                    if (standsRemainingToHarvest <= harvestedStands)
                    {
                        standsRemainingToHarvest = 0;
                    }
                    else
                    {
                        standsRemainingToHarvest -= harvestedStands;
                    }
                    standsRemainingRatio = (double)standsRemainingToHarvest / (double)standsToHarvest;
                }
            }
        }
        
        
        /// <summary>
        /// Return the rankings array, so stands can be added or removed explicitly
        /// </summary>
        public StandRanking[] Rankings {
            get {
                return rankings;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// Indicates if the prescription was applied during the current timestep
        ///</summary>
        public bool ApplyPrescription
        {
            get
            {
                return this.applyPrescription;
            }
            set
            {
                this.applyPrescription = value;
            }
        }

        //---------------------------------------------------------------------
    }
}
