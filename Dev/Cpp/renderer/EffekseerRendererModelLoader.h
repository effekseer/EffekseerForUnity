
#pragma once

#include "../common/EffekseerPluginModel.h"
#include "../unity/IUnityInterface.h"

#ifdef __EFFEKSEER_FROM_MAIN_CMAKE__
#include <Effekseer/Effekseer.h>
#else
#include <Effekseer.h>
#endif

#include "../common/EffekseerPluginCommon.h"
#include <Effekseer/Model/ProcedualModelGenerator.h>

#include <map>
#include <memory>
#include <string>
#include <vector>

namespace EffekseerRendererUnity
{
class ModelLoader : public Effekseer::ModelLoader
{
	EffekseerPlugin::ModelLoaderLoad load;
	EffekseerPlugin::ModelLoaderUnload unload;

	std::vector<uint8_t> internalBuffer;

public:
	ModelLoader(EffekseerPlugin::ModelLoaderLoad load, EffekseerPlugin::ModelLoaderUnload unload);

	virtual ~ModelLoader() = default;
	virtual Effekseer::ModelRef Load(const EFK_CHAR* path);
	virtual void Unload(Effekseer::ModelRef source);
};

class ProcedualModelGenerator : public Effekseer::ProcedualModelGenerator
{
private:
	EffekseerPlugin::ProcedualModelGeneratorGenerate generate_;
	EffekseerPlugin::ProcedualModelGeneratorUngenerate ungenerate_;
	Effekseer::CustomVector<Effekseer::Model::Vertex> vertecies_;
	Effekseer::CustomVector<Effekseer::Model::Face> faces_;

public:
	ProcedualModelGenerator(EffekseerPlugin::ProcedualModelGeneratorGenerate generate,
							EffekseerPlugin::ProcedualModelGeneratorUngenerate ungenerate)
		: generate_(generate), ungenerate_(ungenerate)
	{
	}
	~ProcedualModelGenerator() override = default;

	Effekseer::ModelRef Generate(const Effekseer::ProcedualModelParameter* parameter) override;

	void Ungenerate(Effekseer::ModelRef model) override;
};

} // namespace EffekseerRendererUnity
