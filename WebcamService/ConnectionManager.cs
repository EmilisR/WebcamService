using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace WebcamService
{
    public static class ConnectionManager
    {
        private static string url;
        public static bool isHD = true;
        public static string videoUrl;

        public static string GetVideoUrl(bool _isHD)
        {
            if (!isHD)
                isHD = _isHD;
            if (videoUrl == null)
                return PostXMLData(getUrl(), getRequest(isHD));
            else
                return videoUrl;
        }

        private static async Task<List<string>> GetSoapResponsesFromCamerasAsync()
        {
            var result = new List<string>();

            using (var client = new UdpClient())
            {
                var ipEndpoint = new IPEndPoint(IPAddress.Parse("192.168.1.255"), 3702);
                client.EnableBroadcast = true;
                try
                {
                    var soapMessage = GetBytes(CreateSoapRequest());
                    var timeout = DateTime.Now.AddSeconds(2);
                    await client.SendAsync(soapMessage, soapMessage.Length, ipEndpoint);

                    while (timeout > DateTime.Now)
                    {
                        if (client.Available > 0)
                        {
                            var receiveResult = await client.ReceiveAsync();
                            var text = GetText(receiveResult.Buffer);
                            result.Add(text);
                        }
                        else
                        {
                            await Task.Delay(10);
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }
            }

            return result;
        }

        private static string CreateSoapRequest()
        {
            Guid messageId = Guid.NewGuid();
            const string soap = @"
            <?xml version=""1.0"" encoding=""UTF-8""?>
            <e:Envelope xmlns:e=""http://www.w3.org/2003/05/soap-envelope""
            xmlns:w=""http://schemas.xmlsoap.org/ws/2004/08/addressing""
            xmlns:d=""http://schemas.xmlsoap.org/ws/2005/04/discovery""
            xmlns:dn=""http://www.onvif.org/ver10/device/wsdl"">
            <e:Header>
            <w:MessageID>uuid:{0}</w:MessageID>
            <w:To e:mustUnderstand=""true"">urn:schemas-xmlsoap-org:ws:2005:04:discovery</w:To>
            <w:Action a:mustUnderstand=""true"">http://schemas.xmlsoap.org/ws/2005/04/discovery/Probe</w:Action>
            </e:Header>
            <e:Body>
            <d:Probe>
            <d:Types>dn:Device</d:Types>
            </d:Probe>
            </e:Body>
            </e:Envelope>
            ";

            var result = string.Format(soap, messageId);
            return result;
        }

        private static byte[] GetBytes(string text)
        {
            return Encoding.ASCII.GetBytes(text);
        }

        private static string GetText(byte[] bytes)
        {
            return Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }

        private static string GetAddress(string soapMessage)
        {
            var xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("g", "http://schemas.xmlsoap.org/ws/2005/04/discovery");

            var element = XElement.Parse(soapMessage).XPathSelectElement("//g:XAddrs[1]", xmlNamespaceManager);
            return element?.Value ?? string.Empty;
        }

        private static List<String> GetWebcamIp()
        {
            var result = new List<string>();

            foreach (var response in GetSoapResponsesFromCamerasAsync().Result)
            {
                result.Add(GetAddress(response));
            }

            return result;
        }

        private static string PostXMLData(string destinationUrl, string requestXml)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(destinationUrl);
            byte[] bytes;
            bytes = System.Text.Encoding.ASCII.GetBytes(requestXml);
            request.ContentLength = bytes.Length;
            request.Method = "POST";
            Stream requestStream = request.GetRequestStream();
            requestStream.Write(bytes, 0, bytes.Length);
            requestStream.Close();
            HttpWebResponse response;
            response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream responseStream = response.GetResponseStream();
                string responseStr = new StreamReader(responseStream).ReadToEnd();
                Regex regex = new Regex("<tt:Uri>(.|\n)*?</tt:Uri>");
                responseStr = regex.Match(responseStr).Value.Replace("<tt:Uri>", "").Replace("</tt:Uri>", "");
                return responseStr;
            }
            return null;
        }

        private static string getStreamByQuality(bool isHD)
        {
            return isHD ? "stream0_0" : "stream0_1";
        }

        private static string getRequest(bool isHD)
        {
            return $@"<?xmlversion=""1.0""encoding=""utf-8""?>
                                <soap:Envelope xmlns:soap=""http://www.w3.org/2003/05/soap-envelope""
                                xmlns:trt=""http://www.onvif.org/ver10/media/wsdl""
                                xmlns:tt=""http://www.onvif.org/ver10/schema"">
                                    <soap:Body>
                                        <trt:GetStreamUri>
                                            <trt:StreamSetup>
                                                <tt:Stream>RTP-Unicast</tt:Stream>
                                                <tt:Transport>
                                                    <tt:Protocol>UDP</tt:Protocol>
                                                </Transport>
                                            </trt:StreamSetup>
                                            <trt:ProfileToken>{getStreamByQuality(isHD)}</trt:ProfileToken>
                                        </trt:GetStreamUri>
                                    </soap:Body>
                                </soap:Envelope>";
        }

        private static string getUrl()
        {
            if (url == null)
                url = string.Join("", GetWebcamIp().FirstOrDefault().Split('/').Take(3)).Replace("http:", "http://");
            return url;
        }
    }
}