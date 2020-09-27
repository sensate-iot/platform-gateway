/*
 * Flush user by UUID command handler.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

#include <sensateiot/commands/abstractcommandhandler.h>
#include <sensateiot/commands/flushsensorcommandhandler.h>
#include <sensateiot/util/log.h>

namespace sensateiot::commands
{
	FlushSensorCommandHandler::FlushSensorCommandHandler(services::MessageService& services) : m_messageService(services)
	{
	}

	void FlushSensorCommandHandler::Execute(const Command& cmd)
	{
		try {
			util::Log::GetLog() << "Flushing sensor with ID: " << cmd.args << "!" << util::Log::NewLine;
			this->m_messageService->FlushSensor(cmd.args);
		} catch (std::exception& ex) {
			util::Log::GetLog() << "Unable to flush sensor with ID: " << cmd.args << " because: " << ex.what() << util::Log::NewLine;
		}
	}
}
