import unittest

from analyze_writing_opportunity import choice_funnel, demo_inventory, plan_funnel


def event(week, slot, state, proposed=1, prepared='', actual='', record='', reason=''):
    return dict(week=str(week), slotId=slot, format='Single', proposedWeek=str(proposed),
                artistId='act', evidence='career=Rising;gap=4', state=state,
                preparedWeek=str(prepared), actualWeek=str(actual), recordId=record, reason=reason)


class OpportunityChecks(unittest.TestCase):
    def test_inventory_counts_available_songs_not_licenses(self):
        rows = [dict(week='0', status='Available', availableWeek='0', priorityUntilWeek='2', compositionHook='.8'),
                dict(week='0', status='Licensed', availableWeek='0', priorityUntilWeek='0', compositionHook='.5'),
                dict(week='1', status='Available', availableWeek='2', priorityUntilWeek='0', compositionHook='.9')]
        result = demo_inventory(rows)
        self.assertEqual((result['observedWeeks'], result['meanAvailable'], result['meanPriority']), (2, .5, .5))
        self.assertAlmostEqual(result['meanAvailableHook'], .8)

    def test_choice_census_retains_eligible_acts_without_selection(self):
        rows = [dict(format='Single', eligible='1', selected='0', artistId='a', careerState='Rising'),
                dict(format='Single', eligible='1', selected='1', artistId='b', careerState='Rising'),
                dict(format='Single', eligible='1', selected='0', artistId='b', careerState='Rising')]
        result = next(x for x in choice_funnel(rows) if x['career'] == 'all')
        self.assertEqual((result['eligibleActs'], result['selectedActs']), (2, 1))
        self.assertEqual((result['eligibleCandidateSlotObservations'], result['selectedObservations']), (3, 1))

    def test_complete_window_and_distinct_plan_events(self):
        rows = [event(1, 'a', 'Proposed'), event(5, 'a', 'Ready', prepared=5),
                event(6, 'a', 'Issued', prepared=5, actual=6, record='r'),
                event(1, 'b', 'Proposed'), event(28, 'b', 'Issued', actual=28, record='late'),
                event(80, 'c', 'Proposed', proposed=80)]
        result = plan_funnel(rows, [dict(recordId='r', source='ExternalProfessional')], [], 104)
        all_plans = next(x for x in result if x['career'] == 'all')
        self.assertEqual((all_plans['plans'], all_plans['prepared'], all_plans['issued']), (2, 1, 1))
        self.assertEqual(all_plans['issueSources'], {'ExternalProfessional': 1})
        self.assertEqual(all_plans['missingLaunchProvenance'], 0)

    def test_search_and_reasons_are_counted_once_per_plan(self):
        rows = [event(1, 'a', 'Proposed'), event(2, 'a', 'Postponed', reason='cash'),
                event(3, 'a', 'Postponed', reason='cash'), event(4, 'a', 'Cancelled', reason='cash')]
        searches = [dict(slotId='a', week='2'), dict(slotId='a', week='3')]
        result = next(x for x in plan_funnel(rows, [], searches, 104) if x['career'] == 'all')
        self.assertEqual((result['searched'], result['cancelled']), (1, 1))
        self.assertEqual(result['delayedOrCancelledReasons'], {'cash': 1})


if __name__ == '__main__':
    unittest.main()
