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
using System.Net;
using System.Net.Sockets;
using ARSoft.Tools.Net.Dns;

namespace Advtools.AdvInterceptor
{
    public class State
    {
        private readonly CertificatesManager certificates_;

        internal Logger Logger { get; private set; }
        internal Configuration Config { get; private set; }

        private readonly IPAddress defaultIpv4_;
        private readonly IPAddress defaultIpv6_;
        private readonly IPAddress[] localIps_;
        private readonly AdvDnsClient dns_;

        public State()
        {
            Logger = new Logger();
            Config = new Configuration();
            localIps_ = GetDefaultNetworkInfo(out defaultIpv4_, out defaultIpv6_);
            dns_ = GetDns();
            certificates_ = new CertificatesManager(this);
        }

        public State(Configuration config, LogLevel level)
        {
            Logger = new Logger(level);
            Config = config;
            localIps_ = GetDefaultNetworkInfo(out defaultIpv4_, out defaultIpv6_);
            dns_ = GetDns();
            certificates_ = new CertificatesManager(this);
        }

        private IPAddress[] GetDefaultNetworkInfo(out IPAddress ipv4, out IPAddress ipv6)
        {
            IPAddress[] ips = Advtools.AdvInterceptor.Network.GetIpsByWmi();
            ipv4 = null;
            ipv6 = null;

            foreach(IPAddress ip in ips)
            {
                if(ipv4 == null && ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Logger.Config("Default IPv4 address is {0}", ip);
                    ipv4 = ip;
                }

                if(ipv6 == null && ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    Logger.Config("Default IPv6 address is {0}", ip);
                    ipv6 = ip;
                }

                if(ipv4 != null && ipv6 != null)
                    break;
            }

            if(ipv4 == null)
            {
                Logger.Warning("No valid default IPv4 address found, take the loopback");
                ipv4 = IPAddress.Loopback;
            }

            if(ipv6 == null)
            {
                Logger.Warning("No valid default IPv6 address found, take the loopback");
                ipv6 = IPAddress.IPv6Loopback;
            }

            return ips;
        }

        private AdvDnsClient GetDns()
        {
            string ip = Config.Dns.Server;
            int timeout = Config.Dns.Timeout;

            if(string.IsNullOrWhiteSpace(ip))
            {
                Logger.Config("Take the default DNS configured on this host");
                return new AdvDnsClient(DnsClient.Default);
            }

            Logger.Config("Take the configured DNS: {0} with timeout {1}", ip, timeout);
            return new AdvDnsClient(IPAddress.Parse(ip), timeout);
        }

        internal IPAddress DefaultIPv4 { get { return defaultIpv4_; } }
        internal IPAddress DefaultIPv6 { get { return defaultIpv6_; } }
        internal AdvDnsClient Dns { get { return dns_; } }
        
        internal System.Net.IPAddress AllocateIP(string name, IPAddress ip)
        {
            // TODO
            return ip;
        }

        internal System.Net.IPAddress AllocateIP(string name)
        {
            throw new NotImplementedException();
        }

        internal bool IsLocal(IPAddress ip)
        {
            foreach(IPAddress localIp in localIps_)
            {
                if(ip.Equals(localIp))
                    return true;
            }

            return false;
        }
    }
}
