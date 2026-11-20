"""Render the office entrance foley to a mono WAV. Run with Python and NumPy."""

import sys
import wave
from pathlib import Path

import numpy as np


RATE = 24000
rng = np.random.default_rng(1930)
mix = np.zeros(int(11.65 * RATE))


def noise(length, smooth):
    values = rng.normal(0, 1, length)
    window = np.hanning(smooth + 2)[1:-1]
    window /= window.sum()
    return np.convolve(values, window, mode="same") * np.sqrt(smooth)


def event(start, duration, sound):
    t = np.arange(int(duration * RATE)) / RATE
    sample = sound(t) * np.clip(t / .004, 0, 1) * np.clip((duration - t) / .019, 0, 1)
    index = int(start * RATE)
    mix[index:index+len(t)] += sample


def tap(start, volume, frequency):
    event(start, .17, lambda t: volume * (
        .48 * np.sin(2*np.pi*frequency*t) * np.exp(-31*t)
        + .32 * noise(len(t), 62) * np.exp(-42*t)
        + .12 * noise(len(t), 8) * np.exp(-70*t)))


def scuff(start, duration, volume):
    event(start, duration, lambda t: volume * (noise(len(t), 30) + .27*noise(len(t), 7))
          * np.sin(np.pi*t/duration)**1.55 * (.67 + .33*np.sin(2*np.pi*18*t)**2))


def wood_creak(start, duration, volume, pitch):
    def sound(t):
        envelope = np.sin(np.pi*t/duration)**1.4
        bend = 2*np.pi*(pitch*t - 39*t*t + 1.2*np.sin(9*t))
        friction = (.35 + .65*np.sin(2*np.pi*23*t)**2)
        return volume * envelope * friction * (
            .48*np.sin(bend) + .23*np.sin(2.09*bend) + .16*noise(len(t), 33))
    event(start, duration, sound)


# Latch, push, two steps across the threshold, a dragged heel and the door settling.
for start, pitch, gain in ((.26, 1280, .052), (.38, 880, .062), (4.94, 1350, .043)):
    event(start, .11, lambda t: gain * (noise(len(t), 9) + .45*np.sin(2*np.pi*pitch*t)) * np.exp(-54*t))
wood_creak(.45, .79, .064, 340)
tap(1.08, .21, 97)
tap(1.70, .28, 83)
tap(2.26, .26, 102)
scuff(2.58, .29, .09)
tap(2.74, .31, 78)
wood_creak(3.50, 1.18, .028, 232)
tap(4.98, .29, 70)

# Footsteps around the desk, then vinyl and the swivel frame taking the player's weight.
for start, gain, pitch in ((5.46, .17, 94), (6.09, .19, 88), (6.78, .22, 91), (7.48, .19, 84),
                           (8.17, .22, 97), (8.89, .18, 86), (9.51, .15, 92)):
    tap(start, gain, pitch)
scuff(9.73, .38, .067)
wood_creak(10.02, .88, .077, 177)
tap(10.37, .17, 65)
scuff(10.44, .43, .039)

dry = mix.copy()
for delay, gain in ((.024, .12), (.056, .08), (.109, .041)):
    samples = int(delay * RATE)
    mix[samples:] += dry[:-samples] * gain
pcm = (np.clip(mix, -1, 1) * 32767).astype("<i2")
with wave.open(str(Path(sys.argv[1])), "wb") as output:
    output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
    output.writeframes(pcm.tobytes())
