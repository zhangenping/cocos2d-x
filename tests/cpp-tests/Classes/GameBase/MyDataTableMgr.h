#pragma once

#include "google/protobuf/message.h"
#include <unordered_map>

class MyDataTableMgr
{
	using DataTablePtr = google::protobuf::Message*;
public:
	static MyDataTableMgr& GetInstance();

	void Init();

	//允许返回失败，并不打印日志，用于尝试是否存在某种数据的情况（允许不配置）
	template<typename T, typename DT>
	bool TryGetByID(unsigned int id, const DT*& message);

	template<typename T, typename DT>
	bool TryGetByIndex(int index, const DT*& message);

	//预期一定会成功，如果失败将自动打印日志
	template<typename T, typename DT>
	const DT& GetByID(unsigned int id);

	template<typename T, typename DT>
	const DT& GetByIndex(int index);

	void MarkDataForReload(const char* szDataTable);
	void MarkAllDataForReload();

	// function 返回 true 继续遍历，否则打断遍历
	template<typename T, typename DT>
	void ForEach(const std::function<bool(const DT&)>& func);

	template<typename T>
	int GetDataCount();

	float GetTotalMemoryUsageMB() const;

protected:
	~MyDataTableMgr();

	template<typename T>
	const T* FetchDataTable();
	struct DataWrapperInner
	{
		DataTablePtr pRuntimeData = nullptr;
		bool bDirty = true;
		std::string strFile;
		float fMemoryUsageMB = 0.0f;
	};

	template<typename T>
	void LoadData(DataWrapperInner& dataWrapper);
private:
	MyDataTableMgr() = default;
	MyDataTableMgr(const MyDataTableMgr&) = delete;
	MyDataTableMgr& operator=(const MyDataTableMgr&) = delete;

	template<typename T>
	void DefineRuntimeData();

	template<typename Config, typename DT>
	void ForEachImpl(const Config& config, const std::function<bool(const DT&)>& func);

	template<typename Config, typename DT>
	bool TryGetByIDImpl(const Config& config, unsigned int id, const DT*& message);

	template<typename Config, typename DT>
	bool TryGetByIndexImpl(const Config& config, int index, const DT*& message);

	template<typename Config, typename DT>
	void ReloadImpl(Config* pOldConfig, const Config& newConfig);

	// 针对RepeatedPtrField的重载版本
	template<typename DT>
	void ForEachImpl(const google::protobuf::RepeatedPtrField<DT>& config, const std::function<bool(const DT&)>& func);

	template<typename DT>
	bool TryGetByIndexImpl(const google::protobuf::RepeatedPtrField<DT>& config, int index, const DT*& message);

	template<typename DT>
	void ReloadImpl(google::protobuf::RepeatedPtrField<DT>* pOldConfig, const google::protobuf::RepeatedPtrField<DT>& newConfig);

	// 针对Map的重载版本
	template<typename DT>
	void ForEachImpl(const google::protobuf::Map<uint32_t, DT>& config, const std::function<bool(const DT&)>& func);

	template<typename DT>
	bool TryGetByIDImpl(const google::protobuf::Map<uint32_t, DT>& config, unsigned int id, const DT*& message);

	template<typename DT>
	void ReloadImpl(google::protobuf::Map<uint32_t, DT>* pOldConfig, const google::protobuf::Map<uint32_t, DT>& newConfig);

private:
	std::unordered_map<std::string, DataWrapperInner> m_mapDataContainer;
};
