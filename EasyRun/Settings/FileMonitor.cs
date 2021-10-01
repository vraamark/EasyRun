using EasyRun.PubSubEvents;
using PubSub;
using System;
using System.IO;

namespace EasyRun.Settings
{
    public class FileMonitor : IDisposable
    {
        private readonly Hub pubSub = Hub.Default;
        private readonly FileSystemEventHandler eventHandler;

        private string fullPath;
        private string id;

        private FileSystemWatcher watcher;
        private bool disposedValue;

        public FileMonitor()
        {
            eventHandler = new FileSystemEventHandler(FileChanged);
        }

        public bool Pause { get; set; }

        public void Start(string fullPath, string id)
        {
            if (watcher != null)
            {
                Stop();
            }

            this.fullPath = fullPath;
            this.id = id;

            var path = Path.GetDirectoryName(this.fullPath);
            var file = Path.GetFileName(this.fullPath);
            watcher = new FileSystemWatcher(path, file)
            {
                EnableRaisingEvents = true,
                NotifyFilter = NotifyFilters.LastWrite
            };

            watcher.Changed += eventHandler;
        }

        public void Stop()
        {
            if (watcher != null)
            {
                watcher.Changed -= eventHandler;
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                watcher = null;
            }
        }

        private void FileChanged(object sender, FileSystemEventArgs e)
        {
            if (!Pause)
            {
                if (IsFileReady(e.FullPath))
                {
                    pubSub.Publish(new PubSubFileMonitor(id));
                }
            }
        }

        private bool IsFileReady(string path)
        {
            try
            {
                // If we can't open the file, it's still copying
                using (var file = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Stop();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
