"""Completed-world 2x2 LP assortment/channel-choice comparison; generated files stay in SimLogs."""
import argparse
import json

import pandas as pd

from analyze_budget_lp_products import analyse
from analyze_single_capital_risk import ROOT


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--completed-only', action='store_true')
    args = parser.parse_args()
    folder = ROOT / args.tag
    result = analyse(folder / 'plan.json', args.completed_only)
    annual = pd.DataFrame(result['annual'])
    effects = []
    arms = ['budget', 'budgetmix', 'budgetchoice', 'budgetmixchoice']
    metrics = ['storesM', 'allRacksM', 'frontRacksM', 'budgetRacksM', 'totalLpM', 'frontSwitchedM', 'switchedStoreM']
    for (seed, year), group in annual.groupby(['seed', 'year']):
        rows = {a: group[group.arm == a].iloc[0] for a in arms if (group.arm == a).sum() == 1}
        if len(rows) != 4:
            continue
        if len({int(r.observedWeeks) for r in rows.values()}) != 1:
            raise ValueError('Factorial cells have unequal year coverage')
        base, assortment, choice, both = [rows[a] for a in arms]
        for name, left, right in [('assortmentWithoutChoice', assortment, base), ('assortmentWithChoice', both, choice),
                                  ('choiceWithoutAssortment', choice, base), ('choiceWithAssortment', both, assortment)]:
            effects.append(dict(seed=int(seed), year=int(year), partial=bool(base.partial), effect=name,
                                **{m: float(left[m] - right[m]) for m in metrics}))
        effects.append(dict(seed=int(seed), year=int(year), partial=bool(base.partial), effect='interaction',
                            **{m: float(both[m] - assortment[m] - choice[m] + base[m]) for m in metrics}))
    result['factorialEffects'] = effects
    suffix = '-provisional' if args.completed_only else ''
    (folder / f'factorial-review{suffix}.json').write_text(json.dumps(result, indent=2) + '\n')
    annual.to_csv(folder / f'budget-annual{suffix}.csv', index=False)
    pd.DataFrame(effects).to_csv(folder / f'factorial-effects{suffix}.csv', index=False)
    print(annual[['arm', 'year', 'observedWeeks', 'storesM', 'allRacksM', 'frontRacksM', 'budgetRacksM', 'totalSwitchedM']].to_string(index=False))
