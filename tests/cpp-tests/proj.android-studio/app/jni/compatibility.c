#include <unistd.h>
#include <sys/resource.h>

// Android 兼容性实现
int getdtablesize(void) {
    struct rlimit rlim;
    if (getrlimit(RLIMIT_NOFILE, &rlim) == 0) {
        return (int)rlim.rlim_cur;
    }
    // 失败时返回默认值
    return 1024;
}