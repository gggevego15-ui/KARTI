# KARTI 1.0.2 — First Windows Liquid Glass release

## Highlights
- Custom WPF setup window; no stock NSIS wizard is shown to the user.
- Layered translucent glass surfaces, soft blur, turquoise and violet glow.
- Accent glow samples the current Windows desktop wallpaper and blends it into the glass palette. If wallpaper sampling is unavailable, the default palette remains.
- Light/dark theme toggle, install/location/about tabs, editable install path and folder picker.
- Runs the real embedded KARTI Electron installer, reports failures, verifies the installed executable, and offers to launch KARTI.

## Downloads
The Windows installer and Android APK are produced by the GitHub Actions workflow. The Windows wrapper is published as the `KARTI-Liquid-Glass-Setup.exe` artifact. A signed Android release APK is only produced when the repository signing secrets are configured; otherwise the workflow produces a debug APK.

## Verification notes
A successful CI build verifies compilation and packaging, not a full interactive Windows installation test. Before calling the installer fully verified, launch the EXE on Windows and confirm installation to a custom path, shortcuts, relaunch, and uninstall behavior.
