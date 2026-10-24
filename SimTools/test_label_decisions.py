import unittest

import pandas as pd

from analyze_label_decisions import choice_decisions, reconcile_proposals


class LabelDecisionAuditTests(unittest.TestCase):
    def fixture(self):
        return pd.DataFrame([
            dict(artistId='a', eligible=1, selected=1, rank=1, score=11., expectedNet=-10., contribution=-.1,
                 hot=1, due=1, careerState='Established', attachedAudienceAtAudit=1000),
            dict(artistId='b', eligible=1, selected=0, rank=2, score=2., expectedNet=200., contribution=1.,
                 hot=0, due=0, careerState='NewSigning', attachedAudienceAtAudit=0),
            dict(artistId='c', eligible=0, selected=0, rank=None, score=None, expectedNet=None, contribution=None,
                 hot=None, due=None, careerState='Rising', attachedAudienceAtAudit=None),
        ]).assign(week=1, slotId='slot', labelId='label', format='Single', intendedWeek=3, financed=0)

    def test_retains_alternatives_exclusions_and_negative_forecasts(self):
        q = choice_decisions(self.fixture()).iloc[0]
        self.assertEqual(q.eligibleCandidates, 2)
        self.assertEqual(q.artistId, 'a')
        self.assertEqual(q.selectedNonpositive, 1)
        self.assertEqual(q.bestAvailableNet, 200)
        self.assertEqual(q.nonpositiveWithPositiveAlternative, 1)
        self.assertEqual(q.allNonpositive, 0)
        self.assertEqual(q.higherNetPassedOver, 1)
        self.assertEqual(q.newSigningAvailable, 1)
        self.assertEqual(q.selectedNewSigning, 0)

    def test_empty_opportunity_is_retained(self):
        rows = self.fixture().iloc[2:]
        q = choice_decisions(rows).iloc[0]
        self.assertEqual(q.planned, 0)
        self.assertEqual(q.eligibleCandidates, 0)
        self.assertIsNone(q.artistId)

    def test_wrong_selection_ranking_duplicates_and_context_are_rejected(self):
        rows = self.fixture()
        invalid = rows.copy(); invalid.loc[0, 'selected'] = 0
        with self.assertRaisesRegex(ValueError, 'one selected'):
            choice_decisions(invalid)
        invalid = rows.copy(); invalid.loc[1, 'score'] = 100
        with self.assertRaisesRegex(ValueError, 'score ordering'):
            choice_decisions(invalid)
        invalid = rows.copy(); invalid.loc[1, 'rank'] = 3
        with self.assertRaisesRegex(ValueError, 'ranking'):
            choice_decisions(invalid)
        with self.assertRaisesRegex(ValueError, 'duplicate'):
            choice_decisions(pd.concat([rows, rows.iloc[:1]]))
        invalid = rows.copy(); invalid.loc[1, 'intendedWeek'] = 10
        with self.assertRaisesRegex(ValueError, 'slot context'):
            choice_decisions(invalid)

    def test_initial_selection_reconciles_even_if_later_reassigned(self):
        decisions = choice_decisions(self.fixture())
        slate = pd.DataFrame([
            dict(week=1, slotId='slot', artistId='a', state='Proposed', reason='programme-review'),
            dict(week=3, slotId='slot', artistId='b', state='Issued', reason='issued'),
        ])
        q = reconcile_proposals(decisions, slate, 10).iloc[0]
        self.assertTrue(q.reassigned)
        self.assertEqual(q.artistId, 'a')
        self.assertEqual(q.artistId_final, 'b')
        invalid = slate.copy(); invalid.loc[0, 'artistId'] = 'b'
        with self.assertRaisesRegex(ValueError, 'Initial intention'):
            reconcile_proposals(decisions, invalid, 10)


if __name__ == '__main__':
    unittest.main()
