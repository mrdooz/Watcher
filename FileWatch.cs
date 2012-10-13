using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Watcher
{
    class FileWatch
    {
        //For use with WM_COPYDATA
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, ref WatcherStructs.COPYDATASTRUCT lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWindow(IntPtr hWnd);

        private Timer _timer;
        private Dictionary<string, DateTime> _lastSeen = new Dictionary<string, DateTime>();
        private FileSystemWatcher _watcher;
        private Form _form;

        public string Path { get; private set; }
        public string Filter { get; private set; }

        public int NumListeners()
        {
            return _listeners.Count;
        }

        class Listener
        {
            public IntPtr hwnd;
            public int token;
        }

        private List<Listener> _listeners = new List<Listener>();

        public FileWatch(Form form, string path, string filter)
        {
            Path = path;
            Filter = filter;
            _form = form;
            _watcher = new FileSystemWatcher {Path = path, Filter = filter};

            _watcher.Changed += (o, args) => _form.Invoke((MethodInvoker)delegate
            {
                _lastSeen[args.FullPath] = DateTime.Now;
                if (_timer == null) {
                    _timer = new Timer { Interval = 500 };

                    _timer.Tick += (sender, eventArgs) => {
                        var now = DateTime.Now;
                        var toRemove = new List<string>();
                        foreach (var kv in _lastSeen) {
                            // check if it's been over 1000 ms since the last event
                            if (now - kv.Value > TimeSpan.FromMilliseconds(1000)) {
                                // notify all our listeners
                                var cds = new WatcherStructs.COPYDATASTRUCT {cbData = 0, lpData = IntPtr.Zero};
                                foreach (var x in _listeners) {
                                    cds.dwData = (IntPtr)x.token;
                                    int res = SendMessage(x.hwnd, WatcherStructs.WM_COPYDATA, (int)_form.Handle, ref cds);
                                }
                                toRemove.Add(kv.Key);
                            }
                        }

                        // remove the tracking on all the files that have been processed
                        toRemove.ForEach(x => _lastSeen.Remove(x));
                        if (_lastSeen.Count == 0)
                            _timer.Stop();
                    };
                    _timer.Start();
                }

                if (!_timer.Enabled) {
                    _timer.Start();
                }
            });
        }

        public bool IsValid()
        {
            _listeners.RemoveAll(x => !IsWindow(x.hwnd));
            return _listeners.Count > 0;
        }

        public void Start()
        {
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            _watcher.EnableRaisingEvents = false;
        }

        public void AddListener(IntPtr hwnd, int token)
        {
            _listeners.Add(new Listener {hwnd = hwnd, token = token});
        }

        public void RemoveListener(IntPtr hwnd, int token)
        {
            _listeners.RemoveAll(listener => listener.hwnd == hwnd && listener.token == token);
        }
    }
}
