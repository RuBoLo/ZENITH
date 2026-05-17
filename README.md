# ZENITH

ZENITH is an early-stage desktop application intended to become an image-processing workspace for astronomical data. It is being built with Avalonia.

## Main Features


- Image loading and viewport rendering
- Support for PNG and JPEG
- Workspace-based image/document views
- Integrated console service



## Planned Features


- Support for FITS, TIFF, XISF image decoding
- Swap file system
- Common integration and rejection algorithms
- Possible "migration" to GTK for performance

## Tech Stack

- C#
- .NET
- Avalonia UI

## Requirements


- .NET SDK

## Build



```bash
dotnet restore

dotnet build
```

## Run


```bash
dotnet run --project ZENITH.csproj
```

