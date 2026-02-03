# ZplRenderer - ZPL Rendering Library for .NET

**ZplRenderer** is a high-performance C# library designed to interpret Zebra Programming Language (ZPL) commands and render high-quality label previews as bitmaps. Built on **SkiaSharp**, it offers cross-platform compatibility (Windows, Linux, Docker) and industrial-grade rendering accuracy.

This project is open-source and released under the **Unlicense** (Public Domain).

## Key Features

- **SkiaSharp Backend**: Fast, portable, and high-quality graphics rendering.
- **Accurate Text Rendering**: 
    - Support for ZPL fonts (`^A`, `^CF`).
    - Smart font scaling (`^A0` condensed vs regular widths).
    - Baseline positioning support (`^FT`) vs Top-Left positioning (`^FO`).
    - Field Block (`^FB`) support for wrapping and alignment.
- **Industrial Barcodes**: Full support via ZXing.Net for:
    - **1D**: Code 128 (`^BC`), Code 39 (`^B3`), EAN-13 (`^BE`), UPC-A (`^BU`), Code 93 (`^BA`).
    - **2D**: Data Matrix (`^BX`), QR Code (`^BQ`), PDF417 (`^B7`), Aztec (`^B0`), MaxiCode (`^BD`).
    - Correct Handling of ZPL Rotations (0, 90, 180, 270 degrees).
- **Shapes & Graphics**:
    - Graphic Boxes (`^GB`).
    - Graphic Ellipses (`^GE`) with Shape support (Fill/Stroke).
    - Graphic Circles (`^GC`).

## Usage

### Installation

Ensure your project references the necessary packages:

```xml
<PackageReference Include="SkiaSharp" Version="2.88.7" />
<PackageReference Include="ZXing.Net.Bindings.SkiaSharp" Version="0.16.14" />
```

### Basic Example

```csharp
using ZplRenderer;
using SkiaSharp;

// 1. Instantiate the Engine
var engine = new ZplEngine();

// 2. Define ZPL Code
string zplCode = "^XA^FO50,50^A0N,50,50^FDHello World^FS^XZ";

// 3. Render to Bitmap
using (SKBitmap bitmap = engine.Render(zplCode))
{
    // Save to file
    using (var image = SKImage.FromBitmap(bitmap))
    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
    using (var stream = System.IO.File.OpenWrite("label.png"))
    {
        data.SaveTo(stream);
    }
}
```

## Architecture

The library follows a **Parser-Model-Drawer** architecture to separate concerns and allow easy extensibility.

1.  **Parser**: The `ZplInterpreter` parses the raw string and identifies commands.
2.  **Model**: Commands populate a `RenderContext` and build a list of abstract `ZplElement` objects (e.g., `ZplTextField`, `ZplBarcode`).
    - This separation allows the ZPL logic (state machines, coordinate math) to be decoupled from specific drawing calls.
3.  **Renderer**: The `ElementRenderer` iterates through the list of elements.
4.  **Drawers**: A `DrawerFactory` matches each `ZplElement` to a specific `IElementDrawer` implementation (e.g., `BarcodeDrawer`, `TextFieldDrawer`) which executes the actual SkiaSharp calls.

### Directory Structure

- `src/ZplRenderer/Commands`: ZPL Command implementations (Factory pattern).
- `src/ZplRenderer/Elements`: Logical models representing label components.
- `src/ZplRenderer/Drawers`: Rendering logic using SkiaSharp.
- `src/ZplRenderer/Rendering`: Core pipeline and Context.

## Contributing

We welcome contributions! To add support for a new ZPL command:

1.  **Create a Command Class**: Inherit from `ZplCommand` in the `Commands` namespace. Implement `Parse()` and `Execute()`.
2.  **Update Factory**: Register your new command code in `CommandFactory.cs`.
3.  **Update Model (Optional)**: If the command draws something new, create a new `ZplElement` model.
4.  **Create Drawer (Optional)**: If you added a new Element, implement `IElementDrawer` and register it in `DrawerFactory.cs`.
5.  **Test**: Add a test case in `ZplRenderer.Tests`.

## License

This software is released into the Public Domain (**Unlicense**). You are free to use, modify, distribute, and sell this software for any purpose. See `LICENSE` file for details.
