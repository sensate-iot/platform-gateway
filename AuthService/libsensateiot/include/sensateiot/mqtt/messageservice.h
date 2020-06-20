/*
 * MQTT message service.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

#pragma once

#include <sensateiot.h>
#include <config/config.h>

#include <boost/uuid/uuid.hpp>

#include <sensateiot/mqtt/measurementhandler.h>
#include <sensateiot/mqtt/imqttclient.h>

#include <sensateiot/data/datacache.h>
#include <sensateiot/data/measurementvalidator.h>

#include <sensateiot/services/abstractuserrepository.h>
#include <sensateiot/services/abstractapikeyrepository.h>
#include <sensateiot/services/abstractsensorrepository.h>

#include <sensateiot/models/sensor.h>
#include <sensateiot/models/user.h>
#include <sensateiot/models/apikey.h>
#include <sensateiot/models/objectid.h>

#include <string>
#include <atomic>
#include <shared_mutex>

namespace sensateiot::mqtt
{
	class MessageService {
	public:
		explicit MessageService(IMqttClient& client,
		                        services::AbstractUserRepository& users,
		                        services::AbstractApiKeyRepository& keys,
		                        services::AbstractSensorRepository& sensors,
		                        const config::Config& conf);

		std::time_t Process();
		void AddMeasurement(std::string msg);

	private:

		mutable std::shared_mutex m_lock;
		config::Config m_conf;
		std::atomic_size_t m_index;
		std::vector<MeasurementHandler> m_handlers;

		data::DataCache m_cache;
		data::MeasurementValidator m_validator;
		std::atomic_uint m_count;

		stl::ReferenceWrapper<services::AbstractApiKeyRepository> m_keyRepo;
		stl::ReferenceWrapper<services::AbstractUserRepository> m_userRepo;
		stl::ReferenceWrapper<services::AbstractSensorRepository> m_sensorRepo;

		static constexpr int Increment = 1;

		void RawProcess();
		void Load(std::vector<models::ObjectId>& objIds);
	};
}
