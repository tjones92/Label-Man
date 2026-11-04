using System;
using System.Collections.Generic;
using Godot;

public partial class CompetitorManager {
	private Dictionary<string, RecordClubChannel.LabelWeek> recordClubWeek;

	/// <summary>Allocates this settlement's club and package units across the catalogues.</summary>
	private void AllocateRecordClubWeek(ChartManager.CompletedWeekSettlement settlement) {
		recordClubWeek = RecordClubChannel.Enabled && settlement.SettlementId > 0 && ChartManager.Instance != null
			? RecordClubChannel.Allocate(aiLabels, ChartManager.Instance.GetAllRecords(), TimeManager.Instance?.CurrentDate.year ?? 1960)
			: null;
	}

	/// <summary>
	/// Books one label's club week and returns its receipts. Clubs sell by mail, so nothing is deferred, skimmed or
	/// shelved, and no record's figures move. Prices and costs are the FTC's period figures (RecordClubChannel):
	/// - an operator's own masters: the club price less the club's operating cost, pressing and mechanicals, and
	///   its acts' club royalty;
	/// - an operator's licensed masters: the club price less the full licensed-record cost, which already pays
	///   the licensor;
	/// - a licensor's or packager's masters: the 17.8c licence royalty, out of which its acts are paid.
	/// </summary>
	private float SettleRecordClub(AILabel label) {
		if (recordClubWeek == null || !recordClubWeek.TryGetValue(label.labelId, out var week)) return 0f;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		double ownCost = GetPressingCostPerUnit(ReleaseFormat.Album) + albumPackagingCostPerUnit * .5f
			+ (LabelPressingEconomics.PeriodAlbumCosts ? LabelPressingEconomics.ExpectedAlbumCompositionCost : 0f)
			+ RecordClubChannel.ClubOperatingCost;
		double ownGross = week.OwnUnits * (double)RecordClubChannel.ClubPrice;
		double licensedGross = week.LicensedInUnits * (double)RecordClubChannel.ClubPrice;
		double licenceGross = (week.LicensedOutUnits + week.PackageUnits) * (double)RecordClubChannel.LicensorRoyalty;
		double ownCogs = week.OwnUnits * ownCost;
		double licensedCogs = week.LicensedInUnits * (double)RecordClubChannel.ClubTotalCostLicensed;

		// Acts are paid by whoever owns the master. A licensed or packaged act's royalty cannot exceed the licence.
		double ownRoyalty = 0, licenceRoyalty = 0;
		long masterUnits = week.OwnUnits + week.LicensedOutUnits + week.PackageUnits;
		double ownFraction = masterUnits > 0 ? week.OwnUnits / (double)masterUnits : 0;
		foreach (var share in week.Artists) {
			var artist = ArtistManager.Instance?.GetArtist(share.ArtistId);
			double rate = (artist?.royaltyRate ?? .05f) * RecordClubChannel.ArtistClubFactor;
			double ownPart = share.Units * ownFraction * RecordClubChannel.ClubPrice * rate;
			double licencePart = Math.Min(share.Units * (1 - ownFraction) * RecordClubChannel.ClubPrice * rate,
				share.Units * (1 - ownFraction) * RecordClubChannel.LicensorRoyalty);
			ownRoyalty += ownPart; licenceRoyalty += licencePart;
			// No recoupment: the act may now be under contract elsewhere, and this is a former label's catalogue.
			if (artist != null) artist.totalRoyaltyEarnings += (float)(ownPart + licencePart);
		}

		double gross = ownGross + licensedGross + licenceGross;
		double cogs = ownCogs + licensedCogs;
		double royalty = ownRoyalty + licenceRoyalty;
		double receipts = gross - cogs - royalty;
		LabelCompanyAccounts.RecordSale(label, gross, week.OwnUnits + week.LicensedInUnits + week.LicensedOutUnits + week.PackageUnits);
		label.weeklyGrossRevenue += (float)gross;
		label.weeklyCogs += (float)cogs;
		label.weeklyArtistRoyalty += (float)royalty;
		label.weeklyNetRevenue += (float)receipts;

		RecordClubChannel.Record(year, label, "OperatorOwn", week.OwnUnits, ownGross, ownGross - ownCogs - ownRoyalty, ownRoyalty);
		RecordClubChannel.Record(year, label, "OperatorLicensed", week.LicensedInUnits, licensedGross, licensedGross - licensedCogs, 0);
		long licenceUnits = week.LicensedOutUnits + week.PackageUnits;
		double licenceNet = licenceGross - licenceRoyalty;
		RecordClubChannel.Record(year, label, "Licensor", week.LicensedOutUnits, week.LicensedOutUnits * (double)RecordClubChannel.LicensorRoyalty,
			licenceUnits > 0 ? licenceNet * week.LicensedOutUnits / licenceUnits : 0, licenceUnits > 0 ? licenceRoyalty * week.LicensedOutUnits / licenceUnits : 0);
		RecordClubChannel.Record(year, label, "Package", week.PackageUnits, week.PackageUnits * (double)RecordClubChannel.LicensorRoyalty,
			licenceUnits > 0 ? licenceNet * week.PackageUnits / licenceUnits : 0, licenceUnits > 0 ? licenceRoyalty * week.PackageUnits / licenceUnits : 0);
		return (float)receipts;
	}
}
