"""Replicate StationNetwork roster generation (deterministic parts) + FormatMatch + the
ReporterRadioPlay denominator, from the authored regions in chart_manager.tscn."""
import re, math
from collections import OrderedDict

TSCN = r"C:\Project\Label-Man\chart_manager.tscn"
txt = open(TSCN, encoding="utf-8").read()
heads = re.findall(r"\n\[(?:sub_resource|resource|node|ext_resource)([^\]]*)\]\n", txt)
blocks = re.split(r"\n\[(?:sub_resource|resource|node|ext_resource)[^\]]*\]\n", txt)
byid = {}
for h, b in zip(heads, blocks[1:]):
    m = re.search(r'id="([^"]+)"', h)
    if m: byid[m.group(1)] = b

def field(b, name, default=None):
    if b is None: return default
    m = re.search(rf"^{name} = (.+)$", b, re.M)
    if not m: return default
    v = m.group(1).strip()
    if v in ("true", "false"): return v == "true"
    if v.startswith('"'): return v.strip('"')
    if v.startswith("SubResource"):
        return byid.get(re.search(r'SubResource\("([^"]+)"\)', v).group(1))
    try: return float(v)
    except Exception: return v

TIER = {0: "Major", 1: "Regional", 2: "Secondary", 3: "Minor"}
REPORTERS = {"Major": 11, "Regional": 9, "Secondary": 7, "Minor": 6}
regions = []
for rid, b in byid.items():
    if re.search(r'^regionId = "', b, re.M):
        media = field(b, "media")
        regions.append(dict(
            regionId=field(b, "regionId"), tier=TIER[int(field(b, "tier", 0.0))],
            population=field(b, "population", 0.0), urbanization=field(b, "urbanization", 0.0),
            averageIncome=field(b, "averageIncome", 0.0), youthPercentage=field(b, "youthPercentage", 0.0),
            blackPopulation=field(b, "blackPopulation", 0.0), collegeCount=field(b, "collegeCount", 0.0),
            integrationLevel=field(b, "integrationLevel", 0.0),
            culturalProgressivism=field(b, "culturalProgressivism", 0.0),
            churchNetworkStrength=field(b, "churchNetworkStrength", 0.25),
            radioReach=field(media, "radioReach", 0.0), hasCountryStations=bool(field(media, "hasCountryStations", False)),
            hasFMUnderground=bool(field(media, "hasFMUnderground", False))))
regions.sort(key=lambda r: -r["population"])

def seg_shares(r, year, integration):
    colleges = r["collegeCount"]/r["population"] if r["population"] else 0
    fm = min(max((year-1966)*.25, 0), 1) if (r["hasFMUnderground"] and year >= 1967) else 0.0
    raw = {"MainstreamAM": .38 + r["radioReach"]*.10,
           "Youth": .10 + r["youthPercentage"]*.35,
           "AdultMOR": .14 + r["averageIncome"]*.04,
           "UrbanRnB": .03 + r["blackPopulation"]*(.12 + integration*.08),
           "CountryWestern": .05 + (1-r["urbanization"])*.16 + (.06 if r["hasCountryStations"] else 0),
           "CollegeFolk": .02 + min(max(colleges/25, 0), .12),
           "UndergroundFM": fm*(.02 + r["culturalProgressivism"]*.08),
           "JazzHiFiClassical": .04 + r["urbanization"]*.06,
           "GospelChurch": .02 + r["churchNetworkStrength"]*.12,
           "RegionalLatin": .12 if r["regionId"] == "southwest" else (.06 if r["regionId"] == "eastcoast" else .015),
           "FamilyChildrens": .04 + (1-r["youthPercentage"])*.03}
    t = sum(raw.values()); return {k: v/t for k, v in raw.items()}

FMTS = ["Top40","RnB","Country","MOR","FullService","UndergroundFM","Gospel","Jazz"]
ORD = {f: i for i, f in enumerate(FMTS)}

def allocate(r, s, year, fmviable):
    w = {}
    def add(f, x): w[f] = w.get(f, 0)+max(0.0, x)
    add("Top40", s["MainstreamAM"]+s["Youth"]+s["RegionalLatin"]); add("RnB", s["UrbanRnB"])
    add("Country", s["CountryWestern"]); add("MOR", s["AdultMOR"]+s["FamilyChildrens"])
    add("Jazz", s["JazzHiFiClassical"]); add("Gospel", s["GospelChurch"])
    add("UndergroundFM" if fmviable else "FullService", s["CollegeFolk"])
    if fmviable: add("UndergroundFM", s["UndergroundFM"] + r["culturalProgressivism"]*0.05)
    slots = REPORTERS[r["tier"]]
    res = {"Top40": 1}
    for f, thr in (("RnB",.06),("Country",.10),("MOR",.10),("Gospel",.10),("Jazz",.08)):
        if w.get(f, 0) >= thr: res[f] = max(1, res.get(f, 0))
    if fmviable and w.get("UndergroundFM", 0) >= .02: res["UndergroundFM"] = max(1, res.get("UndergroundFM", 0))
    remaining = max(0, slots-sum(res.values())); tot = sum(w.values())
    if tot > 0 and remaining > 0:
        exact = {k: v/tot*remaining for k, v in w.items()}
        for k, v in exact.items(): res[k] = res.get(k, 0)+math.floor(v)
        leftover = slots-sum(res.values())
        for k, _ in sorted(exact.items(), key=lambda kv: (-(kv[1]-math.floor(kv[1])), ORD[kv[0]]))[:max(0, leftover)]:
            res[k] = res.get(k, 0)+1
    if year < 1963 and res.get("Top40", 0) > 1:
        conv = min(math.ceil(res["Top40"]*0.5), res["Top40"]-1)
        res["Top40"] -= conv; res["FullService"] = res.get("FullService", 0)+conv
    counts = [[k, v] for k, v in sorted(res.items(), key=lambda kv: (-kv[1], ORD[kv[0]])) if v > 0]
    total = sum(c[1] for c in counts)
    while total > slots:
        idx = -1
        for i in range(len(counts)-1, -1, -1):
            if counts[i][0] == "Top40" and counts[i][1] <= 1: continue
            if idx < 0 or counts[i][1] > counts[idx][1]: idx = i
        if idx < 0: break
        counts[idx][1] -= 1; total -= 1
    return OrderedDict((k, v) for k, v in counts if v > 0)

# ---- genre segment routing (GenreSegmentRouting.Create) ----
SRC = {  # genre: (am, mor, rb, country, college, fm), lean
 "SunshinePop": ((.60,.25,0,0,0,.15), .65), "Soul": ((.40,0,.50,0,.10,0), .55),
 "Bubblegum": ((.85,0,0,0,0,0), .95), "TeenPop": ((.80,0,0,0,.05,0), .90),
 "BritishPop": ((.85,0,0,0,0,0), .90), "EasyListening": ((.25,.70,0,0,0,0), .15),
 "Country": ((.40,.40,0,0,.05,0), None), "TraditionalPop": ((.35,.60,0,0,0,0), .15),
 "DooWop": ((.50,0,.40,0,0,0), None), "RockAndRoll": ((.75,0,.20,0,0,0), .85),
 "PsychedelicRock": ((.30,0,0,0,.25,.40), None), "FolkRock": ((.40,0,0,0,.30,.20), None),
 "BaroquePop": ((.50,.30,0,0,0,.20), .60), "RnB": ((.30,0,.60,0,0,0), None),
 "Funk": ((.30,0,.55,0,.10,.05), None), "GarageRock": ((.60,0,0,0,.20,.10), None),
}
LEAN = {"Country": .70, "DooWop": .60, "PsychedelicRock": .75, "FolkRock": .65,
        "RnB": .60, "Funk": .70, "GarageRock": .85}
FAMILY = {"Soul": "RhythmAndSoul", "RnB": "RhythmAndSoul", "Funk": "RhythmAndSoul", "DooWop": "RhythmAndSoul"}

def seg_weights(g):
    (am, mor, rb, ctry, col, fm), lean = SRC[g]
    if lean is None: lean = LEAN[g]
    w = {"MainstreamAM": am*(1-(.35+.45*lean)), "Youth": am*(.35+.45*lean), "AdultMOR": mor,
         "UrbanRnB": rb, "CountryWestern": ctry, "CollegeFolk": col, "UndergroundFM": fm,
         "JazzHiFiClassical": 0.0, "GospelChurch": 0.0, "RegionalLatin": 0.0, "FamilyChildrens": 0.0}
    if g == "Country": w["CountryWestern"] = max(w["CountryWestern"], .35)  # EnsureMinimum from largest
    t = sum(w.values()); return {k: v/t for k, v in w.items()}

FMT_SEGS = {"Top40": ["MainstreamAM","Youth"], "RnB": ["UrbanRnB"], "Country": ["CountryWestern"],
            "MOR": ["AdultMOR","FamilyChildrens"], "Jazz": ["JazzHiFiClassical"], "Gospel": ["GospelChurch"],
            "UndergroundFM": ["UndergroundFM","CollegeFolk"],
            "FullService": ["MainstreamAM","Youth","AdultMOR","CollegeFolk","UrbanRnB","CountryWestern"]}

def format_match(g, fmt, integration):
    w = seg_weights(g); m = sum(w[s] for s in FMT_SEGS[fmt])
    if fmt == "Top40" and FAMILY.get(g) == "RhythmAndSoul": m += w["UrbanRnB"]*integration*0.6
    return min(max(m, 0.0), 1.0)

def integ(r, year):
    curve = [(1960,0),(1961,.04),(1962,.08),(1963,.16),(1964,.38),(1965,.52),(1966,.62),(1967,.80),(1968,.90),(1969,1.0)]
    p = dict(curve)[year]
    return min(max(r["integrationLevel"] + (1-r["integrationLevel"])*p*0.70, 0), 1)

print("=" * 118)
print("1960 ROSTER ALLOCATION  (BuildRosters runs ONCE at 1960; format mix is frozen for the decade)")
print("=" * 118)
print(f"{'region':<12}{'tier':<10}{'slots':>6}  formats")
grand = {}
for r in regions:
    s = seg_shares(r, 1960, integ(r, 1960))
    a = allocate(r, s, 1960, fmviable=False)
    for k, v in a.items(): grand[k] = grand.get(k, 0)+v
    print(f"{r['regionId']:<12}{r['tier']:<10}{sum(a.values()):>6}  " + ", ".join(f"{k}x{v}" for k, v in a.items()))
print(f"\nNETWORK TOTAL ({sum(grand.values())} reporters): " + ", ".join(f"{k}={v}" for k, v in sorted(grand.items(), key=lambda kv: -kv[1])))
