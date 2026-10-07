using System;
using System.Linq;
using Godot;

/// <summary>
/// The desk's pages that are about work being done (CATALOG, DISTRIBUTION, OFFICE) set as printed work orders; see
/// <see cref="WorkOrder"/> for the sheet. A page becomes a form by running its body inside <see cref="WithForm"/>, which
/// points <c>content</c> and <c>contentRoot</c> at the form's body, so every section the page already builds lands on the
/// sheet, and turns <c>Heading</c> and <c>SectionHeading</c> into the form's printed bands while it runs.
/// </summary>
public partial class PlayerDeskPanel {
	/// <summary>&gt; 0 while a work order is being filled in.</summary>
	private int formDepth;

	private void WithForm(WorkOrder.Form form, Action body) {
		content.AddChild(form.Sheet);
		VBoxContainer savedContent = content, savedRoot = contentRoot;
		content = form.Body; contentRoot = form.Body;
		formDepth++;
		try { body(); }
		finally { formDepth--; content = savedContent; contentRoot = savedRoot; }
	}

	/// <summary>The label's own forms: its crest and name in its lettering across the top.</summary>
	private static WorkOrder.Form HouseForm(AILabel house, string title, string number, string stamp = null, Color? stampInk = null) {
		LabelBrand brand = LabelBrand.For(house);
		return WorkOrder.Begin(brand.DisplayName(house?.labelName ?? "Records"), PaperTheme.Lettering(brand.Lettering),
			brand.Lettering == LetteringStyle.Script ? 30 : 22, title, number, stamp, stampInk, brand, house?.labelName);
	}

	/// <summary>Printed band for a section heading inside a form.</summary>
	private static void StyleFormHeading(Label node) {
		node.Text = node.Text.ToUpperInvariant();
		node.AddThemeFontSizeOverride("font_size", 15);
		node.AddThemeColorOverride("font_color", WorkOrder.FormInk);
		node.AddThemeStyleboxOverride("normal", WorkOrder.SectionBand());
		node.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
	}

	private static int DaysBetween(GameDate from, GameDate to) =>
		(new DateTime(to.year, to.month, to.day) - new DateTime(from.year, from.month, from.day)).Days;

	private void PageDistribution() {
		PlayerDesk desk = PlayerDesk.Instance;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		// A shipping ticket is urgent while the phone is ringing for stock; away from home it is stamped to say so.
		string stamp = desk.OpenInboundCallCount > 0 ? "RUSH" : !desk.AtHome ? "ON THE ROAD" : null;
		Color ink = desk.OpenInboundCallCount > 0 ? RubberStamp.Red : RubberStamp.Blue;
		WithForm(HouseForm(desk.Label, "Route & shipping order", WorkOrder.Serial("dist:" + desk.Label?.labelId + today.month + today.year), stamp, ink), DistributionBody);
	}

	private void PageOffice() {
		PlayerDesk desk = PlayerDesk.Instance;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		WithForm(HouseForm(desk.Label, "Office requisition — staff & services", WorkOrder.Serial("office:" + desk.Label?.labelId + today.month + today.year)), OfficeBody);
	}
}
