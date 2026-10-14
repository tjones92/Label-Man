/// <summary>A completed daily-market decision, retained by the canonical act. This is not a
/// public report, pending offer, visit, introduction or account of the evaluator's hidden reasons.</summary>
public sealed class SceneMarketDecision {
    public string Id { get; set; }
    public string ArtistId { get; set; }
    public string LabelId { get; set; }
    public string LabelName { get; set; }
    public int Day { get; set; }
    public SceneMarketDecisionKind Kind { get; set; }
    public string SelectedLabelId { get; set; }
    public string SelectedLabelName { get; set; }
    public bool SelectedDealCompleted { get; set; }
}
public enum SceneMarketDecisionKind { Signed, AlternativeNotSelected, SigningAttemptNotCompleted }
