# Project Post-Jam Stability Patch (v1.0.2)

This repository contains the post-jam stability update (**v1.0.2**) for the game jam submission.

### Important Note for Jam Organizers & Judges

This update is **strictly a bug-fix and stabilization patch**. No new content, features, or gameplay mechanics have been introduced during the non-competitive period. All modifications were made solely to resolve critical workflow blockers and polish existing implementations.

For full transparency, the original, untouched jam submission source code is available for compliance review here:
👉 https://github.com/Ivan-Mikov/Ludum-Dare-60/tree/v1.0-jam-submission

And build is available here:
👉 https://github.com/Ivan-Mikov/Ludum-Dare-60/releases/tag/v1.0

---

## Changelog & Bug Fixes (v1.0.2)

### 1. Game Flow & Progression Fixes

- **Fixed Hard Mode Softlock:** Resolved a critical progression blocker on Stage 1 (Waves 8-9) where the game loop would softlock, preventing players from advancing.

---

<details>
<summary><b>View Previous Changelog (v1.0.1)</b></summary>

## Changelog & Bug Fixes (v1.0.1)

The fixes are prioritized by their severity, starting with critical progression blockers:

### 1. Critical Core System Fixes

- **Fixed Scene Management Transition Bug:** Resolved a critical issue where the game failed to transition to the next Stage upon completing all waves. Game loop progression is now fully functional.
- **Fixed Post-Defeat Loop Routine:** Fixed a bug where losing a wave incorrectly forced the player back to the Main Menu. Pressing 'R' now properly restarts the current stage immediately, as originally intended.
- **Disabled Debug Commands:** Completely removed the debug stage-skip functionality (previously mapped to the `N` key) to ensure legitimate gameplay flow during judging.

### 2. Main Menu & Gameplay Restrictions

- **Restricted Main Menu Inputs:** Fixed an issue where the player character could input movement (WASD/Arrows) and fire projectiles while the Main Menu UI panel was active. Input handling is now properly suspended outside active gameplay.

### 3. Tutorial Scene Overhaul & Script Corrections

- **Fixed Tutorial Enemy Behavior:** Corrected AI and physics state bugs in the tutorial level. Phantom enemies are now properly initialized as static, non-lethal targets that do not interact with or damage the player.
- **Fixed UI Instruction Order:** Resolved a race condition causing text prompts in the tutorial to disappear or trigger out of sequence. Text routines now follow a strict logical order.
- **Localization Polish:** Replaced all leftover placeholder Russian text strings with full English localization to ensure accessibility for global judges.
- **Audio Integration:** Fixed a missing reference bug that prevented the audio system from playing the designated background track (`Normal Music`) during the tutorial sequence.

### 4. Technical Assets & Polish

- **Fixed Hardware Cursor Import Configuration:** Resolved a platform import issue where the custom 64x64 pixel art cursor texture lacked CPU read/write access permissions. The software crosshair is now correctly rendered via hardware API (`CursorMode.Auto`).

</details>

---

## Technical Specifications

- **Engine Version:** Unity (6000.3.11f1)
- **Input System:** Keyboard & Mouse (WASD / Arrows to move, LMB to shoot)
- **Current Version:** v1.0.2 (Patch)
