import tempfile
import unittest
from pathlib import Path

from analyze_followup_chart_trace import replay_identity


class FollowupTraceIdentityTests(unittest.TestCase):
    def test_offchart_rows_are_additive_but_economic_drift_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            old, new = Path(folder) / 'old', Path(folder) / 'new'
            for n in range(50):
                Path(f'{old}-stream{n}.csv').write_text('week,value\n1,a\n2,b\n3,c\n')
                Path(f'{new}-stream{n}.csv').write_text('week,value\n1,a\n2,b\n')
            Path(f'{old}-records.csv').write_text('week,recordId,currentPosition\n1,hit,10\n2,hit,11\n')
            Path(f'{new}-records.csv').write_text('week,recordId,currentPosition\n1,miss,0\n1,hit,10\n2,hit,11\n2,miss,0\n')
            self.assertEqual(len(replay_identity(old, new, 2)['identicalWeekKeyedStreams']), 51)
            Path(f'{new}-stream7.csv').write_text('week,value\n1,a\n2,changed\n')
            with self.assertRaisesRegex(ValueError, 'Replay changed stream7'):
                replay_identity(old, new, 2)


if __name__ == '__main__':
    unittest.main()
