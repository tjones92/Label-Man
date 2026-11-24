using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// --graduated-founder-overhead[=T] (LpGenreAllocationFollowup.md §36-37; absent = off). A founder-run Small, Boutique or
/// Independent firm stays founder-led while its paid staff is at most T full-time equivalents (bare switch: 2). A
/// founder-led firm pays the staff it hires (wages plus a desk allowance) but keeps home premises and contingent founder
/// pay. Off, the first quarter-employee also switches on the founder's fixed salary and a leased suite, so opening
/// overhead jumps from ~$25 to $1,100-1,900 a month and that jump, set by the opening roster, doubled two-year
/// Independent closures. Above T the firm leases an office and salaries its founder as before. No draws.
/// </summary>
public static class GraduatedFounderOverhead {
    public const string Switch = "--graduated-founder-overhead";
    public const double DefaultStaffLimit = 2;
    public static bool Enabled { get; private set; }
    public static double StaffLimit { get; private set; }

    public static void Configure(IEnumerable<string> arguments) {
        var args = (arguments ?? Array.Empty<string>()).ToArray();
        string arg = args.LastOrDefault(a => a == Switch || a.StartsWith(Switch + "=", StringComparison.Ordinal));
        Enabled = arg != null;
        StaffLimit = !Enabled ? 0 : arg == Switch ? DefaultStaffLimit
            : double.TryParse(arg.Substring(Switch.Length + 1), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double t) && t >= .25 && t <= 10 ? t
            : throw new ArgumentException(Switch + "=T takes paid staff between 0.25 and 10.");
        if (Enabled && !FounderCompensation.Enabled)
            throw new ArgumentException(Switch + " requires --founder-surplus-compensation.");
    }

    internal static void SetForChecks(bool enabled, double staffLimit = DefaultStaffLimit) {
        Enabled = enabled; StaffLimit = enabled ? staffLimit : 0;
    }

    /// <summary>Paid staff a founder-run firm may carry and still work from home on contingent founder pay. 0 when off.</summary>
    internal static double PaidStaffAllowance => Enabled ? StaffLimit : 0;
}
