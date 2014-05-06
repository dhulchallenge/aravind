#region (c) 2010-2012 Lokad All rights reserved

// You must not remove this notice, or any other, from this software.
// This document is the property of LOKAD SAS and must not be disclosed.

#endregion

using System;
using System.Globalization;
using System.IO;

namespace Lokad.Cqrs.Envelope
{
    public static class EnvelopePrinter
    {
        public static string PrintToString(this ImmutableEnvelope envelope, Func<object, string> serializer)
        {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture))
            {
                writer.WriteLine("{0,12}: {1}", "EnvelopeId", envelope.EnvelopeId);

                foreach (var attribute in envelope.Attributes)
                {
                    writer.WriteLine("{0,12}: {1}", attribute.Key, attribute.Value);
                }

                writer.WriteLine(envelope.Message.GetType().Name);
                try
                {
                    var buffer = serializer(envelope.Message);
                    writer.WriteLine(buffer);
                }
                catch (Exception ex)
                {
                    writer.WriteLine("Rendering failure");
                    writer.WriteLine(ex);
                }

                writer.WriteLine();
                writer.Flush();
                return writer.GetStringBuilder().ToString();
            }
        }
    }
}