# Simulation Optimization Reading List

Research pointers for the runtime/calibration optimization work (2026-10-18).
Each entry says what it is and why it maps onto a specific Label Man problem.
Grouped by the two meanings of "optimize": making a run cheaper, and needing
fewer runs.

Context these were picked against:
- Decade runs take ~2.5h; every phase scales with album count (~18.6k active
  albums); two accidental quadratics are known to remain.
- Optimizations are accepted only if CSVs/worlds are byte-identical to a
  reference run (`compare_simulation_benchmarks.py`).
- One extra RNG draw reshuffles the stream downstream, so behavior changes and
  even some refactors move results by noise, not mechanism.
- Single-seed decade A/Bs have a ~50-point noise floor; two identical
  controls differ by up to 6.1.

---

## A. Making a single run cheaper

### 1. Counter-based RNG (the enabler for everything else)
**Salmon, Moraes, Dror, Shaw (2011). "Parallel random numbers: as easy as 1, 2, 3." SC '11.** DOI 10.1145/2063384.2063405.
PDF: https://users.cs.utah.edu/~hari/teaching/bigdata/random123sc11.pdf

Introduces Philox/Threefry: a random number is a keyed hash of a counter,
`rand(key=seed, counter=(entityId, week, decisionId))`, with no sequential
state.

**Why it matters here:** today the game's draws come from shared sequential
streams, so caching a result, skipping an inactive entity or reordering a loop
changes which number every later consumer gets. That makes most real speedups
fail the byte-identical check. With counter-based draws each decision's
randomness is fixed by its identity, not its call order. That makes skipping,
caching and parallelizing safe without changing output. Adopting it is a
one-time re-baseline: output changes once, then stays stable under later
optimizations.

### 2. Per-decision / per-agent random streams in ABMs
**Klein, Abeysuriya, Stuart, Kerr (2024). "Noise-free comparison of stochastic agent-based simulations using common random numbers."** arXiv:2409.02086. https://arxiv.org/abs/2409.02086

Starsim's design: one independent stream per decision type, seeded by hashing
the decision name with the global seed, and draws indexed per agent. Reports
>10x fewer simulations needed for a given standard error in some
applications.

**Why it matters here:** this is the ABM-specific version of #1 and also fixes
the A/B problem in section B. It directly addresses the "one conversion
reshuffles the RNG stream" issue: a change to how one artist decides stops
perturbing every other artist's draws.

**Supporting:** BioDynaMo, arXiv:2503.10796 (https://arxiv.org/pdf/2503.10796).
It found parallel runs only statistically reproducible until each agent got its
own PRNG used only during its own update. Same conclusion, reached from the
parallelism side.

### 3. Data-oriented design / struct-of-arrays
**Mike Acton (2014). "Data-Oriented Design and C++." CppCon keynote.**
https://isocpp.org/blog/2015/01/cppcon-2014-data-oriented-design-and-c-mike-acton

**Ambrosio, De Vinco, Foglia, Spagnuolo, Scarano. "The impact of ECS logic on parallel performance in agent-based model simulations."** CEUR-WS Vol-4124, paper 43. https://ceur-ws.org/Vol-4124/paper43.pdf

The core idea: lay out hot per-entity fields as contiguous arrays and process
them in batches, rather than walking object graphs.

**Why it matters here:** the weekly album and sales passes iterate ~18.6k album
objects plus artists and labels as C# reference objects, which means pointer
chasing and GC pressure. Pulling the few fields the weekly sales/chart pass
reads into parallel arrays (or `struct` spans) is the standard fix. It also
preserves iteration order, so it is byte-identical-safe. Profile first: the
`SimulationPerformanceProfiler` phases should show which loop is memory-bound.

### 4. Simulation level of detail (LOD AI)
**Brom, Šerý, Poch (2007). "Simulation Level of Detail for Virtual Humans." Intelligent Virtual Agents (IVA 2007), LNCS.**
https://www.springerprofessional.de/en/simulation-level-of-detail-for-virtual-humans/2882154
Earlier framing (SOFSEM 2006): https://artemis.ms.mff.cuni.cz/main/papers/IVE-LOD-2006.pdf

Games simulate "important" entities at full fidelity and the rest at reduced
fidelity, with defined promotion and demotion between the two levels.

**Why it matters here:** most of the population never matters to the chart or
the player. Memory notes say ~38% of late acts are never-charted unknowns, and
only 0.51% of careers ever reach Rising. Those acts could be updated monthly
or analytically and promoted to full weekly simulation on a trigger such as a
release, a chart entry or player contact. Promotion and demotion have to be
lossless, or the act's state drifts while it is demoted. This changes output,
so treat it as a calibration change, not a pure optimization.

### 5. Super-individuals (aggregate the long tail)
**Scheffer, Baveco, DeAngelis, Rose, van Nes (1995). "Super-individuals: a simple solution for modelling large populations on an individual basis." Ecological Modelling 80:161–170.**
https://dare.uva.nl/id/f9112ea8-31b6-447b-b442-3d7ad05af8ae

**Counter-evidence: Parry & Evans (2008), Ecological Modelling 214:141–152.**
https://eprints.whiterose.ac.uk/4129/

One model entity stands for N identical real ones. Parry & Evans found
aggregate statistics hide distortions once N exceeds ~10, and that dynamics
changed.

**Why it matters here:** this is a candidate for the inert background
population, such as the fresh/latent formation pool and session players.
**Do not** use it for anything that can chart: the memory notes show chart and
share outcomes are tail- and launch-driven, which is exactly what aggregation
distorts.

### 6. Event-driven / heterogeneous time steps
**Ralitera, Aky, Payet, Courdier (2018). "Steps Towards a Balance between Adequacy and Time Optimization in Agent-based Simulation." SIMULTECH 2018, pp. 160–166.** DOI 10.5220/0006904601600166.
https://www.scitepress.org/Papers/2018/69046/

Fixed-step schedulers spend most of their time ticking agents with nothing to
do. A per-agent "next time I need attention" scheduler avoids this.

**Why it matters here:** the trunk economy books daily while most state only
changes weekly. Any entity whose next state change is known, such as a
contract expiry, a release date or a cooldown, can sit in a priority queue
instead of being polled every tick. This is byte-identical-safe only if RNG
draws are not tied to the polling (see #1).

### 7. Incremental computation (stop recomputing aggregates)
**Acar, Blelloch, Blume, Harper, Tangwongsan. "An experimental analysis of self-adjusting computation." ACM TOPLAS 32(1), 2009.** DOI 10.1145/1596527.1596530.

Record which outputs depend on which inputs, and when an input changes,
recompute only the dependent outputs.

**Why it matters here:** the general theory behind the fix for the two
remaining quadratics. Weekly genre totals, label totals and chart candidate
sets are probably rebuilt from scratch over every album. Maintaining running
sums on insert, retire and update makes them O(changes). You don't need the
full framework; the paper's value is its discipline about tracking
dependencies so incremental results never diverge from a full recompute. Keep
a debug-mode full recompute that asserts equality.

### 8. Practitioner notes from comparable games
**Paradox, "Anatomy of a Game: Simulation Optimization" (Crusader Kings III dev blog).**
https://forum.paradoxplaza.com/forum/threads/anatomy-of-a-game-simulation-optimization.1484301/
**CK2 Dev Diary 18: Optimization and Modding** — slowdown over time from a growing character count, the same shape as Label Man's album-count growth.
https://admin-forum.paradoxplaza.com/forum/developer-diary/ck2-dev-diary-18-optimization-and-modding.962057/

Not peer-reviewed, but these are the closest commercial analogue: a long-run
grand-strategy sim with a growing population, tuned by profiling.

---

## B. Needing fewer / shorter runs (calibration efficiency)

### 9. Common random numbers for A/B comparisons
**Glasserman & Yao (1992). "Some Guidelines and Guarantees for Common Random Numbers." Management Science 38(6).**
https://business.columbia.edu/sites/default/files-efs/pubfiles/4261/glasserman_yao_guidelines.pdf

Plus Klein et al. 2024 (#2), which applies this to ABMs specifically.

**Why it matters here:** CRN works only if the two arms consume the *same*
random numbers for the *same* purposes ("synchronization"). Label Man's shared
streams break synchronization at the first divergent draw, which is why a
small mechanism change looks like ±50 points of noise and why 2–3 seeds are
needed. Synchronized streams could cut the seeds needed for a confident
decade A/B several-fold. That is the largest wall-clock win available, bigger
than any per-run speedup.

### 10. Multi-fidelity screening (short runs before decade runs)
**Li, Jamieson, DeSalvo, Rostamizadeh, Talwalkar (2018). "Hyperband: A Novel Bandit-Based Approach to Hyperparameter Optimization." JMLR 18(185):1–52.**
https://jmlr.org/papers/v18/16-558.html

Successive halving: run many candidates cheaply, keep the top 1/η, and give
the survivors a bigger budget.

**Why it matters here:** the project already does this informally ("short
catastrophe runs per fix, one two-seed decade at the end"). Hyperband
formalizes how many candidates to start with and when to promote them.
**Caveat from memory:** "short-run deltas can invert" (breadth ran +25/−1/+21
by year). Use short runs only to kill catastrophes, never to rank close
candidates, or resume from a saved mid-decade world so the short budget lands
in the window that matters.

### 11. Surrogate / emulator-based calibration
**Lamperti, Roventini, Sani (2018). "Agent-based model calibration using machine learning surrogates." Journal of Economic Dynamics and Control 90.**
Working paper: https://www.lem.sssup.it/WPLem/files/2017-11.pdf

**Kimpton, Challenor, Salter (2024). "Uncertainty Quantification for Agent Based Models: A Tutorial."** arXiv:2409.16776. https://arxiv.org/abs/2409.16776
A Gaussian-process emulator plus history matching, which rules out implausible
parameter regions in waves.

**Platt (2020). "A comparison of economic agent-based model calibration methods." JEDC 113:103859.** https://arxiv.org/abs/1902.05938
Finds that a simple Bayesian likelihood approach beats simulated minimum
distance methods, but warns that all methods degrade with many parameters.

**Why it matters here:** genre keyframes, radio multipliers and acceptance
values are tuned one decade run at a time. With ~20–40 archived decade runs
(the SimLogs ref runs), an emulator mapping knobs to per-genre share/slot
error could propose the next run instead of hand-reasoning. History matching
fits especially well: the memory notes already reason in "this region is
impossible" terms (Sunshine Pop's arithmetic ceiling, DeathYear supply zero),
which is what an implausibility cutoff encodes.

### 12. Sensitivity analysis before tuning
**ten Broeke, van Voorn, Ligtenberg (2016). "Which Sensitivity Analysis Method Should I Use for My Agent-Based Model?" JASSS 19(1):5.** DOI 10.18564/jasss.2857.
https://www.jasss.org/19/1/5/5.pdf

Compares OAT, regression-based and Sobol' sensitivity analysis on an ABM.
Recommends starting with extended one-factor-at-a-time to understand
mechanisms.

**Why it matters here:** several memory notes are "this knob was inert"
discoveries made one expensive decade at a time (salience, momentum service,
static formatMatch). A cheap screening pass over the knob set (Morris/OAT on
resumed mid-decade worlds) finds the inert ones in bulk, before anyone tunes
them.

---

## Suggested order of payoff

1. **#1 + #2 (counter-based, per-decision RNG).** This is one deliberate
   re-baseline. Afterward, #3, #4 and #6 can land as true optimizations, and
   #9 makes every later A/B cheaper.
2. **#7 (incremental aggregates)** for the two known quadratics. This is
   byte-identical-safe today.
3. **#3 (struct-of-arrays on the hot weekly loop).** Also byte-identical-safe
   today if iteration order is kept.
4. **#9 / #10 / #11** once streams are synchronized, to shorten the calibration
   loop itself.
5. **#4 / #5 / #6.** These change model output, so treat them as design
   changes with their own decade A/B.
