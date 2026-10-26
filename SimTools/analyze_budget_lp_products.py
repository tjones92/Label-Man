"""Validate and compare completed budget-product worlds, keeping store/rack errors separate."""
import argparse
import json
from pathlib import Path

import pandas as pd

from analyze_rack_jobber_probe import BILLBOARD
from analyze_single_capital_risk import ROOT, read


def analyse(plan_path, completed_only=False):
    plan = json.loads(plan_path.read_text(encoding='utf-8-sig'))
    expected = {(a, s) for a in plan['arms'] for s in plan['seeds']}
    completed = {(r['arm'], r['seed']) for r in plan['runs'] if r['passed']}
    matrix_complete = expected == completed
    if (not matrix_complete and not completed_only) or any(not c['passed'] for c in plan['checks']):
        raise ValueError('All declared checks and worlds must complete before analysis')
    if not completed:
        raise ValueError('No completed worlds')
    plan['runs'] = [r for r in plan['runs'] if r['passed']]
    annual, checks = [], []
    defaults = plan.get('scenarioDefaults', {})
    for run in plan['runs']:
        prefix = ROOT / run['name']
        sales = read(prefix, 'label-sales-weekly')
        stores = read(prefix, 'rack-store-weekly')
        clubs = read(prefix, 'rack-club-weekly')
        front = read(prefix, 'rack-region-weekly')
        titles = read(prefix, 'rack-title-weekly')
        company = read(prefix, 'company-monthly')
        week_year = sales.groupby('week').year.first()
        if sales.groupby('week').year.nunique().max() != 1:
            raise ValueError('Ambiguous settlement year')
        def equal(left, right, message):
            if left.sub(right, fill_value=0).abs().max() > .01:
                raise ValueError(run['name'] + ': ' + message)
        equal(front.groupby(['week', 'regionId']).units.sum(), titles.groupby(['week', 'regionId']).units.sum(), 'front-line title identity')
        if company.cashReconciliationError.abs().max() > .02:
            raise ValueError('Cash reconciliation fails: ' + run['name'])
        budget, product, finance = None, None, None
        if run['arm'].startswith('budget'):
            budget = read(prefix, 'budget-region-weekly')
            product = read(prefix, 'budget-product-weekly')
            finance = read(prefix, 'budget-finance-weekly')
            if (budget.frontUnits + budget.budgetUnits > budget.shelf).any():
                raise ValueError('Combined shelf exceeded')
            if ((product.switchedStoreUnits > product.units) | (product.switchedStoreUnits < 0)).any():
                raise ValueError('Unfulfilled buyer substitution')
            assortment = front.set_index(['week', 'regionId'])[['hitTitles', 'catalogueTitles']].sum(axis=1)
            if assortment.add(budget.set_index(['week', 'regionId']).budgetOffers, fill_value=0).max() > 75:
                raise ValueError('Duplicate assortment slots')
            equal(budget.groupby(['week', 'regionId']).frontUnits.sum(), front.groupby(['week', 'regionId']).units.sum(), 'shared front units')
            equal(budget.groupby(['week', 'regionId']).budgetUnits.sum(), product.groupby(['week', 'regionId']).units.sum(), 'budget product units')
            equal(budget.groupby(['week', 'regionId']).switchedStoreUnits.sum(), product.groupby(['week', 'regionId']).switchedStoreUnits.sum(), 'buyer identity')
            equal(budget.groupby('week').budgetUnits.sum(), finance.groupby('week').units.sum(), 'budget financial units')
            catalogue = read(prefix, 'budget-product-catalogue')
            equal(product.groupby('productId').units.sum(), catalogue.set_index('productId').units, 'saved depletion')
            release = catalogue.set_index('productId').releaseWeek
            if (product.week < product.productId.map(release)).any():
                raise ValueError('Product sold before holdback')
            if run['arm'] == 'budgetadd' and budget.switchedStoreUnits.sum() != 0:
                raise ValueError('Zero weekly-substitution arm switched buyers')
            if run['arm'] == 'budgetfullswitch' and not budget.budgetUnits.eq(budget.switchedStoreUnits).all():
                raise ValueError('Zero-incremental-pool stress invented buyers')
            if run['arm'] == 'budgetfullswitch' and budget.budgetUnits.sum() <= 0:
                raise ValueError('Zero-incremental-pool stress must exercise actual sales')
        elif (front.units > front.shelf).any():
            raise ValueError('Front-line shelf exceeded')
        checks.append(dict(run=run['name'], cashMaxError=float(company.cashReconciliationError.abs().max()),
                           shelf=True, productAndFinanceIdentity=True))
        for frame in [front, titles, budget, product]:
            if frame is not None:
                frame['year'] = frame.week.map(week_year)
                if frame.year.isna().any():
                    raise ValueError('Week absent from settlement calendar')
        for year, weeks in sales.groupby('year').week.nunique().items():
            def total(frame, field):
                return 0 if frame is None else float(frame.loc[frame.year == year, field].sum())
            row = dict(arm=run['arm'], seed=run['seed'], year=int(year), observedWeeks=int(weeks), partial=bool(weeks < 52),
                       storesM=total(stores, 'lpUnits') / 1e6, frontRacksM=total(front, 'units') / 1e6,
                       budgetRacksM=total(budget, 'budgetUnits') / 1e6, clubsM=total(clubs, 'units') / 1e6,
                       switchedStoreM=total(budget, 'switchedStoreUnits') / 1e6, shelfM=total(front, 'shelf') / 1e6,
                       budgetNet=total(finance, 'net'), frontDemandM=total(front, 'demand') / 1e6,
                       frontHitM=total(front, 'hitUnits') / 1e6, frontCatalogueM=total(front, 'catalogueUnits') / 1e6,
                       budgetDemandM=total(budget, 'budgetDemand') / 1e6)
            if product is not None:
                row['budgetOwnMasterM'] = float(product.loc[(product.year == year) & (product.provenance == 'OwnMaster'), 'units'].sum()) / 1e6
                row['budgetOpeningM'] = float(product.loc[(product.year == year) & (product.provenance == 'OpeningSyntheticBasket'), 'units'].sum()) / 1e6
            row['allRacksM'] = row['frontRacksM'] + row['budgetRacksM']
            row['unusedShelfM'] = row['shelfM'] - row['allRacksM']
            row['totalLpM'] = row['storesM'] + row['allRacksM'] + row['clubsM']
            if year in BILLBOARD and not row['partial']:
                target = BILLBOARD[year]
                row.update(storeErrorM=row['storesM'] - target['stores'], rackErrorM=row['allRacksM'] - target['racks'])
            annual.append(row)
    rows = pd.DataFrame(annual)
    paired = []
    for _, row in rows.iterrows():
        base = rows[(rows.arm == 'racks') & (rows.seed == row.seed) & (rows.year == row.year)]
        if row.arm == 'racks' or len(base) != 1:
            continue
        if int(base.iloc[0].observedWeeks) != int(row.observedWeeks):
            raise ValueError('Unequal comparison coverage')
        paired.append(dict(arm=row.arm, seed=int(row.seed), year=int(row.year), partial=bool(row.partial),
                           **{name + 'Delta': float(row[name] - base.iloc[0][name]) for name in
                              ['storesM', 'frontRacksM', 'budgetRacksM', 'allRacksM', 'totalLpM']}))
    return dict(assemblySha256=plan['assemblySha256'], matrixComplete=matrix_complete, calibrationAccepted=False, checks=checks,
                annual=annual, paired=paired, assumptions=dict(ownMasterOnly=True, openingBasketsSynthetic=True,
                  scenarioDefaults=defaults, scenarios={r['arm']: [a for a in r['args'] if a.startswith(('--budget-', '--rack-turnover='))]
                                                       for r in plan['runs']},
                  warning='Scenario defaults; no market size inferred from a net sales gap. Partial years are never annualized.'))


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--tag', required=True)
    parser.add_argument('--completed-only', action='store_true', help='Write provisional outputs from completed worlds only')
    args = parser.parse_args()
    folder = ROOT / args.tag
    result = analyse(folder / 'plan.json', args.completed_only)
    suffix = '-provisional' if args.completed_only else ''
    (folder / f'budget-review{suffix}.json').write_text(json.dumps(result, indent=2) + '\n')
    pd.DataFrame(result['annual']).to_csv(folder / f'budget-annual{suffix}.csv', index=False)
    pd.DataFrame(result['paired']).to_csv(folder / f'budget-paired{suffix}.csv', index=False)
    print(json.dumps(result, indent=2))
