using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using serssly.Data;
using serssly.Models;

namespace serssly.Pages.Feeds
{
    public class FeedVM
    {
        [Required]
        public FeedType Type { get; set; }

        [StringLength(200)]
        public string? Title { get; set; }

        [Required, Url, StringLength(500)]
        public string? Link { get; set; }
    }

    public class AddFeedModel : PageModel
    {
        private readonly ApplicationDbContext context;

        public AddFeedModel(ApplicationDbContext context)
        {
            this.context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public FeedVM Feed { get; set; } = null!;

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var feed = context.Feeds.FirstOrDefault(feed => feed.Link == Feed.Link!);
            if (feed == null)
            {
                var uri = new Uri(Feed.Link!);
                feed = new Feed { Type = Feed.Type, Title = Feed.Title ?? uri.Host, Link = Feed.Link! };

                try
                {
                    await foreach (var _ in RssAtomFunctions.GetFeedItems(feed, DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc), 1)) ;
                }
                catch
                {
                    ModelState.AddModelError("Feed.Link", "The link is probably invalid or the destination server is not responding.");
                    return Page();
                }

                context.Feeds.Add(feed);
                context.UserFeeds.Add(new UserFeed { UserId = userId, Feed = feed });
                await context.SaveChangesAsync();
            }
            else if (!context.UserFeeds.Any(uf => uf.Feed == feed))
            {
                context.UserFeeds.Add(new UserFeed { UserId = userId, Feed = feed });
                await context.SaveChangesAsync();
            }

            return RedirectToPage("./List");
        }
    }
}
