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
using System.Xml.Serialization;

namespace Advtools.AdvInterceptor
{
    public enum Protocol
    {
        Http,
        Https
    }

    [Serializable]
    public class Interception
    {
        [XmlAttribute] public string Name { get; set; }
        [XmlAttribute] public string IPv4 { get; set; }
        [XmlAttribute] public string IPv6 { get; set; }
        [XmlAttribute] public int Port { get; set; }
        [XmlAttribute] public Protocol Protocol { get; set; }

        public Interception()
        {
            Port = 80;
            Protocol = Protocol.Http;
        }

        public Interception(string name, int port = 80, Protocol protocol = Protocol.Http)
        {
            Name = name;
            Port = port;
            Protocol = protocol;
        }
    }
}
