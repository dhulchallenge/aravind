#region (c) 2010-2012 Lokad All rights reserved

// You must not remove this notice, or any other, from this software.
// This document is the property of LOKAD SAS and must not be disclosed.

#endregion

using System;
using System.IO;

namespace Lokad.Cqrs.Envelope
{
    public sealed class EnvelopeStreamer : IEnvelopeStreamer
    {
        public readonly IMessageSerializer MessageSerializer;

        public EnvelopeStreamer(IMessageSerializer messageSerializer)
        {
            MessageSerializer = messageSerializer;
        }

        const int Signature = 20120807;

        public byte[] SaveEnvelopeData(ImmutableEnvelope envelope)
        {
            using (var mem = new MemoryStream())
            {
                byte[] data;
                using (var block = new MemoryStream())
                {
                    MessageSerializer.WriteAttributes(envelope.Attributes, block);
                    MessageSerializer.WriteMessage(envelope.Message, envelope.Message.GetType(), block);
                    data = block.ToArray();
                }

                MessageSerializer.WriteCompactInt(Signature, mem);

                StorageFramesEvil.WriteFrame(envelope.EnvelopeId, envelope.CreatedUtc.Ticks, data, mem);
                return mem.ToArray();
            }
        }


        public ImmutableEnvelope ReadAsEnvelopeData(byte[] buffer)
        {
            using (var mem = new MemoryStream(buffer))
            {
                var signature = MessageSerializer.ReadCompactInt(mem);
                if (Signature != signature)
                    throw new IOException("Signature bytes mismatch in envelope");

                var frame = StorageFramesEvil.ReadFrame(mem);
            
                using (var mem1 = new MemoryStream(frame.Bytes))
                {
                    var attributes = MessageSerializer.ReadAttributes(mem1);
                    var item = MessageSerializer.ReadMessage(mem1);
                    var created = new DateTime(frame.Stamp, DateTimeKind.Utc);
                    return new ImmutableEnvelope(frame.Name, created, item, attributes);
                }
            }
        }
    }
}