using System;
using Nancy;
using NzbDrone.Common;
using NzbDrone.Common.EnvironmentInfo;

namespace NzbDrone.Api.Frontend
{
    public interface ICacheableSpecification
    {
        bool IsCacheable(NancyContext context);
    }

    public class CacheableSpecification : ICacheableSpecification
    {
        public bool IsCacheable(NancyContext context)
        {
            if (BuildInfo.IsDebug)
            {
                return false;
            }

            if (((DynamicDictionary)context.Request.Query).ContainsKey("h")) return true;

            if (context.Request.Path.StartsWith("/api", StringComparison.CurrentCultureIgnoreCase))
            {
                if (context.Request.Path.ContainsIgnoreCase("/MediaCover")) return true;

                return false;
            }

            if (context.Request.Path.StartsWith("/signalr", StringComparison.CurrentCultureIgnoreCase)) return false;
            if (context.Request.Path.EndsWith("main.js")) return false;
            if (context.Request.Path.StartsWith("/feed", StringComparison.CurrentCultureIgnoreCase)) return false;

            if (context.Request.Path.StartsWith("/log", StringComparison.CurrentCultureIgnoreCase) &&
                context.Request.Path.EndsWith(".txt", StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }

            if (context.Response != null)
            {
                if (context.Response.ContentType.Contains("text/html")) return false;
            }

            return true;
        }
    }
}