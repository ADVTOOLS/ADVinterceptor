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
using ARSoft.Tools.Net.Dns;
using System.Net;

namespace Advtools.AdvInterceptor
{
    internal class AdvDnsClient
    {
        private Dictionary<string, DnsMessage> cache_ = new Dictionary<string, DnsMessage>();
        private DnsClient dns_;
        private object lock_ = new object();

        public AdvDnsClient(IPAddress ip, int timeout)
        {
            dns_ = new DnsClient(ip, timeout);
        }

        public AdvDnsClient(DnsClient dns)
        {
            dns_ = dns;
        }

        private void GetDnsClient()
        {
        }
        
        public DnsMessage Resolve(string name, RecordType recordType, RecordClass recordClass)
        {
            name = name.ToLower();

            lock(lock_)
            {
                if(cache_.ContainsKey(name))
                    return cache_[name];

                DnsMessage message = dns_.Resolve(name);
                if(null == message)
                    return null;

                cache_.Add(name, message);
                return message;
            }
        }

        public DnsMessage Resolve(string name)
        {
            return Resolve(name, RecordType.A, RecordClass.INet);
        }
    }
}
