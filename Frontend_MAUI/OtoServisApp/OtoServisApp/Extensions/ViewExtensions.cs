// Dosya Yolu: OtoServisApp/Extensions/ViewExtensions.cs

using Microsoft.Maui.Controls;

namespace OtoServisApp.Extensions
{
    public static class ViewExtensions
    {
        /// <summary>
        /// Bir görsel öğenin ekrandaki mutlak konumunu (X, Y, Genişlik, Yükseklik) döndürür.
        /// Global dropdown'ı doğru konumlandırmak için kullanılır.
        /// </summary>
        public static Rect? GetAbsoluteBounds(this VisualElement element)
        {
            if (element == null) return null;

            double x = element.X;
            double y = element.Y;
            var parent = element.Parent as VisualElement;

            while (parent != null)
            {
                x += parent.X;
                y += parent.Y;
                parent = parent.Parent as VisualElement;
            }

            return new Rect(x, y, element.Width, element.Height);
        }
    }
}