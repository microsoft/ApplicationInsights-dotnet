// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TelemetryExtensions.cs" company="Microsoft Corporation">
//   Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Functional.Helpers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reactive.Linq;

    using AI;
    using System.Diagnostics;

    public static class TelemetryExtensions
    {
        public static Envelope[] ReceiveItems(
            this TelemetryHttpListenerObservable listener,
            int count,
            int timeOut)
        {
            if (null == listener)
            {
                throw new ArgumentNullException("listener");
            }

            var result = listener
                .Where(item => !(item is TelemetryItem<RemoteDependencyData>))
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(timeOut))
                .Take(count)
                .ToEnumerable()
                .ToArray();

            if (result.Length != count)
            {
                throw new InvalidDataException("Incorrect number of items. Expected: " + count + " Received: " + result.Length);
            }

            return result;
        }

        public static T[] ReceiveItemsOfType<T>(
            this TelemetryHttpListenerObservable listener,
            int count,
            int timeOut)
        {
            var result = listener
                .Where(item => (item is T))
                .Cast<T>()
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(timeOut))
                .Take(count)
                .ToEnumerable()
                .ToArray();

            if (result.Length != count)
            {
                throw new InvalidDataException("Incorrect number of items. Expected: " + count + " Received: " + result.Length);
            }

            return result;
        }

        public static Envelope[] ReceiveItemsOfTypes<T1, T2>(
            this TelemetryHttpListenerObservable listener,
            int count,
            int timeOut)
        {
            var result = listener
                .Where(item => ((item is T1) || (item is T2)))
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(timeOut))
                .Take(count)
                .ToEnumerable()
                .ToArray();

            if (result.Length != count)
            {
                throw new InvalidDataException("Incorrect number of items. Expected: " + count + " Received: " + result.Length);
            }

            return result;
        }

        public static Envelope[] ReceiveAllItemsDuringTime(
            this TelemetryHttpListenerObservable listener,
            int timeOut)
        {
            if (null == listener)
            {
                throw new ArgumentNullException("listener");
            }

            return listener
                .Where(item => !(item is TelemetryItem<RemoteDependencyData>))
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(timeOut))
                .ToEnumerable()
                .ToArray();
        }

        public static T[] ReceiveAllItemsDuringTimeOfType<T>(
            this TelemetryHttpListenerObservable listener,
            int timeOut)
        {
            if (null == listener)
            {
                throw new ArgumentNullException("listener");
            }

            return listener
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(timeOut))
                .Where(item => (item is T))
                .Cast<T>()
                .ToEnumerable()
                .ToArray();
        }

        public static Envelope[] ReceiveAllItemsDuringTimeOfType<T1, T2>(
            this TelemetryHttpListenerObservable listener,
            int timeOut)
        {
            if (null == listener)
            {
                throw new ArgumentNullException("listener");
            }

            return listener
                .TakeUntil(DateTimeOffset.UtcNow.AddMilliseconds(timeOut))
                .Where(item => ((item is T1) || (item is T2)))
                .ToEnumerable()
                .ToArray();
        }
    }
}
