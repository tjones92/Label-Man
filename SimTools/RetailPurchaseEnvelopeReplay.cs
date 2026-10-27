using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using Microsoft.VisualBasic.FileIO;

/// <summary>Reads frozen ledgers and calls the production purse and cohort
/// allocators. Does not advance TimeManager or invoke a simulation tick.</summary>
public static class RetailPurchaseEnvelopeReplay {
    private static IEnumerable<Dictionary<string,string>> Read(string path) {
        using var parser = new TextFieldParser(path) { HasFieldsEnclosedInQuotes=true };
        parser.SetDelimiters(",");
        string[] header=parser.ReadFields();
        while (!parser.EndOfData) {
            string[] values=parser.ReadFields();
            yield return header.Select((h,i)=>(h,values[i])).ToDictionary(p=>p.h,p=>p.Item2);
        }
    }
    private static int I(Dictionary<string,string> row,string field)=>int.Parse(row[field],CultureInfo.InvariantCulture);
    private static double D(Dictionary<string,string> row,string field)=>double.Parse(row[field],CultureInfo.InvariantCulture);

    public static void Run(string prefix) {
        if (!File.Exists(prefix+"-lp-capacity.csv")) throw new FileNotFoundException("Retained LP capacities missing.");
        var calendar=Read(prefix+"-rack-store-weekly.csv").GroupBy(r=>I(r,"week")).ToDictionary(g=>g.Key,g=>I(g.First(),"year"));
        var state=Read(prefix+"-lp-capacity.csv").ToDictionary(r=>(I(r,"week"),r["regionId"]));
        var choice=Read(prefix+"-lp-channel-choice-weekly.csv").ToDictionary(r=>(I(r,"week"),r["regionId"]));
        var shelf=Read(prefix+"-rack-region-weekly.csv").ToDictionary(r=>(I(r,"week"),r["regionId"]),r=>I(r,"shelf"));
        var front=Read(prefix+"-rack-title-weekly.csv").GroupBy(r=>(I(r,"week"),r["regionId"])).ToDictionary(g=>g.Key,g=>g.ToList());
        var budget=Read(prefix+"-budget-product-weekly.csv").GroupBy(r=>(I(r,"week"),r["regionId"])).ToDictionary(g=>g.Key,g=>g.ToList());
        var records=front.Values.SelectMany(v=>v).Select(r=>r["recordId"]).Distinct().ToDictionary(id=>id,
            id=>new RecordRuntimeData(new Record { recordId=id,format=ReleaseFormat.Album }));
        string output=Path.Combine(Path.GetDirectoryName(prefix)!,"retail-envelope-static");
        Directory.CreateDirectory(output);
        using var writer=new StreamWriter(Path.Combine(output,"native-frozen-replay.csv"));
        writer.WriteLine("rackWeight,switchEstimate,year,stores,racks,singles,lpBudget,unspent,lpRetailShare");
        float oldShare=MajorOffChartSingles.Share; bool oldHold=MajorOffChartSingles.HoldUnits;
        MajorOffChartSingles.SetForChecks(.12f,true);
        try {
            // Weight zero selects the production, year-dependent migration curve.
            foreach (double weight in new[] {0d,1d,1.3,1.6,2d}) foreach (double estimate in new[] {0d,.5,1d}) {
                var totals=new Dictionary<int,(long Stores,long Racks,long Singles,long Budget,long Unspent)>();
                foreach (var pair in state.OrderBy(p=>p.Key.Item1).ThenBy(p=>p.Key.Item2,StringComparer.Ordinal)) {
                    var key=pair.Key; int year=calendar[key.Item1];
                    if (year<1960 || year>1969) continue;
                    var l=pair.Value; var c=choice[key]; int sourceYear=I(c,"year"), before=I(c,"storeBefore");
                    var f=front[key]; var b=budget[key];
                    double reach=RackJobberChannel.Reach(sourceYear);
                    var originalFront=f.Select(r=>new {
                        Id=r["recordId"], Store=I(r,"storeUnits"),
                        Auto=Math.Max(0,D(r,"demand")-LpChannelExperiment.Plan(I(r,"storeUnits"),(float)reach,1,1,true)) }).ToList();
                    var originalBudget=b.Select(r=> {
                        int units=I(r,"units"), switched=I(r,"switchedStoreUnits"); double demand=D(r,"demand");
                        double low=units>0 ? demand*switched/units : 0;
                        double high=units>0 ? Math.Min(demand,demand*(switched+1)/units) : demand;
                        double middle=low+(high-low)*estimate;
                        return (Id:r["productId"],Auto:Math.Max(0,demand-middle),Switch:middle);
                    }).ToList();
                    int purse=RetailPurchaseEnvelope.AlbumBudget(I(l,"baseSingleCapacity"),sourceYear);
                    double autonomous=originalFront.Sum(r=>r.Auto)+originalBudget.Sum(r=>r.Auto);
                    var split=RetailPurchaseEnvelope.Split(before,autonomous,purse,weight==0 ? RetailPurchaseEnvelope.RackWeight(sourceYear) : weight);
                    double scale=before>0 ? (double)split.Store/before : 0;
                    var candidates=originalFront.Select(r=>new RackJobberChannel.Candidate {
                        Record=records[r.Id],
                        AutonomousDemand=(float)r.Auto,
                        StoreChoice=LpChannelExperiment.Plan((int)Math.Floor(r.Store*scale),(float)reach,1,1,true)
                    }).ToList();
                    var offers=originalBudget.Select(r=>new BudgetLpChannel.Offer {
                        Product=new BudgetLpProduct { Id=r.Id },SwitchDemand=r.Switch*scale,
                        Demand=r.Auto+r.Switch*scale,StoreLimit=split.Store
                    }).ToList();
                    RackJobberChannel.AllocatePurchasingCohorts(candidates,offers,shelf[key],split.Autonomous);
                    int switchedCount=candidates.Sum(r=>r.StoreChoiceCleared)+offers.Sum(r=>r.Switched);
                    int rackUnits=candidates.Sum(r=>r.Units)+offers.Sum(r=>r.Units);
                    int stores=split.Store-switchedCount, unspent=purse-stores-rackUnits;
                    if (stores<0 || unspent<0 || rackUnits-switchedCount>split.Autonomous || rackUnits>shelf[key])
                        throw new InvalidOperationException($"Frozen replay buyer conservation failed: week={key.Item1} region={key.Item2} weight={weight} estimate={estimate} store={stores} allocated={split.Store} rack={rackUnits} switches={switchedCount} purse={purse} unspent={unspent} allowance={split.Autonomous} shelf={shelf[key]}");
                    var t=totals.GetValueOrDefault(year);
                    totals[year]=(t.Stores+stores,t.Racks+rackUnits,t.Singles+RetailPurchaseEnvelope.SingleBudget(I(l,"singleBudget")),
                        t.Budget+purse,t.Unspent+unspent);
                }
                foreach (var (year,t) in totals.OrderBy(p=>p.Key))
                    writer.WriteLine(FormattableString.Invariant($"{weight},{estimate},{year},{t.Stores},{t.Racks},{t.Singles},{t.Budget},{t.Unspent},{(double)(t.Stores+t.Racks)/(t.Stores+t.Racks+t.Singles)}"));
            }
        } finally { MajorOffChartSingles.SetForChecks(oldShare,oldHold); }
        GD.Print("RETAIL_ENVELOPE_FROZEN_REPLAY_OK no_simulation_ticks output="+output);
    }
}
