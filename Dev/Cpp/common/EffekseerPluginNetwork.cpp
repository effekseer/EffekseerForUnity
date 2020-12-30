#include "EffekseerPluginNetwork.h"

#ifndef _SWITCH

namespace EffekseerPlugin
{
	extern Effekseer::ManagerRef g_EffekseerManager;

	std::shared_ptr<Network> Network::instance;

	Network::Network()
	{

	}

	Network::~Network()
	{
		Stop();
	}

	bool Network::Start(uint16_t port)
	{
		if (server != nullptr)
		{
			return false;
		}

		server = Effekseer::Server::Create();

		if (!server->Start(port))
		{
			ES_SAFE_DELETE(server);
			return false;
		}

		return true;
	}

	void Network::Stop()
	{
		if (server != nullptr)
		{
			server->Stop();
			ES_SAFE_DELETE(server);
		}
	}

	void Network::Update()
	{
		if (server != nullptr && g_EffekseerManager != nullptr)
		{
			server->Update(nullptr, 0, Effekseer::ReloadingThreadType::Render);
		}
	}

	void Network::Register(const char16_t* key, Effekseer::EffectRef effect)
	{
		if (server != nullptr)
		{
			server->Register(key, effect);
		}
	}

	void Network::Unregister(Effekseer::EffectRef effect)
	{
		if (server != nullptr)
		{
			server->Unregister(effect);
		}
	}

	bool Network::IsRunning() const
	{
		return server != nullptr;
	}

	std::shared_ptr<Network>& Network::GetInstance()
	{
		if (instance == nullptr)
		{
			instance = std::make_shared<Network>();
		}

		return instance;
	}
}

#endif

extern "C"
{
	UNITY_INTERFACE_EXPORT int UNITY_INTERFACE_API StartNetwork(int port)
	{
#ifdef _SWITCH
		return 0;
#else
		return static_cast<int>(EffekseerPlugin::Network::GetInstance()->Start(port));
#endif
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API StopNetwork()
	{
#ifdef _SWITCH
		return;
#else
		EffekseerPlugin::Network::GetInstance()->Stop();
#endif
	}

	UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UpdateNetwork()
	{
#ifdef _SWITCH
		return;
#else
		EffekseerPlugin::Network::GetInstance()->Update();
#endif
	}
}
