"""Shared helpers for the re-evaluation notebooks (notebooks/reeval_*.ipynb).

Every model is scored with the same protocol: stratified 5-fold cross-validation over
recordings (all windows or frames of a recording sit on one side of the split), repeated
with three shuffles. Nothing from a test fold is used for training, tuning or early stopping.
"""
from __future__ import annotations

import json
import platform
from collections import Counter
from datetime import date
from pathlib import Path

import matplotlib.pyplot as plt
import numpy as np
import pandas as pd
import sklearn
from matplotlib.colors import LinearSegmentedColormap
from sklearn.metrics import accuracy_score, confusion_matrix, f1_score
from sklearn.model_selection import StratifiedKFold, train_test_split

ROOT = Path(__file__).resolve().parents[1]
DATA_CSV = ROOT / "data" / "keypoint_sequences.csv"
RESULTS_DIR = ROOT / "results" / "reeval"

N_SLOTS, N_KEYPOINTS, N_VALUES = 100, 25, 4  # frame slots, OpenPose BODY_25, (x, y, depth, confidence)
WINDOW = 25  # displacement steps per window, as in the thesis pipeline
N_SPLITS = 5
SEEDS = (0, 1, 2)

# The thesis models merged squat_front and squat_side into one class. This order matches
# sklearn's LabelEncoder and the ExerciseType enum of the real-time system.
CLASS_MERGE = {"squat_front": "squat", "squat_side": "squat"}
CLASSES = ["bent-over_rows", "biceps", "latheral_raises", "rdl", "shoulder_press", "squat"]

# Summary table of the thesis document: one 80/20 split, 67 test windows (the SVM classified
# single frames), on a larger dataset that no longer exists.
THESIS_RESULTS = {"LSTM": 0.93, "Random Forest": 0.84, "2D CNN": 0.66, "SVM": 0.63}

# Chart tokens (light surface), sequential blue ramp for magnitude.
SURFACE, INK, INK_SECONDARY, INK_MUTED, GRID = "#fcfcfb", "#0b0b0b", "#52514e", "#898781", "#e1e0d9"
ACCENT = "#2a78d6"
BLUE_RAMP = ["#fcfcfb", "#cde2fb", "#86b6ef", "#3987e5", "#1c5cab", "#0d366b"]


# ---------------------------------------------------------------- data

def load_recordings(path: Path = DATA_CSV) -> list[dict]:
    """One entry per recording: id, original label, merged label and a (frames, 25, 4) array."""
    df = pd.read_csv(path)
    next_index: dict[str, int] = {}
    records = []
    for row in df.itertuples(index=False):
        source = row[0]
        index = next_index.get(source, 1 if source == "squat_side" else 0)  # squat_side has no 0.json
        next_index[source] = index + 1
        slots = np.asarray(row[1:], dtype=float).reshape(N_SLOTS, N_KEYPOINTS, N_VALUES)
        used = np.flatnonzero(np.abs(slots).reshape(N_SLOTS, -1).sum(axis=1) > 0)
        records.append({
            "rec_id": f"{source}_{index}",
            "source_label": source,
            "label": CLASS_MERGE.get(source, source),
            "frames": slots[: used.max() + 1],  # drop the zero padding after the last frame
        })
    return records


def recordings_table(records: list[dict]) -> pd.DataFrame:
    return pd.DataFrame([
        {"rec_id": r["rec_id"], "source_label": r["source_label"], "label": r["label"], "n_frames": len(r["frames"])}
        for r in records
    ])


def displacement_windows(records: list[dict]):
    """Thesis features: frame-to-frame (dx, dy) per keypoint, cut into non-overlapping 25-step windows.

    Same computation as calculateFrameVectors in the original notebooks and
    FrameManager.CalculateFrameVectors in the C# client. Returns X (windows, 25, 25, 2),
    y (labels) and groups (recording ids).
    """
    X, y, groups = [], [], []
    for r in records:
        xy = r["frames"][:, :, :2]
        deltas = xy[1:] - xy[:-1]
        for w in range(len(deltas) // WINDOW):
            X.append(deltas[w * WINDOW:(w + 1) * WINDOW])
            y.append(r["label"])
            groups.append(r["rec_id"])
    return np.stack(X), np.array(y), np.array(groups)


def single_frames(records: list[dict], with_frame_number: bool = True):
    """Features of the original SVM notebook: raw (x, y, depth, confidence) of one frame, plus its frame number."""
    X, y, groups = [], [], []
    for r in records:
        for i, frame in enumerate(r["frames"]):
            features = frame.reshape(-1)
            if with_frame_number:
                features = np.concatenate([[i + 1], features])
            X.append(features)
            y.append(r["label"])
            groups.append(r["rec_id"])
    return np.stack(X), np.array(y), np.array(groups)


def one_hot(labels) -> np.ndarray:
    return np.eye(len(CLASSES))[[CLASSES.index(label) for label in labels]]


# ---------------------------------------------------------------- splits

def recording_splits(groups: np.ndarray, records: list[dict], n_splits: int = N_SPLITS, seeds=SEEDS):
    """Stratified K-fold over recordings, repeated per seed. Every notebook gets the same folds."""
    ids = np.array([r["rec_id"] for r in records])
    labels = np.array([r["label"] for r in records])
    for seed in seeds:
        skf = StratifiedKFold(n_splits=n_splits, shuffle=True, random_state=seed)
        for fold, (train, test) in enumerate(skf.split(ids, labels)):
            yield seed, fold, np.isin(groups, ids[train]), np.isin(groups, ids[test])


def random_sample_splits(n_samples: int, seeds=SEEDS, test_size: float = 0.2):
    """The original SVM protocol: a random split over individual samples, ignoring recordings."""
    for seed in seeds:
        train, test = train_test_split(np.arange(n_samples), test_size=test_size, random_state=seed)
        train_mask, test_mask = np.zeros(n_samples, bool), np.zeros(n_samples, bool)
        train_mask[train], test_mask[test] = True, True
        yield seed, 0, train_mask, test_mask


# ---------------------------------------------------------------- evaluation

def recording_vote_accuracy(y_true, y_pred, groups) -> float:
    """Accuracy after a majority vote over each recording's predictions, as the real-time system does."""
    hits = []
    for rec in np.unique(groups):
        mask = groups == rec
        vote = Counter(y_pred[mask]).most_common(1)[0][0]
        hits.append(vote == y_true[mask][0])
    return float(np.mean(hits))


def evaluate(fit_predict, X, y, groups, splits, **info) -> dict:
    """Run fit_predict(X_train, y_train, groups_train, X_test, seed) on every split and collect metrics."""
    folds, pooled_true, pooled_pred = [], [], []
    for seed, fold, train, test in splits:
        pred = np.asarray(fit_predict(X[train], y[train], groups[train], X[test], seed * 10 + fold))
        majority = Counter(y[train]).most_common(1)[0][0]
        folds.append({
            "seed": seed,
            "fold": fold,
            "n_train": int(train.sum()),
            "n_test": int(test.sum()),
            "test_recordings": sorted(set(groups[test])),
            "accuracy": float(accuracy_score(y[test], pred)),
            "macro_f1": float(f1_score(y[test], pred, labels=CLASSES, average="macro", zero_division=0)),
            "recording_accuracy": recording_vote_accuracy(y[test], pred, groups[test]),
            "baseline_accuracy": float(np.mean(y[test] == majority)),
        })
        pooled_true.extend(y[test])
        pooled_pred.extend(pred)

    frame = pd.DataFrame(folds)
    summary = {}
    for metric in ["accuracy", "macro_f1", "recording_accuracy", "baseline_accuracy"]:
        summary[f"{metric}_mean"] = float(frame[metric].mean())
        summary[f"{metric}_std"] = float(frame[metric].std(ddof=1)) if len(frame) > 1 else 0.0
    return {
        **info,
        "classes": CLASSES,
        "n_samples": int(len(y)),
        "n_recordings": int(len(np.unique(groups))),
        "n_splits_run": len(folds),
        "summary": summary,
        "confusion_matrix": confusion_matrix(pooled_true, pooled_pred, labels=CLASSES).tolist(),
        "folds": folds,
        "environment": {"python": platform.python_version(), "numpy": np.__version__, "sklearn": sklearn.__version__},
        "run_date": date.today().isoformat(),
    }


def save_results(result: dict, name: str) -> Path:
    RESULTS_DIR.mkdir(parents=True, exist_ok=True)
    path = RESULTS_DIR / f"{name}.json"
    path.write_text(json.dumps(result, indent=2))
    return path


def load_results() -> dict[str, dict]:
    return {p.stem: json.loads(p.read_text()) for p in sorted(RESULTS_DIR.glob("*.json"))}


def summary_table(results: list[dict]) -> pd.DataFrame:
    rows = []
    for r in results:
        s = r["summary"]
        rows.append({
            "model": r["model"],
            "variant": r["variant"],
            "split": r["split"],
            "samples": r["n_samples"],
            "splits run": r["n_splits_run"],
            "accuracy": f"{s['accuracy_mean']:.1%} ± {s['accuracy_std']:.1%}",
            "macro F1": f"{s['macro_f1_mean']:.1%} ± {s['macro_f1_std']:.1%}",
            "recording vote": f"{s['recording_accuracy_mean']:.1%} ± {s['recording_accuracy_std']:.1%}",
            "majority baseline": f"{s['baseline_accuracy_mean']:.1%}",
        })
    return pd.DataFrame(rows)


# ---------------------------------------------------------------- charts

def _style(ax):
    ax.set_facecolor(SURFACE)
    for side in ["top", "right", "left"]:
        ax.spines[side].set_visible(False)
    ax.spines["bottom"].set_color(GRID)
    ax.tick_params(colors=INK_MUTED, length=0)


def plot_confusion(result: dict, title: str, filename: str):
    """Row-normalised confusion matrix pooled over all folds, sequential blue."""
    cm = np.array(result["confusion_matrix"], dtype=float)
    share = cm / cm.sum(axis=1, keepdims=True)
    fig, ax = plt.subplots(figsize=(7, 5.6), facecolor=SURFACE)
    cmap = LinearSegmentedColormap.from_list("blue_ramp", BLUE_RAMP)
    ax.pcolormesh(share, cmap=cmap, vmin=0, vmax=1, edgecolors=SURFACE, linewidth=2)
    for i in range(len(CLASSES)):
        for j in range(len(CLASSES)):
            if cm[i, j] == 0:
                continue
            ax.text(j + 0.5, i + 0.5, f"{share[i, j]:.0%}", ha="center", va="center", fontsize=9,
                    color="#ffffff" if share[i, j] > 0.55 else INK)
    ax.set_xticks(np.arange(len(CLASSES)) + 0.5, CLASSES, rotation=35, ha="right")
    ax.set_yticks(np.arange(len(CLASSES)) + 0.5, CLASSES)
    ax.invert_yaxis()
    ax.set_xlabel("Predicted", color=INK_SECONDARY)
    ax.set_ylabel("True", color=INK_SECONDARY)
    _style(ax)
    ax.spines["bottom"].set_visible(False)
    ax.set_title(title, loc="left", color=INK, fontsize=11)
    fig.tight_layout()
    RESULTS_DIR.mkdir(parents=True, exist_ok=True)
    fig.savefig(RESULTS_DIR / filename, dpi=160, facecolor=SURFACE)
    return fig
