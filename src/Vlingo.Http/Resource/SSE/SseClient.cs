﻿// Copyright (c) 2012-2019 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;

namespace Vlingo.Http.Resource.SSE
{
    public class SseClient
    {
        private static readonly ResponseHeader Connection;
        private static readonly ResponseHeader ContentType;
        private static readonly ResponseHeader TransferEncoding;
        private static readonly Headers<ResponseHeader> Headers;

        static SseClient()
        {
            Connection = ResponseHeader.Of(ResponseHeader.Connection, "keep-alive");
            ContentType = ResponseHeader.Of(ResponseHeader.ContentType, "text/event-stream");
            TransferEncoding = ResponseHeader.Of(ResponseHeader.TransferEncoding, "chunked");
            Headers = Http.Headers.Empty<ResponseHeader>().And(Connection).And(ContentType).And(TransferEncoding);
        }

        private readonly StringBuilder _builder;
        private readonly RequestResponseContext<object> _context;
        private readonly int _maxMessageSize;

        public SseClient(RequestResponseContext<object> context)
        {
            _context = context;
            _builder = new StringBuilder();
            _maxMessageSize = Configuration.Instance.Sizing.MaxMessageSize;
        }

        public void Close() => _context.Abandon();

        public string Id => _context.Id;

        public void Send(SseEvent @event)
        {
            var buffer = BasicConsumerByteBuffer.Allocate(1, _maxMessageSize);
            var entity = @event.Sendable();
            var withContentLength = Headers.Copy().And(ResponseHeader.WithContentLength(entity));
            var response = Response.Of(Response.ResponseStatus.Ok, withContentLength, entity);
            _context.RespondWith(response.Into(buffer));
        }

        public void Send(params SseEvent[] events)
        {
            Send(events.ToList());
        }

        public void Send(ICollection<SseEvent> events)
        {
            var entity = Flatten(events);
            Send(entity, Headers.Copy().And(ResponseHeader.WithContentLength(entity)));
        }

        public void Send(ICollection<SseEvent> events, string correlationId)
        {
            var entity = Flatten(events);
            Send(
                entity, 
                Headers.Copy().And(ResponseHeader.WithContentLength(entity)).And(ResponseHeader.WithCorrelationId(correlationId)));
        }

        private void Send(string entity, Headers<ResponseHeader> headers)
        {
            var buffer = BasicConsumerByteBuffer.Allocate(1, _maxMessageSize);
            var response = Response.Of(Response.ResponseStatus.Ok, Headers, entity);
            _context.RespondWith(response.Into(buffer));
        }

        private string Flatten(ICollection<SseEvent> events)
        {
            _builder.Clear();

            foreach (var @event in events)
            {
                _builder.Append(@event.Sendable());
            }

            return _builder.ToString();
        }
    }
}
