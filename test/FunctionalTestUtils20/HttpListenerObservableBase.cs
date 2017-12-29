namespace FunctionalTestUtils
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reactive.Linq;
    using System.Reactive.Threading.Tasks;
    using System.Threading.Tasks;

    public abstract class HttpListenerObservableBase<T> : IObservable<T>, IDisposable
    {
        private readonly HttpListener listener;
        private IObservable<T> stream;

        public HttpListenerObservableBase(string url)
        {
            this.listener = new HttpListener();
            this.listener.Prefixes.Add(url);
        }

        public void Start()
        {
            OnStart();
            if (this.stream != null)
            {
                this.Stop();
            }

            if (!this.listener.IsListening)
            {
                this.listener.Start();
            }

            this.stream = this.CreateStream();
        }

        protected virtual void OnStart()
        { }

        public void Stop()
        {
            this.Dispose();
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (this.stream == null)
            {
                throw new InvalidOperationException("Call HttpListenerObservable.Start before subscribing to the stream");
            }

            return this.stream
                .Subscribe(observer);
        }

        public void Dispose()
        {
            if (listener != null && listener.IsListening)
            {
                listener.Stop();
                listener.Close();
                this.stream = null;
            }
        }

        private IObservable<T> CreateStream()
        {
            return Observable
                .Create<T>
                (obs =>
                    Task.Factory.FromAsync(
                        (a, c) => this.listener.BeginGetContext(a, c),
                        ar => this.listener.EndGetContext(ar),
                        null)
                        .ToObservable()
                        .SelectMany(this.CreateNewItemsFromContext)
                        .Subscribe(obs)
                )
              .Repeat()
              .Publish()
              .RefCount();
        }

        protected abstract IEnumerable<T> CreateNewItemsFromContext(HttpListenerContext context);
    }
}
