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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using HttpServer;
using HttpListener = HttpServer.HttpListener;
using System.Security.Cryptography.X509Certificates;

namespace Advtools.AdvInterceptor
{
    public class AdvWebServer
    {
        private readonly State state_;
        private List<HttpListener> listeners_ = new List<HttpListener>();
        private Interceptor interceptor_;
        private CertificatesManager certificatesMgr_;

        public AdvWebServer(State state)
        {
            state_ = state;
            interceptor_ = new Interceptor(state);
            certificatesMgr_ = new CertificatesManager(state);
        }

        public void Start()
        {
            //HttpServer.Logging.LogFactory.Assign(new HttpServer.Logging.ConsoleLogFactory(null));

            // TODO: more than one Interception can be configured with the same port and IP

            foreach(var interception in state_.Config.Interceptions)
            {
                IPAddress ip = GetIp(interception.IPv4);
                if(ip == null)
                {
                    state_.Logger.Error("Invalid IPv4 address: {0}", interception.IPv4);
                    continue;
                }

                state_.Logger.Information("Intercept {0} {2} {3}:{1}", interception.Protocol, interception.Port, interception.Name, ip);

                try
                {
                    HttpListener listener = interception.Protocol == Protocol.Https ?
                        HttpListener.Create(ip, interception.Port, certificatesMgr_.GetCertificate(interception.Name)) :
                        HttpListener.Create(ip, interception.Port);
                    listener.RequestReceived += OnRequest;
                    listener.Start(state_.Config.Web.WebBacklog);
                    listeners_.Add(listener);
                }
                catch(System.Net.Sockets.SocketException e)
                {
                    state_.Logger.Exception(e, "Error setting up listener on port {0}", interception.Port);
                }
            }
        }

        private IPAddress GetIp(string ip)
        {
            if(ip == null)
                return IPAddress.Any;

            IPAddress ipAddress;
            if(!IPAddress.TryParse(ip, out ipAddress))
                return null;
            return ipAddress;
        }

        private void OnRequest(object sender, RequestEventArgs e)
        {
            interceptor_.OnBeginRequest(e.Context);
        }

        private void DisplayADVinterceptor(RequestEventArgs e)
        {
            e.Response.Connection.Type = HttpServer.Headers.ConnectionType.Close;

            byte[] buffer = Encoding.UTF8.GetBytes("<html><body>ADVinterceptor</body></html>");
            e.Response.Body.Write(buffer, 0, buffer.Length);
        }
    }
}
