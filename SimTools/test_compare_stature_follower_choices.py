import csv
import json
import tempfile
import unittest
from pathlib import Path

from compare_stature_follower_choices import compare


class FollowerChoiceChecks(unittest.TestCase):
    def test_eligible_denominator_and_comparable_proposals(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            left, right, output = root / 'a.csv', root / 'b.csv', root / 'out.json'
            fields = ('week', 'labelId', 'slotId', 'artistId', 'eligible', 'selected', 'legacyState',
                      'oldMultiplier', 'appliedMultiplier', 'candidateMultiplier', 'tier')
            for path, candidates in ((left, ('2', '', '')), (right, ('1.5', '2', ''))):
                with path.open('w', newline='') as stream:
                    writer = csv.DictWriter(stream, fieldnames=fields)
                    writer.writeheader()
                    for i, candidate in enumerate(candidates):
                        writer.writerow(dict(week='1', labelId='label', slotId='slot', artistId=str(i),
                            eligible='0' if i == 2 else '1', selected='0', legacyState='Rising',
                            oldMultiplier='2', appliedMultiplier='2', candidateMultiplier=candidate, tier='Small'))
            compare(left, right, output)
            counts = json.loads(output.read_text())['counts']
            self.assertEqual(counts['eligible'], 2)
            self.assertEqual(counts['jointlyComparable'], 1)
            self.assertEqual(counts['lowerCandidateValue'], 1)
            self.assertEqual(counts['availabilityChanged'], 1)


if __name__ == '__main__':
    unittest.main()
