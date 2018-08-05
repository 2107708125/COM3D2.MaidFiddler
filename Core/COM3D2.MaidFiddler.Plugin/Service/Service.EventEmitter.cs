﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZeroRpc.Net;
using Dict = System.Collections.Generic.Dictionary<string, object>;

namespace COM3D2.MaidFiddler.Core.Service
{
    public partial class Service
    {
        private Client client;
        private int currentCache;
        private Coroutine emitLoop;

        private readonly List<Dict>[] eventCaches = new List<Dict>[2];

        private bool HasEventHandler => client != null;

        private bool EmitEvents { get; set; } = true;

        public void SubscribeToEventHandler(string address)
        {
            Unsubscribe();

            eventCaches[0] = new List<Dict>();
            eventCaches[1] = new List<Dict>();
            currentCache = 0;

            emitLoop = parent.StartCoroutine(EmitLoop());

            client = new Client(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(30));
            client.Connect(address);
            Console.WriteLine($"Subscribed to event handler on {address}");
        }

        public void DisconnectEventHander()
        {
            Unsubscribe();
        }

        internal IEnumerator EmitLoop()
        {
            while (true)
            {
                yield return new WaitForFixedUpdate();

                if (eventCaches[currentCache].Count != 0)
                {
                    int cache = currentCache;
                    currentCache = 1 - currentCache;
                    client?.InvokeAsync("emit", new object[] {eventCaches[cache].ToArray()});
                    eventCaches[cache].Clear();
                }
            }
        }

        internal void StopEventEmitter()
        {
            if (client == null)
                return;

            client.InvokeAsync("dispose_handler");
            Unsubscribe();
        }

        internal void Unsubscribe()
        {
            if (client == null)
                return;
            parent.StopCoroutine(emitLoop);
            emitLoop = null;

            try
            {
                client.Dispose();
            }
            catch (Exception)
            {
                // Boop!
            }

            client = null;
        }

        private void Emit(string eventName, Dict arguments)
        {
            if (client != null && EmitEvents)
                eventCaches[currentCache].Add(new Dict {["event_name"] = eventName, ["args"] = arguments});
        }
    }
}