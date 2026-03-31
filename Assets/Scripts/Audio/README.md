# Sound System

## Overview
`SoundManager` is the public master class used by gameplay code. It delegates work to internal classes:
- `SfxController`: plays one-shot SFX through an `AudioSource` pool.
- `MusicController`: controls menu music through an FMOD `EventInstance`.
- `BusController`: applies FMOD bus volumes (`bus:/Music`, `bus:/SFX`).
- `AudioCatalog`: optional ScriptableObject mapping `SoundType` to clips (and FMOD event references for future use).

Gameplay scripts should call only:
- `SoundManager.Play3D(...)`
- `SoundManager.Play2D(...)`
- `SoundManager.SetMusicVolume(...)`
- `SoundManager.SetSfxVolume(...)`

## Scripts
- `SoundManager.cs`
  - Singleton, creates controllers
  - Holds inspector fields for pool settings, menu music event, and bus paths.
- `SfxController.cs`
  - Builds pooled `AudioSource` children under `SoundManager`.
  - Chooses clips from `AudioCatalog` first, then legacy `SoundList[]` fallback.
  - Handles 3D/2D one-shot playback.
- `MusicController.cs`
  - Starts menu music when active scene name matches `menuSceneName`.
    - Will be extended in the future for general music control
  - Stops with fade when leaving menu.
  - Releases FMOD instance on manager destroy.
- `BusController.cs`
  - Lazy-resolves FMOD buses once.
  - Clamps and sets music/sfx volumes.
- `AudioCatalog.cs`
  - Data asset: `SoundType -> AudioClip[]` (current playback path).
  - Includes `EventReference` field per entry (not used by `SfxController` yet).
  - Currently we have one audio catalog that I'm planning to hold all our audio clips, but potentially this will be broken up later

## Initial Setup
1. Ensure one `SoundManager` object exists at startup.
2. In `SoundManager` inspector:
   - Assign `Audio Catalog` (recommended).
   - Set `Main Menu Music` event (`menuMusicEvent`).
   - Verify `menuSceneName` matches your menu scene name exactly.
   - Confirm bus paths (`bus:/Music`, `bus:/SFX`) match your FMOD project.
3. Keep an FMOD listener in active scenes (for example on active camera/player).

## Adding a New Sound (SFX)
1. Add a new enum value to `SoundType` in `SoundManager.cs`.
2. Open your `AudioCatalog` asset and add an entry:
   - `soundType`: new enum value
   - `clips`: one or more `AudioClip`s
   - `fmodEvent`: optional (stored for future FMOD SFX migration)
3. Trigger it from gameplay:
   - 3D: `SoundManager.Play3D(SoundType.YourNewSound, worldPos, volume, minDistance, maxDistance);`
   - 2D: `SoundManager.Play2D(SoundType.YourNewSound, volume);`

Note:
- If `MainAudioCatalog` is missing or an entry has no clips, system falls back to legacy `SoundList[]`.

## Menu Music
1. Create/choose your FMOD music event.
2. Assign it to `menuMusicEvent` on `SoundManager`.
3. Ensure `menuSceneName` matches the menu scene.
4. Enter menu scene: music starts automatically.
5. Leave menu scene: music stops with fadeout.

## Common Issues
- No SFX plays:
  - Missing `SoundManager` instance in scene.
  - `AudioCatalog` entry missing clips for that `SoundType`.
  - Unity audio disabled while SFX are still clip-based.
- Menu music does not play:
  - `menuMusicEvent` not assigned.
  - FMOD banks not built/imported.
  - Scene name mismatch with `menuSceneName`.