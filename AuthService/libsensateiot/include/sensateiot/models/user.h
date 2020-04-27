/*
 * User model.
 *
 * @author Michel Megens
 * @email  michel@michelmegens.net
 */

#pragma once

#include <sensateiot.h>

#include <string>

namespace sensateiot::models
{
	class DLL_EXPORT User {
	public:
		void SetId(const std::string& id);
		void SetLockout(bool lockout);
		void SetBanned(bool banned);

		[[nodiscard]]
		const std::string& GetId() const;

		[[nodiscard]]
		bool GetLockout() const;

		[[nodiscard]]
		bool GetBanned() const;

	private:
		std::string m_id;
		bool m_lockout{};
		bool m_banned{};
	};
}
