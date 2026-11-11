import csv
import tempfile
import unittest
import json
from pathlib import Path

from summarize_stature_marketing_trial import calibration_pair_check, summarize_reads, run
from stature_normalization import file_digest


class MarketingTrialChecks(unittest.TestCase):
    def summarize(self, changes=None, causal=True, blend=1.0, legacy_campaign_share=False):
        row = dict(date='7/8/1960', labelId='label', artistId='artist', context='launch', snapshotDate='1960-07-08',
                   status='Available', legacyCareer='Star', standingCareer='Declining', legacyBudget='200',
                   proposedBudget='50', requestedBudget='50')
        row.update(changes or {})
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as temp:
            path = Path(temp) / 'marketing.csv'
            with path.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=row.keys())
                writer.writeheader(); writer.writerow(row)
            return summarize_reads(path, causal, blend, legacy_campaign_share)

    def test_legacy_campaign_share_uses_old_budget_only_for_booking(self):
        row = {'context': 'programme-campaign-share', 'requestedBudget': '200'}
        self.assertEqual(self.summarize(row, legacy_campaign_share=True)['groups']['all']['changed'], 0)
        with self.assertRaisesRegex(ValueError, 'reader rule'):
            self.summarize(row)
        self.assertEqual(self.summarize(legacy_campaign_share=True)['groups']['all']['lower'], 1)

    def test_half_blend_keeps_cuts_and_increases(self):
        self.assertEqual(self.summarize({'requestedBudget': '125'}, blend=.5)['groups']['all']['lower'], 1)
        self.assertEqual(self.summarize({'legacyCareer': 'Declining', 'standingCareer': 'Star',
            'legacyBudget': '50', 'proposedBudget': '200', 'requestedBudget': '125'}, blend=.5)['groups']['all']['higher'], 1)
        with self.assertRaisesRegex(ValueError, 'reader rule'):
            self.summarize(blend=.5)

    def test_lower_budget_and_same_day_snapshot(self):
        counts = self.summarize()['groups']['all']
        self.assertEqual((counts['changed'], counts['lower'], counts['higher']), (1, 1, 0))

    def test_shadow_counterfactual_is_not_applied(self):
        self.assertEqual(self.summarize({'requestedBudget': '200'}, False)['groups']['all']['changed'], 0)
        with self.assertRaisesRegex(ValueError, 'reader rule'):
            self.summarize(causal=False)

    def test_future_stale_missing_and_wrong_table_are_rejected(self):
        for changes in ({'snapshotDate': '1960-07-09'}, {'snapshotDate': '1960-06-30'},
                        {'proposedBudget': '51', 'requestedBudget': '51'}, {'standingCareer': ''},
                        {'status': 'Missing'}, {'legacyBudget': 'NaN'}, {'requestedBudget': '-1'}):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.summarize(changes)

    def test_missing_falls_back_and_seven_day_snapshot_is_valid(self):
        self.assertEqual(self.summarize({'status': 'Missing', 'standingCareer': '', 'snapshotDate': '',
                                         'proposedBudget': '200', 'requestedBudget': '200'})['groups']['all']['available'], 0)
        self.summarize({'snapshotDate': '1960-07-01'})

    def test_causal_pair_needs_no_disabled_run_or_neutrality_claim(self):
        self.check_pair(calibration_pair=False)

    def test_calibration_pair_applies_reader_in_both_arms(self):
        self.check_pair(calibration_pair=True)

    def check_pair(self, calibration_pair):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as temp:
            directory = Path(temp); metadata = directory / 'writing-calibration'; metadata.mkdir()
            requests = {}
            for arm in ('control', 'treatment'):
                name = f'pair-1001-{arm}'
                flags = ['--seed=1001', '--weeks=522', f'--run={name}']
                if arm == 'treatment' or calibration_pair:
                    flags.append('--stature-marketing-reader')
                if arm == 'treatment' and not calibration_pair:
                    flags.append('--profile-performance')
                request = dict(flags=flags, effectiveArguments=flags, assemblySha256='build', engineSha256='engine',
                               calibrationSha256='calibration', sourceRevision='revision', sourceContentSha256={})
                if calibration_pair:
                    calibration = Path(__file__).with_name('StatureShadowNationalHighTiers.json' if arm == 'treatment' else 'StatureShadowExperimental.json')
                    flags.append(f'--stature-calibration={calibration}')
                    request.update(validationMode='calibration-pair', calibrationSha256=file_digest(calibration))
                    requests[arm] = request
                standing, proposed = ('Star', 200) if calibration_pair and arm == 'control' else ('Declining', 50)
                requested = proposed if arm == 'treatment' or calibration_pair else 200
                (metadata / f'{name}-request.json').write_text(json.dumps(request))
                (metadata / f'{name}.log').write_text(f'CHART_AUDIT_COMPLETE run={name} weeks=522\n')
                for suffix in ('-errors.log', '-engine.log'): (metadata / f'{name}{suffix}').write_text('')
                tables = {
                    'release-campaign-costs': 'marketingCost,productionCost\n50,100\n',
                    'record-campaigns': 'kind,spend\nFollowUp,10\n',
                    'release-slate': 'format,state\nAlbum,Issued\n',
                    'programme-issue-refusals': 'format,gate\n',
                    'decade-annual-rollup': 'year,singleUnits,albumUnits\n1960,100,50\n',
                    'stature-marketing': 'date,artistId,labelId,context,snapshotDate,status,legacyCareer,standingCareer,legacyBudget,proposedBudget,requestedBudget\n'
                        f'7/8/1960,artist,label,launch,1960-07-08,Available,Star,{standing},200,{proposed},{requested}\n'}
                manifest = []
                if arm == 'treatment':
                    tables['performance-profile'] = 'year,wallSeconds\n1960,1.5\n'
                for suffix, content in tables.items():
                    path = directory / f'{name}-{suffix}.csv'; path.write_text(content)
                    manifest.append(dict(name=path.name, sha256=file_digest(path), bytes=path.stat().st_size))
                (metadata / f'{name}-outputs.json').write_text(json.dumps(manifest))
            run(directory, 'pair', 1001, causal_only=True, calibration_pair=calibration_pair)
            summary = json.loads((metadata / 'pair-1001-summary.json').read_text())
            self.assertFalse(summary['neutrality']['performed'])
            self.assertNotIn('disabled', summary['arms'])
            self.assertEqual(summary['arms']['treatment']['marketing']['groups']['all']['lower'], 1)
            if calibration_pair:
                self.assertEqual(summary['firstRequestedDifference']['result'], 'requested budget changed')
                self.assertEqual(summary['firstRequestedDifference']['control']['requestedBudget'], '200')
                requests['control']['effectiveArguments'].append('--stature-marketing-blend=0.5')
                with self.assertRaisesRegex(ValueError, 'differs in settings'):
                    calibration_pair_check(requests)


if __name__ == '__main__':
    unittest.main()
