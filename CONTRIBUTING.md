# Contributing to osu! lite

## Reporting bugs

Please open an issue at https://github.com/waspflannel/osu-lite/issues/new/choose. Include:
- Steps to reproduce
- Log files (available from the General settings section or storage directory)
- Your platform (Windows/macOS/Linux)

## Submitting pull requests

1. Fork the repository.
2. Create a topic branch from `master`.
3. Make your changes, keeping commits focused and descriptive.
4. Build with `dotnet build osu.Desktop -c Debug` and verify zero errors and warnings.
5. Run a manual smoke check for the path you changed. At minimum, launch with a fresh profile; import a valid local `.osz` when changing beatmap, storage, skin, or gameplay code.
6. Open a pull request against `master`.

## Code conventions

- Follow the existing code style in the repository.
- Keep the scope narrow — this is a focused local desktop player.
- Do not reintroduce online/API/mod/editor/skin-selection functionality.
- Remove unused code rather than commenting it out.
- Check [final-trim.md](final-trim.md) before changing a retained boundary; it records the still-binding product contract and known incomplete trim clusters.

## Scope

osu! lite is intentionally narrow. Feature additions that expand the product scope beyond the [README](README.md) are unlikely to be accepted without prior discussion.
