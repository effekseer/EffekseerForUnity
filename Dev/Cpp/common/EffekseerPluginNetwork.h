
#pragma once

#include <vector>
#include <memory>

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include "Effekseer.h"
#endif

#include "../unity/IUnityInterface.h"

#ifndef _SWITCH

namespace EffekseerPlugin
{
	class Network
	{
	private:
		static std::shared_ptr<Network> instance;
		Effekseer::ServerRef server = nullptr;
		
	public:
		Network();
		
		virtual ~Network();

		bool Start(uint16_t port);

		void Stop();

		void Update();

		void Register(const char16_t* key, Effekseer::EffectRef effect);

		void Unregister(Effekseer::EffectRef effect);

		bool IsRunning() const;

		static std::shared_ptr<Network>& GetInstance();
	};
}

#endif