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
    internal abstract class DnsProcessQuery
    {
        private readonly State state_;
        private readonly IPAddress defaultIp_;

        internal DnsProcessQuery(State state, IPAddress defaultIp)
        {
            state_ = state;
            defaultIp_ = defaultIp;
        }

        internal List<DnsRecordBase> ProcessQuery(DnsQuestion question)
        {
            IPAddress ip = null;

            if(state_.Config.Dns.CatchAll)
                ip = CatchAllProcessQuery(question);
            else
            {
                Interception interception = state_.Config.GetInterception(question.Name);
                if(interception == null)
                {
                    state_.Logger.Debug("No interception for '{0}' found", question.Name);
                    return null;
                }

                ip = GetIp(interception);
            }

            if(null == ip)
            {
                state_.Logger.Information("No IP found for '{0}'", question.Name);
                return null;
            }

            List<DnsRecordBase> result = new List<DnsRecordBase>();
            state_.Logger.Information("DNS {1} {0} TTL {2}", ip, question.Name, state_.Config.Dns.AnswerTtl);
            result.Add(new ARecord(question.Name, state_.Config.Dns.AnswerTtl, ip));
            return result;
        }

        private IPAddress CatchAllProcessQuery(DnsQuestion question)
        {
            // If there is an interception configured, take it
            Interception interception = state_.Config.GetInterception(question.Name);
            if(interception != null)
                return GetIp(interception);

            return state_.AllocateIP(question.Name);
        }

        private IPAddress GetIp(Interception interception)
        {
            string configuredIp = GetConfiguredIp(interception);
            if(String.IsNullOrWhiteSpace(configuredIp))
            {
                state_.Logger.Debug("No IP configured, take the default one: {0}", defaultIp_);
                return defaultIp_;
            }

            try
            {
                return state_.AllocateIP(interception.Name, IPAddress.Parse(configuredIp));
            }
            catch(FormatException)
            {
                state_.Logger.Error("{0} is not a valid IP address", configuredIp);
                return null;
            }
        }

        protected abstract string GetConfiguredIp(Interception interception);
        protected abstract DnsRecordBase CreateRecord(string name, int timeToLive, IPAddress address);

    }

    internal class DnsProcessAQuery : DnsProcessQuery
    {
        internal DnsProcessAQuery(State state, IPAddress defaultIp) :
            base(state, defaultIp)
        {
        }
        
        protected override string GetConfiguredIp(Interception interception)
        {
            return interception.IPv4;
        }

        protected override DnsRecordBase CreateRecord(string name, int timeToLive, IPAddress address)
        {
            return new ARecord(name, timeToLive, address);
        }
    }

    internal class DnsProcessAaaaQuery : DnsProcessQuery
    {
        internal DnsProcessAaaaQuery(State state, IPAddress defaultIp) :
            base(state, defaultIp)
        {
        }
        
        protected override string GetConfiguredIp(Interception interception)
        {
            return interception.IPv6;
        }

        protected override DnsRecordBase CreateRecord(string name, int timeToLive, IPAddress address)
        {
            return new AaaaRecord(name, timeToLive, address);
        }
    }
}
