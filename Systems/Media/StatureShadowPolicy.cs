using System;

/// <summary>
/// Pure standing-policy kernel. It does not collect evidence, persist state,
/// choose a calibration or change any simulation decision.
/// </summary>
public static class StatureShadowPolicy {
	public enum Band { BelowRising, Rising, Established, Star, Superstar }
	public enum ResultStatus { BelowRising, Observed, MissingEvidence }

	public readonly struct Range {
		public readonly double Lower, Upper;
		public Range(double lower, double upper) {
			if (!double.IsFinite(lower) || !double.IsFinite(upper) || lower < 0 || upper > 1 || lower > upper)
				throw new ArgumentOutOfRangeException(nameof(lower), "Evidence bounds must be ordered within [0,1].");
			Lower = lower; Upper = upper;
		}
		public static Range Exact(double value) => new(value, value);
		public static Range Missing => new(0, 1);
	}

	public readonly struct Route {
		public readonly Range Response, Draw, Reach;
		public Route(Range response, Range draw, Range reach) { Response = response; Draw = draw; Reach = reach; }
	}

	public sealed class Boundary {
		public double Entry, Exit;
		// Regional, record, conversion, in that order.
		public double[] ReachWeights = Array.Empty<double>();
	}

	public sealed class Calibration {
		public string Version;
		public DateTime EffectiveDate;
		// Rising through Superstar, in that order.
		public Boundary[] Bands = Array.Empty<Boundary>();
	}

	public readonly struct Support {
		public readonly Range Value;
		public readonly double Boundary;
		/// <summary>Support bounds in regional, record, conversion order.</summary>
		public readonly Range[] Routes;
		public Support(Range value, double boundary, Range[] routes) { Value = value; Boundary = boundary; Routes = routes; }
	}

	public sealed class Result {
		public Band Band;
		public ResultStatus Status;
		public Support[] Bands;
	}

	private static bool Unit(double n) => double.IsFinite(n) && n >= 0 && n <= 1;

	public static void Validate(Calibration c) {
		if (c == null || string.IsNullOrWhiteSpace(c.Version) || c.EffectiveDate == default || c.Bands?.Length != 4)
			throw new ArgumentException("Calibration requires version, date and four bands.");
		Boundary previous = null;
		foreach (Boundary band in c.Bands) {
			if (band == null || !Unit(band.Entry) || !Unit(band.Exit) || band.Exit > band.Entry || band.ReachWeights?.Length != 3)
				throw new ArgumentException("Invalid band boundaries or reach weights.");
			for (int r = 0; r < 3; r++)
				if (!Unit(band.ReachWeights[r]) || (previous != null && band.ReachWeights[r] < previous.ReachWeights[r]))
					throw new ArgumentException("Higher bands cannot require less reach.");
			if (previous != null && (band.Entry < previous.Entry || band.Exit < previous.Exit))
				throw new ArgumentException("Higher bands cannot have lower boundaries.");
			previous = band;
		}
	}

	public static Result Evaluate(Route[] routes, Calibration calibration, DateTime date, Band previous = Band.BelowRising) {
		Validate(calibration);
		if (date.Date < calibration.EffectiveDate.Date) throw new ArgumentException("Calibration is not yet effective.");
		if (routes?.Length != 3 || (int)previous < (int)Band.BelowRising || (int)previous > (int)Band.Superstar)
			throw new ArgumentException("Three routes and a valid previous band are required.");
		var supports = new Support[4];
		for (int i = 0; i < 4; i++) {
			Boundary band = calibration.Bands[i];
			double threshold = i + 1 <= (int)previous ? band.Exit : band.Entry;
			double lower = 0, upper = 0;
			var routeSupports = new Range[3];
			for (int r = 0; r < 3; r++) {
				Route route = routes[r]; double weight = band.ReachWeights[r];
				double routeLower = Math.Clamp(Math.Max(route.Response.Lower, route.Draw.Lower) * (1 - weight + weight * route.Reach.Lower), 0, 1);
				double routeUpper = Math.Clamp(Math.Max(route.Response.Upper, route.Draw.Upper) * (1 - weight + weight * route.Reach.Upper), 0, 1);
				routeSupports[r] = new Range(routeLower, routeUpper);
				lower = Math.Max(lower, routeLower); upper = Math.Max(upper, routeUpper);
			}
			supports[i] = new Support(new Range(lower, upper), threshold, routeSupports);
		}
		for (int i = 3; i >= 0; i--) {
			Support support = supports[i];
			if (support.Value.Lower >= support.Boundary)
				return new Result { Band = (Band)(i + 1), Status = ResultStatus.Observed, Bands = supports };
			if (support.Value.Upper >= support.Boundary)
				return new Result { Band = Band.BelowRising, Status = ResultStatus.MissingEvidence, Bands = supports };
		}
		return new Result { Band = Band.BelowRising, Status = ResultStatus.BelowRising, Bands = supports };
	}
}
