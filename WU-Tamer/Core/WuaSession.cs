using System;
using WUApiLib;

namespace Wu_change.Core
{
    /// <summary>
    /// Wraps the Windows Update Agent COM API session.
    /// Must be run with administrator privileges.
    /// </summary>
    public class WuaSession : IDisposable
    {
        private readonly UpdateSession _session;
        private bool _disposed = false;

        public WuaSession()
        {
            _session = new UpdateSession();
            _session.ClientApplicationID = "WU-Tamer";
        }

        public IUpdateSearcher CreateSearcher() => _session.CreateUpdateSearcher();
        public IUpdateDownloader CreateDownloader() => _session.CreateUpdateDownloader();
        public IUpdateInstaller CreateInstaller() => _session.CreateUpdateInstaller();

        public void Dispose()
        {
            if (!_disposed)
                _disposed = true;
        }
    }
}
