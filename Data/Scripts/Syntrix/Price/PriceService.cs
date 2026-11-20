// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;

namespace Syntrix.Price
{
    /// <summary>
    /// Price requests (API 4). One mod registers as price giver for a service name (e.g. "Hangar.Store"); other mods ask
    /// with a description of the service and get an amount. Without a price giver, or when the giver has no price, the
    /// answer is -1 ("no price") and the asking mod uses its own fallback. Runs where it is called (usually the server).
    /// The description is plain text, one "key=value" per line; the keys are agreed between the mods (see README).
    /// </summary>
    public sealed class PriceService
    {
        private struct Entry
        {
            public string Mod;
            public Func<string, string, long> Giver;
        }

        private readonly Dictionary<string, Entry> _givers = new Dictionary<string, Entry>();

        /// <summary>Registers the price giver of a service; null removes it (only the mod that registered it).</summary>
        public void Register(string mod, string service, Func<string, string, long> giver)
        {
            if (string.IsNullOrEmpty(service))
                return;
            Entry old;
            bool had = _givers.TryGetValue(service, out old);
            if (giver == null)
            {
                if (had && old.Mod == mod)
                    _givers.Remove(service);
                return;
            }
            if (had && old.Mod != mod)
                Log.Warn("Price giver of " + service + " taken over by " + mod + " from " + old.Mod);
            _givers[service] = new Entry { Mod = mod, Giver = giver };
        }

        /// <summary>True if a price giver is registered for the service.</summary>
        public bool Has(string service) { return service != null && _givers.ContainsKey(service); }

        /// <summary>Price of a service, -1 if there is no price giver or it has no price.</summary>
        public long Ask(string mod, string service, string description)
        {
            Entry e;
            if (service == null || !_givers.TryGetValue(service, out e))
                return -1;
            try
            {
                long price = e.Giver(service, description ?? "");
                return price < 0 ? -1 : price;
            }
            catch (Exception ex)
            {
                Log.Error("price giver " + e.Mod + " for " + service + " (asked by " + mod + ")", ex);
                return -1;
            }
        }

        /// <summary>Forgets all price givers.</summary>
        public void Stop() { _givers.Clear(); }
    }
}
