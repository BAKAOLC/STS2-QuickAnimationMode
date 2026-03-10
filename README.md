# Speed Control

A mod for Slay the Spire 2 that adds a customizable game speed multiplier to the settings screen.

## Features

- Adds a "Speed Multiplier" paginator control in Settings > General, below the Fast Mode toggle
- Supports 1x, 1.5x, 2x, 3x, 4x, 5x, 8x, 10x speed options
- Works on top of the game's built-in Fast Mode (Normal / Fast / Instant)
- Speed setting is persisted across game sessions
- Hit stop effects are preserved proportionally at higher speeds

## How It Works

The mod uses Godot's `Engine.TimeScale` to globally accelerate all game animations, timers, and tweens. This is the same mechanism the game's debug mode uses internally (clamped to 0.1-4.0x), but the mod extends it up to 10x.

## Installation

Place the mod DLL and `mod_manifest.json` in the Slay the Spire 2 mods folder.
