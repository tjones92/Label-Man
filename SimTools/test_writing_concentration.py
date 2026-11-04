import csv
import tempfile
import unittest
from pathlib import Path

import analyze_writing_concentration as audit


class WritingConcentrationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs')
        self.old = audit.LOGS
        audit.LOGS = Path(self.temp.name)

    def tearDown(self):
        audit.LOGS = self.old
        self.temp.cleanup()

    def write(self, name, header, data):
        with (audit.LOGS / f'test-{name}.csv').open('w', newline='', encoding='utf-8') as f:
            w = csv.writer(f)
            w.writerow(header)
            w.writerows(data)

    def test_title_duration_identity_and_complete_launch_windows(self):
        self.write('records', ['week','recordId','artistId','currentPosition'],
                   [[1,'a','a',1],[2,'a','a',1],[3,'a','a',1],[4,'b','b',40],[20,'c','c',90]])
        self.write('single-launch-quality', ['week','singleId','artistId','tier','careerState','hook','production','runtimeQuality'],
                   [[1,'a','a','Major','Star',.8,.7,.75],[15,'c','c','Small','NewSigning',.5,.5,.5]])
        result = audit.summarize('test')
        self.assertAlmostEqual(result['slotShare'], .6)
        self.assertAlmostEqual(result['leaderTitleShare'] * result['relativeTitleDuration'], .6)
        groups = [r for r in result['conversions'] if r['definition'] == 'leaders']
        self.assertEqual(sum(r['releases'] for r in groups), 1)

    def test_prepared_is_not_released_and_recent_orders_are_censored(self):
        self.write('writing-orders', ['week','id','songId','artistId','labelId','status','deliveryWeek'],
                   [[1,'o1','s1','a','l','Delivered',1],[2,'o1','s1','a','l','Delivered',1],
                    [40,'o2','s2','b','l','Delivered',40]])
        fields = ['week','songId','artistId','labelId','status','released','holdUntilWeek','orderId',
                  'shortlistActive','shortlistRecorded','shortlistArchived','compositionHook','careerState']
        self.write('writing-demos', fields, [
            [1,'s1','a','l','Licensed','False',13,'o1',1,0,0,.6,'Rising'],
            [10,'s1','a','l','Recorded','False',13,'o1',0,1,0,.6,'Rising'],
            [52,'s1','a','l','Recorded','False',13,'o1',0,0,0,.6,'Rising'],
            [40,'s2','b','l','Licensed','False',52,'o2',0,0,0,.7,'Star']])
        result = audit.material_flow('test', 52, {'a'}, [])
        total = next(r for r in result['commissions'] if r['definition'] == 'all')
        self.assertEqual(total['mature26WeekDeliveries'], 1)
        self.assertEqual(total['preparedButUnreleased'], 1)
        self.assertEqual(total['releasedAnyObserved'], 0)
        self.assertEqual(total['shortlistedObserved'], 1)
        self.assertEqual(result['completedHolds'][0]['holds'], 1)

    def test_opening_holds_are_not_new_licenses(self):
        self.write('writing-demos', ['week','songId','artistId','labelId','status','released','holdUntilWeek','orderId',
            'shortlistActive','shortlistRecorded','shortlistArchived','compositionHook','careerState'],
            [[0,'s','a','l','Recorded','True',12,'',0,0,0,.8,'Star'],
             [52,'s','a','l','Recorded','True',12,'',0,0,0,.8,'Star']])
        self.assertEqual(audit.material_flow('test',52,set(),[])['completedHolds'], [])

    def test_album_destinations_exclude_unreleased_projects_and_deduplicate_tracks(self):
        self.write('writing-orders', ['week','id','songId','artistId','labelId','status','deliveryWeek'],
                   [[5,'one','s1','a','l','Delivered',5],[5,'two','s2','a','l','Delivered',5]])
        self.write('writing-album-tracks', ['songId','artistId','terminalState'],
                   [['s1','a','Released'],['s1','a','Released'],['s2','a','Cancelled']])
        result = audit.material_flow('test',13,set(),[])
        self.assertEqual(result['deliveredDestinations'], {'albumOnly':1,'neitherObserved':1})
        self.assertEqual(result['commissions'], [])

    def test_recycled_release_does_not_count_as_original_client_success(self):
        self.write('writing-orders', ['week','id','songId','artistId','labelId','status','deliveryWeek'],
                   [[1,'o','s','buyer','l','Delivered',1]])
        self.write('writing-demos', ['week','songId','artistId','labelId','status','released','holdUntilWeek','orderId',
            'shortlistActive','shortlistRecorded','shortlistArchived','compositionHook','careerState'],
            [[1,'s','buyer','l','Licensed','False',13,'o',0,0,0,.7,'NewSigning'],
             [15,'s','other','rival','Licensed','False',27,'o',0,0,0,.7,'Established'],
             [20,'s','other','rival','Recorded','True',27,'o',0,0,0,.7,'Established']])
        result = audit.material_flow('test',52,set(),[])
        total = next(r for r in result['commissions'] if r['definition'] == 'all')
        self.assertEqual(total['releasedOriginalAct'], 0)
        self.assertEqual(total['releasedOtherAct'], 1)
        self.assertEqual(total['releasedWithin26'], 0)
        holds = {r['route']:r for r in result['completedHolds']}
        self.assertEqual(holds['OriginalCommission']['releasedObserved'], 0)
        self.assertEqual(holds['RecycledCommissionLicense']['releasedObserved'], 1)

    def test_linked_plan_using_another_song_is_not_commission_use(self):
        self.write('writing-orders', ['week','id','songId','artistId','labelId','status','deliveryWeek','releasePlanId'],
                   [[1,'o','paid-song','a','l','Delivered',1,'plan']])
        self.write('release-slate', ['slotId','state','actualWeek','recordId'], [['plan','Issued',5,'other-master']])
        launches = [dict(recordId='other-master',songId='other-song',artistId='a',labelId='l',week='5'),
                    dict(recordId='late-master',songId='paid-song',artistId='a',labelId='l',week='40')]
        result = audit.material_flow('test',52,set(),launches)
        total = next(r for r in result['commissions'] if r['definition'] == 'all')
        self.assertEqual(total['linkedPlanIssued26'], 1)
        self.assertEqual(total['linkedPlanUsed26'], 0)
        self.assertEqual(total['singleReleased'], 1)
        self.assertEqual(total['singleReleasedWithin26'], 0)

    def test_preparation_inside_booking_week_is_detected(self):
        self.write('writing-orders', ['week','id','songId','artistId','labelId','status','startedWeek','deliveryWeek','releasePlanId'],
                   [[1,'o','s','a','l','Delivered',1,4,'plan']])
        self.write('release-slate', ['week','slotId','state','preparedWeek','actualWeek','recordId'],
                   [[1,'plan','Ready',1,0,'master'],[6,'plan','Issued',1,6,'master']])
        result = audit.material_flow('test',52,set(),[])
        self.assertEqual(result['waitingPlansPreparedEarly'], 1)

    def test_later_cancellation_does_not_erase_earlier_preparation_violation(self):
        self.write('writing-orders', ['week','id','songId','artistId','labelId','status','startedWeek','deliveryWeek','releasePlanId'],
                   [[1,'o','','a','l','Pending',1,7,'plan'],[4,'o','','a','l','Cancelled',1,7,'plan']])
        self.write('release-slate', ['week','slotId','state','preparedWeek','actualWeek','recordId'],
                   [[1,'plan','Ready',1,0,'master']])
        self.assertEqual(audit.material_flow('test',52,set(),[])['waitingPlansPreparedEarly'], 1)
        self.write('release-slate', ['week','slotId','state','preparedWeek','actualWeek','recordId'],
                   [[5,'plan','Ready',5,0,'master']])
        self.assertEqual(audit.material_flow('test',52,set(),[])['waitingPlansPreparedEarly'], 0)


if __name__ == '__main__':
    unittest.main()
