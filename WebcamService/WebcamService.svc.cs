using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace WebcamService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class WebcamService : IWebcamService
    {
        private static readonly HttpClient client = new HttpClient();

        public Bitmap GetSnap()
        {
            return new ImageManager().GetImage(GetVideoUrl(ConnectionManager.isHD == true ? "1" : "0"));
        }

        public string GetVideoUrl(string isHD)
        {
            var url = ConnectionManager.GetVideoUrl(isHD == "1" ? true : false);
        
            return url;
        }
    }
}
