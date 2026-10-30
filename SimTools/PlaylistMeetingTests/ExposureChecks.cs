using System;
using System.Collections.Generic;

// Minimal boundary stubs: tests execute production exposure scheduling, not the demand model.
public enum ReleaseFormat { Single, Album }
public sealed class RecordBase { public ReleaseFormat format; public string recordId = "record", labelId = "label"; }
public sealed class RecordRuntimeData {
	public RecordBase baseRecord = new(); public int weeksSinceRelease = 2, currentPosition;
	public float awareness; public List<int> fitRelivePositions; public List<float> fitReliveAwareness;
	public Dictionary<string, RegionalRecordData> regionalData = new();
}
public sealed class RegionalRecordData { public int fitExposureAge, localSalesRank, localIntroAge = -1; public string fitExposureCause; }
public sealed class AILabel { public string homeRegion; public float EffectiveNationalReachForRecord(string id) => 0; }
public sealed class SimulatedArtist { }
public static class StationRegionalSupport { public static bool ChartReadsNational => false; }
public static class SingleCommercialFit {
	public static bool Enabled => true; public static float HitShare => .15f; public static double UnexposedCapLog => -1;
	public const string HitSharePrefix = "hit-share"; public static bool IsConnected = true;
	public static bool Applies(RecordRuntimeData r) => true;
	public static bool Connected(RecordRuntimeData r, AILabel l) => IsConnected;
}
public static class RegionalAudience {
	public static bool LocalLifecycleClock => true; public static bool HometownAudience => false;
	public static string HomeRegionId(SimulatedArtist a) => null;
}
public static class BandLife { public static ulong WorldSeed => 1; }
public static class PhiloxCounterRandom { public static double Unit(ulong s, string r, string g, int w, uint d) => .5; }
public static class ChartManager { public static string[] GetNeighborRegionIds(string r) => r == "a" ? new[] { "b" } : Array.Empty<string>(); }
namespace Godot { public static class GD { public static void Print(string text) { } } }

static class ExposureChecks {
	public static void Run() {
		void Check(bool ok, string text) { if (!ok) throw new Exception(text); }
		RecordRuntimeData Record() {
			var r = new RecordRuntimeData();
			r.regionalData["a"] = new();
			r.regionalData["b"] = new() { fitExposureAge = RegionalFitExposure.NotExposed };
			return r;
		}
		var record = Record();
		RegionalFitExposure.UpdateWeek(new[] { record }, _ => new(), (_, _) => true);
		Check(record.regionalData["b"].fitExposureAge == RegionalFitExposure.NotExposed, "Disabled exposure must be inert");
		RegionalFitExposure.Configure(new[] { RegionalFitExposure.Flag, RegionalFitExposure.StationsFlag });
		RegionalFitExposure.UpdateWeek(new[] { record }, _ => new(), (_, region) => region == "a");
		Check(record.regionalData["b"].fitExposureAge == 4 && record.regionalData["b"].fitExposureCause == "neighbour-station", "Sustained source must schedule neighbouring exposure with lag");
		record.weeksSinceRelease = 3;
		RegionalFitExposure.UpdateWeek(new[] { record }, _ => new(), (_, _) => false);
		Check(record.regionalData["b"].localIntroAge == 4, "Local lifecycle must restart on arrival");
		record = Record();
		RegionalFitExposure.UpdateWeek(new[] { record }, _ => new(), (_, region) => region == "b");
		Check(record.regionalData["b"].fitExposureAge == 3 && record.regionalData["b"].fitExposureCause == "station-sustained", "Local station exposure must arrive next week");
		record = Record(); SingleCommercialFit.IsConnected = false;
		RegionalFitExposure.UpdateWeek(new[] { record }, _ => new(), (_, _) => true);
		Check(record.regionalData["b"].fitExposureAge == RegionalFitExposure.NotExposed, "Station support must not change latent connection");
		SingleCommercialFit.IsConnected = true;
		Console.WriteLine("Exposure checks passed: opt-in, local and neighbour lags, lifecycle, fixed latent fit.");
	}
}
