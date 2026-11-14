"""Python replica of the keyed commercial-fit draws (PhiloxCounterRandom + SingleCommercialFit + RegionalFitExposure).

Reading these draws costs the simulation nothing: they are counter-based and keyed on (world seed, recordId, key).
"""
from math import cos, erf, exp, log, pi, sqrt

M32 = 0xFFFFFFFF
M64 = 0xFFFFFFFFFFFFFFFF


def _identity(s):
    h = 14695981039346656037
    for ch in s:
        h ^= ord(ch)
        h = (h * 1099511628211) & M64
    return h


def _block(a, b, c, d, k0, k1):
    for _ in range(10):
        left = 0xD2511F53 * a
        right = 0xCD9E8D57 * c
        a, b, c, d = ((right >> 32) ^ b ^ k0) & M32, right & M32, ((left >> 32) ^ d ^ k1) & M32, left & M32
        k0 = (k0 + 0x9E3779B9) & M32
        k1 = (k1 + 0xBB67AE85) & M32
    return a, b, c, d


def unit(seed, entity_id, region_id, week, decision):
    e = _identity(entity_id)
    key = (seed ^ _identity(region_id)) & M64
    A, B, _, _ = _block(e & M32, e >> 32, week & M32, decision & M32, key & M32, key >> 32)
    return (((A << 21) | (B >> 11)) & M64) * (1.0 / 9007199254740992.0)


def normal(seed, record_id, key, decision):
    u1 = unit(seed, record_id, key, 0, decision)
    u2 = unit(seed, record_id, key, 0, decision | 1)
    return sqrt(-2.0 * log(max(u1, 1e-300))) * cos(2.0 * pi * u2)


def ncdf(z):
    return 0.5 * (1 + erf(z / sqrt(2)))


SIGMA, UPSIDE, HIT_SHARE, LAUNCH_WEIGHT, FLOOR, LAUNCH_REF = 2.5, 0.15, 0.15, 1.25, 0.12, 0.2
NATIONAL_DECISION, LAUNCH_DECISION = 0x43460000, 0x43460200


def _kink():
    lo, hi = -8.0, 8.0
    for _ in range(100):
        mid = (lo + hi) / 2
        if ncdf(mid) < 1 - HIT_SHARE: lo = mid
        else: hi = mid
    return (lo + hi) / 2


KINK = _kink()


ACT_RECORD_WEIGHT, UPSIDE_CAP = 0.0, 0.0  # --single-commercial-fit-act-record-weight / -upside-cap (off)
ACT_RECORD_ODDS_ONLY = False  # --single-commercial-fit-act-record-odds-only


def _logmean(k):
    below = exp(-SIGMA * k + .5 * SIGMA ** 2) * ncdf(k - SIGMA)
    if UPSIDE_CAP <= 0 or UPSIDE <= 0:
        return log(below + exp(-UPSIDE * k + .5 * UPSIDE ** 2) * (1 - ncdf(k - UPSIDE)))
    top = k + UPSIDE_CAP / UPSIDE
    return log(below + exp(-UPSIDE * k + .5 * UPSIDE ** 2) * (ncdf(top - UPSIDE) - ncdf(k - UPSIDE)) + exp(UPSIDE_CAP) * (1 - ncdf(top)))


LOG_NORM = _logmean(KINK)
UNEXPOSED_CAP = max(-SIGMA * KINK - LOG_NORM, log(FLOOR))


def national_log(z, aw, act_record=0.0):
    """act_record: the single's actChartRecordAtLaunch (single-launch-attribution.csv when the switch is on)."""
    k0 = KINK - LAUNCH_WEIGHT * (aw - LAUNCH_REF)
    k = k0 - ACT_RECORD_WEIGHT * act_record
    above = UPSIDE * max(0.0, z - (k0 if ACT_RECORD_ODDS_ONLY else k))
    if UPSIDE_CAP > 0: above = min(above, UPSIDE_CAP)
    v = (SIGMA * (z - k) if z < k else above) - LOG_NORM
    return max(v, log(FLOOR))


def national_z(seed, record_id):
    return normal(seed, record_id, "national", NATIONAL_DECISION)


def launch_exposed(seed, record_id, region_id, reach, base=0.3, slope=0.7):
    return unit(seed, record_id, region_id, 0, LAUNCH_DECISION) < base + slope * reach


def configure(launch_weight=None, upside=None, act_record_weight=0.0, upside_cap=0.0, act_record_odds_only=False):
    """Set the fit shape and recompute the constants derived from it (LOG_NORM and UNEXPOSED_CAP depend on UPSIDE and the cap)."""
    global LAUNCH_WEIGHT, UPSIDE, LOG_NORM, UNEXPOSED_CAP, ACT_RECORD_WEIGHT, UPSIDE_CAP, ACT_RECORD_ODDS_ONLY
    if launch_weight is not None: LAUNCH_WEIGHT = launch_weight
    if upside is not None: UPSIDE = upside
    ACT_RECORD_WEIGHT, UPSIDE_CAP, ACT_RECORD_ODDS_ONLY = act_record_weight, upside_cap, act_record_odds_only
    LOG_NORM = _logmean(KINK)
    UNEXPOSED_CAP = max(-SIGMA * KINK - LOG_NORM, log(FLOOR))


# --lp-retail-v2-profile supplies these; an explicit flag replaces them (SingleCommercialFit.cs).
PROFILE_LAUNCH_WEIGHT, PROFILE_UPSIDE = 1.25, 0.15


def params_for_run(run, root=None):
    """(launch weight, upside, act-record weight, upside cap) a run was launched with, from
    SimLogs/writing-calibration/<run>-request.json; pass to configure(*params)."""
    import json, os
    root = root or os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "SimLogs", "writing-calibration")
    path = os.path.join(root, run + "-request.json")
    with open(path, encoding="utf-8-sig") as f:
        flags = json.load(f)["flags"]
    lw, up, ar, cap = PROFILE_LAUNCH_WEIGHT, PROFILE_UPSIDE, 0.0, 0.0
    odds = "--single-commercial-fit-act-record-odds-only" in flags
    for x in flags:
        if x.startswith("--single-commercial-fit-launch-weight="): lw = float(x.split("=", 1)[1])
        elif x.startswith("--single-commercial-fit-upside="): up = float(x.split("=", 1)[1])
        elif x.startswith("--single-commercial-fit-act-record-weight="): ar = float(x.split("=", 1)[1])
        elif x.startswith("--single-commercial-fit-upside-cap="): cap = float(x.split("=", 1)[1])
        elif x.startswith(("--single-commercial-fit=", "--single-commercial-fit-hit-share=", "--single-commercial-fit-floor=",
                           "--single-commercial-fit-push-weight=", "--single-commercial-fit-own-normalization=")):
            raise ValueError(f"{run}: {x} is not replicated by keyed_fit_draws")
    return lw, up, ar, cap, odds
