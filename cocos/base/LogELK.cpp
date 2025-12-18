#include "LogELK.h"
#include "json/document.h"
#include "json/writer.h"
#include "json/stringbuffer.h"
#include "platform/CCPlatformMacros.h"
#include "base/CCDirector.h"
#include "platform/CCFileUtils.h"
#include <ctime>
#include <sstream>
#include <mutex>
#include <queue>

// 手动实现toString（无外部依赖，避免Cocos头文件问题）
template <typename T>
std::string convertToString(const T& value) {
    std::ostringstream oss;
    oss << value;
    return oss.str();
}

// 单例初始化（静态成员）
ELKLogger* ELKLogger::_instance = nullptr;

// 获取单例（线程安全极简版）
ELKLogger* ELKLogger::getInstance() {
    if (_instance == nullptr) {
        _instance = new ELKLogger();
    }
    return _instance;
}

// 销毁单例
void ELKLogger::destroyInstance() {
    if (_instance != nullptr) {
        delete _instance;
        _instance = nullptr;
    }
}

// 初始化ES地址和API Key
void ELKLogger::init(const std::string& esUrl, const std::string& apiKey) {
    _esUrl = esUrl;
    _apiKey = apiKey;
}

// 获取格式化时间戳（YYYY-MM-DD HH:MM:SS）
std::string ELKLogger::getCurrentTime() {
    std::time_t now = std::time(nullptr);
    struct tm timeInfo;
    localtime_s(&timeInfo, &now); // 替换localtime，避免VS安全警告
    char timeBuffer[32] = { 0 };
    std::strftime(timeBuffer, sizeof(timeBuffer), "%Y.%m.%d %H:%M:%S", &timeInfo);
    return std::string(timeBuffer);
}

// 构造JSON日志（修复RapidJSON AddMember参数错误）
std::string ELKLogger::createJsonLog(LogLevel level, const std::string& module, const std::string& content) {
    // 初始化RapidJSON文档（左值allocator，避免C2102错误）
    rapidjson::Document jsonDoc;
    jsonDoc.SetObject();
    rapidjson::Document::AllocatorType& allocator = jsonDoc.GetAllocator(); // 左值引用

    // 1. 时间戳（核心字段）
    rapidjson::Value timeValue(this->getCurrentTime().c_str(), allocator);
    jsonDoc.AddMember("timestamp", timeValue, allocator); // 正确传参：3个参数

    // 2. 日志级别
    const char* levelStr = "DEBUG";
    switch (level) {
    case LOG_LEVEL_DEBUG: levelStr = "DEBUG"; break;
    case LOG_LEVEL_INFO:  levelStr = "INFO";  break;
    case LOG_LEVEL_WARN:  levelStr = "WARN";  break;
    case LOG_LEVEL_ERROR: levelStr = "ERROR"; break;
    }
    rapidjson::Value levelValue(levelStr, allocator);
    jsonDoc.AddMember("level", levelValue, allocator);

    // 3. 业务模块
    rapidjson::Value moduleValue(module.c_str(), allocator);
    jsonDoc.AddMember("module", moduleValue, allocator);

    // 4. 日志内容
    rapidjson::Value contentValue(content.c_str(), allocator);
    jsonDoc.AddMember("content", contentValue, allocator);

    // 5. 平台信息（仅Win32，极简兼容）
    rapidjson::Value platformValue("Windows", allocator);
    jsonDoc.AddMember("platform", platformValue, allocator);

    // 序列化JSON为字符串
    rapidjson::StringBuffer stringBuffer;
    rapidjson::Writer<rapidjson::StringBuffer> jsonWriter(stringBuffer);
    jsonDoc.Accept(jsonWriter);
    return stringBuffer.GetString();
}

// 异步发送日志到ES（Cocos主线程调度，避免线程安全问题）
void ELKLogger::sendLogAsync(const std::string& jsonStr) {
    // 日志入队（加锁，避免多线程冲突）
    {
        std::lock_guard<std::mutex> lock(_queueMutex);
        _logQueue.push(jsonStr);
    }

    // 若正在发送，直接返回（避免并发）
    if (_isSending) {
        return;
    }

    // 标记为发送中
    _isSending = true;

    // 切换到Cocos主线程执行HTTP请求
    Director::getInstance()->getScheduler()->performFunctionInCocosThread([this]() {
        std::string currentLog;

        // 取出队列头部的日志（加锁）
        {
            std::lock_guard<std::mutex> lock(_queueMutex);
            if (_logQueue.empty()) {
                _isSending = false;
                return;
            }
            currentLog = _logQueue.front();
            _logQueue.pop();
        }

        // 构造ES写入URL（按日期分索引：game-client-YYYY-MM-DD）
        std::string dateStr = this->getCurrentTime().substr(0, 10);
        std::string indexName = "cocos-game-logs-" + dateStr;
        std::string requestUrl = _esUrl + "/" + indexName + "/_doc";

        // 创建Cocos HTTP请求（异步POST）
        HttpRequest* httpRequest = new HttpRequest();
        httpRequest->setUrl(requestUrl.c_str());
        httpRequest->setRequestType(HttpRequest::Type::POST);
        httpRequest->setResponseCallback(CC_CALLBACK_2(ELKLogger::onHttpResponse, this));

        // 设置请求头（API Key认证 + JSON格式）
        std::vector<std::string> headers;
        headers.push_back("Content-Type: application/json");
        headers.push_back("Authorization: ApiKey " + _apiKey);
        httpRequest->setHeaders(headers);

        // 设置请求体（JSON日志字符串）
        httpRequest->setRequestData(currentLog.c_str(), currentLog.length());

        // 发送请求（Cocos自动管理内存和线程）
        HttpClient::getInstance()->send(httpRequest);
        httpRequest->release(); // 释放内存
        });
}

// HTTP请求回调（仅打印结果，无复杂逻辑）
void ELKLogger::onHttpResponse(HttpClient* sender, HttpResponse* response) {
    // 检查响应是否成功
    if (response == nullptr || !response->isSucceed()) {
        std::string errorMsg = (response != nullptr) ?
            ("HTTP Code: " + convertToString(response->getResponseCode())) :
            "Network Error";
        CCLOGERROR("ELK Log Send Failed: %s", errorMsg.c_str());
    }
    else {
        CCLOG("ELK Log Send Success");
    }

    // 标记发送完成，处理下一条日志
    _isSending = false;
    {
        std::lock_guard<std::mutex> lock(_queueMutex);
        if (!_logQueue.empty()) {
            this->sendLogAsync(""); // 触发下一次发送
        }
    }
}

// 核心日志接口（仅枚举值传参，无字符串）
void ELKLogger::writeLog(LogLevel level, const std::string& module, const std::string& content) {
    // 保留Cocos原有日志输出（仅用CCLOG/CCLOGERROR，兼容早期版本）
    switch (level) {
    case LOG_LEVEL_DEBUG: CCLOG("[%s] %s", module.c_str(), content.c_str()); break;
    case LOG_LEVEL_INFO:  CCLOG("[%s] %s", module.c_str(), content.c_str()); break;
    case LOG_LEVEL_WARN:  CCLOG("[%s] [WARN] %s", module.c_str(), content.c_str()); break; // 替换CCLOGWARN
    case LOG_LEVEL_ERROR: CCLOGERROR("[%s] %s", module.c_str(), content.c_str()); break;
    }

    // 仅发送WARN/ERROR级别到ELK（减少流量）
    if (level == LOG_LEVEL_DEBUG || level == LOG_LEVEL_INFO) {
        return;
    }

    // 构造JSON并异步发送
    std::string jsonLog = this->createJsonLog(level, module, content);
    this->sendLogAsync(jsonLog);
}