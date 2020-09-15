/*
 * Forgot password JSON model.
 *
 * @author Michel Megens
 * @email   michel.megens@sonatolabs.com
 */

using System.ComponentModel.DataAnnotations;

namespace SensateService.Common.Data.Dto.Json.In
{
	public class SearchQuery
	{
		[Required]
		public string Query { get; set; }
	}
}