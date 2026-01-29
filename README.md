# ZplRenderer - Libreria di Rendering ZPL per .NET

**ZplRenderer** è una libreria C# progettata per interpretare comandi ZPL (Zebra Programming Language) e renderizzare anteprime di etichette in formato bitmap. La libreria è ottimizzata per l'uso industriale, supportando una vasta gamma di barcode 1D e 2D, gestione avanzata dei font e comandi grafici.

Basata su **SkiaSharp** per prestazioni elevate e compatibilità cross-platform (Windows, Linux, Docker).

## Caratteristiche Principali

*   **Backend SkiaSharp**: Rendering veloce e portabile.
*   **Barcode Industriali**: Supporto completo tramite ZXing per:
    *   **2D**: Data Matrix (`^BX`), QR Code (`^BQ`), PDF417 (`^B7`), Aztec (`^B0`), MaxiCode (`^BD`).
    *   **1D**: Code 128 (`^BC`), Code 39 (`^B3`), EAN-13 (`^BE`), UPC-A (`^BU`), Code 93 (`^BA`).
*   **Testo Avanzato**: 
    *   Gestione font scalabili (`^A@`).
    *   Word wrapping automatico e allineamento (`^FB`).
    *   Supporto caratteri internazionali (`^CI`).
*   **Grafica**:
    *   Forme primitive: Box (`^GB`), Ellissi (`^GE`), Cerchi (`^GC`).
    *   Immagini: Download (`~DG`) e richiamo (`^XG`) di grafica esadecimale.

## Installazione

Aggiungi il riferimento al progetto o compila la libreria e includi la DLL.
Assicurati di avere i seguenti pacchetti NuGet installati nel tuo progetto (la libreria li gestisce come dipendenze):

```xml
<PackageReference Include="SkiaSharp" Version="2.88.7" />
<PackageReference Include="ZXing.Net.Bindings.SkiaSharp" Version="0.16.14" />
```

## Integrazione e Utilizzo Base

Utilizzare la classe `ZplEngine` per convertire il codice ZPL in una `SKBitmap`.

```csharp
using ZplRenderer;
using SkiaSharp;

// 1. Istanzia il motore
var engine = new ZplEngine();

// 2. Definisci il codice ZPL
string zplCode = "^XA^FO50,50^ADN,36,20^FDHello World^FS^XZ";

// 3. Renderizza
// Il metodo restituisce una SKBitmap che puoi salvare o mostrare
using (SKBitmap bitmap = engine.Render(zplCode))
{
    // Salva su file
    using (var image = SKImage.FromBitmap(bitmap))
    using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
    using (var stream = File.OpenWrite("label.png"))
    {
        data.SaveTo(stream);
    }
}
```

## Esempi di Uso Industriale

### 1. Etichetta di Spedizione con DataMatrix e Code128

Questo esempio mostra un'etichetta 4x6 pollici (approx 800x1200 punti a 203 DPI) con diverse tipologie di barcode e formattazione testo.

```csharp
string shippingLabelZpl = @"
^XA
^PW812
^LL1218
^FO50,50^A0N,50,50^FDSPEDIZIONE URGENTE^FS
^FO50,120^GB700,5,5^FS
^FO50,150^ADN,36,20^FDDestinatario:^FS
^FO50,190^A0N,30,30^FB700,4,,L^FDAcme Corp Industrial\nVia dell'Innovazione 99\nMilano, MI 20100^FS
^FO50,350^BCN,100,Y,N,N^FD1234567890^FS
^FO600,350^BXN,10,200^FDPartNr:999-001^FS
^FO50,500^A0N,40,40^FDNote: MANEGGIARE CON CURA^FS
^XZ";

var engine = new ZplEngine();
using (var label = engine.Render(shippingLabelZpl))
{
    // Logica di salvataggio...
}
```

### 2. Etichetta Alimentare con Grafica e QR Code

Esempio di etichetta ingredienti con logo (simulato) e Link QR.

```csharp
string foodLabelZpl = @"
^XA
^PW600
^LL400
^FO20,20^A0N,40,40^FDProdotto Biologico^FS
^FO450,20^BQN,2,4^FDQA,https://certificazione-bio.com^FS
^FO20,80^A0N,25,25^FB400,10,,L^FDIngredienti: Farina di grano tenero tipo 0, Acqua, Lievito naturale, Sale. Puo contenere tracce di soia.^FS
^FO20,250^GB560,3,3^FS
^FO20,270^ADN,18,10^FDLotto: L-2024-001  Scadenza: 31/12/2025^FS
^XZ";

var engine = new ZplEngine();
using (var label = engine.Render(foodLabelZpl))
{
    // Logica di salvataggio...
}
```

## Struttura del Progetto

*   **ZplRenderer**: Libreria principale (.NET Standard 2.0).
    *   `ZplEngine`: Entry point.
    *   `Core`: Tokenizer e parsers.
    *   `Commands`: Implementazioni dei comandi ZPL (Factory pattern).
    *   `Rendering`: Contesto grafico e gestione SkiaSharp.
*   **ZplRenderer.Tests**: Progetto di Unit Test (xUnit).

## Contribuire

Per aggiungere nuovi comandi:
1.  Implementare una classe che eredita da `ZplCommand` o `BarcodeBaseCommand`.
2.  Registrare il comando in `CommandFactory.cs`.
3.  Aggiungere un test unitario in `ZplRenderer.Tests`.
