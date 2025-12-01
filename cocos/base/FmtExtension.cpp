#pragma once
#include "FmtExtension.h"

#include <deque>
#include <mutex>
#include <sstream>
#include <vector>
NS_CC_BEGIN

static bool IsSendErrorToElk()
{
    static bool bSendErrorToElk = true;
    static bool bInit = false;
    if (!bInit)
    {
        bInit = true;
        //::DatBoolGet("ini/client/release/clientconfig.dat", "fmt_option", "bSendFMTErrorToELK", bSendErrorToElk);
    }
	
    return bSendErrorToElk;
}
#if CQ_PLATFORM == CQ_PLATFORM_WIN32 || defined(_DEBUG)
unsigned int count_total_placeholders(const std::string& fmt) {
    unsigned int total = 0;
    bool in_escape = false; // 处理 {{ 和 }} 转义

    for (unsigned int i = 0; i < fmt.size(); ++i)
    {
        if (fmt[i] == '{' && !in_escape)
        {
            if (i + 1 < fmt.size() && fmt[i + 1] == '{')
            {
                in_escape = true; // 转义 {
                ++i;
                continue;
            }

            // 遇到占位符起始符 '{'，开始寻找闭合的 '}'
            unsigned int j = i + 1;
            bool is_closed = false;
            while (j < fmt.size())
            {
                if (fmt[j] == '}')
                {
                    is_closed = true;
                    break;
                }
                ++j;
            }

            // 找到闭合的 '}'，计为一个占位符
            if (is_closed)
            {
                ++total;
                i = j; // 跳转到闭合的 '}' 之后
            }
            // 未闭合的 '{' 留给 fmt 库处理
        }
        else if (fmt[i] == '}' && !in_escape)
        {
            if (i + 1 < fmt.size() && fmt[i + 1] == '}')
            {
                in_escape = true; // 转义 }
                ++i;
            }
        }
        else
        {
            in_escape = false;
        }
    }
    return total;
}
#endif
void do_fmt_error(const std::string& str, const char* pszFileName, int iFileLine, const char* pszReason)
{
    if (!IsSendErrorToElk())
    {
        return;
    }
    static std::mutex s_mutex;
    static std::deque<std::string> s_setQueue;

    std::string strErro;

    { // 加锁区域开始
        std::lock_guard<std::mutex> lock(s_mutex);
    
        // 检查是否已存在相同错误,避免重复打印相同的错误
        auto it = std::find(s_setQueue.begin(), s_setQueue.end(), str);
        if (it != s_setQueue.end()) {
            return;
        }

        // 维护队列容量（最大10条）
        if (s_setQueue.size() >= 10) {
            s_setQueue.pop_front();
        }
        
        s_setQueue.push_back(str);
    } //
    
    /*std::ostringstream oss;
    oss << "文本 : " << str << "\n"
        << "原因: " << (pszReason == NULL ? "未知错误" : pszReason ) << "\n"
        << "请检查 strres.ini id = "  << g_nErrorStrId  << "或者 statustips.ini id =" << g_nErrorStatusId << "\n"
        << "代码文件: " << (pszFileName == NULL ? "未知文件" : pszFileName) << "\n"
        << "代码行: " << iFileLine << "\n"
        << TraceStack();
    strErro = oss.str();
    LogMyMsgDepm(DEPARTMENT_CLIENT, LEV_ERROR, strErro.c_str());*/

#if CQ_PLATFORM == CQ_PLATFORM_WIN32
#if defined(_DEBUG) || defined(PC_DEVELOPMENT)
    //MessageBox(NULL, TEXT(strErro.c_str()), TEXT("文本错误"), MB_OK);
#endif
#endif
}

NS_CC_END