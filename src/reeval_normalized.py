"""Normalised variant of the thesis features for notebooks/reeval_normalized_*.ipynb.

Preprocessing steps, applied cumulatively on top of the thesis displacement windows:
  S1  undetected keypoints (x = y = 0) are filled by linear interpolation over time
  S2  coordinates are divided by the recording's median torso length (neck to mid-hip)
  S3  inputs are standardised with statistics of the training fold only (neural networks)

Splits, metrics and charts come from reeval_common, so the results line up with results/reeval/.
"""
from __future__ import annotations

import json

import numpy as np
from sklearn.preprocessing import StandardScaler

import reeval_common as rc

RESULTS_DIR = rc.ROOT / "results" / "reeval_normalized"
RAW_RESULTS_DIR = rc.ROOT / "results" / "reeval"
NECK, MID_HIP = 1, 8  # OpenPose BODY_25

STEPS = {
    "S0": "thesis features (raw)",
    "S1": "missing-joint interpolation",
    "S2": "S1 + torso-length scaling",
    "S3": "S2 + standardisation",
}
# Ordinal blue ramp (light -> dark), validated with the dataviz palette checker.
STEP_COLORS = {"S0": "#86b6ef", "S1": "#3987e5", "S2": "#1c5cab", "S3": "#0d366b"}


def use_results_dir():
    """Point reeval_common's save and plot helpers at results/reeval_normalized/."""
    rc.RESULTS_DIR = RESULTS_DIR


def load_raw_result(name: str) -> dict:
    """A result of the thesis-feature re-evaluation (results/reeval/), relabelled as step S0."""
    result = json.loads((RAW_RESULTS_DIR / f"{name}.json").read_text())
    result["variant"] = f"S0: {STEPS['S0']}"
    return result


def missing_mask(frames: np.ndarray) -> np.ndarray:
    return (frames[:, :, 0] == 0) & (frames[:, :, 1] == 0)


def interpolate_missing(frames: np.ndarray) -> np.ndarray:
    """(x, y) with undetected keypoints filled by linear interpolation over time.

    Gaps at the start or end take the nearest detected value. A keypoint never detected
    in the recording stays at (0, 0), so its displacement is zero rather than a jump.
    """
    xy = frames[:, :, :2].copy()
    missing = missing_mask(frames)
    t = np.arange(len(xy))
    for k in range(xy.shape[1]):
        gap, seen = missing[:, k], ~missing[:, k]
        if gap.any() and seen.any():
            for d in range(2):
                xy[gap, k, d] = np.interp(t[gap], t[seen], xy[seen, k, d])
    return xy


def torso_length(frames: np.ndarray) -> float:
    """Median neck to mid-hip distance (px) over the frames where both are detected."""
    missing = missing_mask(frames)
    both = ~(missing[:, NECK] | missing[:, MID_HIP])
    if not both.any():
        return 1.0
    return float(np.median(np.linalg.norm(frames[both, NECK, :2] - frames[both, MID_HIP, :2], axis=1)))


def displacement_windows(records: list[dict], interpolate: bool, scale: bool):
    """Thesis windows (25 steps × 25 keypoints × (dx, dy)) with the S1 and S2 steps switched on or off."""
    X, y, groups = [], [], []
    for r in records:
        xy = interpolate_missing(r["frames"]) if interpolate else r["frames"][:, :, :2]
        if scale:
            xy = xy / torso_length(r["frames"])
        deltas = xy[1:] - xy[:-1]
        for w in range(len(deltas) // rc.WINDOW):
            X.append(deltas[w * rc.WINDOW:(w + 1) * rc.WINDOW])
            y.append(r["label"])
            groups.append(r["rec_id"])
    return np.stack(X), np.array(y), np.array(groups)


def spike_share(X: np.ndarray, threshold: float = 100.0) -> float:
    """Share of windows containing a displacement larger than the threshold (unscaled pixels)."""
    return float((np.abs(X) > threshold).any(axis=(1, 2, 3)).mean())


class KeypointScaler:
    """Standardises each keypoint's dx and dy, pooling mean and SD over all time steps of the training windows.

    Pooling over time keeps every step of a sequence on the same scale, which per-feature
    scaling of the flattened window would not.
    """

    def fit(self, X: np.ndarray) -> "KeypointScaler":
        self.scaler = StandardScaler().fit(X.reshape(-1, X.shape[-2] * X.shape[-1]))
        return self

    def transform(self, X: np.ndarray) -> np.ndarray:
        return self.scaler.transform(X.reshape(-1, X.shape[-2] * X.shape[-1])).reshape(X.shape)
