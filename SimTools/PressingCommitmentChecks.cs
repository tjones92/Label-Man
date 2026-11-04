using System;
using Godot;

/// <summary>Inventory-risk probes; no changes to list prices, physical unit costs or demand.</summary>
public static class PressingCommitmentChecks {
	public static void Run() {
		float initial = LabelPressingEconomics.InitialCommitment, weeks = LabelPressingEconomics.RepressWeeks;
		float albumInitial = LabelPressingEconomics.AlbumInitialCommitment, albumWeeks = LabelPressingEconomics.AlbumRepressWeeks;
		bool enabled = LabelPressingEconomics.Enabled;
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Pressing commitment: " + why); checks++; }
		RecordRuntimeData Fresh(string id) => new(new Record { recordId = id, format = ReleaseFormat.Single,
			releaseDate = new GameDate(1961, 1, 6) }) { weeksSinceRelease = 1 };
		try {
			LabelPressingEconomics.SetEnabledForChecks(true);
			var label = new AILabel { labelId = "commitment-check", pressingForecastSingleLogUnits = MathF.Log(8001f) };
			LabelPressingEconomics.SetCommitmentsForChecks(.5f, 3f);
			var control = Fresh("control");
			Check(LabelPressingEconomics.Press(control, label, 300).InitialRun == 4000, "the default keeps the old run");
			RecordRuntimeData Catalog() => new(new Record { recordId = "opening", format = ReleaseFormat.Single,
				releaseDate = new GameDate(1959, 1, 1) }) { weeksSinceRelease = 52 };
			var openingControl = Catalog();
			LabelPressingEconomics.Press(openingControl, label, 900);
			LabelPressingEconomics.SetCommitmentsForChecks(1f, 8f);
			var openingTreatment = Catalog();
			LabelPressingEconomics.Press(openingTreatment, label, 900);
			Check(openingControl.unitsPressed == openingTreatment.unitsPressed, "new-order settings grant no extra free opening inventory");
			var treatment = Fresh("treatment");
			var first = LabelPressingEconomics.Press(treatment, label, 300);
			Check(first.InitialRun == 8000 && first.Repressed == 0, "the full forecast is committed before later sales");
			Check(Math.Abs(LabelPressingEconomics.InitialRunReserve(label, ReleaseFormat.Single, .14f)
				- LabelPressingEconomics.PlantBill(ReleaseFormat.Single, first.InitialRun, false, .14f)) < .01f,
				"the decision reserve and actual first invoice agree");
			Check(treatment.pressingBookSold == control.pressingBookSold && treatment.unitsPressed > control.unitsPressed,
				"a commitment changes inventory, not the sales being settled");
			var next = LabelPressingEconomics.Press(treatment, label, 8000);
			Check(next.InitialRun == 0 && next.Repressed == 64000 && treatment.unitsPressed == 72000,
				"a repress is eight weeks of sales, and previously paid stock is retained");
			Check(LabelPressingEconomics.Press(treatment, label, 0).Repressed == 0, "zero sales do not generate another order");
			var restored = RuntimeRecordSaveData.From(treatment).ToRuntime();
			Check(restored.unitsPressed == treatment.unitsPressed && restored.pressingBookSold == treatment.pressingBookSold
				&& LabelPressingEconomics.Press(restored, label, 500).Repressed == 0, "saved stock covers future sales without rebilling");
			Check(LabelPressingEconomics.Press(Fresh("unknown"), new AILabel(), 20).InitialRun == 500,
				"a label with no history still starts at the plant minimum");
			GameRandom.Seed(9918); float expected = GameRandom.Randf(); GameRandom.Seed(9918);
			LabelPressingEconomics.Press(Fresh("rng"), label, 100);
			Check(GameRandom.Randf() == expected, "pressing consumes no random draws");
			LabelPressingEconomics.SetEnabledForChecks(false);
			Check(LabelPressingEconomics.InitialRunReserve(label, ReleaseFormat.Single, .14f) == 0f, "the disabled path still reserves nothing");
			const string prefix = "--pressing-repress-weeks=";
			Check(LabelPressingEconomics.ReadParameter(Array.Empty<string>(), prefix, 3f, 3f, 26f) == 3f
				&& LabelPressingEconomics.ReadParameter(new[] { prefix + "8.5" }, prefix, 3f, 3f, 26f) == 8.5f,
				"arguments use invariant numbers and preserve the fallback");
			foreach (var args in new[] { new[] { prefix + "NaN" }, new[] { prefix + "Infinity" }, new[] { prefix + "0" },
				new[] { prefix + "27" }, new[] { prefix + "8", prefix + "12" } }) {
				bool rejected = false;
				try { LabelPressingEconomics.ReadParameter(args, prefix, 3f, 3f, 26f); }
				catch (ArgumentException) { rejected = true; }
				Check(rejected, "invalid or duplicate commitment arguments are rejected");
			}
			GD.Print($"PRESSING_COMMITMENT_CHECK passed checks={checks}");
		} finally {
			LabelPressingEconomics.SetEnabledForChecks(enabled);
			LabelPressingEconomics.SetCommitmentsForChecks(initial, weeks);
			LabelPressingEconomics.SetAlbumCommitmentsForChecks(albumInitial, albumWeeks);
		}
	}
}
