#include "MyDataTableMgr.h"
#include "tracy/tracy/Tracy.hpp"

#include "additionattr.pb.h"
#include "achiveactivityshop.pb.h"
#include "achievemilestone.pb.h"

#include <fstream> 
#include <memory> 
#include <string> 
#include <filesystem>
#include <windows.h>


#define EXPLICIT_TEMPLATE_INSTANTIATION(T)	\
	template bool MyDataTableMgr::TryGetByID<T, DT_##T>(unsigned int, const DT_##T*&); \
	template const DT_##T& MyDataTableMgr::GetByID<T, DT_##T>(unsigned int); \
	template bool MyDataTableMgr::TryGetByIndex<T, DT_##T>(int, const DT_##T*&); \
	template const DT_##T& MyDataTableMgr::GetByIndex<T, DT_##T>(int); \
	template void MyDataTableMgr::ForEach<T, DT_##T>(const std::function<bool(const DT_##T&)>&); \
	template int MyDataTableMgr::GetDataCount<T>(); \
	template const T* MyDataTableMgr::FetchDataTable<T>();

MyDataTableMgr& MyDataTableMgr::GetInstance()
{
	static MyDataTableMgr _inst;
	return _inst;
}

template<typename T>
void MyDataTableMgr::DefineRuntimeData()
{
	//这里使用反射接口GetTypeName做Key，其他地方不要使用反射API
	std::string typeNameStr = T::default_instance().GetTypeName();
	const char* tempTypeName = typeNameStr.c_str(); // 此时指针指向typeNameStr的内存，生命周期和typeNameStr一致
	if (tempTypeName == nullptr || *tempTypeName == '\0') {
		return;
	}

	// 强制复制内容到新的std::string，确保内存持久有效
	std::string typeName(tempTypeName);
	if (typeName.empty())
		return;
	
	DataWrapperInner DataWrapper;
	DataWrapper.strFile = typeName + ".bytes";
	LoadData<T>(DataWrapper);
	auto it = m_mapDataContainer.find(typeName);
	if (it != m_mapDataContainer.end())
	{
		if (it->second.pRuntimeData)
		{
			delete(it->second.pRuntimeData);
		}
		m_mapDataContainer.erase(it);
	}

	m_mapDataContainer[typeName] = std::move(DataWrapper);
}

void MyDataTableMgr::Init()
{
	ZoneScopedN("MyDataTableMgr::Init");

	DefineRuntimeData<additionattr>();
	DefineRuntimeData<achiveactivityshop>();
	DefineRuntimeData<achievemilestone>();
}

template<typename T, typename DT>
bool MyDataTableMgr::TryGetByID(unsigned int id, const DT*& message)
{
	const T* wrapperMessage = FetchDataTable<T>();
	if (!wrapperMessage)
	{
		message = nullptr;
		return false;
	}

	return TryGetByIDImpl(wrapperMessage->config(), id, message);
	}

template<typename T, typename DT>
bool MyDataTableMgr::TryGetByIndex(int index, const DT*& message)
{
	const T* wrapperMessage = FetchDataTable<T>();
	if (!wrapperMessage)
	{
	message = nullptr;
	return false;
}

	return TryGetByIndexImpl(wrapperMessage->config(), index, message);
}

template<typename T, typename DT>
const DT& MyDataTableMgr::GetByID(unsigned int id)
{
	const DT* result = nullptr;
	if (TryGetByID<T>(id, result))
	{
		return *result;
	}

	return DT::default_instance();
}

template<typename T, typename DT>
const DT& MyDataTableMgr::GetByIndex(int index)
{
	const DT* result = nullptr;
	if (TryGetByIndex<T>(index, result))
	{
		return *result;
	}

	return DT::default_instance();
}

void MyDataTableMgr::MarkDataForReload(const char* szDataTable)
{
	auto it = m_mapDataContainer.find(szDataTable);
	if (m_mapDataContainer.end() != it)
	{
		it->second.bDirty = true;
	}
}

void MyDataTableMgr::MarkAllDataForReload()
{
	for (auto& container : m_mapDataContainer)
	{
		container.second.bDirty = true;
	}	
}

template<typename T, typename DT>
void MyDataTableMgr::ForEach(const std::function<bool(const DT&)>& func)
{
	const T* wrapperMessage = FetchDataTable<T>();
	if (!wrapperMessage)
		return;
	ForEachImpl(wrapperMessage->config(), func);
}

template<typename Config, typename DT>
void MyDataTableMgr::ForEachImpl(const Config& config, const std::function<bool(const DT&)>& func)
	{
	// 存在未实现的类型
	static_assert(std::is_same<Config, void>::value, "Unimplemented protobuf struct type for ForEachImpl: ");
}

template<typename Config, typename DT>
bool MyDataTableMgr::TryGetByIDImpl(const Config& config, unsigned int id, const DT*& message)
{
	// 存在未实现的类型
	// 该接口目前只支持首列类型为T_KEY的表，其余类型不可使用该接口
	// 首列类型为T_INDEX的表建议使用ForEach遍历实现
	return false;
}

template<typename Config, typename DT>
bool MyDataTableMgr::TryGetByIndexImpl(const Config& config, int index, const DT*& message)
{
	// 存在未实现的类型
	// 该接口目前只支持首列类型为T_INDEX的表，其余类型不可使用该接口
	return false;
}

template<typename Config, typename DT>
void MyDataTableMgr::ReloadImpl(Config* pOldConfig, const Config& newConfig)
{
	// 存在未实现的类型
	static_assert(std::is_same<Config, void>::value, "Unimplemented protobuf struct type for ReloadImpl: ");
}

template<typename DT>
void MyDataTableMgr::ForEachImpl(const google::protobuf::RepeatedPtrField<DT>& config, const std::function<bool(const DT&)>& func)
{
	for (const auto& element : config)
	{
		if (!func(element))
			return;
	}
}

template<typename DT>
bool MyDataTableMgr::TryGetByIndexImpl(const google::protobuf::RepeatedPtrField<DT>& config, int index, const DT*& message)
{
	if(index >= 0 && index < config.size())
	{
		message = &(config.at(index));
		return true;
	}

	message = nullptr;
	return false;
}

template<typename DT>
void MyDataTableMgr::ReloadImpl(google::protobuf::RepeatedPtrField<DT>* pOldConfig, const google::protobuf::RepeatedPtrField<DT>& newConfig)
{
	RETURN_IF_TRUE(nullptr == pOldConfig);
	int i = 0;

	for (; i < newConfig.size(); i++)
	{
		if (i <= pOldConfig->size())
		{
			pOldConfig->at(i).CopyFrom(newConfig.at(i));
		}
		else
		{
			DT element;
			element.CopyFrom(newConfig.at(i));
			pOldConfig->Add(std::move(element));
		}
	}
}

template<typename DT>
void MyDataTableMgr::ForEachImpl(const google::protobuf::Map<uint32_t, DT>& config, const std::function<bool(const DT&)>& func)
{
	for (const auto& element : config)
	{
		if (!func(element.second))
			return;
	}
}

template<typename DT>
bool MyDataTableMgr::TryGetByIDImpl(const google::protobuf::Map<uint32_t, DT>& config, unsigned int id, const DT*& message)
{
	auto it = config.find(id);
	if (it != config.end())
	{
		message = &it->second;
		return true;
	}

	message = nullptr;
	return false;
}

template<typename DT>
void MyDataTableMgr::ReloadImpl(google::protobuf::Map<uint32_t, DT>* pOldConfig, const google::protobuf::Map<uint32_t, DT>& newConfig)
{
	if(nullptr == pOldConfig);
	{
		return;
	}

	for (auto it : newConfig)
	{
		auto itFind = pOldConfig->find(it.first);
		if (pOldConfig->end() != itFind)
		{
			itFind->second.CopyFrom(it.second);
		}
		else
		{
			(*pOldConfig)[it.first] = it.second;
		}
	}
}

template<typename T>
int MyDataTableMgr::GetDataCount()
{
	const T* wrapperMessage = FetchDataTable<T>();
	if (!wrapperMessage)
	{
		return 0;
	}

	return wrapperMessage->config_size();
}


float MyDataTableMgr::GetTotalMemoryUsageMB() const
{
	float totalMB = 0.0f;
	for (const auto& data : m_mapDataContainer)
	{
		totalMB += data.second.fMemoryUsageMB;
	}
	return totalMB;
}

MyDataTableMgr::~MyDataTableMgr()
{
	for (auto& container : m_mapDataContainer)
	{
		if (container.second.pRuntimeData)
		{
			delete container.second.pRuntimeData;
		}
	}
	m_mapDataContainer.clear();
}

template<typename T>
const T* MyDataTableMgr::FetchDataTable()
{
	//这里使用反射接口GetTypeName做Key，其他地方不要使用反射API
	std::string typeName = T::default_instance().GetTypeName();
	auto needReload = m_mapDataContainer.find(typeName);
	if (needReload == m_mapDataContainer.end())
		return nullptr;
	
	DataWrapperInner& container = needReload->second;
	if (container.bDirty)
	{
		LoadData<T>(container);
	}
	return dynamic_cast<const T*>(container.pRuntimeData);	
}

std::string GetDirectoryFromPath(const std::string& fullPath)
{
	// 查找最后一个反斜杠/斜杠的位置（兼容Windows/Linux路径格式）
	size_t lastSepPos = fullPath.find_last_of("\\/");
	if (lastSepPos == std::string::npos)
	{
		// 没有找到分隔符，返回空字符串（理论上exe路径必有分隔符）
		return "";
	}
	// 截取从开头到最后一个分隔符的部分（即目录路径）
	return fullPath.substr(0, lastSepPos);
}

template<typename T>
void MyDataTableMgr::LoadData(DataWrapperInner& container)
{
	container.bDirty = false;
	std::string strRealFileName;

	char buffer[MAX_PATH] = { 0 };
	DWORD pathLen = GetModuleFileNameA(NULL, buffer, MAX_PATH);
	if (pathLen == 0 || pathLen >= MAX_PATH)
	{
		return;
	}

	std::string exeFullPath(buffer);
	std::string exeDir = GetDirectoryFromPath(exeFullPath);

	strRealFileName = exeDir + "\\ini\\client\\common\\databytes\\" + container.strFile;

	std::ifstream input(strRealFileName, std::ios::in | std::ios::binary);
	if (!input)
	{
		return;
	}

	std::unique_ptr<T> pProtoData(T::default_instance().New());
	if (nullptr == pProtoData || !pProtoData->ParseFromIstream(&input))
	{
		return;
	}

	if (container.pRuntimeData == nullptr)
	{
		container.pRuntimeData = pProtoData.release();
	}
	else
	{
		auto pOldData = dynamic_cast<T*>(container.pRuntimeData);
		if (pOldData == nullptr)
			return;
		auto pOldConfig = pOldData->mutable_config();
		auto newConfig = pProtoData->config();
		ReloadImpl(pOldConfig, newConfig);
	}

	if (container.pRuntimeData != nullptr)
	{
		container.fMemoryUsageMB = static_cast<float>(container.pRuntimeData->ByteSizeLong()) / (1024.0f * 1024.0f);
	}
}
EXPLICIT_TEMPLATE_INSTANTIATION(additionattr);
EXPLICIT_TEMPLATE_INSTANTIATION(achiveactivityshop);
EXPLICIT_TEMPLATE_INSTANTIATION(achievemilestone);

