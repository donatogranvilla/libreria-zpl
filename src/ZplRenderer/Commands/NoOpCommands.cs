using ZplRenderer.Rendering;

namespace ZplRenderer.Commands
{
    /// <summary>
    /// Comandi ZPL che non influenzano il rendering ma devono essere riconosciuti
    /// dal parser per evitare warning "comando non supportato".
    /// Questi comandi controllano solo il comportamento fisico della stampante.
    /// </summary>

    /// <summary>
    /// ^MM: Media Mode - controlla la modalita' di stampa (tear-off, peel-off, cutter, ecc.).
    /// Non ha effetto sul rendering visivo dell'etichetta.
    /// Formato: ^MMm,p dove m = modalita' (T/P/R/C/U/K), p = pre-peel (Y/N)
    /// </summary>
    public class MediaModeCommand : ZplCommand
    {
        public override string CommandCode => "MM";

        public override void Parse(string parameters)
        {
            // Nessun parsing necessario: comando solo stampante
        }

        public override void Execute(RenderContext context)
        {
            // Nessun effetto sul rendering
        }
    }

    /// <summary>
    /// ^FX: Commento nel codice ZPL.
    /// Il tokenizer gestisce il contenuto del commento: tutto fino al prossimo ^
    /// viene incluso nei parametri e ignorato.
    /// Utile per annotazioni e documentazione inline nel codice ZPL.
    /// </summary>
    public class CommentCommand : ZplCommand
    {
        public override string CommandCode => "FX";

        public override void Parse(string parameters)
        {
            // Il commento viene ignorato
        }

        public override void Execute(RenderContext context)
        {
            // Nessun effetto sul rendering
        }
    }

}

