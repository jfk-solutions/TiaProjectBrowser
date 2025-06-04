using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TiaAvaloniaProjectBrowser
{
    public class SimpleWebServer
    {
        public HttpListener listener;
        
        public int pageViews = 0;
        public int requestCount = 0;
        public string pageData =
            "<!DOCTYPE>" +
            "<html>" +
            "  <head>" +
            "    <title>HttpListener Example</title>" +
            "  </head>" +
            "  <body>" +
            "    <p>Page Views: {0}</p>" +
            "    <form method=\"post\" action=\"shutdown\">" +
            "      <input type=\"submit\" value=\"Shutdown\" {1}>" +
            "    </form>" +
            "  </body>" +
            "</html>";

        private CancellationTokenSource tokenSource;
        private CancellationToken stopServer;

        private string basePath = "C:\\Program Files\\Siemens Automation\\SIMATIC Automation Compare Tool\\app-1.2.0\\ACT-CLI\\resources\\app\\assets\\ACT";

        public async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (!stopServer.IsCancellationRequested)
            {
                // Will wait here until we hear from a connection
                HttpListenerContext ctx = await listener.GetContextAsync();

                // Peel out the requests and response objects
                HttpListenerRequest req = ctx.Request;
                HttpListenerResponse resp = ctx.Response;

                var url = req.Url;
                string fnm = "";
                if (url.AbsolutePath == "/")
                {
                    fnm = Path.Combine(basePath, "index.html");
                }
                else
                {
                    fnm = Path.Combine(basePath, "." + url.AbsolutePath);
                }
                fnm = Path.GetFullPath(fnm);
                if (!fnm.StartsWith(basePath))
                {
                    throw new Exception("Try to break out from path");
                }

                var text = File.ReadAllText(fnm);

                byte[] data = Encoding.UTF8.GetBytes(text);
                if (fnm.EndsWith(".js"))
                    resp.ContentType = "text/javascript";
                else if(fnm.EndsWith(".css"))
                    resp.ContentType = "text/css";
                else 
                    resp.ContentType = "text/html";
                resp.ContentEncoding = Encoding.UTF8;
                resp.ContentLength64 = data.LongLength;

                // Write out to the response stream (asynchronously), then close it
                await resp.OutputStream.WriteAsync(data, 0, data.Length, stopServer);
                resp.Close();
            }
        }

        public void Start(int port)
        {
            tokenSource = new CancellationTokenSource();
            stopServer = tokenSource.Token;

            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();

            HandleIncomingConnections();
        }

        public void Stop()
        {
            tokenSource.Cancel();
            listener.Close();
        }
    }
}
