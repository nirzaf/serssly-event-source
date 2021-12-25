using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Atom;
using Microsoft.SyndicationFeed.Rss;
using serssly.Data;
using serssly.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace serssly
{
    public sealed class FeedUpdate
    {
        public string FeedName { get; set; } = "";
    }

    public static class RssAtomFunctions
    {
        private static DiagnosticSource logger = new DiagnosticListener("RssAtomFunctions");

        public static async IAsyncEnumerable<FeedItem> GetFeedItems(Feed feed, DateTime minPubDateUtc, int count = int.MaxValue)
        {
            if (logger.IsEnabled("GetFeedItemsStart"))
            {
                logger.Write("GetFeedItemsStart", new { FeedLink = feed.Link });
            }

            int n = 0;
            try
            {
                using var http = new HttpClient();
                using var resp = await http.GetAsync(feed.Link);
                using var rstream = await resp.Content.ReadAsStreamAsync();
                using var xmlReader = XmlReader.Create(rstream, new XmlReaderSettings { Async = true });
                XmlFeedReader reader = feed.Type == FeedType.RSS ? new RssFeedReader(xmlReader) : new AtomFeedReader(xmlReader);

                while (n < count && await reader.Read())
                {
                    switch (reader.ElementType)
                    {
                        case SyndicationElementType.Item:
                            var item = await reader.ReadItem();
                            if (item.Published >= minPubDateUtc)
                            {
                                yield return new FeedItem {
                                    Id = item.Id,
                                    Title = item.Title,
                                    Description = item.Description,
                                    FeedId = feed.Id,
                                    Feed = feed,
                                    PublishDateUtc = item.Published.UtcDateTime,
                                    Link = item.Links.First().Uri.AbsoluteUri // should be the post link
                                };
                                n += 1;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            finally
            {
                if (logger.IsEnabled("GetFeedItemsStop"))
                {
                    logger.Write("GetFeedItemsStop", new {
                        FeedLink = feed.Link,
                        ItemsCount = n
                    });
                }
            }
        }
    }
}
