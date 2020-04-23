/*
 * Sensate IoT MQTT client.
 *
 * @author Michel Megens
 * @email michel@michelmegens.net
 */

#pragma once

#include <sensateiot.h>

#include <mqtt/async_client.h>
#include <authorization/referencewrapper.h>

namespace sensateiot::mqtt
{
	class DLL_EXPORT MqttCallback :
			public ::mqtt::callback,
			public virtual ::mqtt::iaction_listener {
	public:
		explicit MqttCallback() = default;
		explicit MqttCallback(::mqtt::async_client& cli, ::mqtt::connect_options& connOpts);

		void on_failure(const ::mqtt::token& tok) override;
		void delivery_complete(::mqtt::delivery_token_ptr token) override;
		void on_success(const ::mqtt::token& tok) override;
		void connected(const std::string& cause) override;
		void connection_lost(const std::string& cause) override;
		void message_arrived(::mqtt::const_message_ptr msg) override;

	private:
		sensateiot::stl::ReferenceWrapper<::mqtt::async_client> m_cli;
		sensateiot::stl::ReferenceWrapper<::mqtt::connect_options> m_connOpts;
	};
}
