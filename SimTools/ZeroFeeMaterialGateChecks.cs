using System;
using Godot;

/// <summary>--zero-fee-material-gate-check: off never clears; on, a zero fee clears for any active AI label and nothing else does.</summary>
public static class ZeroFeeMaterialGateChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Zero-fee material gate: " + why); }
		bool was = ZeroFeeMaterialGate.Enabled;
		try {
			var labels = new[] { new AILabel { tier = LabelTier.Major }, new AILabel { tier = LabelTier.MidTier },
				new AILabel { tier = LabelTier.Independent }, new AILabel { tier = LabelTier.Small } };
			var player = new AILabel { tier = LabelTier.Independent, isPlayerOwned = true };
			var dead = new AILabel { tier = LabelTier.Independent, status = LabelStatus.Defunct };
			ZeroFeeMaterialGate.SetForChecks(false);
			foreach (var l in labels) Check(!ZeroFeeMaterialGate.Clears(l, 0f), "off never clears");
			ZeroFeeMaterialGate.SetForChecks(true);
			foreach (var l in labels) {
				Check(ZeroFeeMaterialGate.Clears(l, 0f), $"{l.tier}: a zero fee clears");
				Check(!ZeroFeeMaterialGate.Clears(l, .01f) && !ZeroFeeMaterialGate.Clears(l, 500f) && !ZeroFeeMaterialGate.Clears(l, float.NaN),
					$"{l.tier}: a positive or undefined fee never clears");
			}
			Check(!ZeroFeeMaterialGate.Clears(player, 0f) && !ZeroFeeMaterialGate.Clears(dead, 0f) && !ZeroFeeMaterialGate.Clears(null, 0f),
				"the player, a closed label and no label never clear");
			GD.Print("ZERO_FEE_MATERIAL_GATE_CHECK passed");
		} finally { ZeroFeeMaterialGate.SetForChecks(was); }
	}
}
