using System;
using System.Collections.Generic;

namespace BlazorPrettyCode.Internal
{
    internal class DefaultSettings : IDefaultSettings, IObservable<IDefaultSettings>
    {
        private readonly List<IObserver<IDefaultSettings>> _observers = new List<IObserver<IDefaultSettings>>();
        private bool isDevelopmentMode;
        private string defaultTheme = "PrettyCodeDefault";
        private bool showLineNumbers = true;
        private bool showException = true;
        private bool showCollapse = false;
        private bool attemptToFixTabs = true;
        private bool keepOriginalLineNumbers = false;
        private bool isCollapsed = false;

        public bool IsDevelopmentMode { get => isDevelopmentMode; set { isDevelopmentMode = value; Notify(); } }
        public string DefaultTheme { get => defaultTheme; set { defaultTheme = value; Notify(); } }
        public bool ShowLineNumbers { get => showLineNumbers; set { showLineNumbers = value; Notify(); } }
        public bool ShowException { get => showException; set { showException = value; Notify(); } }
        public bool ShowCollapse { get => showCollapse; set { showCollapse = value; Notify(); } }
        public bool IsCollapsed { get => isCollapsed; set { isCollapsed = value; Notify(); } }
        public bool AttemptToFixTabs { get => attemptToFixTabs; set { attemptToFixTabs = value; Notify(); } }
        public bool KeepOriginalLineNumbers { get => keepOriginalLineNumbers; set { keepOriginalLineNumbers = value; Notify(); } }

        public IDisposable Subscribe(IObserver<IDefaultSettings> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber<IDefaultSettings>(_observers, observer);
        }

        private void Notify()
        {
            foreach (IObserver<IDefaultSettings> observer in _observers)
            {
                observer.OnNext(this);
            }
        }
    }

    internal class Unsubscriber<IDefaultSettings> : IDisposable
    {
        private readonly List<IObserver<IDefaultSettings>> _observers;
        private readonly IObserver<IDefaultSettings> _observer;

        internal Unsubscriber(List<IObserver<IDefaultSettings>> observers, IObserver<IDefaultSettings> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
            {
                _observers.Remove(_observer);
            }
        }
    }
}
