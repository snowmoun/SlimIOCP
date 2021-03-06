﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace SlimIOCP.Win32
{
    public class OutgoingMessage : SlimIOCP.OutgoingMessage, INetworkBuffer<OutgoingMessage, Connection>
    {
        internal Connection Win32Connection;
        internal readonly Peer Peer;
        internal readonly SocketAsyncEventArgs AsyncArgs;

        public int BytesTransferred
        {
            get { return AsyncArgs.BytesTransferred; }
        }

        public Connection Connection
        {
            get { return Win32Connection; }
        }

        internal OutgoingMessage(Peer peer, SocketAsyncEventArgs asyncArgs)
        {
            Peer = peer;
            AsyncArgs = asyncArgs;
        }

        internal override void Destroy()
        {
            base.Destroy();
            Win32Connection = null;
            AsyncArgs.SetBuffer(null, 0, 0);
        }

        internal override void Reset()
        {
            base.Reset();
            Win32Connection = null;
            BufferAssigned();
        }

        internal override void BufferAssigned()
        {
            AsyncArgs.SetBuffer(BufferHandle, BufferOffset, BufferSize);
        }

        public override bool TryQueue()
        {
            if (SendDataBuffer == null)
            {
                // Write the message length
                SendDataOffset = BufferOffset;
                ShortConverter.UShort = (ushort)(SendDataBytesRemaining - 2);
                BufferHandle[BufferOffset + 0] = ShortConverter.Byte0;
                BufferHandle[BufferOffset + 1] = ShortConverter.Byte1;
            }

            lock (Win32Connection)
            {
                if (Win32Connection.Sending)
                {
                    Win32Connection.SendQueue.Enqueue(this);
                }
                else
                {
                    Peer.Send(this);
                }
            }

            return true;
        }
    }
}
