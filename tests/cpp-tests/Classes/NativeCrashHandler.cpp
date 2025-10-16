#include "NativeCrashHandler.h"
#include "base/CCConsole.h"
#include <csignal>
#include <cstdlib>

void NativeCrashHandler::signalHandler(int signal) {
    const char* signalName = "Unknown";
    switch (signal) {
    case 11: signalName = "SIGSEGV"; break;
    case 6:  signalName = "SIGABRT"; break;
    case 4:  signalName = "SIGILL"; break;
    case 8:  signalName = "SIGFPE"; break;
    default: signalName = "UNKNOWN"; break;
    }

    cocos2d::log("NATIVE CRASH: Signal %s (%d)", signalName, signal);

    // 恢复默认处理器
    std::signal(signal, SIG_DFL);
    // 重新触发信号
    std::raise(signal);
}

void NativeCrashHandler::init() {
    cocos2d::log("Initializing crash handlers");

    // 使用正确的函数指针类型
    std::signal(SIGSEGV, NativeCrashHandler::signalHandler);
    std::signal(SIGABRT, NativeCrashHandler::signalHandler);
    std::signal(SIGILL, NativeCrashHandler::signalHandler);
    std::signal(SIGFPE, NativeCrashHandler::signalHandler);

    cocos2d::log("Crash handlers initialized");
}