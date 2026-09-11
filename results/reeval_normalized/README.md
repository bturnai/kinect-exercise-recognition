# Re-evaluation with normalised features

Output of `notebooks/reeval_normalized_*.ipynb` (run on 2026-09-11). Same data, classes,
15 recording-level splits and model settings as `results/reeval/`. Only the input
preprocessing changes, so each step's effect can be read off directly.

## Preprocessing steps

Cumulative, implemented in `src/reeval_normalized.py`:

| Step | What changes |
|---|---|
| S0 | Thesis features, unchanged (results from `results/reeval/`) |
| S1 | Undetected keypoints (x = y = 0) filled by linear interpolation over time |
| S2 | S1, then coordinates divided by the recording's median torso length (neck to mid-hip) |
| S3 | S2, then each keypoint's dx and dy standardised with training-fold statistics (neural networks only) |

## Results

Window accuracy, mean ± 1 SD across the 15 splits. Majority-class baseline: 27%.

| Model | S0 | S1 | S2 | S3 |
|---|---|---|---|---|
| 2D CNN | 71.0% ± 11.2% | 84.5% ± 12.4% | 89.3% ± 8.7% | **95.1% ± 5.8%** |
| LSTM | 77.3% ± 10.9% | **91.4% ± 6.4%** | 45.6% ± 7.9% | 91.3% ± 7.5% |
| Random Forest | 61.2% ± 7.9% | **62.9% ± 7.4%** | 61.7% ± 8.4% | – |
| SVM (windows) | 38.4% ± 5.1% | 47.9% ± 7.6% | **48.2% ± 7.2%** | – |

Best step against S0, on identical splits:

| Model | Best step | Change | Better / equal / worse splits | Per-recording vote |
|---|---|---|---|---|
| 2D CNN | S3 | +24.1 points | 15 / 0 / 0 | 97.6% |
| LSTM | S1 | +14.1 points | 14 / 1 / 0 | 93.4% |
| SVM (windows) | S2 | +9.8 points | 10 / 3 / 2 | 47.2% |
| Random Forest | S1 | +1.7 points | 7 / 5 / 3 | 69.4% |

## Findings

- Interpolating undetected joints is the step that matters (LSTM +14, CNN +14, SVM +10 points).
- Torso-length scaling adds nothing on its own: torso lengths only range from 93 to 117 px.
  Without standardisation it breaks the LSTM (45.6%). Standardising fixes it.
- With all steps the 2D CNN is best (95.1%). It leads the LSTM at S3 in 8 splits, ties
  in 6 and trails in 1: consistent, but not confirmable on 41 recordings.
- The remaining errors sit in bent-over rows.

The full findings and limitations are in `notebooks/reeval_normalized_summary.ipynb`.
The most important limitation: there are no subject IDs, and the steps were chosen after
looking at errors on these same recordings. These numbers describe an improved pipeline,
not the thesis. The real-time system would need the same preprocessing in the C# client
to use it.

## Files

| File | Content |
|---|---|
| `<model>_S<step>.json` | Per-split metrics, pooled confusion matrix, settings, library versions |
| `<model>_S<step>_confusion.png` | Confusion matrix of each model's last step |
| `normalization_steps.png` | Accuracy per model and step |

## Reproduce

Run the thesis-feature notebooks first (see `results/reeval/README.md`), then:

```bash
.venv/bin/jupyter nbconvert --to notebook --execute --inplace notebooks/reeval_normalized_random_forest.ipynb notebooks/reeval_normalized_svm.ipynb notebooks/reeval_normalized_lstm.ipynb notebooks/reeval_normalized_cnn_2d.ipynb notebooks/reeval_normalized_summary.ipynb
```
