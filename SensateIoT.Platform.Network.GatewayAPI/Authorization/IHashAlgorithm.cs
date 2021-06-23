using System.Text.RegularExpressions;

namespace SensateIoT.Platform.Network.GatewayAPI.Authorization
{
	public interface IHashAlgorithm
	{
		Regex GetMatchRegex();
		Regex GetSearchRegex();
		byte[] ComputeHash(byte[] input);
	}
}