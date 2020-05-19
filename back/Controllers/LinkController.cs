using System;
using System.Threading.Tasks;
using Curtme.Extensions;
using Curtme.Models;
using Curtme.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Curtme.Controllers
{
    [ApiController]
    public class LinkController : ControllerBase
    {
        private readonly LinkService linkService;

        public LinkController(LinkService linkService)
        {
            this.linkService = linkService;
        }

        /// <summary>
        /// Create your short link.
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     POST /
        ///     {
        ///        "URL": "https://curtme.org"
        ///     }
        ///
        /// </remarks>
        /// <param name="linkViewModel"></param>
        /// <returns>A newly shorted link</returns>
        /// <response code="200">Returns the newly shorted link</response>
        /// <response code="400">If the linkViewModel is null or has invalid URL</response>  
        [HttpPost]
        [Route("/")]
        [ProducesResponseType(typeof(Link), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Create(LinkViewModel linkViewModel)
        {
            if (linkViewModel == null || !linkViewModel.IsValidURL())
                return this.BadRequest(new { error = "Invalid URL" });

            var link = this.linkService.Create(linkViewModel.URL, this.HttpContext.User.GetId());

            return this.Ok(link);
        }

        /// <summary>
        /// This is the endpoint that people use when they click on a link
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /AAA123 (short URL)
        ///
        /// </remarks>
        /// <param name="shortURL"></param>
        /// <returns>Redirect to long URL</returns>
        /// <response code="302">Redirect to long url</response>
        /// <response code="404">If does not exist a link with that shortURL</response>  
        [HttpGet]
        [Route("/{shortURL}")]
        [ProducesResponseType(StatusCodes.Status302Found)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Visit(String shortURL)
        {
            var link = this.linkService.GetByShortURL(shortURL);

            if (link == null)
                return this.NotFound();

            Task.Run(() => this.linkService.Visited(link));

            return this.Redirect(link.LongURL);
        }

        /// <summary>
        /// Get links
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /links-by-id?ids=AAA123 (shorts URL)
        ///
        /// </remarks>
        /// <param name="ids"></param>
        /// <returns>Get all links for that ids</returns>
        /// <response code="200">Always</response>
        [HttpGet]
        [Route("/links-by-id")]
        [ProducesResponseType(typeof(Link[]), StatusCodes.Status200OK)]
        public IActionResult Get([FromQuery] String[] ids)
        {
            var links = this.linkService.GetById(ids);

            return Ok(links);
        }

        /// <summary>
        /// Get all links for user logged
        /// If user is not logged in return 403
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /links
        ///
        /// </remarks>
        /// <returns>Get all links for current user</returns>
        /// <response code="200">Always</response>
        /// <response code="403">If user not logged in</response>
        [HttpGet]
        [Authorize]
        [Route("/links")]
        [ProducesResponseType(typeof(Link[]), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult GetUserLinks()
        {
            var links = this.linkService.GetAll(this.HttpContext.User.GetId());

            return Ok(links);
        }

        /// <summary>
        /// Set user in their links
        /// </summary>
        /// <remarks>
        /// Sample request:
        ///
        ///     PUT /sync
        ///     {
        ///        [id1, id2, id3]
        ///     }
        ///
        /// </remarks>
        /// <param name="ids"></param>
        /// <returns>Status 200 OK</returns>
        /// <response code="200">Always</response>
        /// <response code="403">If user not logged in</response>
        [HttpPut]
        [Authorize]
        [Route("/sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public IActionResult Sync(String[] ids)
        {
            var userId = this.HttpContext.User.GetId();

            foreach (var id in ids)
            {
                this.linkService.Update(id, userId);
            }

            return Ok();
        }
    }
}