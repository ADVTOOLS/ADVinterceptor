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
using System.Xml.Serialization;
using System.IO;
using System.Net;
using NDesk.Options;

namespace Advtools.AdvInterceptor
{
    [Serializable]
    public class Configuration
    {
        private HashSet<Interception> interceptions_ = new HashSet<Interception>();

        public WebConfig Web { get; set; }
        public DnsConfig Dns { get; set; }
        public ProxyConfig Proxy { get; set; }
        public X509Config X509 { get; set; }
        public HashSet<Interception> Interceptions { get { return interceptions_; } }

        public Configuration()
        {
            Web = new WebConfig();
            Dns = new DnsConfig();
            Proxy = new ProxyConfig();
            X509 = new X509Config();
        }

        public void Save(string path)
        {
            XmlSerializer serializer = new XmlSerializer(GetType());
            StreamWriter writer = new StreamWriter(path);
            serializer.Serialize(writer, this);
        }

        public static Configuration Load(string path)
        {
            if(!File.Exists(path))
                throw new ApplicationException(string.Format("Configuration file '{0}' not found.", path));

            XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
            StreamReader reader = new StreamReader(path);
            return serializer.Deserialize(reader) as Configuration;
        }

        public static Configuration Default
        {
            get
            {
                Configuration config = new Configuration();

                config.Interceptions.Add(new Interception("www.microsoft.com", 80, Protocol.Http));
                config.Interceptions.Add(new Interception("www.microsoft.com", 443, Protocol.Https));

                config.Dns.Server = "8.8.8.8";

                return config;            
            }
        }

        internal Interception GetInterception(string name)
        {
            var result = from i in interceptions_ where string.Compare(i.Name, name, true) == 0 select i;
            return (result.FirstOrDefault());
        }
    }

    [Serializable]
    public class DnsConfig
    {
        [XmlAttribute] public string Server { get; set; }
        [XmlAttribute] public int Timeout { get; set; }
        [XmlAttribute] public int AnswerTtl { get; set; }
        [XmlAttribute] public int UdpListeners { get; set; }
        [XmlAttribute] public int TcpListeners { get; set; }
        [XmlAttribute] public bool CatchAll { get; set; }

        public DnsConfig()
        {
            Server = null;
            Timeout = 500;
            AnswerTtl = 3600;
            UdpListeners = 10;
            TcpListeners = 10;
            CatchAll = false;
        }
    }

    [Serializable]
    public class WebConfig
    {
        [XmlAttribute] public int WebBacklog { get; set; }

        public WebConfig()
        {
            WebBacklog = 24;
        }
    }

    [Serializable]
    public class ProxyConfig
    {
        [XmlAttribute] public string Server { get; set; }
        [XmlAttribute] public int Port { get; set; }

        public ProxyConfig()
        {
            Server = null;
            Port = 8080;
        }
    }

    [Serializable]
    public class X509Config
    {
        [XmlAttribute] public string AuthorityName { get; set; }
        [XmlAttribute] public int RootValidity { get; set; }
        [XmlAttribute] public int Validity { get; set; }

        public X509Config()
        {
            AuthorityName = "ADVinterceptor Root Certification Authority";
            RootValidity = 365 * 4; // 4 years
            Validity = 365 * 2; // 2 years
        }
    }
}
