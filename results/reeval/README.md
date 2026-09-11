# Re-evaluation results

Output of `notebooks/reeval_*.ipynb` (run on 2026-09-11). The thesis notebooks
(`notebooks/lstm.ipynb`, `random_forest.ipynb`, `cnn_2d.ipynb`, `svm.ipynb`) are unchanged.

## Protocol

- Data: `data/keypoint_sequences.csv`, the 41 Kinect recordings that survived (121 windows).
- 6 classes, as in the thesis models (`squat_front` and `squat_side` merged into `squat`).
- Stratified 5-fold cross-validation over recordings, repeated with 3 shuffles (15 splits).
  All windows of a recording stay on one side of each split. Test folds are never used
  for training, hyperparameter search or early stopping.
- Features and model settings are the same as in the thesis notebooks. Deviations are
  listed at the top of each notebook.

## Results

Window accuracy, mean ± 1 SD across splits. Majority-class baseline: 27%.

| Model | Re-evaluation | Thesis (one split, lost data) |
|---|---|---|
| LSTM | 77.3% ± 10.9% | 92.5% |
| 2D CNN | 71.0% ± 11.2% | 68.7% |
| Random Forest | 61.2% ± 7.9% | 88.1% |
| SVM, single frames, recording-level split (B) | 58.7% ± 11.4% | – |
| SVM, windows, recording-level split (C) | 38.4% ± 5.1% | – |
| SVM, single frames, random frame split (A, leaky) | 97.6% ± 0.8% | 96% |

The findings and limitations are in `notebooks/reeval_summary.ipynb`.

## Files

| File | Content |
|---|---|
| `*.json` | Per-split metrics, pooled confusion matrix, settings and library versions |
| `*_confusion.png` | Row-normalised confusion matrices |
| `model_comparison.png` | Accuracy per model with the spread across splits |

## Reproduce

```bash
python3.11 -m venv .venv
.venv/bin/pip install -r requirements-reeval.txt
.venv/bin/jupyter nbconvert --to notebook --execute --inplace notebooks/reeval_random_forest.ipynb notebooks/reeval_svm.ipynb notebooks/reeval_lstm.ipynb notebooks/reeval_cnn_2d.ipynb notebooks/reeval_summary.ipynb
```

Runs on CPU in about six minutes (Apple M2). Neural-network results can differ slightly
between machines.
