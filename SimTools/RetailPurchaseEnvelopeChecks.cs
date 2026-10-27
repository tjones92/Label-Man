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
            string replay=OS.GetCmdlineUserArgs().FirstOrDefault(a=>a.StartsWith("--retail-envelope-replay="));
            if (replay!=null) RetailPurchaseEnvelopeReplay.Run(replay["--retail-envelope-replay=".Length..]);
        } finally {
            RetailPurchaseEnvelope.ResetForChecks(); RetailPurchaseEnvelope.Configure(SimulationArguments.Get());
        }
        GD.Print($"RETAIL_PURCHASE_ENVELOPE_CHECKS_OK {checks}");
    }
}
