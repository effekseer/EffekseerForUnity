
#pragma once

#include <vector>
#include <memory>

#include "Effekseer.h"
#include "../unity/IUnityInterface.h"

namespace EffekseerPlugin
{
	class Network
	{
	private:
		static std::shared_ptr<Network> instance;
		Effekseer::Server* server = nullptr;
		
	public:
		Network();
		
		virtual ~Network();

		bool Start(uint16_t port);

		void Stop();

		void Update();

		void Register(const char16_t* key, Effekseer::Effect* effect);

		void Unregister(Effekseer::Effect* effect);

		bool IsRunning() const;

		static std::shared_ptr<Network>& GetInstance();
	};
}