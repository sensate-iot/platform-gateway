/*
 * Authentication token controller.
 *
 * @author Michel Megens
 * @email   dev@bietje.net
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

using Swashbuckle.AspNetCore.SwaggerGen;

using SensateService.Attributes;
using SensateService.Infrastructure.Repositories;
using SensateService.Infrastructure.Sql;
using SensateService.Models;
using SensateService.Models.Json.In;
using SensateService.Models.Json.Out;
using SensateService.Helpers;
using SensateService.Enums;

namespace SensateService.Controllers.V1
{
	[Produces("application/json")]
	[Route("v{version:apiVersion}/[controller]")]
	[ApiVersion("1")]
	public class TokensController : AbstractController
	{
		private readonly IUserTokenRepository _tokens;
		private readonly SignInManager<SensateUser> _signin_manager;
		private readonly UserAccountSettings _settings;
		private readonly IAuditLogRepository _audit_log;

		public TokensController(
			IUserTokenRepository tokens,
			IOptions<UserAccountSettings> options,
			IUserRepository users,
			SignInManager<SensateUser> signInManager,
			IAuditLogRepository auditLog
		) : base(users)
		{
			this._tokens = tokens;
			this._signin_manager = signInManager;
			this._settings = options.Value;
			this._audit_log = auditLog;
		}

		[HttpPost("request")]
		[ValidateModel]
		[ProducesResponseType(typeof(Status), 400)]
		[ProducesResponseType(typeof(TokenRequestReply), 400)]
		public async Task<ActionResult> RequestToken([FromBody] Login login)
		{
			var user = await this._users.GetByEmailAsync(login.Email);
			bool result;
			var data = this.RouteData;
			Microsoft.AspNetCore.Identity.SignInResult signInResult;
			UserToken token;

			if(user == null)
				return NotFound();

			result = await this._signin_manager.CanSignInAsync(user);

			if(!result) {
				var status = new Status();
				status.ErrorCode = ReplyCode.NotAllowed;
				status.Message = "Not allowed to sign in!";
				return new BadRequestObjectResult(status);
			}

			signInResult = await this._signin_manager.PasswordSignInAsync(user, login.Password, false, false);
			
			if(!signInResult.Succeeded) {
				await this._audit_log.CreateAsync(
					this.CurrentRoute(), RequestMethod.HttpPost,
					GetRemoteAddress(), null
				);

				return new UnauthorizedResult();
			}

			await this._audit_log.CreateAsync(
				this.CurrentRoute(), RequestMethod.HttpPost,
				GetRemoteAddress(), user
			);

			token = this.CreateUserTokenEntry(user);
			await this._tokens.CreateAsync(token);

			var roles = this._users.GetRoles(user);
			var reply = new TokenRequestReply {
				RefreshToken = token.Value,
				ExpiresInMinutes = this._settings.JwtRefreshExpireMinutes,
				JwtToken = this._tokens.GenerateJwtToken(user, roles, _settings)
			};

			return new OkObjectResult(reply);
		}

		[HttpPost("refresh")]
		[ValidateModel]
		[ProducesResponseType(typeof(Status), 400)]
		[ProducesResponseType(typeof(TokenRequestReply), 400)]
		public async Task<ActionResult> RefreshToken([FromBody] RefreshLogin login)
		{
			var user = await this._users.GetByEmailAsync(login.Email);
			TokenRequestReply reply;
			UserToken token;

			if(user == null)
				return Unauthorized();

			token = this._tokens.GetById(user, login.RefreshToken);
			await this._audit_log.CreateAsync(this.CurrentRoute(),
				RequestMethod.HttpPost, GetRemoteAddress(), user);

			if(token == null || !token.Valid)
				return Unauthorized();

			if(token.ExpiresAt < DateTime.Now) {
				await this._tokens.InvalidateTokenAsync(token);
				return Unauthorized();
			}

			reply = new TokenRequestReply();
			var newToken = this.CreateUserTokenEntry(user);
			await this._tokens.CreateAsync(newToken);
			await this._tokens.InvalidateTokenAsync(token);

			reply.RefreshToken = newToken.Value;
			reply.ExpiresInMinutes = this._settings.JwtRefreshExpireMinutes;
			reply.JwtToken = this._tokens.GenerateJwtToken(
				user, this._users.GetRoles(user), this._settings
			);

			return new OkObjectResult(reply);
		}

		[HttpDelete("{token}", Name = "RevokeToken")]
		[NormalUser]
		[ProducesResponseType(typeof(Status), 404)]
		[SwaggerResponse(200)]
		public async Task<IActionResult> Revoke(string token)
		{
			UserToken authToken;
			var user = await this.GetCurrentUserAsync();

			if(user == null)
				return Unauthorized();

			if(token == null || token.Length == 0)
				return InvalidInputResult("Token not found!");

			authToken = this._tokens.GetById(user, token);
			await this._audit_log.CreateAsync(
				this.CurrentRoute(),
				RequestMethod.HttpDelete,
				GetRemoteAddress(),
				user
			);

			if(authToken == null)
				return this.NotFoundInputResult("Token not found!");

			if(!authToken.Valid)
				return this.InvalidInputResult("Token already invalid!");

			await this._tokens.InvalidateTokenAsync(authToken);
			return Ok();
		}

		[HttpDelete(Name = "RevokeAll")]
		[NormalUser]
		[SwaggerResponse(200)]
		public async Task<IActionResult> RevokeAll()
		{
			IEnumerable<UserToken> tokens;
			var user = await this.GetCurrentUserAsync();

			tokens = this._tokens.GetByUser(user);
			await this._audit_log.CreateAsync(
				this.CurrentRoute(), RequestMethod.HttpDelete,
				GetRemoteAddress(), user
			);
			await this._tokens.InvalidateManyAsync(tokens);
			return Ok();
		}

		private UserToken CreateUserTokenEntry(SensateUser user)
		{
			var token = new UserToken {
				UserId = user.Id,
				User = user,
				ExpiresAt = DateTime.Now.AddMinutes(this._settings.JwtRefreshExpireMinutes),
				LoginProvider = UserTokenRepository.JwtRefreshTokenProvider,
				Value = this._tokens.GenerateRefreshToken()
			};

			return token;
		}
	}
}
