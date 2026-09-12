# Kinect Exercise Recognition

Real-time recognition of gym exercises from Kinect video: OpenPose keypoints, frame-to-frame
displacement features and an LSTM classifier, wrapped in a C# client and two Python servers.
BSc thesis project, Óbuda University, 2025.

![Python](https://img.shields.io/badge/Python-3.7%20%7C%203.11-3776AB?style=flat-square&logo=python&logoColor=white)
![TensorFlow](https://img.shields.io/badge/TensorFlow-Keras-FF6F00?style=flat-square&logo=tensorflow&logoColor=white)
![scikit-learn](https://img.shields.io/badge/scikit--learn-ML-F7931E?style=flat-square&logo=scikitlearn&logoColor=white)
![OpenPose](https://img.shields.io/badge/OpenPose-CMU-blueviolet?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=flat-square&logo=windows&logoColor=white)
![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

The system classifies six exercises: bent-over row, biceps curl, lateral raise, Romanian
deadlift, overhead press and squat. After a recording it also gives rule-based form feedback
(movement speed, arm width, hip angle).

## How it works

```
Kinect v1 (640x480 RGB + depth, 30 fps)
        |
        v  every 4th frame
 +--------------------------+
 |  OpenPose server         |  Python, TCP :1111
 |  25 BODY_25 keypoints    |
 +-----------+--------------+
             |  JSON: [x, y, confidence] per frame
             v
 +--------------------------+
 |  C# client               |
 |  displacement vectors    |  (dx, dy) between consecutive frames, per keypoint
 |  25-step windows         |  ~3 seconds of movement
 +-----------+--------------+
             |  JSON: windows
             v
 +--------------------------+
 |  LSTM classifier server  |  Python, TCP :2222
 |  majority vote over the  |
 |  windows of a recording  |
 +-----------+--------------+
             |
             v
   WPF interface + rule-based form feedback
```

The same feature computation exists twice, once in Python for training and once in C# for live
use (`FrameManager.CalculateFrameVectors`), and the class order of the `ExerciseType` enum matches
the label encoder, so training and serving stay in step.

## Results

Three evaluations are documented here. They answer different questions, so the numbers are not
interchangeable.

### 1. Thesis evaluation

From the summary table of the thesis: one 80/20 train/test split, 67 test windows. LSTM, Random
Forest and the 2D CNN were tested on windows from recordings held out from training; the SVM
classified single frames.

| Model | Accuracy | F1 |
|---|:---:|:---:|
| **LSTM** (chosen for the live system) | **93%** | **0.91** |
| Random Forest | 84% | 0.84 |
| 2D CNN | 66% | 0.62 |
| SVM (single frames) | 63% | 0.63 |

That dataset, which also contained hand-picked clips from a public Kaggle set, was lost with the
machine it lived on. These numbers cannot be reproduced from this repository.

### 2. Re-evaluation on the surviving data

The 41 Kinect recordings that survived (121 windows), with the thesis features and model settings
unchanged, scored with stratified 5-fold cross-validation over recordings, repeated with 3 shuffles.
No test fold is used for training or tuning. Majority-class baseline: 27%.

| Model | Window accuracy |
|---|:---:|
| LSTM | 77.3% ± 10.9% |
| 2D CNN | 71.0% ± 11.2% |
| Random Forest | 61.2% ± 7.9% |
| SVM (single frames) | 58.7% ± 11.4% |
| SVM (windows) | 38.4% ± 5.1% |

Details: [`results/reeval/README.md`](results/reeval/README.md).

An earlier notebook had reported 96% for an SVM. That run split frames of the same recordings
randomly between training and test, so nearly identical neighbouring frames sat on both sides.
Repeating that protocol here reproduces the effect (97.6%), which is why the number is not used.

### 3. With normalised features

Same data and same splits, with the preprocessing the thesis pipeline lacked.

| Model | Thesis features | Best preprocessing | Change |
|---|:---:|:---:|:---:|
| 2D CNN | 71.0% | **95.1% ± 5.8%** (interpolation + scaling + standardisation) | +24 points, better in 15/15 splits |
| LSTM | 77.3% | **91.4% ± 6.4%** (interpolation) | +14 points, better in 14/15 splits |
| Random Forest | 61.2% | 62.9% | +2 points |
| SVM (windows) | 38.4% | 48.2% | +10 points |

Filling in undetected joints accounts for most of the gain: OpenPose writes (0, 0) when it loses a
joint, which turned 45% of the windows into fake 100-pixel jumps. Details:
[`results/reeval_normalized/README.md`](results/reeval_normalized/README.md).

## Key decisions

**Displacement vectors instead of raw coordinates.** The same squat gives different coordinates
depending on where the person stands, so the models work on the (dx, dy) movement of each keypoint
between consecutive frames. This removes the dependence on position in the frame, though not on
body size or distance from the sensor.

**Depth left out of the features.** Kinect v1 depth is noisy beyond 1.8 m and worst on the lower
body, and this pipeline reads the depth image at colour-pixel coordinates without registering the
two cameras. An RGB-only feature set was the more predictable choice. Depth is still stored in the
data files.

**Majority vote per recording.** A single window can be ambiguous, so the classifier server votes
over all windows of a recording. In the re-evaluation this adds 2-5 points over window accuracy.

**Rule-based side detection.** Whether the user stands left or right of the camera is decided from
the average horizontal offset between neck and mid-hip. An earlier K-means version was removed:
cluster labels are arbitrary, so its output did not reliably encode direction.

## Data

41 Kinect recordings of three people (the author and two friends), 75-76 frames each at 7.5 fps,
25 OpenPose BODY_25 keypoints with x, y, depth and confidence, plus 7 further recordings in a
different format. Subject identifiers were never stored.

See [`data/README.md`](data/README.md) for the file formats, what was lost and the licence of the
external [Kaggle workout video dataset](https://www.kaggle.com/datasets/hasyimabdillah/workoutfitness-video)
(CC BY-NC-SA 4.0) that the lost dataset drew on. No video from it is redistributed here.

## Repository layout

```
kinect-exercise-recognition/
├── realtime/
│   ├── DepthBasics-WPF/          # C# client: Kinect stream, feature building, form feedback
│   └── python/
│       ├── server.py             # OpenPose keypoint server (TCP :1111), starts the classifier too
│       └── classifier_server.py  # LSTM classifier (TCP :2222)
├── notebooks/
│   ├── lstm.ipynb                # thesis notebooks, unchanged
│   ├── random_forest.ipynb
│   ├── cnn_2d.ipynb
│   ├── svm.ipynb
│   ├── reeval_*.ipynb            # re-evaluation with the thesis features
│   └── reeval_normalized_*.ipynb # re-evaluation with normalised features
├── src/
│   ├── reeval_common.py          # data loading, recording-level splits, metrics, charts
│   └── reeval_normalized.py      # interpolation, torso-length scaling, standardisation
├── models/
│   ├── lstm/                     # the model the live system loads (TensorFlow SavedModel)
│   └── lstm_reeval.keras         # LSTM trained on all 41 surviving recordings
├── results/reeval/               # per-split metrics, confusion matrices, model comparison
├── results/reeval_normalized/    # the same with normalised features
├── data/                         # surviving keypoint data
├── requirements.txt              # thesis environment (Python 3.7)
└── requirements-reeval.txt       # re-evaluation environment (Python 3.11)
```

## Running it

**The live system cannot be run today.** It needs a Microsoft Kinect v1 (discontinued in 2015),
the Kinect SDK 1.8 on Windows, and a working OpenPose build. No recording of it in action exists.

**The notebooks run anywhere**, on CPU, in about six minutes:

```bash
python3.11 -m venv .venv
.venv/bin/pip install -r requirements-reeval.txt
.venv/bin/jupyter nbconvert --to notebook --execute --inplace notebooks/reeval_*.ipynb
```

## Limitations

- **No subject identifiers.** Recordings of the same person can fall on both sides of a split, so
  the scores describe recognition on new recordings, not on new people.
- **Small dataset.** 41 recordings and 121 windows; fold accuracies range from 58% to 100%, so
  differences of a few points mean little.
- **The thesis numbers come from a single split** whose test set was also used as validation data
  during training, on data that no longer exists.
- **The normalisation steps were chosen after looking at the errors** on these same recordings.
  Confirming the 95% would need recordings that played no part in that choice.
- Bent-over rows and Romanian deadlifts remain the pair the models confuse most.

## Thesis archive

The original, unchanged thesis repository: [bturnai/ProjektMunka](https://github.com/bturnai/ProjektMunka).
This repository is the cleaned-up version: the committed virtualenv, dead experiments and
intermediate data files were left out, and the re-evaluation was added.

## License

Code under the [MIT License](LICENSE). The keypoint data was recorded by the author with the
participants' consent.

**Bálint Turnai** · Óbuda University, John von Neumann Faculty of Informatics · Thesis NIK-CMJ194, 2024/25
