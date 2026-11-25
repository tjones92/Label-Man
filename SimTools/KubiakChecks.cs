using System;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life §3.5 H2: Walter Kubiak's jukebox rights. While he holds the paper a home-town operator reaches half its machines
/// for your records (its crowd and its order), and nowhere else is touched; another holder binds nothing. Taking his first
/// move puts the claimed single on his machines on delivery: an appeal lift at the home town's counters for that single only,
/// fading to nothing over twelve weeks. Turning it down shuts the home town's operators for the year, acetate orders included.
/// The route date survives a save.
///   --path . SimTools/KubiakChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints KUBIAK_CHECKS_PASS / _FAIL.
/// </summary>
public partial class KubiakChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .0005f) => Math.Abs(a - b) <= tol;

	private static readonly PartyMemory Walter = new() { Where = PartyWhere.BarBackRoom, Who = PartyWho.RouteMan, Brag = PartyBrag.Money, End = PartyEnd.Car };
	private GameDate today;

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance;
			Check(desk.FoundLabel("Kubiak Checks", "detroit", FoundingArchetype.PawnShopOwner, null, Walter, out var message), "Found: " + message);
			today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
			Rights(desk);
			Accepted(desk);
			Refused(desk);
			GD.Print($"KUBIAK_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("KUBIAK_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private void Rights(PlayerDesk desk) {
		desk.ClearPressingForChecks();
		desk.FoundLienForChecks(Walter, LienHolder.Kubiak);
		var homeOp = desk.StopForChecks(StopKind.Op, true);
		var awayOp = desk.StopForChecks(StopKind.Op, false);
		var homeShop = desk.StopForChecks(StopKind.Shop, true);
		Check(homeOp != null && awayOp != null && homeShop != null, "There are operators at home and away, and a home shop");
		Check(Near(desk.RouteReachForChecks(homeOp), KubiakRouteReach), "A home-town operator reaches only Kubiak's machines");
		Check(Near(desk.RouteReachForChecks(awayOp), 1f) && Near(desk.RouteReachForChecks(homeShop), 1f), "Away operators and home shops are untouched");
		float bound = desk.CounterCrowdCeilingForChecks(homeOp, .6f);
		Check(desk.KubiakRouteLine(desk.Label.homeCityId)?.Contains("his machines") == true, "The home town's operators say why");
		Check(desk.KubiakRouteLine(awayOp.CityId) == null, "Nowhere else does");

		desk.FoundLienForChecks(Walter, LienHolder.Winters);
		float free = desk.CounterCrowdCeilingForChecks(homeOp, .6f);
		Check(Near(desk.RouteReachForChecks(homeOp), 1f) && desk.KubiakRouteLine(desk.Label.homeCityId) == null, "Under another holder the home operators are free");
		Check(Near(bound, free * KubiakRouteReach, .01f), $"His rights halve the route's crowd ({bound:F1} of {free:F1})");
	}

	private void Accepted(PlayerDesk desk) {
		var clock = TimeManager.Instance;
		desk.ClearPressingForChecks();
		desk.FoundLienForChecks(Walter, LienHolder.Kubiak);
		var arrives = today.AddDays(20);
		desk.PressFirstSingleForChecks("kubiak-route", 1000, 1300f, today, arrives);
		var lien = desk.Lien;
		desk.ProcessHolderMoveDawnForChecks(lien.MoveDue);
		Check(desk.ResolveHolderMove(true, out var m), "Kubiak's claim taken: " + m);
		var record = new Record { recordId = "kubiak-route" };
		var other = new Record { recordId = "kubiak-other" };
		string home = desk.Label.homeCityId, away = desk.StopForChecks(StopKind.Op, false).CityId;
		Check(Near(desk.KubiakRouteAppealForChecks(record, home), 0f), "Nothing on his machines before the run comes in");
		clock.RestoreClock(arrives, 9);
		desk.DeliverArrivedPressingsForChecks(arrives);
		Check(lien.KubiakRouteDate == arrives, "His copies go on the machines the day the run comes in");
		Check(Near(desk.KubiakRouteAppealForChecks(record, home), KubiakRouteLift), "The home town's bars hear it: the full lift");
		Check(Near(desk.KubiakRouteAppealForChecks(record, away), 0f), "Not in another town");
		Check(Near(desk.KubiakRouteAppealForChecks(other, home), 0f), "Not for another record");
		clock.RestoreClock(arrives.AddDays(7 * KubiakRouteWeeks / 2), 9);
		Check(Near(desk.KubiakRouteAppealForChecks(record, home), KubiakRouteLift / 2), "Half the lift halfway through");
		clock.RestoreClock(arrives.AddDays(7 * KubiakRouteWeeks + 1), 9);
		Check(Near(desk.KubiakRouteAppealForChecks(record, home), 0f), "Gone once the records rotate out");
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.KubiakRouteDate == arrives, "The route date survives a save");
		var old = desk.CaptureLienForChecks();
		old.KubiakRouteDate = null;
		desk.RestoreLienForChecks(old);
		Check(desk.Lien.KubiakRouteDate == null, "An older save has none");
		clock.RestoreClock(today, 9);
	}

	private void Refused(PlayerDesk desk) {
		desk.ClearPressingForChecks();
		desk.FoundLienForChecks(Walter, LienHolder.Kubiak);
		desk.PressFirstSingleForChecks("kubiak-refused", 1000, 1300f, today, today.AddDays(20));
		desk.ProcessHolderMoveDawnForChecks(desk.Lien.MoveDue);
		Check(desk.ResolveHolderMove(false, out _), "Kubiak's claim torn up");
		Check(desk.KubiakRouteLine(desk.Label.homeCityId)?.Contains("turned him down") == true, "The operators' line says you turned him down");
		var homeOp = desk.StopForChecks(StopKind.Op, true);
		Check(!desk.PitchAcetateAtStop(homeOp.StopId, "kubiak-refused", out var m) && m.Contains("Kubiak"), "No acetate orders from his town's operators either: " + m);
	}
}
