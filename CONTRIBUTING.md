# Contributing

The native rewrite uses C# and WPF.

## Setup

Install the .NET 8 SDK.

\`\`\`powershell
dotnet restore FNFAssetPrep.sln
dotnet run --project src/FNFAssetPrep/FNFAssetPrep.csproj
\`\`\`

## Rules

- Keep the UI desktop-first.
- Do not add HTML, CSS, Electron, or a browser-based UI.
- Do not overwrite source files.
- Images must output as PNG.
- Audio must output as OGG Vorbis.
- Keep changes easy to understand.
- Test paths with spaces and non-English characters.

## Pull requests

Explain what changed, why it changed, and which file types you tested.
