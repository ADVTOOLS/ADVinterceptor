/*
 * This file is part of ADVinterceptor
 * Copyright (c) 2011 - ADVTOOLS SARL
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Web;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Text;
using HttpServer;
using HttpServer.Messages;
using HttpServer.Headers;
using ARSoft.Tools.Net.Dns;

namespace Advtools.AdvInterceptor
{
    class Interceptor
    {
        private readonly State state_;
        private static int messageNumber_ = 0;

        public Interceptor(State state)
        {
            state_ = state;

            if(string.IsNullOrEmpty(state_.Config.Proxy.Server))
                state_.Logger.Config("No proxy");
            else
                state_.Logger.Config("Use proxy {0}:{1}", state_.Config.Proxy.Server, state_.Config.Proxy.Port);
        }

        public void OnBeginRequest(IHttpContext context)
        {
            if(0 == string.Compare(context.Request.Uri.AbsolutePath, "/Test.html", true))
                return;

            int messageNumber = System.Threading.Interlocked.Increment(ref messageNumber_);
            BuildRequestResponse(context.Request, context.RemoteEndPoint, context.Response, messageNumber);

            //context.Application.CompleteRequest();
        }

        private void BuildRequestResponse(IRequest originalRequest, IPEndPoint remote, IResponse originalResponse, int messageNumber)
        {
            state_.Logger.Information("{0} {1} {2}{3}", originalRequest.Method, originalRequest.Uri.Scheme, originalRequest.Uri.Host, originalRequest.Uri.AbsolutePath);
            state_.Logger.Debug("  " + originalRequest.Uri.Query);

            HttpWebRequest request = PrepareRequest(originalRequest, remote, messageNumber);
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                WriteResponse(originalRequest, originalResponse, response, messageNumber);
            }
            catch(WebException ex)
            {
                state_.Logger.Exception(ex, "Error requesting {0}", request.RequestUri);
                if(ex.Response != null)
                    WriteResponse(originalRequest, originalResponse, (HttpWebResponse)ex.Response, messageNumber);
                else
                    originalResponse.Status = HttpStatusCode.InternalServerError;
            }
        }

        private HttpWebRequest PrepareRequest(IRequest originalRequest, IPEndPoint remote, int messageNumber)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(ConstructUrl(originalRequest, remote));
            request.Host = originalRequest.Uri.Host;

            request.Method = originalRequest.Method;
            if(originalRequest.ContentType != null)
                request.ContentType = originalRequest.ContentType.Value;

            PrepareProxy(request);
            PrepareRequestCookies(request, originalRequest);
            PrepareRequestHeaders(request, originalRequest);
            PrepareRequestContent(request, originalRequest, messageNumber);

            return request;
        }

        private void PrepareProxy(HttpWebRequest request)
        {
            string proxyServer = state_.Config.Proxy.Server;
            if(string.IsNullOrEmpty(proxyServer))
                return;

            request.Proxy = new WebProxy(proxyServer, state_.Config.Proxy.Port);
        }

        private void PrepareRequestCookies(HttpWebRequest request, IRequest originalRequest)
        {
            request.CookieContainer = new CookieContainer();

            foreach(RequestCookie originalCookie in originalRequest.Cookies)
            {
                Cookie cookie = new Cookie();
                cookie.Name = originalCookie.Name;
                cookie.Value = originalCookie.Value.Replace(',', ' '); // TODO: fix that
                //cookie.Path = originalCookie.Path;
                cookie.Domain = GetRemoteHost(originalRequest);
                //cookie.HttpOnly = originalCookie.HttpOnly;
                //cookie.Secure = originalCookie.Secure;
                //cookie.Expires = originalCookie.Expires;

                // TODO: Handle the subcookies case

                request.CookieContainer.Add(cookie);
            }
        }

        private void PrepareRequestHeaders(HttpWebRequest request, IRequest originalRequest)
        {
            request.Headers.Clear();
            foreach(IHeader header in originalRequest.Headers)
            {
                if(IsNotSettableHeader(header.Name))
                    continue;
                request.Headers.Add(header.Name, header.HeaderValue);
            }

            /*
            if(originalRequest.AcceptTypes != null)
            {
                StringBuilder accepts = new StringBuilder();
                foreach(string accept in originalRequest.AcceptTypes)
                {
                    if(accepts.Length > 0)
                        accepts.Append(", ");
                    accepts.Append(accept);
                }
                request.Accept = accepts.ToString();
            }*/

            if(null != originalRequest.Headers["Referrer"])
                request.Referer = ProcessRequestValue(originalRequest.Headers["Referrer"].HeaderValue);
            if(null != originalRequest.Headers["User-Agent"])
                request.UserAgent = originalRequest.Headers["User-Agent"].HeaderValue;

            if(originalRequest.ContentType != null)
                request.ContentType = originalRequest.ContentType.HeaderValue;
        }

        private void PrepareRequestContent(HttpWebRequest request, IRequest originalRequest, int messageNumber)
        {
            if(originalRequest.ContentLength.Value <= 0)
            {
                //SaveRequest(originalRequest, messageNumber);
                return;
            }

            using(Stream stream = request.GetRequestStream())
            {
                //SaveRequest(originalRequest, messageNumber);
                ReadWriteStreams(originalRequest.Body, stream);
            }
        }

        private bool IsNotSettableHeader(string headerName)
        {
            string[] names = new string[]
            {
                "Accept",
                "Connection",
                "Content-Length",
                "Content-Type",
                "Expect",
                "Date",
                "Host",
                "If-Modified-Since",
                "Range",
                "Referer",
                "Transfer-Encoding",
                "User-Agent",
                "Set-Cookie"
            };

            foreach(string name in names)
                if(0 == string.Compare(name, headerName, true))
                    return true;

            return false;
        }

        private Uri ConstructUrl(IRequest originalRequest, IPEndPoint remote)
        {
            if(!state_.IsLocal(remote.Address))
                return originalRequest.Uri;

            DnsMessage message = state_.Dns.Resolve(originalRequest.Uri.DnsSafeHost);
            if(null == message)
                throw new ApplicationException(string.Format("Could not resolve '{0}' with DNS '{1}'", originalRequest.Uri.DnsSafeHost, state_.Config.Dns.Server));

            foreach(DnsRecordBase recordBase in message.AnswerRecords)
            {
                ARecord record = recordBase as ARecord;
                if(record == null)
                    continue;

                string host = originalRequest.Uri.Host;
                string url = originalRequest.Uri.AbsoluteUri.Replace(host, record.Address.ToString());

                return new Uri(url);
            }

            return originalRequest.Uri;
        }

        private string GetRemoteHost(IRequest request)
        {
            return request.Uri.Host;
        }

        private string GetClientHost(IRequest request)
        {
            return request.Uri.Host;
        }

        private void WriteResponse(IRequest originalRequest, IResponse originalResponse, HttpWebResponse response, int messageNumber)
        {
            // TODO: Encoding

            //originalResponse.Charset = response.CharacterSet;
            originalResponse.ContentType = new ContentTypeHeader(response.ContentType);
            //originalResponse.ContentEncoding = Encoding.UTF8;

            WriteResponseCookies(originalRequest, originalResponse, response);
            WriteResponseHeaders(originalResponse, response);
            WriteResponseContent(originalResponse, response, messageNumber);

            originalResponse.Status = response.StatusCode;
        }

        private void WriteResponseCookies(IRequest originalRequest, IResponse originalResponse, HttpWebResponse response)
        {
            if(response.Cookies != null && response.Cookies.Count > 0)
            {
                foreach(Cookie cookie in response.Cookies)
                {
                    string path = cookie.Path;
                    if(0 == string.Compare(path, originalRequest.Uri.AbsolutePath, true))
                        path = "/";

                    ResponseCookie constructedCookie = new ResponseCookie(cookie.Name, cookie.Value, cookie.Expires);
                    constructedCookie.Value = cookie.Value;
                    constructedCookie.Path = path;
                    /*constructedCookie.Domain = GetRemoteHost(originalRequest);
                    constructedCookie.HttpOnly = cookie.HttpOnly;
                    constructedCookie.Secure = cookie.Secure;*/
                    constructedCookie.Expires = cookie.Expires;

                    originalResponse.Cookies.Add(constructedCookie);
                }
            }
        }

        private void WriteResponseHeaders(IResponse originalResponse, HttpWebResponse response)
        {
            foreach(string headerName in response.Headers.AllKeys)
            {
                if(IsNotSettableHeader(headerName))
                    continue;

                /*IHeader header = originalResponse.Headers[headerName];
                if(header != null)
                    header.HeaderValue = ProcessResponseValue(response.Headers[headerName]);
                else*/
                    originalResponse.Add(new StringHeader(headerName, ProcessResponseValue(response.Headers[headerName])));
            }
        }

        private MemoryStream GetResponseData(HttpWebResponse response)
        {
            MemoryStream memoryStream = new MemoryStream();
            if(response.ContentLength <= 0)
                return memoryStream;

            using(Stream stream = response.GetResponseStream())
                ReadWriteStreams(stream, memoryStream, false);

            memoryStream.Position = 0;
            return memoryStream;
        }

        private void WriteResponseContent(IResponse originalResponse, HttpWebResponse response, int messageNumber)
        {
            using(Stream stream = response.GetResponseStream())
            {
                ReadWriteStreams(stream, originalResponse.Body, false);
            }

            //MemoryStream stream = GetResponseData(response);
            //SaveResponse(response, stream, messageNumber);

            //if(!Config.Rewrite || !IsTextContentType(response.ContentType))
                //ReadWriteStreams(stream, originalResponse.Body);
            /*else
            {
                string content = ProcessResponse(stream);
                using(StreamWriter writer = new StreamWriter(originalResponse.OutputStream, Encoding.UTF8))
                    writer.Write(content);
            }*/
        }

        private string ProcessResponse(Stream stream)
        {
            StreamReader reader = new StreamReader(stream);
            string content = reader.ReadToEnd();
            return ProcessResponseValue(content);
        }

        private string ProcessResponseValue(string value)
        {
            /*if(Config.Rewrite)
            {
                value = TextUtils.Replace(value, "http://" + GetRemoteHost(originalRequest), "http://" + Config.RewriteHost, StringComparison.OrdinalIgnoreCase);
                value = TextUtils.Replace(value, "https://" + GetRemoteHost(originalRequest), "https://" + Config.RewriteHost, StringComparison.OrdinalIgnoreCase);
            }*/
            return value;
        }

        private string ProcessRequestValue(string value)
        {
            /*
            if(Config.Rewrite)
            {
                value = TextUtils.Replace(value, "http://" + Config.RewriteHost, "http://" + GetRemoteHost(originalRequest), StringComparison.OrdinalIgnoreCase);
                value = TextUtils.Replace(value, "https://" + Config.RewriteHost, "https://" + GetRemoteHost(originalRequest), StringComparison.OrdinalIgnoreCase);
            }*/
            return value;
        }

        private bool IsTextContentType(string contentType)
        {
            string[] parts = contentType.Split(';');
            if(parts.Length <= 0)
                return false;

            switch(parts[0].ToLower())
            {
                case "text/html": return true;
                case "application/x-javascript": return true;
                default: break;
            }
            return false;
        }

        private void ReadWriteStreams(Stream input, Stream output)
        {
            ReadWriteStreams(input, output, true);
        }

        private void ReadWriteStreams(Stream input, Stream output, bool resetInputPosition)
        {
            byte[] buffer = new byte[BufferSize];
            int read = input.Read(buffer, 0, BufferSize);
            while(read > 0)
            {
                output.Write(buffer, 0, read);
                read = input.Read(buffer, 0, BufferSize);
            }

            if(resetInputPosition)
                input.Position = 0;
        }

        private byte[] GetAllBytes(Stream input)
        {
            using(MemoryStream stream = new MemoryStream())
            {
                ReadWriteStreams(input, stream);
                return stream.ToArray();
            }
        }

        /*private void SaveResponse(HttpWebResponse response, Stream responseStream, int number)
        {
            if(string.IsNullOrEmpty(Config.SaveDirectory))
                return;

            string filenameData = string.Format("{0:00000}-response.data", number);
            string filenameHeaders = string.Format("{0:00000}-response.txt", number);

            using(FileStream stream = new FileStream(Path.Combine(Config.SaveDirectory, filenameHeaders), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using(StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("{0} {1} {2}", "HTTP/1.1", (int)response.StatusCode, response.StatusDescription);
                    
                    foreach(string headerName in response.Headers.Keys)
                        writer.WriteLine("{0}: {1}", headerName, response.Headers[headerName]);
                    
                    if(response.Cookies.Count > 0)
                    {
                        writer.Write("Cookie: ");
                        foreach(Cookie cookie in response.Cookies)
                            writer.Write("{0}={1};", cookie.Name, cookie.Value);
                        writer.WriteLine();
                    }
                }
            }

            using(FileStream stream = new FileStream(Path.Combine(Config.SaveDirectory, filenameData), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                if(null != responseStream)
                {
                    ReadWriteStreams(responseStream, stream);
                    responseStream.Position = 0;
                }
            }
        }
        */

        /*
        private void SaveRequest(HttpRequest originalRequest, int number)
        {
            if(string.IsNullOrEmpty(Config.SaveDirectory))
                return;

            Uri originalUrl = originalRequest.Url;
            string filenameHeaders = string.Format("{0:00000}-request-{1}-{2}.txt", number, originalUrl.Host, originalUrl.Port);
            string filenameData = string.Format("{0:00000}-request-{1}-{2}.data", number, originalUrl.Host, originalUrl.Port);

            using(FileStream stream = new FileStream(Path.Combine(Config.SaveDirectory, filenameHeaders), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using(StreamWriter writer = new StreamWriter(stream))
                {
                    writer.WriteLine("{0} {1} {2}", originalRequest.HttpMethod, originalUrl.PathAndQuery, "HTTP/1.1");
                    
                    foreach(string headerName in originalRequest.Headers.Keys)
                        writer.WriteLine("{0}: {1}", headerName, originalRequest.Headers[headerName]);

                    if(originalRequest.Cookies.Count > 0)
                    {
                        writer.Write("Cookie: ");
                        foreach(string cookie in originalRequest.Cookies)
                            writer.Write("{0}={1};", cookie, originalRequest.Cookies[cookie]);
                        writer.WriteLine();
                    }
                }
            } 
            
            using(FileStream stream = new FileStream(Path.Combine(Config.SaveDirectory, filenameData), FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                if(originalRequest.ContentLength > 0)
                {
                    ReadWriteStreams(originalRequest.InputStream, stream);
                    originalRequest.InputStream.Position = 0;
                }
            }
        }
         * */

        private const int BufferSize = 32 * 1024;
    }
}
