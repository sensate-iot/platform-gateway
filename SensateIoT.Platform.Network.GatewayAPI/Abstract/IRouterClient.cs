﻿/*
 * Router client interface.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

using System.Threading;
using System.Threading.Tasks;
using SensateIoT.Platform.Network.Contracts.DTO;
using SensateIoT.Platform.Network.Contracts.RPC;

namespace SensateIoT.Platform.Network.GatewayAPI.Abstract
{
	public interface IRouterClient
	{
		Task<RoutingResponse> RouteAsync(MeasurementData data, CancellationToken ct);
		Task<RoutingResponse> RouteAsync(TextMessageData data, CancellationToken ct);
		Task<RoutingResponse> RouteAsync(ControlMessage data, CancellationToken ct);
	}
}
