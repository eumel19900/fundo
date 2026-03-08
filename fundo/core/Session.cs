using System;
using fundo.core.Persistence;

namespace fundo.core
{
    /// <summary>
    /// Application-wide session singleton
    /// </summary>
    internal sealed class Session : IDisposable
    {
        private static Session? _current;

        // Public static getter for the singleton session instance
        public static Session Instance
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException("Session is not initialized. Call Session.Initialize() at app startup.");
                }

                return _current;
            }
        }


        private Session()
        {
           
        }

        /// <summary>
        /// Initializes the singleton session instance. Should be called once from App at startup.
        /// </summary>
        public static void Initialize()
        {
            if (_current == null)
            {
                _current = new Session();
                SearchIndexContext context = SearchIndexStore.CreateContext();
                context.Dispose();
            }
        }

        public void Dispose()
        {
            
        }
    }
}
