using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogsDtoCloneTest.WebSite.Models.Blog;
using LogsDtoCloneTest.WebSite.Services.Blog;
using Microsoft.AspNetCore.Mvc;

namespace LogsDtoCloneTest.WebSite.Controllers;

public class BlogController : Controller
{
    private readonly IBlogService _blogService;

    public BlogController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet("/blog")]
    [HttpGet("/blog/index")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var posts = await _blogService.GetAllPostsAsync(cancellationToken);

        var summaries = posts
            .OrderByDescending(post => post.PublishedAt)
            .Select(post => new BlogPostSummaryViewModel
            {
                Slug = post.Slug,
                Title = post.Title,
                Summary = post.Summary,
                HeroImageUrl = post.HeroImageUrl,
                PublishedAt = post.PublishedAt,
                ReadingTimeMinutes = post.ReadingTimeMinutes,
                AuthorName = post.AuthorName,
                AuthorRole = post.AuthorRole,
                Tags = post.Tags
            })
            .ToList();

        var viewModel = new BlogListViewModel
        {
            Posts = summaries
        };

        return View(viewModel);
    }

    [HttpGet("blog/{slug}")]
    public async Task<IActionResult> Details(string slug, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return RedirectToAction(nameof(Index));
        }

        var post = await _blogService.GetBySlugAsync(slug, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        post.TotalViews = await _blogService.RegisterViewAsync(post.Id, HttpContext.Connection?.RemoteIpAddress, cancellationToken);

        var viewModel = await BuildDetailViewModelAsync(post, cancellationToken);

        return View(viewModel);
    }

    [HttpPost("blog/{slug}/comment")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(string slug, [Bind(Prefix = "NewComment")] BlogCommentFormModel form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return RedirectToAction(nameof(Index));
        }

        var post = await _blogService.GetBySlugAsync(slug, cancellationToken);
        if (post is null)
        {
            return NotFound();
        }

        form ??= new BlogCommentFormModel();
        form.BlogId = post.Id;

        if (form.ParentId == Guid.Empty)
        {
            form.ParentId = null;
        }

        if (!ModelState.IsValid)
        {
            var invalidViewModel = await BuildDetailViewModelAsync(post, cancellationToken, form);
            return View("Details", invalidViewModel);
        }

        var success = await _blogService.AddCommentAsync(post.Id, form.AuthorName, form.Content, form.AuthorEmail, form.ParentId, cancellationToken);
        if (!success)
        {
            ModelState.AddModelError(string.Empty, "ثبت دیدگاه با خطا مواجه شد. لطفاً مجدداً تلاش کنید.");
            var failedViewModel = await BuildDetailViewModelAsync(post, cancellationToken, form);
            return View("Details", failedViewModel);
        }

        TempData["Alert.Title"] = "ثبت دیدگاه";
        TempData["Alert.Message"] = "دیدگاه شما ثبت شد و پس از بررسی نمایش داده می‌شود.";
        TempData["Alert.Type"] = "success";

        var redirectUrl = Url.Action(nameof(Details), new { slug });
        if (!string.IsNullOrWhiteSpace(redirectUrl))
        {
            return Redirect(redirectUrl + "#comments");
        }

        return RedirectToAction(nameof(Details), new { slug });
    }

    private async Task<BlogDetailViewModel> BuildDetailViewModelAsync(BlogPost post, CancellationToken cancellationToken, BlogCommentFormModel? form = null)
    {
        var relatedPosts = (await _blogService
                .GetLatestPostsAsync(6, cancellationToken))
            .Where(other => !string.Equals(other.Slug, post.Slug, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(other => other.PublishedAt)
            .Take(3)
            .Select(other => new BlogPostSummaryViewModel
            {
                Slug = other.Slug,
                Title = other.Title,
                Summary = other.Summary,
                HeroImageUrl = other.HeroImageUrl,
                PublishedAt = other.PublishedAt,
                ReadingTimeMinutes = other.ReadingTimeMinutes,
                AuthorName = other.AuthorName,
                AuthorRole = other.AuthorRole,
                Tags = other.Tags
            })
            .ToList();

        var comments = await _blogService.GetCommentsAsync(post.Id, cancellationToken);
        post.CommentCount = CountComments(comments);

        var commentForm = form ?? new BlogCommentFormModel
        {
            BlogId = post.Id
        };

        return new BlogDetailViewModel
        {
            Post = post,
            RelatedPosts = relatedPosts,
            Comments = comments,
            NewComment = commentForm
        };
    }

    private static int CountComments(IReadOnlyList<BlogCommentViewModel> comments)
    {
        if (comments.Count == 0)
        {
            return 0;
        }

        return comments.Sum(comment => 1 + CountComments(comment.Replies));
    }
}
