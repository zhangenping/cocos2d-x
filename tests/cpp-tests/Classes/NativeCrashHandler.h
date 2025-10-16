#ifndef NATIVE_CRASH_HANDLER_H
#define NATIVE_CRASH_HANDLER_H

class NativeCrashHandler {
public:
    static void init();

private:
    static void signalHandler(int signal);
};

#endif // NATIVE_CRASH_HANDLER_H