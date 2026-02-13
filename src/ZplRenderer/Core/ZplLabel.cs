using System;
using System.Collections.Generic;

namespace ZplRenderer.Core
{
    /// <summary>
    /// Rappresenta un'etichetta ZPL parsata contenente tutti i comandi da renderizzare.
    /// </summary>
    public class ZplLabel
    {
        /// <summary>
        /// Lista di comandi ZPL parsati in ordine di esecuzione.
        /// </summary>
        public List<Commands.ZplCommand> Commands { get; } = new List<Commands.ZplCommand>();

        /// <summary>
        /// Larghezza dell'etichetta in dots (impostata da ^PW o default).
        /// </summary>
        public int WidthDots { get; set; } = 812; // ~4 pollici a 203 DPI

        /// <summary>
        /// Altezza dell'etichetta in dots (impostata da ^LL o default).
        /// </summary>
        public int HeightDots { get; set; } = 1218; // ~6 pollici a 203 DPI

        /// <summary>
        /// Offset X della label home (impostato da ^LH).
        /// </summary>
        public int LabelHomeX { get; set; } = 0;

        /// <summary>
        /// Offset Y della label home (impostato da ^LH).
        /// </summary>
        public int LabelHomeY { get; set; } = 0;

        // === MIGLIORIE OPZIONALI ===

        /// <summary>
        /// Densità di stampa in DPI (impostata da ^PR o rilevata dalla stampante).
        /// Valori comuni: 203, 300, 600 DPI.
        /// </summary>
        public int PrintDensity { get; set; } = 203;

        /// <summary>
        /// Encoding internazionale corrente (impostato da ^CI).
        /// 0 = USA1, 1-15 = vari encoding internazionali.
        /// </summary>
        public int CharacterEncoding { get; set; } = 0;

        /// <summary>
        /// Orientamento dell'etichetta (impostato da ^PO).
        /// N = Normal, I = Inverted, R = Rotated 90°, B = Bottom/Rotated 270°
        /// </summary>
        public char PrintOrientation { get; set; } = 'N';

        /// <summary>
        /// Modalità di stampa (impostata da ^MM).
        /// T = Tear-off, P = Peel-off, R = Rewind, C = Cutter, D = Delayed cut, K = Kiosk
        /// </summary>
        public char MediaType { get; set; } = 'T';

        /// <summary>
        /// Indica se il formato è valido (ha ^XA e ^XZ).
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Messaggi di validazione o warning raccolti durante il parsing.
        /// </summary>
        public List<string> ValidationMessages { get; } = new List<string>();

        /// <summary>
        /// Metadata aggiuntivi dell'etichetta (nome formato, versione, ecc.).
        /// </summary>
        public Dictionary<string, string> Metadata { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Applico le impostazioni dal contesto di parsing alla label.
        /// Questo metodo viene chiamato dal parser dopo aver processato tutti i comandi.
        /// </summary>
        internal void ApplyContext(ZplParserContext context)
        {
            if (context == null)
                return;

            LabelHomeX = context.LabelHomeX;
            LabelHomeY = context.LabelHomeY;
            CharacterEncoding = context.CurrentEncoding;
        }

        /// <summary>
        /// Calcolo la larghezza in pollici basandomi sulla densità.
        /// </summary>
        public double WidthInches => (double)WidthDots / PrintDensity;

        /// <summary>
        /// Calcolo l'altezza in pollici basandomi sulla densità.
        /// </summary>
        public double HeightInches => (double)HeightDots / PrintDensity;

        /// <summary>
        /// Calcolo la larghezza in millimetri.
        /// </summary>
        public double WidthMillimeters => WidthInches * 25.4;

        /// <summary>
        /// Calcolo l'altezza in millimetri.
        /// </summary>
        public double HeightMillimeters => HeightInches * 25.4;
    }
}
