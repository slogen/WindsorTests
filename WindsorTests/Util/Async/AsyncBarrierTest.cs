using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace WindsorTests.Util.Async
{
    public class AsyncBarrierTest
    {
        protected async Task<bool> CompletedBefore(Task task, TimeSpan span)
            => await Task.WhenAny(task, Task.Delay(span)).ConfigureAwait(false) == task;

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(100)]
        [TestCase(1000000, Explicit = true)]
        public async Task AsyncBarrierShouldBlockExactlyRightAmount(int count)
        {
            var ct = default(CancellationToken);
            var barrier = new AsyncBarrier(count);
            long[] j = {0};
            Func<Task> act = async () =>
            {
                await barrier.SignalAndWait(ct).ConfigureAwait(false);
                Interlocked.Increment(ref j[0]);
            };
            for (var i = 0; i < 2; ++i)
            {
                var tasks = Enumerable.Range(0, count - 1).Select(_ => act()).ToList();
                // No Wait has retured
                Interlocked.Read(ref j[0]).Should().Be(i * count);
                // Wait, so some of the tasks could potentially run
                await Task.Yield();
                if (tasks.Any())
                    (await CompletedBefore(Task.WhenAny(tasks), TimeSpan.FromSeconds(0.25)))
                        .Should().BeFalse();
                // No Wait has retured, yet -- still missing 1
                Interlocked.Read(ref j[0]).Should().Be(i * count);
                var lastOne = act();
                (await CompletedBefore(lastOne, TimeSpan.FromSeconds(.25)))
                    .Should().BeTrue();
                Interlocked.Read(ref j[0]).Should().BeGreaterThan(i * count);
                await Task.WhenAll(tasks).ConfigureAwait(false);
                Interlocked.Read(ref j[0]).Should().Be((i + 1) * count);
            }
        }
    }
}