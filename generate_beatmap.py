import librosa
import numpy as np
import json
import random
import os

audio_path = "assets/audio/MinigameDia0.mp3"
output_path = "assets/data/beatmap.json"

print(f"Loading audio from {audio_path}...")
y, sr = librosa.load(audio_path, sr=None)

print("Extracting tempo, beats and energy...")
tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)
beat_times = librosa.frames_to_time(beat_frames, sr=sr)

# Calculate RMS energy for each frame
rms = librosa.feature.rms(y=y)[0]
energy_threshold = np.percentile(rms, 70) # 70th percentile for "high energy"

spaced_beat_times = []
for i, b_time in enumerate(beat_times):
    frame_idx = librosa.time_to_frames(b_time, sr=sr)
    beat_energy = rms[min(frame_idx, len(rms)-1)]
    
    is_high_energy = beat_energy >= energy_threshold
    
    if is_high_energy:
        if i % 2 == 0:
            spaced_beat_times.append(b_time)
    else:
        if i % 4 == 0:
            spaced_beat_times.append(b_time)

print(f"Extracted {len(spaced_beat_times)} dynamic beats out of {len(beat_times)} total beats.")

bpm = float(tempo[0]) if isinstance(tempo, (list, tuple, np.ndarray)) else float(tempo)
print(f"Estimated BPM: {bpm}")

food_types = ["bread", "cookie", "donut"]

notes = []
last_position = -1

for time in spaced_beat_times:
    f_type = random.choice(food_types)
    
    # Avoid picking the exact same position twice in a row
    pos = random.randint(0, 39)
    while pos == last_position:
        pos = random.randint(0, 39)
    last_position = pos
    
    notes.append({
        "time": float(round(time, 3)),
        "type": f_type,
        "position": pos
    })

beatmap = {
    "song_name": "MinigameDia0",
    "bpm": bpm,
    "offset_seconds": 0.0,
    "timeout_seconds": 3.0,
    "notes": notes
}

os.makedirs(os.path.dirname(output_path), exist_ok=True)
with open(output_path, "w", encoding="utf-8") as f:
    json.dump(beatmap, f, indent=4)

print(f"Successfully wrote {len(notes)} notes to {output_path}")
