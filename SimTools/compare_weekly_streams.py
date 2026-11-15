"""Byte-compare the weekly CSV streams of a short run against the first weeks of a longer run.

Run from SimLogs/: python ../SimTools/compare_weekly_streams.py fh-v1-2002-off cand-v3-2002-off [--weeks=27]
A stream is a CSV whose header has a `week` column and whose rows are written in week order. Rows of the long run
with week <= the short run's last week are compared as text with the short run's rows. Files without a week column,
and files the short run wrote only at close, are listed as not compared.
"""
import csv, glob, os, sys
short, long_ = [a for a in sys.argv[1:] if not a.startswith('--')]
weeks = next((int(a[8:]) for a in sys.argv[1:] if a.startswith('--weeks=')), 27)
same, differ, skipped = [], [], []
for path in sorted(glob.glob(f'{short}-*.csv')):
    name = path[len(short) + 1:]
    other = f'{long_}-{name}'
    if not os.path.exists(other): skipped.append(name + ' (missing)'); continue
    with open(path, newline='', encoding='utf-8') as a, open(other, newline='', encoding='utf-8') as b:
        ha, hb = a.readline(), b.readline()
        cols = next(csv.reader([ha]), [])
        if ha != hb or 'week' not in cols: skipped.append(name); continue
        i = cols.index('week')
        def rows(f):
            out = []
            for line in f:
                try: w = float(next(csv.reader([line]))[i].strip('"'))
                except (ValueError, IndexError, StopIteration): out.append(line); continue
                if w <= weeks: out.append(line)
            return out
        ra, rb = rows(a), rows(b)
        if ra == rb: same.append((name, len(ra)))
        else:
            first = next((k for k, (x, y) in enumerate(zip(ra, rb)) if x != y), min(len(ra), len(rb)))
            differ.append((name, len(ra), len(rb), first))
print(f'{short} vs {long_}, weeks <= {weeks}: {len(same)} streams identical ({sum(n for _, n in same)} rows), {len(differ)} differ')
for d in differ: print('  DIFFERS', d)
print('  not compared:', len(skipped))
if '--list' in sys.argv: print('   ', ', '.join(skipped))
