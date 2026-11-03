import unittest

import pandas as pd

from analyze_programme_followup_conversion import build_pairs


class PairChecks(unittest.TestCase):
    def test_closed_window_and_prior_only_evidence(self):
        chart = pd.DataFrame([
            (0,'opening','old',1,'Star'),
            (10,'hit','a',30,'Rising'),
            (16,'hit','a',20,'Rising'),
            (20,'hit','a',5,'Rising'),
            (21,'next','a',15,'Established'),
            (200,'end','z',90,'Star'),
        ], columns=['week','recordId','artistId','currentPosition','launchCareerState'])
        launches = pd.DataFrame([(9,'before','a'),(17,'next','a'),(62,'last','a'),(63,'late','a')],
                                columns=['week','singleId','artistId'])
        pairs, actors = build_pairs(chart,launches)
        self.assertEqual(list(pairs.singleId), ['next','last'])
        self.assertEqual(list(pairs.recentTop10), [False,True])
        self.assertEqual(list(pairs.predecessorOnChart), [True,False])
        self.assertEqual(actors.iloc[0].firstGap,7)

    def test_no_release_acts_are_retained(self):
        chart = pd.DataFrame([(10,'h','a',30,'NewSigning'),(200,'z','z',90,'Star')],
                              columns=['week','recordId','artistId','currentPosition','launchCareerState'])
        launches = pd.DataFrame([(11,'r','a')],columns=['week','singleId','artistId'])
        chart = pd.concat([chart,pd.DataFrame([(12,'b','b',35,'Rising')],columns=chart.columns)])
        pairs, actors = build_pairs(chart,launches)
        self.assertEqual(len(pairs),1)
        self.assertEqual(actors.set_index('artistId').loc['b','releases52'],0)


if __name__ == '__main__':
    unittest.main()
