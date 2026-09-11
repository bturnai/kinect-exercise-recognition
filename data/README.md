# Data

Pose keypoints extracted with OpenPose (BODY_25 model, 25 keypoints; order as in
[`OpenposeJointType.cs`](../realtime/DepthBasics-WPF/Models/OpenposeJointType.cs)).
Every 4th Kinect frame was processed (30 fps → 7.5 fps).

## `keypoint_sequences.csv`

One row per recording: **41 recordings, 7 classes**.

| Class | Recordings |
|---|---:|
| bent-over_rows | 6 |
| biceps | 6 |
| latheral_raises | 6 |
| rdl | 6 |
| shoulder_press | 6 |
| squat_front | 6 |
| squat_side | 5 |

- Columns: `video_name` (class label), then `feature_1` … `feature_10000`.
- Layout: 100 frame slots × 25 keypoints × 4 values `(x, y, depth, confidence)`.
  Recordings have 75–76 frames; the remaining slots are zero-padded.
- `x`, `y`: pixel coordinates in the 640×480 colour frame. `x = y = 0` means the keypoint was not detected.
- `depth`: Kinect v1 depth (mm), read from the depth image at the colour-pixel position
  without depth–colour registration. `0` means no reading.
- Row order within a class matches the original recording index (`0.json`, `1.json`, …; `squat_side` has no `0.json` and starts at `1.json`).

## `extra_recordings/`

7 recordings, one per class, 125–150 frames each. They do not overlap with
`keypoint_sequences.csv`. Format:

```json
{"video_name": "biceps", "frames": [{"frame_number": 1, "keypoints": [{"x": 0.0, "y": 0.0, "depth": 0.0}]}]}
```

No confidence values. `notebooks/lstm.ipynb` uses these as an additional test set.

## What is not here

The original per-recording folder (`frames/`) is lost. It later also held hand-picked
single-person clips from the Kaggle dataset
[Workout/Exercises Video](https://www.kaggle.com/datasets/hasyimabdillah/workoutfitness-video)
(CC BY-NC-SA 4.0). The Random Forest, LSTM and 2D CNN notebooks were trained on that
larger folder (284 windows, 6 classes), so their results cannot be reproduced from the
files above.

## Participants and limitations

- The Kinect recordings show three people: the author and two friends, who agreed to publication.
- Subject IDs were not stored, so it is unknown which recording belongs to whom.
  Evaluation can be grouped by recording but not by person.
- All files here contain Kinect depth values, so they appear to be Kinect recordings rather than Kaggle clips.
- The origin of `extra_recordings/` (who, when) is unknown.

## License

Released under the repository's [MIT License](../LICENSE).
