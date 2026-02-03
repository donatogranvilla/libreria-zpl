using System.Collections.Generic;
using SkiaSharp;
using ZplRenderer.Drawers;
using ZplRenderer.Elements;

namespace ZplRenderer.Rendering
{
    public class ElementRenderer
    {
        private readonly DrawerFactory _drawerFactory;

        public ElementRenderer()
        {
            _drawerFactory = new DrawerFactory();
        }

        public void Render(List<ZplElement> elements, SKCanvas canvas, int dpiX = 203, int dpiY = 203)
        {
            var context = new DrawerContext(canvas, dpiX, dpiY);

            foreach (var element in elements)
            {
                var drawer = _drawerFactory.GetDrawer(element);
                
                if (drawer != null && drawer.CanDraw(element))
                {
                    drawer.Draw(element, context);
                }
            }
        }
    }
}
