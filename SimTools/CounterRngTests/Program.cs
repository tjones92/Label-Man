using System;
using System.Diagnostics;
using System.Linq;

int checks = 0;
void Check(bool condition, string name) {
    if (!condition) throw new InvalidOperationException(name);
    checks++;
}
Check(PhiloxCounterRandom.Block(0, 0, 0, 0, 0, 0) == (0x6627e8d5U, 0xe169c58dU, 0xbc57ac4cU, 0x9b00dbd8U), "zero reference vector");
Check(PhiloxCounterRandom.Block(uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue, uint.MaxValue) ==
    (0x408f276dU, 0x41c83b0eU, 0xa20bc7c6U, 0x6d5451fdU), "all-ones reference vector");
Check(PhiloxCounterRandom.Block(0x243f6a88, 0x85a308d3, 0x13198a2e, 0x03707344, 0xa4093822, 0x299f31d0) ==
    (0xd16cfe09U, 0x94fdccebU, 0x5001e420U, 0x24126ea1U), "mixed reference vector");
var ids = Enumerable.Range(0, 1024).Select(i => "record-" + i).ToArray();
var before = ids.ToDictionary(id => id, id => PhiloxCounterRandom.Unit(1001, id, "eastcoast", 123, 0x534c0000));
foreach (var id in ids.Reverse()) {
    _ = PhiloxCounterRandom.Unit(1001, "unrelated-insert", "eastcoast", 123, 0x534c0000);
    Check(before[id] == PhiloxCounterRandom.Unit(1001, id, "eastcoast", 123, 0x534c0000), "order/insertion invariance");
}
int samples = 250000;
var bins = new int[20]; double sum = 0, squares = 0, adjacent = 0, prior = 0;
var watch = Stopwatch.StartNew();
for (int i = 0; i < samples; i++) {
    double value = PhiloxCounterRandom.Unit(1001, "record-123", "eastcoast", i, 0x534c0000);
    Check(value >= 0 && value < 1, "unit range");
    bins[(int)(value * bins.Length)]++;
    sum += value; squares += value * value;
    if (i > 0) adjacent += (prior - .5) * (value - .5);
    prior = value;
}
double mean = sum / samples, variance = squares / samples - mean * mean;
Check(Math.Abs(mean - .5) < .003, "uniform mean smoke check");
Check(Math.Abs(variance - 1.0 / 12) < .002, "uniform variance smoke check");
Check(bins.All(n => Math.Abs(n - samples / 20.0) < samples / 20.0 * .06), "uniform buckets smoke check");
Check(Math.Abs(adjacent / (samples - 1) / variance) < .015, "adjacent weeks smoke check");
Check(before[ids[0]] != PhiloxCounterRandom.Unit(2002, ids[0], "eastcoast", 123, 0x534c0000), "seed separation");
Check(before[ids[0]] != PhiloxCounterRandom.Unit(1001, ids[0], "westcoast", 123, 0x534c0000), "region separation");
Check(before[ids[0]] != PhiloxCounterRandom.Unit(1001, ids[0], "eastcoast", 123, 0x534c0001), "decision separation");
Console.WriteLine($"COUNTER_RNG_CHECK_PASS checks={checks} samples={samples} mean={mean:R} variance={variance:R} sampleLoopMs={watch.Elapsed.TotalMilliseconds:F1}");
