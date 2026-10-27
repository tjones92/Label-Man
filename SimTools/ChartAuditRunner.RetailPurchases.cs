using System.IO;

public partial class ChartAuditRunner {
    private StreamWriter retailPurchaseWriter;
    private void OpenRetailPurchaseOutputs(string directory) {
        if (!RetailPurchaseEnvelope.Enabled) return;
        retailPurchaseWriter = CreateWriter(Path.Combine(directory, $"{runName}-retail-purchase-envelope.csv"));
        retailPurchaseWriter.WriteLine("week,year,sourceYear,regionId,policy,budget,storeBefore,storeAllocated,autonomousAllowance,finalStores,rackUnits,switchers,autonomousUnits,unspent");
    }
    private void WriteRetailPurchaseRows(int week, int year) {
        if (retailPurchaseWriter == null) return;
        foreach (var r in RetailPurchaseEnvelope.Rows)
            retailPurchaseWriter.WriteLine(string.Join(",", week, year, r.Year, Csv(r.RegionId), RetailPurchaseEnvelope.PolicyVersion,
                r.Budget, r.StoreBefore, r.StoreAllocated, r.AutonomousAllowance, r.FinalStores, r.RackUnits, r.Switchers, r.AutonomousUnits, r.Unspent));
        retailPurchaseWriter.Flush();
    }
    private void CloseRetailPurchaseOutputs() { retailPurchaseWriter?.Dispose(); retailPurchaseWriter = null; }
}
