using Ozeki.Camera;
using Ozeki.Media;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Web;

namespace WebcamService
{
    public class ImageManager
    {
        public Bitmap GetImage(string streamUrl)
        {
            Bitmap bitmap = new Bitmap(1280, 720);
            MediaConnector mediaConnector = new MediaConnector();
            DrawingImageProvider imageProvider = new DrawingImageProvider();
            IIPCamera _camera = IPCameraFactory.GetCamera(streamUrl, "root", "pass");
            mediaConnector.Connect(_camera.VideoChannel, imageProvider);
            _camera.Start();
            VideoViewerWF video = new VideoViewerWF();
            video.SetImageProvider(imageProvider);
            video.Name = "videoViewerWF1";
            video.Size = new Size(300, 300);
            video.Start();
            Thread.Sleep(10000);
            try
            {
                video.DrawToBitmap(bitmap, new System.Drawing.Rectangle()
                {
                    Height = 720,
                    Width = 1280
                });
            }
            catch (Exception ex)
            {

            }
            finally
            {
                video.Stop();
                _camera.Stop();
            }
            return bitmap;
        }
    }
}