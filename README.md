# ZplRenderer

A high-performance C# library that interprets **Zebra Programming Language (ZPL)** commands and renders label previews as bitmaps. Built on **SkiaSharp** for cross-platform compatibility and **ZXing.Net** for barcode generation.

> **Target**: .NET Standard 2.0 — works with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5/6/7/8/9, Xamarin, and Unity.

## Features

### Text & Fonts
- Font selection and sizing (`^A`, `^A0`, `^A@`, `^CF`)
- Condensed and regular width rendering
- Baseline positioning (`^FT`) and Top-Left positioning (`^FO`)
- Field Block wrapping and alignment (`^FB` — Left, Center, Right, Justified)
- Reverse print (`^FR`)
- International character encoding (`^CI28` UTF-8)
- Hex character references (`^FH`)

### Barcodes (via ZXing.Net)

| 1D Barcodes | 2D Barcodes |
|-------------|-------------|
| Code 128 (`^BC`) | QR Code (`^BQ`) |
| Code 39 (`^B3`) | Data Matrix (`^BX`) |
| EAN-13 (`^BE`) | PDF 417 (`^B7`) |
| UPC-A (`^BU`) | Aztec (`^B0`) |
| Code 93 (`^BA`) | MaxiCode (`^BD`) |
| Interleaved 2 of 5 (`^B2`) | |
| Codabar (`^BK`) | |

- Barcode defaults (`^BY` — module width, ratio, height)
- Barcode rotation (0°, 90°, 180°, 270°)
- Interpretation line (above/below)
- QR error correction levels (H, Q, M, L)

### Graphics
- Graphic Box (`^GB`) — rectangles with border and fill
- Graphic Ellipse (`^GE`) — with stroke/fill
- Graphic Circle (`^GC`)
- Graphic Diagonal Line (`^GD`)
- Graphic Field / download (`^GF`, `^DG`, `^XG`, `^IM`) — embedded bitmap images

### Label Control
| Command | Description |
|---------|-------------|
| `^XA` / `^XZ` | Start / End label format |
| `^PW` | Print width |
| `^LL` | Label length |
| `^PQ` | Print quantity |
| `^PO` | Print orientation (Normal/Inverted) |
| `^LH` | Label home position |
| `^LT` | Label top offset |
| `^LS` | Label shift |
| `^MD` | Media darkness |
| `^PR` | Print rate |
| `^MM` | Media mode |
| `^FW` | Field default orientation |
| `^FX` | Comment (ignored) |
| `^SN` | Serialization data |
| `^FN` | Field number |

## Quick Start

### Installation

Add a project reference to `ZplRenderer.csproj`, or reference the compiled DLL. Your project will also need:

```xml
<PackageReference Include="SkiaSharp" Version="2.88.7" />
<PackageReference Include="ZXing.Net.Bindings.SkiaSharp" Version="0.16.14" />
```

### Render a Label

```csharp
using ZplRenderer;
using SkiaSharp;

var engine = new ZplEngine();

string zpl = @"^XA
^FO50,50^A0N,50,50^FDHello World^FS
^FO50,120^BQN,2,5^FDQA,https://example.com^FS
^XZ";

// Render with default dimensions (812×1218 dots, 203 DPI)
using (SKBitmap bitmap = engine.Render(zpl))
{
    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    using var stream = File.OpenWrite("label.png");
    data.SaveTo(stream);
}
```

### Render with Custom Dimensions

```csharp
// By dots
using var bmp1 = engine.Render(zpl, widthDots: 600, heightDots: 900, dpi: 203);

// By millimeters
using var bmp2 = engine.RenderMm(zpl, widthMm: 100, heightMm: 150, dpi: 203);

// By inches
using var bmp3 = engine.RenderInches(zpl, widthInches: 4, heightInches: 6, dpi: 203);

// Save directly to file
engine.RenderToFile(zpl, "label.png", widthDots: 812, heightDots: 1218);
```

### Validate ZPL

```csharp
var errors = engine.Validate(zpl);
foreach (var msg in errors)
    Console.WriteLine(msg);
```

## ZplViewer (WinForms)

A ready-to-use Windows Forms application is included at `examples/ZplViewer/`. It provides a live ZPL editor with instant preview — paste or type ZPL code on the left, click **Render** to see the label on the right.

```
dotnet run --project examples/ZplViewer/ZplViewer.csproj
```

## Architecture

The library follows a **Parser → Model → Drawer** pipeline:

```
ZPL String → ZplParser → ZplLabel (elements) → ElementRenderer → SKBitmap
                              ↑                        ↓
                         RenderContext           DrawerFactory
                       (state machine)        (BarcodeDrawer,
                                               TextFieldDrawer,
                                               GraphicDrawer...)
```

### Directory Structure

```
src/ZplRenderer/
├── Commands/       # ZPL command implementations (^FO, ^BC, etc.)
├── Elements/       # Logical models (ZplTextField, ZplBarcode, ZplGraphicBox)
├── Drawers/        # SkiaSharp rendering (BarcodeDrawer, TextFieldDrawer)
├── Rendering/      # Core pipeline, RenderContext, BitmapRenderer
└── ZplEngine.cs    # Main entry point
```

## Extending

To add support for a new ZPL command:

1. **Create a Command** in `Commands/` — inherit `ZplCommand`, implement `Parse()` and `Execute()`.
2. **Register** in `CommandFactory.cs`.
3. **Create an Element** in `Elements/` if it draws something new.
4. **Create a Drawer** in `Drawers/` implementing `IElementDrawer`, register it in `DrawerFactory.cs`.

## License

This software is released into the Public Domain (**Unlicense**). See [LICENSE](LICENSE) for details.
