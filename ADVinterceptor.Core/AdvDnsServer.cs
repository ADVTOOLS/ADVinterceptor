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
using System.Net.Sockets;

namespace Advtools.AdvInterceptor
{
    public class AdvDnsServer
    {
        private readonly State state_;
        private readonly bool intercept_ = true;
        private DnsServer server_;
        private DnsProcessQuery processAQuery_;
        private DnsProcessQuery processAaaaQuery_;

        public AdvDnsServer(State state, bool intercept)
        {
            state_ = state;
            intercept_ = intercept;
        }

        public void Start()
        {
            GetProcessors();

            server_ = new DnsServer(IPAddress.Any, state_.Config.Dns.UdpListeners, state_.Config.Dns.TcpListeners, ProcessDnsQuery);
            server_.Start();
        }

        private void GetProcessors()
        {
            processAQuery_ = new DnsProcessAQuery(state_, state_.DefaultIPv4);
            processAaaaQuery_ = new DnsProcessAaaaQuery(state_, state_.DefaultIPv6);
        }

        private DnsMessageBase ProcessDnsQuery(DnsMessageBase message, IPAddress clientAddress, ProtocolType protocol)
        {
            state_.Logger.Debug("DNS query received");

            message.IsQuery = false;

            DnsMessage query = message as DnsMessage;
            if(query == null)
            {
                message.ReturnCode = ReturnCode.ServerFailure;
                return message;
            }

            foreach(DnsQuestion question in query.Questions)
            {
                state_.Logger.Debug("DNS question of type {0} received", question.RecordType);

                List<DnsRecordBase> records = ProcessQuestion(question);

                if(records == null)
                    records = ForwardQuery(question);

                if(records == null)
                {
                    message.ReturnCode = ReturnCode.ServerFailure;
                    return message;
                }
                else
                    query.AnswerRecords.AddRange(records);
            }

            return message;
        }

        private List<DnsRecordBase> ProcessQuestion(DnsQuestion question)
        {
            if(!intercept_)
                return null;

            switch(question.RecordType)
            {
                case RecordType.A:
                    return processAQuery_.ProcessQuery(question);

                case RecordType.Aaaa:
                    //records = processAaaaQuery_.ProcessQuery(question);
                    break;

                default:
                    break;
            }

            return null;
        }

        private List<DnsRecordBase> ForwardQuery(DnsQuestion question)
        {
            List<DnsRecordBase> result = new List<DnsRecordBase>();

            state_.Logger.Debug("Ask the DNS server. Name: {0}, Type: {1}, Class: {2}", question.Name, question.RecordType, question.RecordClass);
            DnsMessage answer = state_.Dns.Resolve(question.Name, question.RecordType, question.RecordClass);
            if(answer == null)
            {
                state_.Logger.Debug("No answer");
                return null;
            }

            foreach(DnsRecordBase record in answer.AnswerRecords)
            {
                state_.Logger.Information("{0}", record);
                result.Add(record);
            }

            foreach(DnsRecordBase record in answer.AdditionalRecords)
            {
                state_.Logger.Information("{0}", record);
                result.Add(record);
            }

            return result;
        }
    }
}
