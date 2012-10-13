using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Watcher
{
    public partial class WatcherForm : Form
    {
        private Dictionary<string, DateTime> _lastSeen = new Dictionary<string, DateTime>();
        private List<FileWatch> _fileWatches = new List<FileWatch>();
        private bool _running = true;
        private Timer _timer;

        public WatcherForm()
        {
            InitializeComponent();
            _timer = new Timer { Interval = 5000 };
            _timer.Tick += (sender, eventArgs) => {
                // check that all the file watchers are still valid
                int oldCount = _fileWatches.Count;
                _fileWatches.RemoveAll(x => !x.IsValid());
                if (oldCount != _fileWatches.Count)
                    UpdateListbox();
            };
            _timer.Start();
        }

        void UpdateListbox()
        {
            watchedFiles.Items.Clear();
            foreach (var w in _fileWatches) {
                watchedFiles.Items.Add(Path.Combine(w.Path, w.Filter) + "(" + w.NumListeners() + ")");
            }
        }

        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == WatcherStructs.WM_COPYDATA) {
                var cds = (WatcherStructs.COPYDATASTRUCT)Marshal.PtrToStructure(m.LParam, typeof (WatcherStructs.COPYDATASTRUCT));
                var watchDataBase = (WatcherStructs.WatchDataBase)Marshal.PtrToStructure(cds.lpData, typeof(WatcherStructs.WatchDataBase));
                if (watchDataBase.add) {
                    var watchData = (WatcherStructs.WatchData)Marshal.PtrToStructure(cds.lpData, typeof(WatcherStructs.WatchData));

                    // look for an existing filewatch
                    bool watchFound = false;
                    foreach (var w in _fileWatches) {
                        if (w.Path == watchData.path && w.Filter == watchData.filter) {
                            w.AddListener(watchData.hwnd, watchData.token);
                            watchFound = true;
                            break;
                        }
                    }

                    if (!watchFound) {
                        var watcher = new FileWatch(this, watchData.path, watchData.filter);
                        watcher.AddListener(watchData.hwnd, watchData.token);
                        _fileWatches.Add(watcher);
                        watchedFiles.Items.Add(watchData.path + "[" + watchData.filter + "]");
                        if (_running)
                            watcher.Start();
                    }
                   
                } else {
                    var toRemove = new List<FileWatch>();
                    foreach (var w in _fileWatches) {
                        w.RemoveListener(watchDataBase.hwnd, watchDataBase.token);
                        if (w.NumListeners() == 0) {
                            toRemove.Add(w);
                        }
                    }

                    _fileWatches.RemoveAll(toRemove.Contains);
                }

                UpdateListbox();

            } else {
                base.DefWndProc(ref m);
            }
        }

        private void StartButtonClick(object sender, EventArgs e)
        {
            if (_running) {
                startBtn.Text = "Start";
                lblStatus.Text = "Status: Stopped";
                _fileWatches.ForEach(x => x.Stop());
            } else {
                startBtn.Text = "Stop";
                lblStatus.Text = "Status: Running";
                _fileWatches.ForEach(x => x.Start());
            }
            _running = !_running;
        }
    }
}
