﻿using Net.Sockets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Net.Collections
{
    /// <summary>
    /// Internal implementation detail
    /// </summary>
    /// <typeparam name="T">Struct (due to shared generics) that holds the collection state.</typeparam>
    public abstract class AbstractSocketCollection<T> where T: struct, ISocketHolder
    {
        private protected readonly ConcurrentDictionary<SocketId, StrongBox<T>> Sockets;

        private protected AbstractSocketCollection()
        {
            this.Sockets = new ConcurrentDictionary<SocketId, StrongBox<T>>();
        }

        public int Count => this.Sockets.Count;
        public IEnumerable<ISocket> Values => this.Sockets.Values.Select(d => d.Value.Socket);

        public bool Contains(ISocket socket) => this.Sockets.ContainsKey(socket.Id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected StrongBox<T> CreateSocketHolder(ISocket socket)
        {
            this.CreateSocketHolder(socket, out T handler);

            return new StrongBox<T>(handler);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected StrongBox<T> CreateSocketHolder(T holder)
        {
            return new StrongBox<T>(holder);
        }

        public virtual bool TryAdd(ISocket socket)
        {
            StrongBox<T> handler = this.CreateSocketHolder(socket);
            if (this.Sockets.TryAdd(socket.Id, handler))
            {
                try
                {
                    this.OnAdded(socket, ref handler.Value);
                }
                catch
                {
                    //Well, someone fucked up, lets clean up
                    this.TryRemove(socket);

                    throw;
                }

                //Do last so we don't execute OnRemoved code while doing the add
                socket.Disconnected += this.OnDisconnect;

                return true;
            }

            return false;
        }

        public virtual bool TryRemove(ISocket socket)
        {
            if (this.Sockets.TryRemove(socket.Id, out StrongBox<T>? handler))
            {
                //Cleanup first
                socket.Disconnected -= this.OnDisconnect;

                this.OnRemoved(socket, ref handler.Value);

                return true;
            }

            return false;
        }

        private protected void OnDisconnect(ISocket socket) => this.TryRemove(socket);

        public Task SendAsync<TPacket>(in TPacket data)
        {
            List<Task> tasks = new List<Task>(this.Sockets.Count);
            foreach (ISocket socket in this.Values)
            {
                tasks.Add(socket.SendAsync(data));
            }

            return Task.WhenAll(tasks);
        }

        private protected abstract void CreateSocketHolder(ISocket socket, out T holder);

        protected virtual void OnAdded(ISocket socket, ref T holder)
        {
            //NOP
        }

        protected virtual void OnRemoved(ISocket socket, ref T holder)
        {
            //NOP
        }
    }
}