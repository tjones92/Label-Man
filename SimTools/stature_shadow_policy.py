"""Experimental, offline band policy. Inputs and calibration are supplied by the caller.

This module does not derive reach, fit parameters, or affect simulation state.
Missing inputs are unknown, not zero; an unknown higher band makes the result
indeterminate even when a lower band has support.
"""
import math
from datetime import date

BANDS = ('Rising', 'Established', 'Star', 'Superstar')
ROUTES = ('regional', 'record', 'conversion')


def _unit(value):
    if value is not None and (not isinstance(value, (int, float)) or isinstance(value, bool)
                              or not math.isfinite(value) or not 0 <= value <= 1):
        raise ValueError('Evidence and reach must be between zero and one, or missing')
    return value


def _bounds(value):
    if value is None:
        return (0.0, 1.0)
    if isinstance(value, (list, tuple)) and len(value) == 2:
        lo, hi = (_unit(v) for v in value)
        if lo is None or hi is None or lo > hi:
            raise ValueError('Invalid evidence bounds')
        return lo, hi
    scalar = _unit(value)
    return scalar, scalar


def validate(calibration):
    """Reject policies where any higher band can be easier than a lower band."""
    if not calibration.get('version') or not calibration.get('effectiveDate'):
        raise ValueError('Calibration requires a version and effective date')
    date.fromisoformat(calibration['effectiveDate'])
    bands = calibration['bands']
    previous = None
    for name in BANDS:
        band = bands[name]
        entry, exit_ = band['entry'], band['exit']
        _unit(entry); _unit(exit_)
        if entry is None or exit_ is None or exit_ > entry:
            raise ValueError('Invalid hysteresis boundaries')
        for route in ROUTES:
            weight = _unit(band['reachWeight'][route])
            if weight is None:
                raise ValueError('Missing reach weight')
            if previous and weight < previous['reachWeight'][route]:
                raise ValueError('Higher bands cannot require less reach')
        if previous and (entry < previous['entry'] or exit_ < previous['exit']):
            raise ValueError('Higher bands cannot have lower boundaries')
        previous = band


def evaluate(evidence, calibration, observation_date, previous_band=None):
    """Return per-band support and a candidate, or an explicit unknown result.

    Each route contains normalized response, draw and reach. Response and draw
    measure overlapping audiences, so their maximum is used; if either is
    missing, the route's maximum is bounded by [known, 1]. Reach has the same
    bounds when missing. Continuous support is magnitude * ((1-w) + w*reach).
    This exposes uncertainty instead of silently treating missing as zero.
    """
    validate(calibration)
    if date.fromisoformat(observation_date) < date.fromisoformat(calibration['effectiveDate']):
        raise ValueError('Calibration is not yet effective')
    if previous_band not in (None, *BANDS):
        raise ValueError('Unknown previous band')
    previous_index = BANDS.index(previous_band) if previous_band else -1
    results = {}
    for index, band_name in enumerate(BANDS):
        band = calibration['bands'][band_name]
        boundary = band['exit'] if index <= previous_index else band['entry']
        route_results = {}
        for route in ROUTES:
            inputs = evidence[route]
            response, draw, reach = (_bounds(inputs[key]) for key in ('response', 'draw', 'reach'))
            low = max(response[0], draw[0])
            high = max(response[1], draw[1])
            weight = band['reachWeight'][route]
            lower = low * (1 - weight + weight * reach[0])
            upper = high * (1 - weight + weight * reach[1])
            route_results[route] = {'lower': lower, 'upper': upper}
        lower = max(r['lower'] for r in route_results.values())
        upper = max(r['upper'] for r in route_results.values())
        results[band_name] = {'boundary': boundary, 'lower': lower, 'upper': upper,
                              'routes': route_results,
                              'status': 'supported' if lower >= boundary else
                                        'unsupported' if upper < boundary else 'unknown'}
    for band_name in reversed(BANDS):
        status = results[band_name]['status']
        if status == 'unknown':
            return {'band': None, 'status': 'missing_evidence', 'bands': results}
        if status == 'supported':
            return {'band': band_name, 'status': 'observed', 'bands': results}
    return {'band': None, 'status': 'below_rising', 'bands': results}
