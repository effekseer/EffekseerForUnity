#include "RenderThreadEventQueue.h"
#include <assert.h>

namespace EffekseerPlugin
{
	std::shared_ptr< RenderThreadEventQueue> RenderThreadEventQueue::instance_ = nullptr;

	RenderThreadEventQueue::~RenderThreadEventQueue()
	{
		if (functions_.size() > 0)
		{
			printf("Warning Invalid Execution.\n");
		}

		Execute();
	}

	void RenderThreadEventQueue::AddEvent(std::function<void()> function)
	{
		std::lock_guard<std::mutex> lock(mtx_);
		functions_.push_back(function);
	}

	void RenderThreadEventQueue::Execute()
	{
		std::lock_guard<std::mutex> lock(mtx_);

		for (auto& f : functions_)
		{
			f();
		}

		functions_.clear();
	}

	void RenderThreadEventQueue::Initialize()
	{
		assert(instance_ == nullptr);
		instance_ = std::make_shared<RenderThreadEventQueue>();
	}

	void RenderThreadEventQueue::Terminate()
	{
		assert(instance_ != nullptr);
		instance_ = nullptr;
	}
}