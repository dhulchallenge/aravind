#region (c) 2010-2011 Lokad - CQRS for Windows Azure - New BSD License 

// Copyright (c) Lokad 2010-2011, http://www.lokad.com
// This code is released as Open Source under the terms of the New BSD Licence

#endregion


namespace Lokad.Cqrs
{
    /// <summary>
    /// Is responsible for reading and writing message envelopes 
    /// </summary>
    public interface IEnvelopeStreamer
    {
        /// <summary>
        /// Saves message envelope as array of bytes.
        /// </summary>
        /// <param name="envelope">The message envelope.</param>
        /// <returns></returns>
        byte[] SaveEnvelopeData(ImmutableEnvelope envelope);
        /// <summary>
        /// Reads the buffer as message envelope
        /// </summary>
        /// <param name="buffer">The buffer to read.</param>
        /// <returns>mes    sage envelope</returns>
        ImmutableEnvelope ReadAsEnvelopeData(byte[] buffer);
    }
}