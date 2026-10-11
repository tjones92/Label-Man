using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

/// <summary>Render the real A&R board and exercise room selection; never a normal game startup path.</summary>
public partial class SceneRoomUiRunner : Node {
    private static IEnumerable<Node> Descendants(Node node) {
        foreach (Node child in node.GetChildren()) { yield return child; foreach (var other in Descendants(child)) yield return other; }
    }
    public override async void _Ready() {
        try {
            AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
            var desk = PlayerDesk.Instance;
            if (!desk.FoundLabel("Room UI Records", "nashville", out string message)) throw new InvalidOperationException(message);
            TimeManager.Instance.RestoreClock(GameDate.StartDate, 17);
            var panel = Descendants(this).OfType<PlayerDeskPanel>().Single();
            panel.OpenAtTab("A&R");
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var fold = Descendants(this).OfType<Button>().FirstOrDefault(b => b.IsVisibleInTree() && b.Text == "FOLD THE PAPER  ×");
            fold?.EmitSignal(Button.SignalName.Pressed);
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            if (OS.GetCmdlineUserArgs().Contains("--scouting-contract-outlook-ui-check")) {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var prospect = ScoutingContractOutlookChecks.PrepareAct(desk);
                prospect.Artist.evolution.artisticAmbition = 0.95f;
                if (!desk.ApproachToSign(prospect, out message) || prospect.Talk == null) throw new InvalidOperationException("Need a negotiation: " + message);
                var talk = prospect.Talk; var ask = talk.ask;
                typeof(PlayerDeskPanel).GetField("negotiating", flags).SetValue(panel, prospect);
                async System.Threading.Tasks.Task Show(string tab) { panel.OpenAtTab(tab); for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
                IEnumerable<Label> Labels() => Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion() && l.IsVisibleInTree());
                Button Find(Func<Button, bool> match) => Descendants(panel).OfType<Button>().SingleOrDefault(b => !b.IsQueuedForDeletion() && match(b));
                void NoHidden() {
                    if (Labels().Any(l => l.Text.Contains("reservation") || l.Text.Contains("patience") || l.Text.Contains("credib") || l.Text.Contains("stake")))
                        throw new InvalidOperationException("Hidden acceptance values must not render.");
                }
                string outlookDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-contract-outlook-ui-v1"); Directory.CreateDirectory(outlookDir);
                async System.Threading.Tasks.Task Shot(string name) {
                    foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(outlookDir, name));
                }
                await Show("A&R");
                var block = Descendants(panel).OfType<VBoxContainer>().SingleOrDefault(b => !b.IsQueuedForDeletion() && b.Name == "ContractOutlook");
                if (block == null || !block.IsVisibleInTree() || !Labels().Any(l => l.Text.Contains("never heard of your label")) ||
                    !Labels().Any(l => l.Name == "OwnLabelSummary" && l.Text.StartsWith("Your label:")))
                    throw new InvalidOperationException("The table must show what the act is weighing and the player's own label.");
                NoHidden();
                await Shot("weighing.png");
                var bound = ScoutingContractOutlookChecks.LabelBoundTerms(talk) ?? throw new InvalidOperationException("Need label-bound terms.");
                if (!desk.TableOffer(talk, bound.Advance, bound.RoyaltyRate, bound.TermYears, bound.SinglesObligation, bound.LabelOwnsPublishing,
                        bound.ArtistCreativeControl, out message) || talk.capabilityObjection == null) throw new InvalidOperationException("Need a label objection: " + message);
                await Show("A&R");
                var push = Find(b => b.Text == "PROMISE A PUSH");
                if (push == null || !Labels().Any(l => l.Text.Contains("what your label can't do yet"))) throw new InvalidOperationException("Label objection must offer the push and say what's wrong.");
                NoHidden();
                await Shot("objection.png");
                float cash = desk.Label.cashReserves; int minute = TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute;
                push.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var terms = Labels().SingleOrDefault(l => l.Text.StartsWith("Your offer includes a half-page trade ad"));
                if (!talk.promotionPromised || terms == null || !terms.Text.Contains($"${PlayerDesk.PromotionPromiseQuote():N0}") || Find(b => b.Name == "DropPromotionPromise") == null)
                    throw new InvalidOperationException("Tabling form must show the promised push, its price and a way to drop it.");
                if (desk.Label.cashReserves != cash || TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute != minute || desk.ContractCommitments.Count > 0)
                    throw new InvalidOperationException("Proposing must cost nothing and commit nothing.");
                await Shot("tabling.png");
                if (!desk.TableOffer(talk, ask.Advance, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, ask.LabelOwnsPublishing,
                        ask.ArtistCreativeControl, out message) || talk.stage != ContractTalkStage.Done) throw new InvalidOperationException("Promised package at the ask must sign: " + message);
                typeof(PlayerDeskPanel).GetField("negotiating", flags).SetValue(panel, null);
                await Show("ROSTER");
                int due = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.FundedPromotion).DueDay;
                if (!Labels().Any(l => l.Text == $"Record promised out by {LocalSceneRoomService.Date(due).ToShortString()}, then a trade ad"))
                    throw new InvalidOperationException("Roster card must show the open promotion promise.");
                await Shot("roster.png");
                GD.Print("SCOUTING_CONTRACT_OUTLOOK_UI_PASS weighing=true ownLabel=true labelObjection=true push=true free=true hidden=true roster=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-credible-offers-ui-check")) {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var prospect = ScoutingContractOutlookChecks.PrepareAct(desk); var act = prospect.Artist;
                act.evolution.artisticAmbition = 0.95f;
                var bidder = ScoutingCredibleOffersChecks.OpenRival(act) ?? throw new InvalidOperationException("Need a reachable rival.");
                desk.MakeRivalOfferForCheck(act, bidder);
                if (!desk.ApproachToSign(prospect, out message) || prospect.Talk == null) throw new InvalidOperationException("Need a negotiation: " + message);
                var talk = prospect.Talk; var offer = desk.PendingRivalOffer(act.artistId);
                typeof(PlayerDeskPanel).GetField("negotiating", flags).SetValue(panel, prospect);
                async System.Threading.Tasks.Task Show(string tab) { panel.OpenAtTab(tab); for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); }
                IEnumerable<Label> Labels() => Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion() && l.IsVisibleInTree());
                Button Find(Func<Button, bool> match) => Descendants(panel).OfType<Button>().SingleOrDefault(b => !b.IsQueuedForDeletion() && match(b));
                void NoHidden() {
                    if (Labels().Any(l => l.Text.Contains("reservation") || l.Text.Contains("patience") || l.Text.Contains("credib") || l.Text.Contains("stake")))
                        throw new InvalidOperationException("Hidden acceptance values must not render.");
                }
                string offersDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-credible-offers-ui-v1"); Directory.CreateDirectory(offersDir);
                async System.Threading.Tasks.Task Shot(string name) {
                    foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(offersDir, name));
                }
                await Show("A&R");
                if (!Labels().Any(l => l.Text.StartsWith("OTHER PAPER:") && l.Text.Contains(bidder.labelName) && l.Text.Contains("$")) ||
                    !Labels().Any(l => l.Text.Contains(bidder.labelName) && l.Text.Contains("They answer by")))
                    throw new InvalidOperationException("The table must state the rival's real offer and its decision day.");
                NoHidden();
                await Shot("offer.png");
                float cash = desk.Label.cashReserves; int minute = TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute;
                var add = Find(b => b.Name == "AddDistributionPromise") ?? throw new InvalidOperationException("Tabling form must offer the distribution promise.");
                add.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!talk.distributionPromised || !Labels().Any(l => l.Text.StartsWith("Your offer includes a wholesale house")) || Find(b => b.Name == "DropDistributionPromise") == null)
                    throw new InvalidOperationException("Tabling form must show the distribution promise and a way to drop it.");
                if (desk.Label.cashReserves != cash || TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute != minute || desk.ContractCommitments.Count > 0)
                    throw new InvalidOperationException("Proposing must cost nothing and commit nothing.");
                await Shot("tabling.png");
                var (losing, winning) = ScoutingCredibleOffersChecks.RivalBoundTerms(desk, talk, offer);
                if (!losing.HasValue || !winning.HasValue) throw new InvalidOperationException("Need losing and winning packages.");
                var lose = losing.Value;
                if (!desk.TableOffer(talk, lose.Advance, lose.RoyaltyRate, lose.TermYears, lose.SinglesObligation, lose.LabelOwnsPublishing,
                        lose.ArtistCreativeControl, out message) || talk.stage != ContractTalkStage.Objection) throw new InvalidOperationException("Need the rival objection: " + message);
                await Show("A&R");
                if (!Labels().Any(l => l.Text.Contains(bidder.labelName) && l.Text.Contains("Beat it")) || Find(b => b.Text == "SWEETEN IT") == null)
                    throw new InvalidOperationException("The objection must name the rival's paper and offer a way back to the table.");
                NoHidden();
                await Shot("objection.png");
                var win = winning.Value;
                desk.PlayNegotiationCounter(talk, ContractCounter.SweetenAxis, out _);
                if (!desk.TableOffer(talk, win.Advance, win.RoyaltyRate, win.TermYears, win.SinglesObligation, win.LabelOwnsPublishing,
                        win.ArtistCreativeControl, out message) || talk.stage != ContractTalkStage.Done) throw new InvalidOperationException("Paper that beats the rival must sign: " + message);
                typeof(PlayerDeskPanel).GetField("negotiating", flags).SetValue(panel, null);
                await Show("A&R");
                if (!Labels().Any(l => l.Text.Contains(bidder.labelName) && l.Text.Contains("turned down")))
                    throw new InvalidOperationException("The notebook must record how the rival's offer ended.");
                await Shot("notebook.png");
                await Show("ROSTER");
                int due = desk.ContractCommitments.Single(c => c.Kind == ContractCommitmentKind.Distribution).DueDay;
                if (!Labels().Any(l => l.Text == $"Record promised out by {LocalSceneRoomService.Date(due).ToShortString()}, then a wholesale line"))
                    throw new InvalidOperationException("Roster card must show the open distribution promise.");
                await Shot("roster.png");
                GD.Print("SCOUTING_CREDIBLE_OFFERS_UI_PASS offer=true distribution=true free=true rivalObjection=true hidden=true notebook=true roster=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-session-promise-ui-check")) {
                var prospect = ScoutingSessionPromiseChecks.PrepareNegotiation(desk); var talk = prospect.Talk; var ask = talk.ask;
                if (!desk.TableOffer(talk, PlayerDesk.MinimumAdvanceForAcceptance(talk) - 5, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation,
                        ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message) || talk.stage != ContractTalkStage.Objection)
                    throw new InvalidOperationException("Need a real objection: " + message);
                typeof(PlayerDeskPanel).GetField("negotiating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(panel, prospect);
                panel.OpenAtTab("A&R");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                float cash = desk.Label.cashReserves; int minute = TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute;
                var promise = Descendants(panel).OfType<Button>().SingleOrDefault(b => !b.IsQueuedForDeletion() && b.Text == "PROMISE A SESSION");
                if (promise == null) throw new InvalidOperationException("Objection must offer the session promise.");
                promise.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var terms = Descendants(panel).OfType<Label>().SingleOrDefault(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("Your offer includes a funded studio session"));
                var drop = Descendants(panel).OfType<Button>().SingleOrDefault(b => !b.IsQueuedForDeletion() && b.Name == "DropSessionPromise");
                if (!talk.sessionPromised || terms == null || drop == null || !terms.Text.Contains($"${desk.SessionPromiseQuote():N0}"))
                    throw new InvalidOperationException("Tabling form must show the promised session, its quote and a way to drop it.");
                if (desk.Label.cashReserves != cash || TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute != minute || desk.ContractCommitments.Count > 0)
                    throw new InvalidOperationException("Proposing must cost nothing and commit nothing.");
                if (Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && (l.Text.Contains("reservation") || l.Text.Contains("patience"))))
                    throw new InvalidOperationException("Hidden acceptance values must not render.");
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                string promiseDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-session-promise-ui-v1"); Directory.CreateDirectory(promiseDir);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(promiseDir, "tabling.png"));
                drop.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (talk.sessionPromised || Descendants(panel).OfType<Button>().Any(b => !b.IsQueuedForDeletion() && b.Name == "DropSessionPromise"))
                    throw new InvalidOperationException("Dropping must take the promise off the table.");
                var add = Descendants(panel).OfType<Button>().SingleOrDefault(b => !b.IsQueuedForDeletion() && b.Name == "AddSessionPromise");
                if (add == null) throw new InvalidOperationException("Tabling form must let the promise be added back.");
                add.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (!talk.sessionPromised || !desk.TableOffer(talk, ask.Advance, ask.RoyaltyRate, ask.TermYears,
                        ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl, out message) || talk.stage != ContractTalkStage.Done)
                    throw new InvalidOperationException("Promised package at the ask must sign: " + message);
                typeof(PlayerDeskPanel).GetField("negotiating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(panel, null);
                panel.OpenAtTab("ROSTER");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var due = desk.ContractCommitments.Single(c => c.Status == ContractCommitmentStatus.Open).DueDay;
                var fact = Descendants(panel).OfType<Label>().SingleOrDefault(l => !l.IsQueuedForDeletion() && l.Text == "Session promised by " + LocalSceneRoomService.Date(due).ToShortString());
                if (fact == null || !fact.IsVisibleInTree()) throw new InvalidOperationException("Roster card must show the open promise and its due date.");
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(promiseDir, "roster.png"));
                GD.Print("SCOUTING_SESSION_PROMISE_UI_PASS offered=true quoted=true free=true hidden=true dropped=true rosterDue=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-contract-continuity-ui-check")) {
                var act = ScoutingContractContinuityChecks.PrepareRenewal(desk, true); string actId = act.artistId;
                var ask = desk.PendingRenewal.Ask;
                if (!desk.TableOffer(desk.PendingRenewal.Talk, ask.Advance * .75f, ask.RoyaltyRate, ask.TermYears, ask.SinglesObligation, true, false, out message) ||
                    desk.PendingRenewal.Talk.stage != ContractTalkStage.Objection) throw new InvalidOperationException("Need a real objection: " + message);
                int rounds = desk.PendingRenewal.Talk.roundsPlayed, patience = desk.PendingRenewal.Talk.patienceLeft;
                string slot = "Renewal UI " + Guid.NewGuid().ToString("N");
                try { if (!SaveGameService.Save(slot, out message) || !SaveGameService.Load(slot, out message)) throw new InvalidOperationException("Compressed save/load: " + message); }
                finally { SaveGameService.Delete(slot); }
                // A fresh panel session holds no renewal page; the retained discussion must be reachable from the roster.
                typeof(PlayerDeskPanel).GetField("renewingArtist", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(panel, null);
                panel.OpenAtTab("ROSTER");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var live = ArtistManager.Instance.GetArtist(actId);
                var retained = Descendants(panel).OfType<Label>().SingleOrDefault(l => !l.IsQueuedForDeletion() && l.Text == "Renewal discussion retained for " + live.stageName + ".");
                if (retained == null) throw new InvalidOperationException("Roster must announce the retained renewal after reload.");
                var terms = RenewalContractIdentity.From(live); float cashBefore = desk.Label.cashReserves;
                int minute = TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute;
                Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "RESUME RENEWAL DISCUSSION").EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var heading = Descendants(panel).OfType<Label>().SingleOrDefault(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("RENEWING — " + live.stageName.ToUpperInvariant()));
                var talk = desk.PendingRenewal?.Talk;
                if (heading == null || talk == null || talk.stage != ContractTalkStage.Objection || talk.roundsPlayed != rounds || talk.patienceLeft != patience ||
                    !Descendants(panel).OfType<Button>().Any(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("HOLD FIRM")))
                    throw new InvalidOperationException("Resume must reopen the same objection with its rounds and patience.");
                if (desk.Label.cashReserves != cashBefore || TimeManager.Instance.CurrentHour * 60 + TimeManager.Instance.CurrentMinute != minute)
                    throw new InvalidOperationException("Resuming must cost no time or money.");
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                string continuityDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-contract-continuity-ui-v1"); Directory.CreateDirectory(continuityDir);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(continuityDir, "renewal.png"));
                typeof(PlayerDeskPanel).GetField("renewingArtist", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(panel, null);
                panel.OpenAtTab("ROSTER");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "STEP BACK FROM RENEWAL").EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (desk.PendingRenewal != null || RenewalContractIdentity.From(live) != terms || live.labelId != desk.Label.labelId || desk.Label.cashReserves != cashBefore ||
                    Descendants(panel).OfType<Button>().Any(b => !b.IsQueuedForDeletion() && b.Text == "RESUME RENEWAL DISCUSSION"))
                    throw new InvalidOperationException("Stepping back must clear the discussion without changing the contract, roster or cash.");
                GD.Print("SCOUTING_CONTRACT_CONTINUITY_UI_PASS reload=true resume=true sameObjection=true free=true stepBack=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-offer-planning-ui-check")) {
                var booking = ScoutingOfferPlanningChecks.PrepareDemo(desk);
                var clock = TimeManager.Instance;
                clock.RestoreClock(LocalSceneRoomService.Date(booking.Day), booking.StartMinute / 60 - 1, booking.StartMinute % 60);
                desk.Label.cashReserves = 10000;
                if (!desk.OpenScoutingDossier(booking.ArtistId, out message)) throw new InvalidOperationException(message);
                var prospect = desk.Slate.Single(p => p.Artist.artistId == booking.ArtistId);
                if (!desk.ApproachToSign(prospect, out message)) throw new InvalidOperationException(message);
                prospect.Posture = NegotiationPosture.Pushover; prospect.Talk = null;
                typeof(PlayerDeskPanel).GetField("negotiating", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(panel, prospect);
                panel.OpenAtTab("A&R");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var resource = Descendants(panel).OfType<Label>().Single(l => !l.IsQueuedForDeletion() && l.Name == "OfferResourcePlan");
                if (!resource.Text.Contains("Recording estimate") || !resource.Text.Contains("passes an agreed start") || !resource.TooltipText.Contains(booking.ArtistName))
                    throw new InvalidOperationException("Contract needs recording estimate, named unpaid fee and actual appointment conflict.");
                var inputs = Descendants(panel).OfType<SpinBox>().Where(s => !s.IsQueuedForDeletion()).ToArray();
                var advanceInput = inputs.Single(s => s.MaxValue == 100000);
                float cash = desk.Label.cashReserves; int evidence = desk.ScoutingDossiers.Sum(d => d.Observations.Count);
                string before = resource.Text;
                advanceInput.Value = cash - booking.Fee + 1;
                if (resource.Text == before || !resource.Text.Contains("exceeds current cash") || desk.Label.cashReserves != cash ||
                    desk.ScoutingDossiers.Sum(d => d.Observations.Count) != evidence || desk.ScoutingMeetings.Single().Paid)
                    throw new InvalidOperationException("Editing must update the combined shortfall without spending money or acquiring evidence.");
                advanceInput.Value = 100;
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var scroll = Descendants(panel).OfType<ScrollContainer>().Single(s => s.IsAncestorOf(resource));
                scroll.ScrollVertical = (int)resource.Position.Y;
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                string output = ProjectSettings.GlobalizePath("res://SimLogs/scouting-offer-planning-ui-v1"); Directory.CreateDirectory(output);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(output, "contract.png"));
                var submit = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("OFFER CONTRACT"));
                if (submit.Disabled || resource.Size.X <= 0) throw new InvalidOperationException("Forecast must leave the choice available.");
                GD.Print("SCOUTING_OFFER_PLANNING_UI_PASS sharedForm=true liveBudget=true namedFees=true risk=true noSpend=true choice=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-audience-ui-check")) {
                var audienceClock = TimeManager.Instance;
                audienceClock.RestoreClock(GameDate.StartDate, 9);
                var appearance = desk.ScoutingItinerary().First();
                if (!desk.WaitForSceneAppearance(appearance.BillId, appearance.EventId, out message) || !desk.ListenSceneAppearance(appearance.BillId, appearance.EventId, out message)) throw new InvalidOperationException(message);
                var heard = desk.ScoutingDossiers.Single(d => d.ArtistId == appearance.ArtistId).Observations.Single(o => o.Kind == ScoutingEvidenceKind.Performance);
                var controlled = desk.CaptureState(); controlled.ScoutingKnowledge.Dossiers = new() { ScoutingAudienceChecks.ContextFixture(heard) };
                if (!desk.RestoreState(controlled, out message)) throw new InvalidOperationException(message);
                panel.OpenAtTab("A&R");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var audienceSummary = Descendants(panel).OfType<Label>().Single(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("Audience:"));
                if (!audienceSummary.Text.Contains("2 distinct whole-bill")) throw new InvalidOperationException("Notebook needs one crowd per bill.");
                Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "AUDIENCE EVIDENCE").EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var sourceLine = Descendants(panel).OfType<Label>().Single(l => l.IsVisibleInTree() && l.Text.Contains("150 attendees against 200 capacity"));
                if (!sourceLine.Text.Contains("Fixture booker") || !sourceLine.Text.Contains("Received") || !sourceLine.Text.Contains("admission not recorded")) throw new InvalidOperationException("Exact source counts need provenance and unknown admission.");
                var visualLine = Descendants(panel).OfType<Label>().Single(l => l.IsVisibleInTree() && l.Text.StartsWith("Your listening:"));
                if (!visualLine.Text.Contains("roughly three-quarters full") || visualLine.Text.Contains("149 attendees")) throw new InvalidOperationException("Your listening stays a broad occupancy impression.");
                var audienceScroll = Descendants(panel).OfType<ScrollContainer>().Single(s => s.IsAncestorOf(sourceLine));
                if (audienceScroll.Size.Y <= 0 || audienceScroll.Size.Y > 540) throw new InvalidOperationException("Audience detail needs bounded scrolling.");
                var done = Descendants(panel).OfType<Button>().Last(b => b.IsVisibleInTree() && b.Text == "DONE");
                if (!done.IsVisibleInTree()) throw new InvalidOperationException("Audience evidence needs an accessible dismissal.");
                string audienceDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-audience-ui-v1"); Directory.CreateDirectory(audienceDir);
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(audienceDir, "evidence.png"));
                done.EmitSignal(Button.SignalName.Pressed);
                GD.Print("SCOUTING_AUDIENCE_UI_PASS billDedup=true sourcedCounts=true broadListening=true bounded=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-founding-history-ui-check")) {
                var clock = TimeManager.Instance;
                clock.RestoreClock(GameDate.StartDate, 9);
                var plan = desk.ScoutingItinerary().First();
                if (!desk.WaitForSceneAppearance(plan.BillId, plan.EventId, out message) || !desk.ListenSceneAppearance(plan.BillId, plan.EventId, out message)) throw new InvalidOperationException(message);
                var act = ArtistManager.Instance.GetArtist(plan.ArtistId);
                act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
                act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
                clock.RestoreClock(clock.CurrentDate, 10, 15); desk.Label.cashReserves = 100000;
                var prospect = desk.Slate.Single(p => p.Artist.artistId == act.artistId);
                if (!desk.ApproachToSign(prospect, out message)) throw new InvalidOperationException(message);
                prospect.Posture = NegotiationPosture.Pushover; prospect.Talk = null; var terms = prospect.Baseline;
                if (!desk.OfferContract(prospect, terms.Advance, terms.RoyaltyRate, terms.TermYears, terms.SinglesObligation, terms.LabelOwnsPublishing, terms.ArtistCreativeControl, out message)) throw new InvalidOperationException(message);
                desk.SetScoutingPriority(act.artistId, ScoutingPriority.Archived);
                ArtistManager.Instance.DropArtist(act, clock.CurrentDate.year, desk.Label, ArtistDropReason.Voluntary);
                panel.OpenAtTab("A&R");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var history = Descendants(panel).OfType<Label>().Single(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("FIRST NAME ON THE LABEL:"));
                if (history.Text != desk.FoundingActSummary() || !history.Text.Contains("publishing") || history.Text.Contains(plan.EventId)) throw new InvalidOperationException("First signature needs original public terms and acquired context.");
                foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(history)))
                    scroll.ScrollVertical = System.Math.Max(0, scroll.ScrollVertical + (int)(history.GlobalPosition.Y - scroll.GlobalPosition.Y) - 140);
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                string foundingDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-founding-history-ui-v1"); Directory.CreateDirectory(foundingDir);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(foundingDir, "history.png"));
                GD.Print("SCOUTING_FOUNDING_HISTORY_UI_PASS archived=true departed=true originalTerms=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-commitment-planning-ui-check")) {
                var clock = TimeManager.Instance;
                clock.RestoreClock(GameDate.StartDate, 9);
                var planned = desk.ScoutingItinerary().First(o => o.EndMinute < System.Math.Min(clock.HardStopHour * 60, SceneRoomCatalog.Get(o.RoomId).CloseHour * 60));
                if (!desk.WaitForSceneAppearance(planned.BillId, planned.EventId, out message) || !desk.ListenSceneAppearance(planned.BillId, planned.EventId, out message)) throw new InvalidOperationException(message);
                var act = ArtistManager.Instance.GetArtist(planned.ArtistId);
                act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active; act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
                var invite = desk.PreviewScoutingInvitation(act.artistId);
                if (invite == null || !desk.ArrangeScoutingMeeting(act.artistId, invite.Id, out message)) throw new InvalidOperationException(message);
                desk.SetScoutingPriority(act.artistId, ScoutingPriority.Archived);
                var meeting = desk.ScoutingMeetings.Single();
                clock.RestoreClock(LocalSceneRoomService.Date(meeting.Day), meeting.StartMinute / 60, meeting.StartMinute % 60);
                panel.OpenAtTab("A&R");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var agenda = Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion() && l.Text == "AGREED MEETINGS").ToArray();
                if (agenda.Length != 1) throw new InvalidOperationException("Archived appointment must appear once on the city board.");
                var warning = Descendants(panel).OfType<Label>().FirstOrDefault(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("This choice passes an agreed start:"));
                if (warning == null || !warning.Text.Contains(meeting.ArtistName)) throw new InvalidOperationException("Time choice needs the specific appointment warning.");
                var continueButton = Descendants(panel).OfType<Button>().FirstOrDefault(b => !b.IsQueuedForDeletion() && (b.Text.StartsWith("LISTEN TO THIS SET") || b.Text.StartsWith("WAIT FOR THIS SET")) && b.Text.Contains("PASSES APPOINTMENT START"));
                if (continueButton == null || continueButton.Disabled) throw new InvalidOperationException("Player must retain the choice to hear a conflicting set.");
                var attendButton = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("ATTEND MEETING"));
                if (attendButton.Text.Contains("PASSES APPOINTMENT START")) throw new InvalidOperationException("Attending cannot conflict with its own appointment.");
                string riskDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-commitment-planning-ui-v1"); Directory.CreateDirectory(riskDir);
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(riskDir, "choices.png"));
                foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(warning)))
                    scroll.ScrollVertical = System.Math.Max(0, scroll.ScrollVertical + (int)(warning.GlobalPosition.Y - scroll.GlobalPosition.Y) - 160);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(riskDir, "warning.png"));
                int count = desk.ScoutingDossiers.Sum(d => d.Observations.Count);
                Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "CANCEL MEETING").EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (desk.ScoutingMeetings.Single().Status != SceneConversationStatus.Cancelled || desk.ScoutingDossiers.Sum(d => d.Observations.Count) != count + 1 ||
                    Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("This choice passes an agreed start:")))
                    throw new InvalidOperationException("Cancellation must release the warning, preserving only its ordinary cancellation notice.");
                GD.Print("SCOUTING_COMMITMENT_PLANNING_UI_PASS agendaOnce=true archived=true warning=true choice=true cancellation=true"); GetTree().Quit(0); return;
            }
            if (OS.GetCmdlineUserArgs().Contains("--scouting-opening-ui-check")) {
                TimeManager.Instance.RestoreClock(GameDate.StartDate, 9);
                panel.OpenAtTab("A&R");
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var waits = Descendants(panel).OfType<Button>().Where(b => !b.IsQueuedForDeletion() && b.Text == "WAIT FOR THIS SET").ToArray();
                if (waits.Length == 0 || waits.Length > 2) throw new InvalidOperationException("Opening planner needs a short list of real wait choices.");
                var browseRooms = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "BROWSE ALL ROOM CALENDARS");
                browseRooms.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (Descendants(panel).OfType<Button>().Any(b => !b.IsQueuedForDeletion() && b.Text == "WAIT FOR THIS SET") || desk.ScoutingDossiers.Count != 0)
                    throw new InvalidOperationException("Browsing all rooms must hide the short list without acquiring evidence.");
                Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "SHOW NEXT AFFORDABLE SETS").EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                waits = Descendants(panel).OfType<Button>().Where(b => !b.IsQueuedForDeletion() && b.Text == "WAIT FOR THIS SET").ToArray();
                string openingDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-opening-ui-v1"); Directory.CreateDirectory(openingDir);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(openingDir, "planner.png"));
                waits[0].EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (desk.ScoutingDossiers.Count != 0) throw new InvalidOperationException("Waiting must not acquire music evidence.");
                var listenPlan = Descendants(panel).OfType<Button>().First(b => !b.IsQueuedForDeletion() && b.Text == "LISTEN TO THIS SET");
                listenPlan.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (desk.ScoutingDossiers.Count != 1 || desk.Label.roster.Count != 0) throw new InvalidOperationException("Planner must use real listening without signing an act.");
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                GetViewport().GetTexture().GetImage().SavePng(Path.Combine(openingDir, "after-listening.png"));
                GD.Print("SCOUTING_OPENING_UI_PASS planner=true waiting=true evidence=true noSigning=true"); GetTree().Quit(0); return;
            }
            var cards = Descendants(panel).OfType<VenueHandbill>().Where(c => !c.IsQueuedForDeletion()).ToArray();
            if (cards.Length != 6) throw new InvalidOperationException("The board must show six named rooms.");
            foreach (var card in cards) foreach (var label in Descendants(card).OfType<Label>()) {
                if (label.GlobalPosition.Y + label.Size.Y > card.GlobalPosition.Y + card.Size.Y + 1)
                    throw new InvalidOperationException("Room handbill text overflows its paper.");
            }
            cards[1].EmitSignal(Button.SignalName.Pressed);
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            cards = Descendants(panel).OfType<VenueHandbill>().Where(c => !c.IsQueuedForDeletion()).ToArray();
            cards[0].EmitSignal(Button.SignalName.Pressed);
            for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            bool information = OS.GetCmdlineUserArgs().Contains("--scene-information-ui-check");
            bool sources = OS.GetCmdlineUserArgs().Contains("--scene-source-ui-check");
            if (sources) {
                var office = Descendants(panel).OfType<OptionButton>().Single(b => !b.IsQueuedForDeletion() && b.ItemCount == 9);
                if (office.GetItemText(0) != "London (UK)") throw new InvalidOperationException("Source office identity is missing.");
                var introduce = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("ARRANGE INTRODUCTION"));
                float before = desk.Label.cashReserves;
                introduce.EmitSignal(Button.SignalName.Pressed);
                for (int n=0;n<4;n++) await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);
                if (desk.Label.cashReserves != before-150 || SceneSourceService.Connection(desk.Label,"gb_london",0)==null ||
                    !Descendants(panel).OfType<Button>().Any(b=>!b.IsQueuedForDeletion() && b.Text.StartsWith("REVIEW DEMOS")))
                    throw new InvalidOperationException("Source introduction button failed to record cost and access.");
            }
            if (information) {
                var tip = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("ASK ") && b.Text.Contains("LISTENING TIP"));
                var selectedRoom = SceneRoomCatalog.ForPlace(desk.CurrentCityId)[0];
                bool available = TimeManager.Instance.CurrentHour >= selectedRoom.OpenHour && TimeManager.Instance.CurrentHour < selectedRoom.CloseHour;
                if (tip.Disabled == available) throw new InvalidOperationException("Cold booker access follows posted hours, not a familiarity checklist.");
                if (!Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("LOCAL SCENE"))) throw new InvalidOperationException("Local recap is missing.");
            }
            bool ecosystem = OS.GetCmdlineUserArgs().Contains("--scene-ecosystem-ui-check");
            if (ecosystem) {
                float before = desk.Label.cashReserves;
                var fund = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text.StartsWith("FUND 13-WEEK"));
                fund.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (SceneEcosystemService.Programs.Count != 1 || desk.Label.cashReserves != before - SceneEcosystemService.ProgramCost)
                    throw new InvalidOperationException("Funding button must create one program and book its real expense.");
            }
            bool sourceQuestions = OS.GetCmdlineUserArgs().Contains("--scouting-source-ui-check");
            bool competition = OS.GetCmdlineUserArgs().Contains("--scouting-competition-ui-check");
            bool businessCheck = OS.GetCmdlineUserArgs().Contains("--scouting-business-ui-check");
            bool lossCheck = OS.GetCmdlineUserArgs().Contains("--scouting-loss-ui-check");
            bool comparisonCheck = OS.GetCmdlineUserArgs().Contains("--scouting-comparison-ui-check");
            bool termsCheck = OS.GetCmdlineUserArgs().Contains("--scouting-terms-ui-check");
            bool appointmentCheck = OS.GetCmdlineUserArgs().Contains("--scouting-appointment-ui-check");
            bool meetingCheck = appointmentCheck || OS.GetCmdlineUserArgs().Contains("--scouting-meeting-ui-check");
            bool followup = meetingCheck || comparisonCheck || termsCheck || lossCheck || businessCheck || competition || sourceQuestions || OS.GetCmdlineUserArgs().Contains("--scouting-followup-ui-check");
            bool evidenceDetails = OS.GetCmdlineUserArgs().Contains("--scouting-evidence-ui-check");
            bool hiredCheck = OS.GetCmdlineUserArgs().Contains("--scouting-hired-listener-ui-check");
            bool listenerCheck = hiredCheck || OS.GetCmdlineUserArgs().Contains("--scouting-listener-ui-check");
            bool scouting = listenerCheck || followup || evidenceDetails || OS.GetCmdlineUserArgs().Contains("--scouting-experience-ui-check");
            if (scouting) {
                var bill = LocalSceneRoomService.Calendar(desk.CurrentCityId, TimeManager.Instance.CurrentDate)
                    .First(b => (listenerCheck || lossCheck ? b.Appearances.Count >= 2 : b.Appearances.Count > 0) &&
                        b.Appearances[lossCheck ? 1 : 0].EndHour <= TimeManager.Instance.HardStopHour && b.Status == SceneBillStatus.Scheduled);
                if (listenerCheck && !hiredCheck) ArtistManager.Instance.GetArtist(bill.Appearances[1].ArtistId).labelId = desk.Label.labelId;
                var targetRoom = SceneRoomCatalog.Get(bill.RoomId);
                if (meetingCheck) {
                    var act = ArtistManager.Instance.GetArtist(bill.Appearances[0].ArtistId);
                    act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
                    act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.None;
                    desk.Label.cashReserves = 100000;
                }
                TimeManager.Instance.RestoreClock(LocalSceneRoomService.Date(bill.Day), bill.Appearances[0].StartHour);
                cards = Descendants(panel).OfType<VenueHandbill>().Where(c => !c.IsQueuedForDeletion()).ToArray();
                cards.First(c => Descendants(c).OfType<Label>().Any(l => l.Text == targetRoom.Name.ToUpperInvariant())).EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var listen = Descendants(panel).OfType<Button>().First(b => !b.IsQueuedForDeletion() && (listenerCheck ? (hiredCheck ? b.Text.StartsWith("HIRE LISTENER (") : b.Text.StartsWith("LISTEN TOGETHER (")) : b.Text.StartsWith("LISTEN (")));
                if (listenerCheck) {
                    Input.WarpMouse(new Vector2(10, 880));
                    foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(listen)))
                        scroll.ScrollVertical = Math.Max(0, scroll.ScrollVertical + (int)(listen.GlobalPosition.Y - scroll.GlobalPosition.Y) - 180);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    string controlsDir = ProjectSettings.GlobalizePath(hiredCheck ? "res://SimLogs/scouting-hired-listener-ui-v1" : "res://SimLogs/scouting-listener-ui-v1"); Directory.CreateDirectory(controlsDir);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(controlsDir, "controls.png"));
                }
                float hiredCash = desk.Label.cashReserves;
                listen.EmitSignal(Button.SignalName.Pressed);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (listenerCheck) {
                    var dossier = desk.ScoutingDossiers.Single();
                    if (hiredCheck && (!dossier.Observations.Any(o => o.Kind == ScoutingEvidenceKind.ListenerOpinion &&
                        o.Channel == "hired local musician" && o.ConsultationFee > 0) || desk.Label.cashReserves >= hiredCash ||
                        !desk.ScoutingProvenance(dossier.ArtistId).Any(l => l.Contains("consultation paid"))))
                        throw new InvalidOperationException("The hired-listener control must charge the displayed fee and retain paid provenance.");
                    if (dossier.Observations.Count != 2 || !dossier.Observations.Any(o => o.Kind == ScoutingEvidenceKind.ListenerOpinion) ||
                        !desk.ScoutingSummary(dossier.ArtistId).Any(l => l.Contains("Two judgments of one performance")))
                        throw new InvalidOperationException("The actual co-listen control must save a separate source-correlated opinion.");
                }
                var text = Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion()).Select(l => l.Text).ToArray();
                if (desk.ScoutingDossiers.Count == 0 || !text.Any(t => t.StartsWith("Ensemble / execution:")) ||
                    !Descendants(panel).OfType<Button>().Any(b => !b.IsQueuedForDeletion() && b.Text == "SOURCES & COVERAGE") ||
                    text.Any(t => t.Contains("Package read:") || t.Contains("Advance floor:") || t.Contains("round(s) of patience")))
                    throw new InvalidOperationException("Listening must create an honest notebook with provenance and no hidden contract thresholds.");
                var question = Descendants(panel).OfType<OptionButton>().First(b => !b.IsQueuedForDeletion() && b.TooltipText.StartsWith("Pin a question"));
                question.Select((int)ScoutingQuestion.Ensemble);
                question.EmitSignal(OptionButton.SignalName.ItemSelected, (long)ScoutingQuestion.Ensemble);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (desk.ScoutingDossiers.First().Question != ScoutingQuestion.Ensemble ||
                    !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("another date is needed")))
                    throw new InvalidOperationException("Listening question selection must persist and explain missing independent evidence.");
                if (meetingCheck) {
                    Button MeetingButton(string label) => Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == label);
                    var clock = TimeManager.Instance; int beforeMinute = clock.CurrentHour * 60 + clock.CurrentMinute; float cash = desk.Label.cashReserves;
                    MeetingButton(appointmentCheck ? "ASK FOR DEMO (15min · no invitation fee)" : "ASK FOR RETURN MEETING (15min · no fee)").EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var meeting = desk.ScoutingMeetings.Single();
                    if (meeting.Status != SceneConversationStatus.Scheduled || clock.CurrentHour * 60 + clock.CurrentMinute != beforeMinute + 15 ||
                        desk.Label.cashReserves != cash || desk.ScoutingDossiers.Single().Observations.Any(o => o.Kind == ScoutingEvidenceKind.BusinessStatement))
                        throw new InvalidOperationException("Invitation must acquire consent for fifteen minutes without inventing terms.");
                    var acceptedPlayer = desk.CaptureState(); var acceptedRooms = WorldStateService.Capture().SceneRooms;
                    desk.SetScoutingPriority(meeting.ArtistId, ScoutingPriority.Archived);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var wait = MeetingButton("WAIT FOR MEETING");
                    foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(wait)))
                        scroll.ScrollVertical = Math.Max(0, scroll.ScrollVertical + (int)(wait.GlobalPosition.Y - scroll.GlobalPosition.Y) - 250);
                    foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                    Input.WarpMouse(new Vector2(10, 880));
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    string controlsDir = ProjectSettings.GlobalizePath(appointmentCheck ? "res://SimLogs/scouting-appointment-ui-v1" : "res://SimLogs/scouting-meeting-ui-v1"); Directory.CreateDirectory(controlsDir);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(controlsDir, "controls.png"));
                    MeetingButton("CANCEL MEETING").EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (desk.ScoutingMeetings.Single().Status != SceneConversationStatus.Cancelled || desk.Label.cashReserves != cash ||
                        clock.CurrentHour * 60 + clock.CurrentMinute != beforeMinute + 15)
                        throw new InvalidOperationException("Cancellation must release the actual appointment without time or fee.");
                    // Restore the accepted branch to exercise waiting and attendance independently of cancellation.
                    LocalSceneRoomService.BeginRestore(acceptedRooms); LocalSceneRoomService.CompleteRestore();
                    if (!desk.RestoreState(acceptedPlayer, out message)) throw new InvalidOperationException(message);
                    // Controlled clock isolates successful arrival from the normal daily rival signing market.
                    clock.RestoreClock(LocalSceneRoomService.Date(meeting.Day), meeting.StartMinute / 60 - 1, 45);
                    desk.SetScoutingPriority(meeting.ArtistId, ScoutingPriority.Pursue);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    MeetingButton("WAIT FOR MEETING").EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (clock.CurrentHour * 60 + clock.CurrentMinute != meeting.StartMinute ||
                        desk.ScoutingDossiers.Single().Observations.Any(o => o.Kind == ScoutingEvidenceKind.BusinessStatement))
                        throw new InvalidOperationException("Waiting must reach the agreed start without acquiring terms.");
                    MeetingButton(appointmentCheck ? $"ATTEND DEMO (3h · ${meeting.Fee:0})" : "ATTEND MEETING (1h · no fee)").EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (clock.CurrentHour * 60 + clock.CurrentMinute != meeting.EndMinute || desk.Label.cashReserves != cash - meeting.Fee ||
                        desk.ScoutingMeetings.Single().Status != SceneConversationStatus.Completed ||
                        desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) != 1 ||
                        (appointmentCheck ? desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.DemoSample) != 1 || desk.Session != null :
                        desk.ScoutingDossiers.Single().Observations.Count(o => o.Kind == ScoutingEvidenceKind.BusinessStatement) != 1 ||
                        // An easy act gets the one-click form; one with something at stake opens the negotiation (contract outlook).
                        !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && (l.Text.StartsWith("CONTRACT —") || l.Text.StartsWith("NEGOTIATING —")))))
                        throw new InvalidOperationException("Attendance must consume one hour and display the existing contract form with actual terms.");
                    GD.Print(appointmentCheck ? "SCOUTING_APPOINTMENT_UI_PASS invitation=true archivedAgenda=true cancellation=true waiting=true attendance=true paid=true boundedEvidence=true" : "SCOUTING_MEETING_UI_PASS invitation=true consent=true archivedAgenda=true cancellation=true waiting=true attendance=true contractForm=true noExtraListening=true");
                }
                if (comparisonCheck) {
                    var saved = desk.CaptureState();
                    var heard = saved.ScoutingKnowledge.Dossiers.Single().Observations.Single();
                    var controlled = ScoutingComparisonChecks.ContextFixture(heard);
                    saved.ScoutingKnowledge.Dossiers = new() { controlled };
                    if (!desk.RestoreState(saved, out message)) throw new InvalidOperationException(message);
                    TimeManager.Instance.RestoreClock(LocalSceneRoomService.Date(controlled.Observations.Last().OccurredDay.Value), heard.EndMinute / 60, heard.EndMinute % 60);
                    // A priority action refreshes the actual notebook after restoring matched evidence.
                    desk.SetScoutingPriority(heard.ArtistId, ScoutingPriority.Pursue);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var comparisonText = Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion()).Select(l => l.Text).ToArray();
                    if (!comparisonText.Any(t => t.StartsWith("Consistency:") && t.Contains("broadly similar impressions")) ||
                        !comparisonText.Any(t => t.StartsWith("Listening question:")) || comparisonText.Count(t => t.Contains("Two dated sets")) != 1)
                        throw new InvalidOperationException("Actual notebook must compare separate dates, not same-night range.");
                    controlled.Observations.Last().BackingPersonIds.Add("ui-fixture-different-backing");
                    saved = desk.CaptureState(); saved.ScoutingKnowledge.Dossiers = new() { controlled };
                    if (!desk.RestoreState(saved, out message)) throw new InvalidOperationException(message);
                    desk.SetScoutingPriority(heard.ArtistId, ScoutingPriority.Pursue);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (!Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("Consistency:") &&
                        l.Text.Contains("performing or backing people differed") && l.Text.Contains("cannot establish consistency")))
                        throw new InvalidOperationException("Actual notebook must state changed performance context and its limits.");
                    GD.Print("SCOUTING_COMPARISON_UI_PASS datedPair=true sameNightExcluded=true contextLimits=true");
                }
                if (termsCheck) {
                    var act = ArtistManager.Instance.GetArtist(bill.Appearances[0].ArtistId);
                    act.labelId = null; act.careerState = CareerState.Unsigned; act.lifecycleStatus = ArtistLifecycleStatus.Active;
                    act.prospectMarketStatus = ProspectMarketStatus.Seeking; act.manager = ManagerArchetype.Visionary;
                    desk.SetScoutingPriority(act.artistId, ScoutingPriority.Pursue);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var discuss = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "DISCUSS TERMS");
                    if (discuss.Disabled) throw new InvalidOperationException("Controlled unsigned seeking act must permit a discussion.");
                    discuss.EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var opening = desk.ScoutingDossiers.Single().Observations.Single(o => o.Kind == ScoutingEvidenceKind.BusinessStatement);
                    if (opening.StatedTerms == null || opening.BusinessStatementKind != ScoutingBusinessStatementKind.OpeningAsk)
                        throw new InvalidOperationException("Real discussion button must record public starting terms.");
                    var leave = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "NOT NOW");
                    leave.EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var statements = desk.ScoutingDossiers.Single().Observations.Where(o => o.Kind == ScoutingEvidenceKind.BusinessStatement).ToArray();
                    if (statements.Length != 2 || statements.Last().BusinessStatementKind != ScoutingBusinessStatementKind.SteppedBack ||
                        !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("Business discussion") && l.Text.Contains("Opening ask:")))
                        throw new InvalidOperationException("The notebook must retain the actual ask and voluntary withdrawal.");
                    GD.Print("SCOUTING_TERMS_UI_PASS realApproach=true publicTerms=true withdrawal=true notebook=true");
                }
                if (lossCheck) {
                    var lost = ArtistManager.Instance.GetArtist(bill.Appearances[0].ArtistId);
                    var alternative = ArtistManager.Instance.GetArtist(bill.Appearances[1].ArtistId);
                    alternative.labelId = null; alternative.lifecycleStatus = ArtistLifecycleStatus.Active;
                    string eventId = ScenePerformanceService.EventId(bill, bill.Appearances[1]);
                    if (!desk.WaitForSceneAppearance(bill.Id, eventId, out message) || !desk.ListenSceneAppearance(bill.Id, eventId, out message))
                        throw new InvalidOperationException("Second real listening failed: " + message);
                    // Controlled ownership change exercises loss; it acquires no fabricated signing report.
                    lost.labelId = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned).labelId;
                    desk.SetScoutingPriority(lost.artistId, ScoutingPriority.Pursue);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var review = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "REVIEW " + alternative.stageName.ToUpperInvariant());
                    if (!Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains(alternative.stageName + ": last heard at")))
                        throw new InvalidOperationException("Lost lead needs an actionable alternative with listening provenance.");
                    Input.WarpMouse(new Vector2(10, 880));
                    foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(review)))
                        scroll.ScrollVertical = Math.Max(0, scroll.ScrollVertical + (int)(review.GlobalPosition.Y - scroll.GlobalPosition.Y) - 250);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    string controlsDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-loss-ui-v1"); Directory.CreateDirectory(controlsDir);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(controlsDir, "controls.png"));
                    float beforeCash = desk.Label.cashReserves; int beforeHour = TimeManager.Instance.CurrentHour;
                    int observations = desk.ScoutingDossiers.Sum(d => d.Observations.Count);
                    review.EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (!desk.Slate.Any(p => p.Artist.artistId == alternative.artistId) || desk.Label.cashReserves != beforeCash ||
                        TimeManager.Instance.CurrentHour != beforeHour || desk.ScoutingDossiers.Sum(d => d.Observations.Count) != observations)
                        throw new InvalidOperationException("Review alternative must open the canonical pad without spending time or acquiring evidence.");
                    GD.Print("SCOUTING_LOSS_UI_PASS realListening=true review=true noCost=true provenance=true");
                }
                if (businessCheck) {
                    var act = ArtistManager.Instance.GetArtist(desk.ScoutingDossiers.First().ArtistId);
                    var rival = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned);
                    string owner = act.labelId; act.labelId = rival.labelId;
                    RosterManager.RecordScoutingMarketDecisions(new[] { new RosterManager.DailyTalentMarketAppointment {
                        Date = TimeManager.Instance.CurrentDate, Label = rival, SelectedArtist = act,
                        Outcome = "AcceptedUncontested", WinnerLabelId = rival.labelId } });
                    act.labelId = owner;
                    desk.SetScoutingPriority(act.artistId, ScoutingPriority.Pursue);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var ask = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "ASK ACT ABOUT PAST LABEL DECISIONS (1h · no fee)");
                    if (ask.Disabled) throw new InvalidOperationException("A free member should be available after the set.");
                    Input.WarpMouse(new Vector2(10, 880));
                    foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(ask)))
                        scroll.ScrollVertical = Math.Max(0, scroll.ScrollVertical + (int)(ask.GlobalPosition.Y - scroll.GlobalPosition.Y) - 180);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    string controlsDir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-business-ui-v1"); Directory.CreateDirectory(controlsDir);
                    await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    GetViewport().GetTexture().GetImage().SavePng(Path.Combine(controlsDir, "controls.png"));
                    int beforeHour = TimeManager.Instance.CurrentHour; float beforeCash = desk.Label.cashReserves;
                    ask.EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var claim = desk.ScoutingDossiers.First().Observations.Last();
                    if (claim.Kind != ScoutingEvidenceKind.ActBusinessClaim || claim.Channel != "post-set act conversation" ||
                        !claim.Claim.Contains(rival.labelName) || TimeManager.Instance.CurrentHour != beforeHour + 1 ||
                        desk.Label.cashReserves != beforeCash || desk.ScoutingDossiers.First().Observations.Count(o => o.Kind == ScoutingEvidenceKind.Performance) != 1)
                        throw new InvalidOperationException("The real act conversation must retain its source and history for one hour, without a second performance sample.");
                    ask = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "ASK ACT ABOUT PAST LABEL DECISIONS (1h · no fee)");
                    if (!ask.Disabled || !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("already recorded this account")))
                        throw new InvalidOperationException("Repeated act account must explain its disabled action.");
                }
                if (sourceQuestions) {
                    var sourceQuestion = Descendants(panel).OfType<OptionButton>().First(b => !b.IsQueuedForDeletion() && b.TooltipText.StartsWith("Choose a source question"));
                    sourceQuestion.Select((int)ScoutingSourceQuestion.NextDate);
                    sourceQuestion.EmitSignal(OptionButton.SignalName.ItemSelected, (long)ScoutingSourceQuestion.NextDate);
                    var askBooker = Descendants(panel).OfType<Button>().First(b => !b.IsQueuedForDeletion() && b.Text == "ASK BOOKER (1h · no fee)");
                    if (askBooker.Disabled) throw new InvalidOperationException("A new source question should be available during room hours.");
                    int beforeHour = TimeManager.Instance.CurrentHour; float beforeCash = desk.Label.cashReserves;
                    askBooker.EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var claim = desk.ScoutingDossiers.First().Observations.Last();
                    if (TimeManager.Instance.CurrentHour != beforeHour + 1 || desk.Label.cashReserves != beforeCash ||
                        claim.Kind != ScoutingEvidenceKind.BookerClaim || claim.SourceId != targetRoom.ContactId ||
                        !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("local source claim")))
                        throw new InvalidOperationException("The actual booker button must spend one hour and show a sourced claim.");
                    sourceQuestion = Descendants(panel).OfType<OptionButton>().First(b => !b.IsQueuedForDeletion() && b.TooltipText.StartsWith("Choose a source question"));
                    sourceQuestion.Select((int)ScoutingSourceQuestion.NextDate);
                    sourceQuestion.EmitSignal(OptionButton.SignalName.ItemSelected, (long)ScoutingSourceQuestion.NextDate);
                    askBooker = Descendants(panel).OfType<Button>().First(b => !b.IsQueuedForDeletion() && b.Text == "ASK BOOKER (1h · no fee)");
                    if (!askBooker.Disabled || !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("already recorded this answer")))
                        throw new InvalidOperationException("A repeated source answer must explain why the button is disabled.");
                }
                if (competition) {
                    var act = ArtistManager.Instance.GetArtist(desk.ScoutingDossiers.First().ArtistId);
                    var rival = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned);
                    act.labelId = rival.labelId;
                    act.sceneRecruitmentHistory ??= new();
                    act.sceneRecruitmentHistory.Add(new SceneRecruitmentRecord { ArtistId = act.artistId, LabelId = rival.labelId,
                        Week = 4, Phase = "DailyMarket", BasePlaceId = desk.CurrentCityId });
                    TimeManager.Instance.RestoreClock(LocalSceneRoomService.Date(35), 10, 15);
                    desk.SetScoutingPriority(act.artistId, ScoutingPriority.Pursue);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var read = Descendants(panel).OfType<Button>().Single(b => !b.IsQueuedForDeletion() && b.Text == "SAVE LOCAL REPORTS & MARK READ");
                    float before = desk.Label.cashReserves;
                    read.EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    if (desk.ScoutingDossiers.First().Observations.Count(o => o.Kind == ScoutingEvidenceKind.PublicReport) != 1 ||
                        desk.Label.cashReserves != before || TimeManager.Instance.CurrentHour != 10 ||
                        !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.Contains("public signing report") && l.Text.Contains(rival.labelName)) ||
                        !Descendants(panel).OfType<Label>().Any(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("Other leads")))
                        throw new InvalidOperationException("Recap button must retain an honestly dated public signing report without resources.");
                }
                foreach (var scroll in Descendants(panel).OfType<ScrollContainer>()) scroll.ScrollVertical = 100000;
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                if (evidenceDetails) {
                    var details = Descendants(panel).OfType<Button>().First(b => !b.IsQueuedForDeletion() && b.Text == "SOURCES & COVERAGE");
                    details.EmitSignal(Button.SignalName.Pressed);
                    for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    var modal = Descendants(panel).OfType<PaperModal>().Single(m => !m.IsQueuedForDeletion());
                    var labels = Descendants(modal).OfType<Label>().ToArray();
                    if (!Descendants(modal).OfType<ScrollContainer>().Any() ||
                        !labels.Any(l => l.Text.Contains("Heard material:") && l.Text.Contains("coverage") && l.Text.Contains("acquired")) ||
                        labels.Any(l => l.Text.Contains("performance-v1|")))
                        throw new InvalidOperationException("Evidence details need bounded scrolling, acquired coverage/material and no raw event keys.");
                    var done = Descendants(modal).OfType<Button>().Single(b => b.Text == "DONE");
                    if (done.GlobalPosition.Y < 0 || done.GlobalPosition.Y + done.Size.Y > GetViewport().GetVisibleRect().Size.Y)
                        throw new InvalidOperationException("Evidence notebook dismissal must remain within the viewport.");
                }
            }
            string dir = ProjectSettings.GlobalizePath(businessCheck ? "res://SimLogs/scouting-business-ui-v1" : hiredCheck ? "res://SimLogs/scouting-hired-listener-ui-v1" : listenerCheck ? "res://SimLogs/scouting-listener-ui-v1" : competition ? "res://SimLogs/scouting-competition-ui-v1" : sourceQuestions ? "res://SimLogs/scouting-source-ui-v1" : followup ? "res://SimLogs/scouting-followup-ui-v1" : evidenceDetails ? "res://SimLogs/scouting-migration-ui-v1" : scouting ? "res://SimLogs/scouting-ui-v1" : sources ? "res://SimLogs/scene7-source-ui-v1" : information ? "res://SimLogs/scene6-information-ui-v1" : ecosystem ? "res://SimLogs/scene5-ecosystem-ui-v1" : "res://SimLogs/scene3-ui-v1"); Directory.CreateDirectory(dir);
            if (lossCheck) { dir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-loss-ui-v1"); Directory.CreateDirectory(dir); }
            if (termsCheck) { dir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-terms-ui-v1"); Directory.CreateDirectory(dir); }
            if (comparisonCheck) { dir = ProjectSettings.GlobalizePath("res://SimLogs/scouting-comparison-ui-v1"); Directory.CreateDirectory(dir); }
            if (meetingCheck) { dir = ProjectSettings.GlobalizePath(appointmentCheck ? "res://SimLogs/scouting-appointment-ui-v1" : "res://SimLogs/scouting-meeting-ui-v1"); Directory.CreateDirectory(dir); }
            File.WriteAllText(Path.Combine(dir, "ui.txt"), string.Join("\n", Descendants(panel).OfType<Label>().Where(l => !l.IsQueuedForDeletion()).Select(l => l.Text)));
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            if (termsCheck) {
                var note = Descendants(panel).OfType<Label>().First(l => !l.IsQueuedForDeletion() && l.Text.Contains("Business discussion") && l.Text.Contains("Opening ask:"));
                foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(note)))
                    scroll.ScrollVertical = Math.Max(0, scroll.ScrollVertical + (int)(note.GlobalPosition.Y - scroll.GlobalPosition.Y) - 160);
            }
            if (comparisonCheck) {
                var note = Descendants(panel).OfType<Label>().First(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("Consistency:"));
                foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(note)))
                    scroll.ScrollVertical = Math.Max(0, scroll.ScrollVertical + (int)(note.GlobalPosition.Y - scroll.GlobalPosition.Y) - 180);
            }
            if (meetingCheck) foreach (var scroll in Descendants(panel).OfType<ScrollContainer>()) scroll.ScrollVertical = 0;
            if (meetingCheck || comparisonCheck || termsCheck || listenerCheck || businessCheck) {
                foreach (var toast in Descendants(panel).OfType<Control>().Where(c => c.Name == "ActionFeedbackToast")) toast.Hide();
                Input.WarpMouse(new Vector2(10, 880));
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
            if (appointmentCheck) {
                for (int n = 0; n < 2; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                var note = Descendants(panel).OfType<Label>().First(l => !l.IsQueuedForDeletion() && l.Text.StartsWith("DemoSample:"));
                foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(note))) {
                    scroll.ScrollVertical = (int)scroll.GetVScrollBar().MaxValue;
                }
                for (int n = 0; n < 2; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                foreach (var scroll in Descendants(panel).OfType<ScrollContainer>().Where(s => s.IsAncestorOf(note)))
                    scroll.ScrollVertical = Math.Max(0, scroll.ScrollVertical + (int)(note.GlobalPosition.Y - scroll.GlobalPosition.Y) - 100);
                for (int n = 0; n < 4; n++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            }
            var image = GetViewport().GetTexture().GetImage();
            if (image.SavePng(Path.Combine(dir, "board.png")) != Error.Ok) throw new InvalidOperationException("Screenshot failed.");
            GD.Print(businessCheck ? "SCOUTING_BUSINESS_UI_PASS history=true source=true hour=true noFee=true repeatDisabled=true" : hiredCheck ? "SCOUTING_HIRED_LISTENER_UI_PASS paidListening=true opinion=true sharedEvent=true notebook=true" : listenerCheck ? "SCOUTING_LISTENER_UI_PASS coListening=true opinion=true sharedEvent=true notebook=true" : competition ? "SCOUTING_COMPETITION_UI_PASS reportSaved=true dates=true source=true noCost=true" : sourceQuestions ? "SCOUTING_SOURCE_UI_PASS listening=true question=true conversation=true repeatDisabled=true" : evidenceDetails ? "SCOUTING_EVIDENCE_UI_PASS sources=true scrolling=true material=true dismissalVisible=true" : scouting ? "SCOUTING_EXPERIENCE_UI_PASS rooms=6 listening=true notebook=true thresholdsHidden=true" : sources ? "SCENE_SOURCE_UI_PASS rooms=6 sourceIntro=true" : information ? "SCENE_INFORMATION_UI_PASS rooms=6 recap=true coldTip=true" : ecosystem ? "SCENE_ECOSYSTEM_UI_PASS rooms=6 funding=true layout=true" : "SCENE_ROOM_UI_PASS rooms=6 selection=true layout=true"); GetTree().Quit(0);
        } catch (Exception ex) { GD.PrintErr("SCENE_ROOM_UI_FAILED: " + ex); GetTree().Quit(1); }
    }
}
