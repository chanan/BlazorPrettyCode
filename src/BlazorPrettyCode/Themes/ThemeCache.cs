using BlazorPrettyCode.Internal;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorPrettyCode.Themes
{
    internal class ThemeCache
    {
        private ConcurrentDictionary<string, string> Cache { get; set; } = new ConcurrentDictionary<string, string>();
        private readonly AsyncLock _asyncLock = new AsyncLock();

        public async Task<string> GetOrAdd(string uri, Func<Task<string>> func)
        {
            if (Cache.TryGetValue(uri, out string result))
            {
                return result;
            }
            string finalResult = null;
            using (await _asyncLock.LockAsync())
            {
                if (Cache.TryGetValue(uri, out string innerResult))
                {
                    finalResult = innerResult;
                }
                if (finalResult == null)
                {
                    finalResult = await func();
                    Cache.TryAdd(uri, finalResult);
                }
            }
            return finalResult;
        }
    }
}
