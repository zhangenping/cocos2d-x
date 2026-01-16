#pragma once

#if CQ_PLATFORM == CQ_PLATFORM_WIN32
#ifdef MY_PROTOBUF_EXPORT
#define PROTOBUF_API __declspec(dllexport)
#else
#define PROTOBUF_API __declspec(dllimport)
#endif
#else
#define PROTOBUF_API
#endif