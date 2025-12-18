#ifndef __LOG_ELK_H__
#define __LOG_ELK_H__

#include "cocos2d.h"
#include "network/HttpClient.h"
#include <string>
#include <queue>
#include <mutex>

USING_NS_CC;
using namespace network;

// 枚举值：纯数字枚举，无字符串关联
enum LogLevel
{
    LOG_LEVEL_DEBUG = 0,
    LOG_LEVEL_INFO,
    LOG_LEVEL_WARN,
    LOG_LEVEL_ERROR
};

class CC_DLL ELKLogger : public Ref {
public:
    static ELKLogger* getInstance();
    static void destroyInstance();

    // 初始化ES配置
    void init(const std::string& esUrl, const std::string& apiKey);

    // 核心日志接口（仅枚举值传参）
    void writeLog(LogLevel level, const std::string& module, const std::string& content);

private:
    ELKLogger() = default;
    ~ELKLogger() = default;
    ELKLogger(const ELKLogger&) = delete;
    ELKLogger& operator=(const ELKLogger&) = delete;

    // 内部辅助函数
    std::string createJsonLog(LogLevel level, const std::string& module, const std::string& content);
    void sendLogAsync(const std::string& jsonStr);
    void onHttpResponse(HttpClient* sender, HttpResponse* response);
    std::string getCurrentTime();

    // 单例实例
    static ELKLogger* _instance;

    // 配置参数
    std::string _esUrl;
    std::string _apiKey;
    std::queue<std::string> _logQueue;
    std::mutex _queueMutex;
    bool _isSending = false;
};

// 唯一调用方式：宏（杜绝手动传参错误）
#define ELK_LOG_DEBUG(module, content) ELKLogger::getInstance()->writeLog(LOG_LEVEL_DEBUG, module, content)
#define ELK_LOG_INFO(module, content)  ELKLogger::getInstance()->writeLog(LOG_LEVEL_INFO, module, content)
#define ELK_LOG_WARN(module, content)  ELKLogger::getInstance()->writeLog(LOG_LEVEL_WARN, module, content)
#define ELK_LOG_ERROR(module, content) ELKLogger::getInstance()->writeLog(LOG_LEVEL_ERROR, module, content)

#endif // __LOG_ELK_H__