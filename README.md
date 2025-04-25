# Simple Dynamic 2D Avatar

## Overview

This application creates a dynamic 2D avatar for streamers using a base image and a corresponding depth map. It renders the avatar with reactions to configurable lighting and supports basic "Move" effect animations (offset/scale) triggered via hotkeys. A transparent preview window allows easy integration with streaming software like OBS.

## Features

* **Profile Management:** Create and manage multiple avatar profiles, each with its own images, lighting, and animations.
* **Dynamic Lighting (CPU/GDI+):**
    * Supports Ambient, Point, Directional, and Tint light types.
    * Calculates diffuse and specular (Blinn-Phong) lighting based on normals derived from the depth map.
    * Global and per-profile light sources can be configured via the Light Editor.
* **Animation System ("Move" Effects):**
    * Define simple offset and scale animations over time using keyframes.
    * Supports Linear and EaseInOutQuad interpolation between keyframes.
    * Animations can be set to loop indefinitely or play once.
    * Edit timelines and keyframes using the Animation Editor.
* **Hotkey Control:**
    * Assign global hotkeys to show/hide the preview window.
    * Assign hotkeys to activate specific profiles.
    * Assign hotkeys to trigger specific animations within the active profile.
    * Pressing the hotkey for a currently looping animation stops it.
* **Transparent Preview Window:** Uses the Windows layered window API for transparency, compatible with streaming software.
* **Performance Caching:**
    * Pre-calculates unlit geometry (color, position, normal) for each animation frame (`_unlitGeometryCache`).
    * Pre-calculates fully lit frames (`_litFrameCache`) by applying current lighting to the unlit geometry, regenerated when lights change.
* **System Tray Integration:** The application can run minimized in the system tray.

## Technology Stack

* **Language:** C#
* **Framework:** .NET Framework 4.7.2
* **UI:** Windows Forms (WinForms)
* **Dependencies:**
    * Newtonsoft.Json (for settings serialization)

## Setup & Configuration

1.  **Image Requirements:** You need pairs of images for each avatar:
    * A base color image with transparency (e.g., PNG).
    * A corresponding depth map image (grayscale, where white is typically closest and black is furthest). Both images **must** have the same dimensions.
2.  **Run the Application:** Launch the executable.
3.  **Profile Manager:** Use the "Avatar Setup" / Profile Manager form to:
    * Create new profiles.
    * Assign a base image and depth map path to each profile.
    * Optionally assign a hotkey to activate the profile.
    * Configure profile-specific settings like Depth Scale, Specular Intensity/Power.
4.  **Light Editor:** Use the "Edit Lights" button to access the Light Editor and configure global and profile-specific lights.
5.  **Animation Editor:** Use the "Edit Current Profile" button, then the "Animations..." button to access the Animation Editor to create/edit animation timelines, keyframes, set looping behavior, and assign animation hotkeys.
6.  **Settings Storage:** Configuration is saved in `%AppData%\SimpleDynamicAvatar\settings.json`.

## Usage

* **Control Panel:** The main window allows selecting the active avatar profile, manually playing animations (or stopping the current loop), accessing setup/editors, and showing/hiding the preview window.
* **Preview Window:** Displays the rendered avatar. It's transparent and can be repositioned by clicking and dragging. Its position is saved.
* **Hotkeys:** Use assigned hotkeys to switch profiles, trigger animations, or toggle the preview window visibility.
* **System Tray:** The application can run in the background via the system tray icon, providing quick access to show/hide functions and exit.

## Caching Mechanism

To improve performance, especially during animation playback with dynamic lighting, the application uses a two-stage caching process:

1.  **Unlit Geometry Cache:** When a profile is loaded or animations are edited, the application pre-calculates the base color, world position, and world normal for every pixel of every frame of every animation timeline for that profile. This data is stored in memory.
2.  **Lit Frame Cache:** When an animation is played for the first time, or when lighting settings change, the application takes the corresponding unlit geometry data and applies the current lighting configuration (global + active profile lights) to generate the final rendered `Bitmap` objects for each frame. These lit bitmaps are cached. Subsequent plays of the same animation (with the same lighting) use the cached bitmaps directly for smooth playback.

Cache invalidation occurs automatically when profiles are edited, animations are changed, or lighting settings are modified.