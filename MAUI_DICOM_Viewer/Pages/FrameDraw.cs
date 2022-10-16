using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_DICOM_Viewer.Pages
{
    internal class FrameDraw : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (!ViewerPage.CanDraw)
                return;

#if IOS || ANDROID || MACCATALYST
            byte[] buff = ViewerPage.GetCurrentFrameBuffer();
            if (buff == null)
                return;

            Microsoft.Maui.Graphics.IImage image;
            using (Stream stream = new MemoryStream(buff))
            {
                image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
                //ImgWidth = (int)image.Width;
                //ImgHeight = (int)image.Height;
            }
            if (image != null)
            {
                float div = image.Width / ViewerPage.DesiredWidth;
                float desiredHeight = image.Height / div;
                canvas.DrawImage(image, 0, 0, ViewerPage.DesiredWidth, desiredHeight);
            }
#endif
        }
    }

}
