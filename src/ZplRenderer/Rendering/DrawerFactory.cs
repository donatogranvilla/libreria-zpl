using System;
using System.Collections.Generic;
using ZplRenderer.Drawers;
using ZplRenderer.Elements;

namespace ZplRenderer.Rendering
{
    public class DrawerFactory
    {
        private readonly Dictionary<Type, IElementDrawer> _drawers = new Dictionary<Type, IElementDrawer>();

        public DrawerFactory()
        {
            // Register standard drawers
            RegisterDrawer<ZplTextField>(new TextFieldDrawer());
            RegisterDrawer<ZplGraphicBox>(new GraphicBoxDrawer());
            RegisterDrawer<ZplGraphicEllipse>(new GraphicEllipseDrawer());
            RegisterDrawer<ZplGraphicImage>(new GraphicImageDrawer());
            RegisterDrawer<ZplBarcode>(new BarcodeDrawer());
            
            // Register others as needed
        }

        public void RegisterDrawer<TElement>(IElementDrawer drawer) where TElement : ZplElement
        {
            _drawers[typeof(TElement)] = drawer;
        }

        public IElementDrawer GetDrawer(ZplElement element)
        {
            if (_drawers.TryGetValue(element.GetType(), out var drawer))
            {
                return drawer;
            }
            return null; // or throw exception? Or return a dummy drawer?
        }
    }
}
