# Speed Control

A mod for Slay the Spire 2 that independently controls game speed, card animations, committed card resolution, and combat presentation.

## Features

- Adds a speed control page to the in-game mod settings
- Supports 0.1x-10x speed
- Works on top of the game's built-in Fast Mode (Normal / Fast / Instant)
- Speed setting is persisted across game sessions
- Hit stop effects are preserved proportionally at higher speeds
- Card animation acceleration can be combined with either global mode
- Card resolution acceleration shortens pauses between a played card's automatic effects without changing overall game speed
- Combat presentation acceleration locally shortens turn banners, enemy intent presentation, paired presentation waits, damage and healing numbers, and blocked text

## How It Works

- **Game speed** changes the pace of the whole game, either continuously or only during selected automatic flows.
- **Card animation speed** affects supported card movement, flying, shuffling, exhaust effects, and hand arrangement.
- **Card resolution speed** shortens pauses between the automatic effects of a card that has already been played.
- **Combat presentation speed** affects battle-start and turn banners, enemy intent presentation, brief presentation pauses, damage and healing numbers, and blocked text.

Each speed control can be used on its own or combined with the others. No card effect, enemy action, player choice, or completion step is skipped or reordered.

## Installation

Place the mod DLL and `mod_manifest.json` in the Slay the Spire 2 mods folder.
