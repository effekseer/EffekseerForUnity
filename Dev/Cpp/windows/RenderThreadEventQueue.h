
#pragma once

#include <functional>
#include <vector>
#include <memory>
#include <mutex>

namespace EffekseerPlugin
{

	class RenderThreadEventQueue final
	{
	private:
		static std::shared_ptr< RenderThreadEventQueue> instance_;

		std::mutex mtx_;
		std::vector<std::function<void()>> functions_;

	public:
		RenderThreadEventQueue() = default;
		~RenderThreadEventQueue();

		void AddEvent(std::function<void()> function);

		void Execute();

		static void Initialize();

		static void Terminate();

		static std::shared_ptr< RenderThreadEventQueue> GetInstance() { return instance_; }
	};

}