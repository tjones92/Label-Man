using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class RetailPurchaseEnvelopeChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); checks++; }
        try {
            RetailPurchaseEnvelope.SetForChecks(false);
            Check(RetailPurchaseEnvelope.SingleBudget(101) == 101, "Disabled Single budget unchanged");
            RetailPurchaseEnvelope.RequireSaved(null);
            RetailPurchaseEnvelope.SetForChecks(true);
            Check(RetailPurchaseEnvelope.SingleBudget(100) == 95, "Retail Single purse correction");
            Check(RetailPurchaseEnvelope.AlbumToSingleRatio(1966) == 133d/129 &&
                RetailPurchaseEnvelope.AlbumToSingleRatio(1968) == 147d/126 &&
                RetailPurchaseEnvelope.AlbumToSingleRatio(1969) == 55d/45, "Documented calibration and authored endpoint");
            Check(RetailPurchaseEnvelope.RackWeight(1960)==1.3 && Math.Abs(RetailPurchaseEnvelope.RackWeight(1967)-1.45)<1e-9 &&
                RetailPurchaseEnvelope.RackWeight(1968)==RetailPurchaseEnvelope.RackWeight(1969), "Outlet migration holds after the measured window");
            Check(RetailPurchaseEnvelope.Split(30, 20, 100) == (30,20), "No forced sales above intentions");
            Check(RetailPurchaseEnvelope.Split(100, 0, 50) == (50,0) &&
                RetailPurchaseEnvelope.Split(0, 100, 50) == (0,50), "Headroom can serve either available cohort");
            var split = RetailPurchaseEnvelope.Split(100, 10, 100);
            Check(split == (90,10), "Unused autonomous allowance returns to real store demand");
            var large=RetailPurchaseEnvelope.Split(400000,300000,500000);
            Check(large.Store>0 && large.Autonomous>0 && large.Store+large.Autonomous==500000,
                "Regional-scale weighted allocation cannot overflow integer multiplication");
            var random = new Random(7182);
            for (int i=0;i<2000;i++) {
                int store=random.Next(10000000), purse=random.Next(20000000); double auto=random.NextDouble()*10000000;
                var x=RetailPurchaseEnvelope.Split(store,auto,purse);
                Check(x.Store>=0 && x.Store<=store && x.Autonomous>=0 && x.Autonomous<=Math.Floor(auto) && x.Store+x.Autonomous<=purse,
                    "Weighted purse conservation");
                double[] d=Enumerable.Range(0,10).Select(_=>random.NextDouble()*100).ToArray();
                int[] u=RetailPurchaseEnvelope.Allocate(d,purse);
                Check(u.Sum()==Math.Min(purse,(int)Math.Floor(d.Sum())) && u.Zip(d).All(p=>p.First>=0 && p.First<=Math.Ceiling(p.Second)),
                    "Demand, capacity and deterministic integer conservation");
            }
            var front = new List<RackJobberChannel.Candidate> { new() {
                Record=new RecordRuntimeData(new Record { recordId="a",format=ReleaseFormat.Album }),
                StoreChoice=60, AutonomousDemand=100 } };
            var budget = new List<BudgetLpChannel.Offer> { new() {
                Product=new BudgetLpProduct { Id="b" },SwitchDemand=20,Demand=120,StoreLimit=100 } };
            RackJobberChannel.AllocatePurchasingCohorts(front,budget,100,10);
            Check(front[0].StoreChoiceCleared+budget[0].Switched==80 && front[0].Units+budget[0].Units==90,
                "Migration uses shelf but cannot spend autonomous purse");
            RackJobberChannel.AllocatePurchasingCohorts(front,budget,50,10);
            Check(front[0].Units+budget[0].Units==50 && front[0].Units==front[0].StoreChoiceCleared && budget[0].Units==budget[0].Switched,
                "Shelf-starved migration creates zero autonomous buyers");
            RackJobberChannel.AllocatePurchasingCohorts(front,budget,100,0);
            Check(front[0].Units+budget[0].Units==80, "Zero autonomous purse preserves pure transfers");
            int week=ChartManager.Instance.GetCurrentChartWeek();
            foreach (int? wrong in new int?[] {null,0,2,-1}) {
                bool rejected=false;
                try { WorldStateService.Apply(new WorldSaveData { RetailPurchasePolicy=wrong,LpDemandScale=LpDemandCalibration.SavedScale },default,null); }
                catch (InvalidOperationException) { rejected=true; }
                Check(rejected && week==ChartManager.Instance.GetCurrentChartWeek(), "Wrong policy rejected before world mutation");
            }
            Check(WorldStateService.Capture().RetailPurchasePolicy==1 && SaveGameService.WritingVersion==36, "v36 captures policy identity");
            // --lp-purse-adult-weight: same national purse, shared by adult buyers.
            MarketRegion R(string id,float pop,float urb,float inc,float youth)=>new MarketRegion { regionId=id,population=pop,urbanization=urb,averageIncome=inc,youthPercentage=youth,
                media=new MediaInfrastructure { totalRadioStations=400,jukeboxCount=43300 },musicIndustry=new MusicInfrastructure { clubCount=690 },
                distribution=new DistributionNetwork { recordStoreCount=181,departmentStoreCount=225 },collegeCount=130 };
            var pair=new[] { R("eastcoast",52.2f,.70f,1.15f,.32f), R("deepsouth",15.0f,.48f,.78f,.40f) };
            int Base(MarketRegion r)=>(int)Math.Round(r.GetRecordBuyingPopulation()/52);
            var adult=RetailPurchaseEnvelope.AdultWeightedBudgets(pair,Base,1961);
            long national=pair.Sum(r=>(long)RetailPurchaseEnvelope.AlbumBudget(Base(r),1961));
            Check(national-adult.Values.Sum()>=0 && national-adult.Values.Sum()<pair.Length, "Adult weighting conserves the national LP purse");
            Check((double)adult["eastcoast"]/adult["deepsouth"] > (double)RetailPurchaseEnvelope.AlbumBudget(Base(pair[0]),1961)/RetailPurchaseEnvelope.AlbumBudget(Base(pair[1]),1961),
                "Adult weighting moves LP purse toward the adult, high-income region");
            // --census-population: census populations, scaled infrastructure, national buying population held.
            Check(CensusPopulation.EconomicScale==1f && pair[1].EconomicPopulation==pair[1].population, "Census off: economic population is the authored one");
            var regions=new[] { "greatlakes","greatplains","eastcoast","westcoast","deepsouth","rockies","southwest" }.Select(id=>id switch {
                "greatlakes"=>R(id,36.1f,.76f,1.08f,.35f), "greatplains"=>R(id,15.5f,.56f,.98f,.35f), "eastcoast"=>R(id,52.2f,.70f,1.15f,.32f),
                "westcoast"=>R(id,20.3f,.82f,1.18f,.38f), "deepsouth"=>R(id,15.0f,.48f,.78f,.40f), "rockies"=>R(id,4.6f,.58f,.95f,.39f),
                _=>R(id,14.2f,.62f,.90f,.41f) }).ToArray();
            double buyersBefore=regions.Sum(r=>(double)r.GetRecordBuyingPopulation());
            try {
                CensusPopulation.ApplyCore(regions);
                var south=regions.Single(r=>r.regionId=="deepsouth");
                Check(south.population==29.9f && Math.Abs(regions.Sum(r=>r.population)-178.5f)<.01f, "Census populations applied");
                Check(south.media.totalRadioStations==797 && south.distribution.recordStoreCount==361 && south.collegeCount==259,
                    "Infrastructure scales with census / authored");
                Check(Math.Abs(regions.Sum(r=>(double)r.GetRecordBuyingPopulation())/buyersBefore-1)<1e-4, "Economic scale holds national buying population");
                Check(Math.Abs(CensusPopulation.EconomicScale-.8977f)<.002f, "Economic scale about 0.90");
                // --census-black-share (§30): the table reproduces the census's 18.86M Black Americans of 178.46M.
                double black=CensusPopulation.Census1960.Sum(p=>(double)p.Value*CensusPopulation.CensusBlackShare1960[p.Key]);
                Check(Math.Abs(black/CensusPopulation.Census1960.Values.Sum()-.1057)<.001, "Census Black shares sum to the national 10.6%");
                Check(!CensusPopulation.BlackShareEnabled && south.blackPopulation==0f, "Census Black share is off unless switched");
            } finally { CensusPopulation.ResetForChecks(); }
            string replay=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--retail-envelope-replay="));
            if (replay!=null) RetailPurchaseEnvelopeReplay.Run(replay["--retail-envelope-replay=".Length..]);
        } finally {
            RetailPurchaseEnvelope.ResetForChecks(); RetailPurchaseEnvelope.Configure(SimulationArguments.Get());
        }
        GD.Print($"RETAIL_PURCHASE_ENVELOPE_CHECKS_OK {checks}");
    }
}
