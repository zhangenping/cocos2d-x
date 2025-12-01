#ifndef _FMT_EXTENSION_H
#define _FMT_EXTENSION_H
#define FMT_HEADER_ONLY

#include "fmt/format.h"
#include <string>
#include "platform/CCPlatformMacros.h"

NS_CC_BEGIN

void CC_DLL do_fmt_error(const std::string& str, const char* pszFileName, int iFileLine, const char* pszReason);
#if CQ_PLATFORM == CQ_PLATFORM_WIN32 || defined(_DEBUG)
unsigned int CC_DLL count_total_placeholders(const std::string& fmt);
#endif

template <typename... T>
auto fmt_format_ic(const char* pszFileName, int iFileLine, const std::string& str, T... args)-> std::string
{
	try
	{
		return fmt::format(str, std::forward<T>(args)...);
	}
	catch (const fmt::format_error& e)
	{
		do_fmt_error(str, pszFileName, iFileLine, e.what());
	}
	catch (...)
	{
		do_fmt_error(str, pszFileName, iFileLine, "Unknown Error");
	}
	return "";
}

template <typename... T>
auto fmt_format(const char* pszFileName, int iFileLine,const std::string& str, T&&... args)-> std::string
{

#if CQ_PLATFORM == CQ_PLATFORM_WIN32 || defined(_DEBUG)
        constexpr size_t num_args = sizeof...(T);
        size_t total_placeholders = count_total_placeholders(str);
        if (total_placeholders != num_args)
        {
            std::string error = "Argument count mismatch. placeholders num is " + std::to_string(total_placeholders) + ", Argument num is  " + std::to_string(num_args);
            do_fmt_error(str, pszFileName, iFileLine, error.c_str());
            return "";
        }

#endif
		return fmt_format_ic(pszFileName, iFileLine, str, args...);
}

#define FMT(STR, ...)  (fmt_format(__FILE__, __LINE__, STR, ##__VA_ARGS__))
#define FMT_IC(STR, ...)  (fmt_format_ic(__FILE__, __LINE__, STR, ##__VA_ARGS__)) //不检查是否匹配参数
#endif

NS_CC_END