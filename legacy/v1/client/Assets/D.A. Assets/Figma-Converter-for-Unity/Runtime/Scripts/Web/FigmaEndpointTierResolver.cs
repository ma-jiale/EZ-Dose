using System;
using System.Text.RegularExpressions;

namespace DA_Assets.FCU
{
    internal static class FigmaEndpointTierResolver
    {
        private sealed class EndpointTierRule
        {
            public EndpointTierRule(string pattern, int tier)
            {
                Pattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
                Tier = tier;
            }

            private Regex Pattern { get; }
            public int Tier { get; }

            public bool Matches(string path)
            {
                return Pattern.IsMatch(path ?? string.Empty);
            }
        }

        private static readonly EndpointTierRule[] TierRules =
        {
            new EndpointTierRule(@"^/v1/files/[^/]+$", 1),
            new EndpointTierRule(@"^/v1/files/[^/]+/nodes$", 1),
            new EndpointTierRule(@"^/v1/files/[^/]+/images$", 2),
            new EndpointTierRule(@"^/v1/files/[^/]+/meta$", 3),
            new EndpointTierRule(@"^/v1/images/[^/]+$", 1),
        };

        public static int GetTier(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return -1;
            }

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri absoluteUri))
            {
                return GetTierFromPath(absoluteUri.AbsolutePath);
            }

            if (url.StartsWith("/", StringComparison.Ordinal))
            {
                return GetTierFromPath(url);
            }

            return -1;
        }

        private static int GetTierFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return -1;
            }

            string normalizedPath = path.EndsWith("/", StringComparison.Ordinal) && path.Length > 1
                ? path.TrimEnd('/')
                : path;

            foreach (EndpointTierRule rule in TierRules)
            {
                if (rule.Matches(normalizedPath))
                {
                    return rule.Tier;
                }
            }

            return -1;
        }
    }
}